namespace Jellyfin.Plugin.MediaCollectionManager.Models;

/// <summary>Facets currently available from the media library.</summary>
public sealed class MetadataFacets
{
    /// <summary>Gets or sets provider values found in local library metadata and NFO sidecars.</summary>
    public IReadOnlyList<string> Providers { get; init; } = [];

    /// <summary>Gets or sets network values found in local library metadata and NFO sidecars.</summary>
    public IReadOnlyList<string> Networks { get; init; } = [];

    /// <summary>Gets or sets tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Gets or sets genres.</summary>
    public IReadOnlyList<string> Genres { get; init; } = [];

    /// <summary>Gets or sets actors.</summary>
    public IReadOnlyList<string> Actors { get; init; } = [];

    /// <summary>Gets or sets studios.</summary>
    public IReadOnlyList<string> Studios { get; init; } = [];

    /// <summary>Gets or sets directors.</summary>
    public IReadOnlyList<string> Directors { get; init; } = [];

    /// <summary>Gets or sets composers.</summary>
    public IReadOnlyList<string> Composers { get; init; } = [];

    /// <summary>Gets or sets writers.</summary>
    public IReadOnlyList<string> Writers { get; init; } = [];

    /// <summary>Gets or sets producers.</summary>
    public IReadOnlyList<string> Producers { get; init; } = [];

    /// <summary>Gets or sets countries or production locations.</summary>
    public IReadOnlyList<string> Countries { get; init; } = [];

    /// <summary>Gets or sets metadata languages.</summary>
    public IReadOnlyList<string> Languages { get; init; } = [];

    /// <summary>Gets or sets content ratings.</summary>
    public IReadOnlyList<string> ContentRatings { get; init; } = [];

    /// <summary>Gets or sets production years.</summary>
    public IReadOnlyList<string> ProductionYears { get; init; } = [];

    /// <summary>Gets scalar Jellyfin item fields and their current values.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> JellyfinFields { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets direct local-NFO fields and their current values.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> NfoFields { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Outcome of one collection reconciliation.</summary>
public sealed record ReconciliationResult(
    Guid RuleId,
    Guid CollectionId,
    int MatchingItems,
    int AddedItems,
    int RemovedItems,
    string CollectionName);

/// <summary>A media item available to the dashboard's scoped manual search.</summary>
public sealed record MediaSearchResult(Guid Id, string Name, string Type, int? ProductionYear);
