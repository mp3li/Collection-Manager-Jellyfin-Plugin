using Jellyfin.Plugin.MediaCollectionManager.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.MediaCollectionManager.Tasks;

/// <summary>Dashboard task that reconciles enabled automatic collection rules.</summary>
public sealed class ReconcileCollectionsTask : IScheduledTask
{
    private readonly CollectionReconciler _reconciler;
    private readonly ManualReconciliationRequestQueue _requests;

    /// <summary>Initializes a new instance of the <see cref="ReconcileCollectionsTask"/> class.</summary>
    public ReconcileCollectionsTask(CollectionReconciler reconciler, ManualReconciliationRequestQueue requests)
    {
        _reconciler = reconciler;
        _requests = requests;
    }

    /// <inheritdoc />
    public string Name => "Reconcile Media Collection Manager rules";

    /// <inheritdoc />
    public string Key => "MediaCollectionManagerReconcile";

    /// <inheritdoc />
    public string Description => "Creates, adds, and removes collection items from the enabled Media Collection Manager rules.";

    /// <inheritdoc />
    public string Category => "Library";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (_requests.TryTakeAllEnabledRulesRequest())
        {
            await _reconciler.ReconcileEnabledRulesAsync(cancellationToken).ConfigureAwait(false);
            progress.Report(100);
            return;
        }

        var requestedRuleIds = _requests.DrainRuleIds();
        if (requestedRuleIds.Count > 0)
        {
            for (var index = 0; index < requestedRuleIds.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _reconciler.ReconcileRuleAsync(requestedRuleIds[index], cancellationToken).ConfigureAwait(false);
                progress.Report((index + 1) * 100d / requestedRuleIds.Count);
            }

            return;
        }

        var configuration = Plugin.Instance?.Configuration;
        if (configuration?.ScheduledReconciliationEnabled != true)
        {
            progress.Report(100);
            return;
        }

        var minimumDelay = TimeSpan.FromMinutes(Math.Clamp(configuration.ScheduledReconciliationMinutes, 15, 10080));
        if (configuration.LastScheduledReconciliationUtc is { } lastRun && DateTime.UtcNow - lastRun < minimumDelay)
        {
            progress.Report(100);
            return;
        }

        await _reconciler.ReconcileEnabledRulesAsync(cancellationToken).ConfigureAwait(false);
        Plugin.Instance?.UpdateConfigurationSafely(updated =>
        {
            updated.LastScheduledReconciliationUtc = DateTime.UtcNow;
            return 0;
        });
        progress.Report(100);
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
            IntervalTicks = TimeSpan.FromMinutes(Math.Clamp(configuration.ScheduledReconciliationMinutes, 15, 10080)).Ticks,
        }];
    }
}
