using Jellyfin.Plugin.MediaCollectionManager.Configuration;

namespace Jellyfin.Plugin.MediaCollectionManager.Models;

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

    /// <summary>Gets or sets optional library root ids.</summary>
    public List<Guid> LibraryIds { get; set; } = [];

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

    /// <summary>Gets or sets optional library root ids.</summary>
    public List<Guid> LibraryIds { get; set; } = [];

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

    /// <summary>Gets or sets its initial media item ids.</summary>
    public List<Guid> ItemIds { get; set; } = [];
}
