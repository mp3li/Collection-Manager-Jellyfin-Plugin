using Jellyfin.Plugin.CollectionManager.Configuration;

namespace Jellyfin.Plugin.CollectionManager.Models;

/// <summary>Values owned by the server-wide Main Settings dashboard tab.</summary>
public sealed class MainSettingsRequest
{
    /// <summary>Gets or sets the selected virtual-folder ids.</summary>
    public List<Guid> LibraryIds { get; set; } = [];

    /// <summary>Gets or sets whether newly added media can join applicable managed collections.</summary>
    public bool AutomaticallyAddNewMediaToApplicableCollections { get; set; }
}

/// <summary>Values owned by the optional full scheduled collection reconciliation setting.</summary>
public sealed class ScheduledReconciliationSettingsRequest
{
    /// <summary>Gets or sets whether the optional full scheduled reconciliation is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the interval between full reconciliations, in whole hours.</summary>
    public int IntervalHours { get; set; } = 24;
}

/// <summary>A request that saves only the selected Jellyfin libraries.</summary>
public sealed class LibrarySelectionRequest
{
    /// <summary>Gets or sets the selected virtual-folder ids.</summary>
    public List<Guid> LibraryIds { get; set; } = [];
}

/// <summary>A request that saves the metadata overview's display color.</summary>
public sealed class MetadataOverviewColorRequest
{
    /// <summary>Gets or sets a CSS hexadecimal color.</summary>
    public string Color { get; set; } = "#00A4DC";
}

/// <summary>Settings controlling which existing collections appear in the collection overview.</summary>
public sealed class CollectionOverviewSettingsRequest
{
    public bool ShowPluginMadeCollections { get; set; } = true;
    public bool ShowNonPluginMadeCollections { get; set; } = true;
}

/// <summary>Colors used to distinguish collection overview changes from the latest scan.</summary>
public sealed class CollectionOverviewColorsRequest
{
    public string AddedColor { get; set; } = "#4CAF50";
    public string RemovedColor { get; set; } = "#F44336";
}

/// <summary>One requested collection title change.</summary>
public sealed class CollectionRenameRequest
{
    public Guid CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
}

/// <summary>One administrator-confirmed request to delete a native Jellyfin collection.</summary>
public sealed class CollectionDeleteRequest
{
    public Guid CollectionId { get; set; }
}

/// <summary>The selected Collection Manager cleanup actions.</summary>
public sealed class CollectionCleanupRequest
{
    public bool UndoLastCollectionAction { get; set; }
    public bool RemoveAllPluginMadeCollections { get; set; }
    public bool RemoveAllMediaAdditionsToExternalCollections { get; set; }
}

/// <summary>One administrator-reviewed collection draft created from a catalog metadata value.</summary>
public sealed class IndividualCollectionDraftRequest
{
    /// <summary>Gets or sets the library where the metadata value was selected.</summary>
    public Guid SourceLibraryId { get; set; }

    /// <summary>Gets or sets the catalog metadata type.</summary>
    public string MetadataType { get; set; } = string.Empty;

    /// <summary>Gets or sets the exact selected metadata value.</summary>
    public string MetadataValue { get; set; } = string.Empty;

    /// <summary>Gets or sets other saved libraries to include when matching the same value.</summary>
    public List<Guid> AdditionalLibraryIds { get; set; } = [];

    /// <summary>Gets or sets the title requested for the native Jellyfin collection.</summary>
    public string CollectionTitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the overview saved on the new native Jellyfin collection.</summary>
    public string? Overview { get; set; }

    /// <summary>Gets or sets how a same-title native collection has been resolved.</summary>
    public string ExistingCollectionAction { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected Collection Manager art preference.</summary>
    public CollectionArtPreference ArtPreference { get; set; } = CollectionArtPreference.JellyfinDefault;
}

/// <summary>One reviewed combined or multi-match collection draft.</summary>
public sealed class TagCollectionDraftRequest
{
    /// <summary>Gets or sets the selected catalog tags.</summary>
    public List<IndividualCollectionDraftRequest> SelectedTags { get; set; } = [];

    /// <summary>Gets or sets extra saved libraries used for every selected tag.</summary>
    public List<Guid> AdditionalLibraryIds { get; set; } = [];

