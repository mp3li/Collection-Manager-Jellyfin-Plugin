using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CollectionManager.Configuration;

/// <summary>Configuration persisted by Jellyfin for Collection Manager.</summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets the metadata sources Collection Manager may use.</summary>
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

    /// <summary>Gets or sets whether the collection overview includes collections created by this plugin.</summary>
    public bool ShowPluginMadeCollections { get; set; } = true;

    /// <summary>Gets or sets whether the collection overview includes collections not created by this plugin.</summary>
    public bool ShowNonPluginMadeCollections { get; set; } = true;

    /// <summary>Gets or sets the color used for newly added collection names and media titles.</summary>
    public string CollectionOverviewAddedColor { get; set; } = "#4CAF50";

    /// <summary>Gets or sets the color used for newly removed collection names and media titles.</summary>
    public string CollectionOverviewRemovedColor { get; set; } = "#F44336";

    /// <summary>Gets or sets the ids of collections created through Collection Manager.</summary>
    public List<Guid> PluginManagedCollectionIds { get; set; } = [];

    /// <summary>Gets or sets the last saved collection overview scan.</summary>
    public CollectionOverviewSnapshot? CollectionOverviewSnapshot { get; set; }

    /// <summary>Gets or sets recent reversible collection actions initiated by this plugin.</summary>
    public List<CollectionActionRecord> CollectionActionHistory { get; set; } = [];
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
