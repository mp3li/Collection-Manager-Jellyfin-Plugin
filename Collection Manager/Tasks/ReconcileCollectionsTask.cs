using Jellyfin.Plugin.CollectionManager.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionManager.Tasks;

/// <summary>Dashboard task that reconciles enabled automatic collection rules.</summary>
public sealed class ReconcileCollectionsTask : IScheduledTask
{
    private static readonly SemaphoreSlim TaskExecutionLock = new(1, 1);
    private readonly CollectionReconciler _reconciler;
    private readonly ManualReconciliationRequestQueue _requests;
    private readonly LibraryIdentityService _libraryIdentities;
    private readonly MediaTaggingManagerActivityGate _mediaTaggingManager;
    private readonly ReconciliationScheduler _scheduler;
    private readonly ILogger<ReconcileCollectionsTask> _logger;

    /// <summary>Initializes a new instance of the <see cref="ReconcileCollectionsTask"/> class.</summary>
    public ReconcileCollectionsTask(
        CollectionReconciler reconciler,
        ManualReconciliationRequestQueue requests,
        LibraryIdentityService libraryIdentities,
        MediaTaggingManagerActivityGate mediaTaggingManager,
        ReconciliationScheduler scheduler,
        ILogger<ReconcileCollectionsTask> logger)
    {
        _reconciler = reconciler;
        _requests = requests;
        _libraryIdentities = libraryIdentities;
        _mediaTaggingManager = mediaTaggingManager;
        _scheduler = scheduler;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Reconcile Collection Manager rules";

    /// <inheritdoc />
    public string Key => "CollectionManagerReconcile";

    /// <inheritdoc />
    public string Description => "Applies queued targeted metadata updates and, when enabled, performs a full scheduled safety reconciliation of Collection Manager rules and saved creation settings.";

    /// <inheritdoc />
    public string Category => "Library";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        await TaskExecutionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_mediaTaggingManager.IsBusy)
            {
                _logger.LogInformation("Collection Manager reconciliation is waiting for Media Tagging Manager or another library scan and a three-minute metadata quiet period.");
                _scheduler.RequestReconciliation(forceTaskRun: true);
                return;
            }

            _libraryIdentities.Reconcile();
            while (_requests.TryTakeSavedRecipeRecreation(out var collectionId))
            {
                try
                {
                    progress.Report(5);
                    var reconciliation = await _reconciler.ReconcileSavedCreationRecipeAsync(collectionId, cancellationToken).ConfigureAwait(false);
                    _requests.CompleteSavedRecipeRecreation(reconciliation);
                    progress.Report(100);
                }
                catch (MediaTaggingManagerBusyException)
                {
                    _requests.DeferSavedRecipeRecreation(collectionId);
                    _scheduler.RequestReconciliation(forceTaskRun: true);
                    return;
                }
                catch (OperationCanceledException)
                {
                    _requests.FailSavedRecipeRecreation(collectionId, "Jellyfin stopped the recreation before it finished.");
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Could not recreate saved Collection Manager recipe {CollectionId}.", collectionId);
                    _requests.FailSavedRecipeRecreation(collectionId, exception.Message);
                    progress.Report(100);
                }
            }

            if (_requests.TryTakeAllEnabledRulesRequest())
            {
                try
                {
                    await _reconciler.ReconcileEnabledRulesAsync(cancellationToken).ConfigureAwait(false);
                    await _reconciler.ReconcileSavedCreationRecipesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (MediaTaggingManagerBusyException)
                {
                    _requests.EnqueueAllEnabledRules();
                    _scheduler.RequestReconciliation(forceTaskRun: true);
                    return;
                }

                progress.Report(100);
                return;
            }

            var requestedRuleIds = _requests.DrainRuleIds();
            if (requestedRuleIds.Count > 0)
            {
                try
                {
                    for (var index = 0; index < requestedRuleIds.Count; index++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        _mediaTaggingManager.ThrowIfBusy();
                        await _reconciler.ReconcileRuleAsync(requestedRuleIds[index], cancellationToken).ConfigureAwait(false);
                        progress.Report((index + 1) * 100d / requestedRuleIds.Count);
                    }
                }
                catch (MediaTaggingManagerBusyException)
                {
                    _requests.RequeueRuleIds(requestedRuleIds);
                    _scheduler.RequestReconciliation(forceTaskRun: true);
                    return;
                }

                return;
            }

            var changedItemIds = _requests.DrainChangedItemIds();
            if (changedItemIds.Count > 0)
            {
                try
                {
                    await _reconciler.ReconcileChangedItemsAsync(changedItemIds, cancellationToken).ConfigureAwait(false);
                }
                catch (MediaTaggingManagerBusyException)
                {
                    _requests.RequeueChangedItems(changedItemIds);
                    _scheduler.RequestReconciliation(forceTaskRun: true);
                    return;
                }

                progress.Report(100);
                return;
            }

            var configuration = Plugin.Instance?.Configuration;
            if (configuration?.ScheduledReconciliationEnabled != true)
            {
                progress.Report(100);
                return;
            }

            var minimumDelay = TimeSpan.FromMinutes(Math.Clamp(configuration.ScheduledReconciliationMinutes, 60, 10080));
            if (configuration.LastScheduledReconciliationUtc is { } lastRun && DateTime.UtcNow - lastRun < minimumDelay)
            {
                progress.Report(100);
                return;
            }

            _logger.LogInformation("Starting full scheduled Collection Manager reconciliation.");
            try
            {
                await _reconciler.ReconcileEnabledRulesAsync(cancellationToken).ConfigureAwait(false);
                await _reconciler.ReconcileSavedCreationRecipesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (MediaTaggingManagerBusyException)
            {
                _scheduler.RequestReconciliation(forceTaskRun: true);
                return;
            }

            Plugin.Instance?.UpdateConfigurationSafely(updated =>
            {
                updated.LastScheduledReconciliationUtc = DateTime.UtcNow;
                return 0;
            });
            _logger.LogInformation("Completed full scheduled Collection Manager reconciliation.");
            progress.Report(100);
        }
        finally
        {
            TaskExecutionLock.Release();
            _requests.CompleteTargetedMetadataReconciliation();
            if (_requests.HasPendingRequests)
            {
                _scheduler.RequestReconciliation();
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        var configuration = Plugin.Instance?.Configuration;
        if (configuration?.ScheduledReconciliationEnabled != true)
        {
            return [];
        }

        return [new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromMinutes(Math.Clamp(configuration.ScheduledReconciliationMinutes, 60, 10080)).Ticks,
        }];
    }
}
