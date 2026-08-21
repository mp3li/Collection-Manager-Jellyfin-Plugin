using Jellyfin.Plugin.CollectionManager.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.CollectionManager.Tasks;

/// <summary>Runs an administrator-requested collection overview scan as a cancellable Jellyfin task.</summary>
public sealed class CollectionOverviewScanTask : IScheduledTask
{
    private readonly CollectionOverviewService _overview;

    /// <summary>Initializes a new instance of the <see cref="CollectionOverviewScanTask"/> class.</summary>
    public CollectionOverviewScanTask(CollectionOverviewService overview) => _overview = overview;

    /// <inheritdoc />
    public string Name => "Scan Collection Manager collection overview";

    /// <inheritdoc />
    public string Key => "CollectionManagerCollectionOverviewScan";

    /// <inheritdoc />
    public string Description => "Builds Collection Manager's saved dashboard overview of native Jellyfin collections.";

    /// <inheritdoc />
    public string Category => "Library";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) =>
        _overview.ScanAsync(progress, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
}
