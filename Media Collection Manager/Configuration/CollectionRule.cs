namespace Jellyfin.Plugin.MediaCollectionManager.Configuration;

/// <summary>Metadata fields that can drive an automatic collection.</summary>
public enum CollectionRuleField
{
    Tag,
    Provider,
    Network,
    Genre,
    Actor,
    Studio,
    Director,
    Composer,
    Writer,
    Producer,
    Country,
    Language,
    ContentRating,
    ProductionYear,
    JellyfinField,
    NfoField,
}

/// <summary>A persistent definition for one plugin-managed Jellyfin collection.</summary>
public sealed class CollectionRule
{
    /// <summary>Gets or sets the rule identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the collection's display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the metadata source to match.</summary>
    public CollectionRuleField Field { get; set; }

    /// <summary>Gets or sets the selected field name for a generic Jellyfin or NFO rule.</summary>
    public string? MetadataFieldName { get; set; }

    /// <summary>Gets or sets values matched with OR logic.</summary>
    public List<string> Values { get; set; } = [];

    /// <summary>Gets or sets optional library root ids. Empty means every library.</summary>
    public List<Guid> LibraryIds { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether this rule participates in automatic reconciliation.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether non-matching items are removed during reconciliation.</summary>
    public bool RemoveItemsNoLongerMatching { get; set; } = true;

    /// <summary>Gets or sets the collection created or claimed by this rule.</summary>
    public Guid? CollectionId { get; set; }

    /// <summary>Gets or sets when this rule was last reconciled.</summary>
    public DateTime? LastRunUtc { get; set; }
}
