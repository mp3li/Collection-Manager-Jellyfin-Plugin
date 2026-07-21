using Jellyfin.Plugin.MediaCollectionManager.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.MediaCollectionManager.Tasks;

/// <summary>Dashboard task that reconciles enabled automatic collection rules.</summary>
public sealed class ReconcileCollectionsTask : IScheduledTask
{
    private readonly CollectionReconciler _reconciler;

    /// <summary>Initializes a new instance of the <see cref="ReconcileCollectionsTask"/> class.</summary>
    public ReconcileCollectionsTask(CollectionReconciler reconciler) => _reconciler = reconciler;

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
        configuration.LastScheduledReconciliationUtc = DateTime.UtcNow;
        Plugin.Instance?.SaveConfiguration(configuration);
        progress.Report(100);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() =>
        [new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.TicksPerHour }];
}
