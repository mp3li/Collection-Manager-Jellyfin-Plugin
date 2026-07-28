using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MediaCollectionManager.Configuration;

/// <summary>Configuration persisted by Jellyfin for Media Collection Manager.</summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets the metadata sources Media Collection Manager may use.</summary>
    public MetadataSourceMode MetadataSourceMode { get; set; } = MetadataSourceMode.Both;

    /// <summary>Gets or sets which source is presented first when both provide a value.</summary>
    public MetadataSourcePriority MetadataSourcePriority { get; set; } = MetadataSourcePriority.Jellyfin;

    /// <summary>Gets or sets a value indicating whether every library is available to this plugin.</summary>
    /// <remarks>Retained for existing configurations. New dashboard settings save explicit library selections.</remarks>
    public bool UseAllLibraries { get; set; }

    /// <summary>Gets or sets the library roots available when <see cref="UseAllLibraries"/> is false.</summary>
    public List<Guid> LibraryIds { get; set; } = [];

    /// <summary>Gets or sets whether newly added media may join applicable managed collections.</summary>
    public bool AutomaticallyAddNewMediaToApplicableCollections { get; set; }

    /// <summary>Gets or sets the overview metadata text color selected by the administrator.</summary>
    public string MetadataTagOverviewColor { get; set; } = "#00A4DC";
    /// <summary>Gets or sets a value indicating whether metadata-change events can reconcile enabled rules.</summary>
    public bool WatchMetadataChanges { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the scheduled task can reconcile enabled rules.</summary>
    public bool ScheduledReconciliationEnabled { get; set; } = true;

    /// <summary>Gets or sets the minimum number of minutes between scheduled reconciliations.</summary>
    public int ScheduledReconciliationMinutes { get; set; } = 360;

    /// <summary>Gets or sets the last scheduled reconciliation completion time.</summary>
    public DateTime? LastScheduledReconciliationUtc { get; set; }

    /// <summary>Gets or sets all stored automatic collection rules.</summary>
    public List<CollectionRule> Rules { get; set; } = [];
}

/// <summary>Controls which existing metadata source may be used for collection matching.</summary>
public enum MetadataSourceMode
{
    /// <summary>Use metadata Jellyfin has assigned to the item.</summary>
    JellyfinOnly,

    /// <summary>Use metadata from the matching local NFO sidecar only.</summary>
    NfoOnly,

    /// <summary>Use both existing Jellyfin and local NFO metadata.</summary>
    Both,
}

/// <summary>Controls value ordering when both existing metadata sources are enabled.</summary>
public enum MetadataSourcePriority
{
    /// <summary>Prefer Jellyfin-assigned metadata.</summary>
    Jellyfin,

    /// <summary>Prefer matching local NFO metadata.</summary>
    LocalNfo,
}