    /// <summary>Gets or sets whether every selected tag must match.</summary>
    public bool RequireAllTags { get; set; }

    /// <summary>Gets or sets the requested native Jellyfin collection title.</summary>
    public string CollectionTitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the overview saved on the new native Jellyfin collection.</summary>
    public string? Overview { get; set; }

    /// <summary>Gets or sets the administrator-selected existing-collection action.</summary>
    public string ExistingCollectionAction { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected Collection Manager art preference.</summary>
    public CollectionArtPreference ArtPreference { get; set; } = CollectionArtPreference.JellyfinDefault;
}

/// <summary>A request to create or update an automatic collection rule.</summary>
public sealed class SaveRuleRequest
{
    /// <summary>Gets or sets an existing rule id, or null to create one.</summary>
    public Guid? Id { get; set; }

    /// <summary>Gets or sets the human-readable collection name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the source metadata field.</summary>
    public CollectionRuleField Field { get; set; }

    /// <summary>Gets or sets the selected field name for a generic Jellyfin or NFO rule.</summary>
    public string? MetadataFieldName { get; set; }

    /// <summary>Gets or sets the selected values.</summary>
    public List<string> Values { get; set; } = [];

    /// <summary>Gets or sets whether automatic reconciliation is enabled for this rule.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets whether reconciliation removes items no longer matching.</summary>
    public bool RemoveItemsNoLongerMatching { get; set; } = true;
}

/// <summary>A request to create many same-field collections from selected values.</summary>
public sealed class BulkCreateRulesRequest
{
    /// <summary>Gets or sets the metadata field.</summary>
    public CollectionRuleField Field { get; set; }

    /// <summary>Gets or sets the selected field name for a generic Jellyfin or NFO rule.</summary>
    public string? MetadataFieldName { get; set; }

    /// <summary>Gets or sets the selected facet values.</summary>
    public List<string> Values { get; set; } = [];

    /// <summary>Gets or sets an optional prefix for generated collection names.</summary>
    public string NamePrefix { get; set; } = string.Empty;
}

/// <summary>A direct collection membership operation.</summary>
public sealed class CollectionMembershipRequest
{
    /// <summary>Gets or sets the target Jellyfin collection id.</summary>
    public Guid CollectionId { get; set; }

    /// <summary>Gets or sets media item ids.</summary>
    public List<Guid> ItemIds { get; set; } = [];
}

/// <summary>A direct request to create one standard Jellyfin collection.</summary>
public sealed class CreateCollectionRequest
{
    /// <summary>Gets or sets the collection name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the overview saved on the new native Jellyfin collection.</summary>
    public string? Overview { get; set; }

    /// <summary>Gets or sets the selected Collection Manager art preference.</summary>
    public CollectionArtPreference ArtPreference { get; set; } = CollectionArtPreference.JellyfinDefault;

    /// <summary>Gets or sets its initial media item ids.</summary>
    public List<Guid> ItemIds { get; set; } = [];
}

/// <summary>One complete, editable saved set of inputs from a Collection Manager creation tab.</summary>
public sealed class CollectionCreationRecipeUpdateRequest
{
    /// <summary>Gets or sets the source creation tab.</summary>
    public CollectionCreationRecipeKind Kind { get; set; }

    /// <summary>Gets or sets the collection title.</summary>
    public string CollectionTitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection overview.</summary>
    public string? Overview { get; set; }

    /// <summary>Gets or sets the selected Collection Manager art preference.</summary>
    public CollectionArtPreference ArtPreference { get; set; } = CollectionArtPreference.JellyfinDefault;

    /// <summary>Gets or sets the fixed media selection for a manual collection.</summary>
    public List<Guid> ManualItemIds { get; set; } = [];

    /// <summary>Gets or sets every source metadata tag selected in a tag-based creation tab.</summary>
    public List<CollectionCreationTagSelection> SelectedTags { get; set; } = [];

    /// <summary>Gets or sets additional libraries selected by a tag-based creation tab.</summary>
    public List<Guid> AdditionalLibraryIds { get; set; } = [];

    /// <summary>Gets or sets whether every selected metadata tag is required.</summary>
    public bool RequireAllTags { get; set; }
}
