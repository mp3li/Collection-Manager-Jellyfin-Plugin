using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.CollectionManager.Services;

/// <summary>Detects optional Media Tagging Manager work without taking a plugin assembly dependency.</summary>
public sealed class MediaTaggingManagerActivityGate
{
    private const string TaskKeyPrefix = "MediaTaggingManager";
    private static readonly HashSet<string> LibraryScanTaskKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "RefreshLibrary",
        "RefreshMediaLibrary",
    };
    private static readonly HashSet<string> CollectionManagerScanTaskKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "CollectionManagerMetadataCatalogScan",
        "CollectionManagerCollectionOverviewScan",
    };
    private readonly ITaskManager _taskManager;

    /// <summary>Initializes a new instance of the <see cref="MediaTaggingManagerActivityGate"/> class.</summary>
    public MediaTaggingManagerActivityGate(ITaskManager taskManager) => _taskManager = taskManager;

    /// <summary>Gets a value indicating whether any Media Tagging Manager scheduled task is active.</summary>
    public bool IsBusy => _taskManager.ScheduledTasks.Any(worker =>
        (worker.ScheduledTask.Key.StartsWith(TaskKeyPrefix, StringComparison.OrdinalIgnoreCase)
            || LibraryScanTaskKeys.Contains(worker.ScheduledTask.Key)
            || CollectionManagerScanTaskKeys.Contains(worker.ScheduledTask.Key))
        && worker.State is TaskState.Running or TaskState.Cancelling);

    /// <summary>Stops a reconciliation at its next safe checkpoint when Media Tagging Manager starts.</summary>
    public void ThrowIfBusy()
    {
        if (IsBusy)
        {
            throw new MediaTaggingManagerBusyException();
        }
    }
}

/// <summary>Signals that reconciliation should remain queued until Media Tagging Manager becomes idle.</summary>
public sealed class MediaTaggingManagerBusyException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="MediaTaggingManagerBusyException"/> class.</summary>
    public MediaTaggingManagerBusyException()
        : base("Media Tagging Manager or another conflicting library scan is still working.")
    {
    }
}
