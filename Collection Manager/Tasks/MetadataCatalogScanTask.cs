using Jellyfin.Plugin.CollectionManager.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.CollectionManager.Tasks;

/// <summary>Runs an administrator-requested metadata catalog scan as a cancellable Jellyfin task.</summary>
public sealed class MetadataCatalogScanTask : IScheduledTask
{
    private readonly MetadataCatalogService _catalog;

    /// <summary>Initializes a new instance of the <see cref="MetadataCatalogScanTask"/> class.</summary>
    public MetadataCatalogScanTask(MetadataCatalogService catalog) => _catalog = catalog;

    /// <inheritdoc />
    public string Name => "Scan Collection Manager metadata catalog";

    /// <inheritdoc />
    public string Key => "CollectionManagerMetadataCatalogScan";

    /// <inheritdoc />
    public string Description => "Builds Collection Manager's local read-only metadata catalog for its dashboard.";

    /// <inheritdoc />
    public string Category => "Library";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken) =>
        _catalog.ScanAsync(progress, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => [];
}
