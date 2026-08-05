using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CollectionManager.Configuration;

/// <summary>Identifies the Collection Manager creation tab that produced a collection.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CollectionCreationRecipeKind
{
    Manual,
    IndividualTag,
    CombinedTags,
    MultiMatchTags,
}

/// <summary>Persists the complete creation-tab inputs for one Collection Manager collection.</summary>
public sealed class CollectionCreationRecipe
{
    public Guid CollectionId { get; set; }

    public CollectionCreationRecipeKind Kind { get; set; }

    public string CollectionTitle { get; set; } = string.Empty;

    public string? Overview { get; set; }

    public CollectionArtPreference ArtPreference { get; set; } = CollectionArtPreference.JellyfinDefault;

    /// <summary>Stores the fixed manual selection for collections made from the manual tab.</summary>
    public List<Guid> ManualItemIds { get; set; } = [];

    /// <summary>Stores the source tag selections for individual, combined, and multi-match collections.</summary>
    public List<CollectionCreationTagSelection> SelectedTags { get; set; } = [];

    /// <summary>Stores the additional libraries selected for a tag-based creation tab.</summary>
    public List<Guid> AdditionalLibraryIds { get; set; } = [];

    /// <summary>Gets or sets whether every selected tag must match.</summary>
    public bool RequireAllTags { get; set; }

    /// <summary>Gets or sets whether a recreated external collection retains the full creation-tab editor on later edits.</summary>
    public bool UsesFullEditorTabs { get; set; }
}

/// <summary>One saved selected tag from a creation-tab recipe.</summary>
public sealed class CollectionCreationTagSelection
{
    public Guid SourceLibraryId { get; set; }

    public string MetadataType { get; set; } = string.Empty;

    public string MetadataValue { get; set; } = string.Empty;
}
