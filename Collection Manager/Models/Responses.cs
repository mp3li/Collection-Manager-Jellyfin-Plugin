namespace Jellyfin.Plugin.CollectionManager.Models;

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

/// <summary>Progress and availability information for the local metadata catalog.</summary>
public sealed record MetadataCatalogStatus(
    bool IsScanning,
    int ProcessedItems,
    int TotalItems,
    DateTime? LastCompletedUtc,
    string Message);

/// <summary>Persisted, read-only metadata-catalog data from the last completed scan.</summary>
public sealed record MetadataCatalogSnapshot(
    DateTime LastCompletedUtc,
    IReadOnlyList<MetadataCatalogSnapshotLibrary> Libraries);

/// <summary>One selected library and its captured metadata catalog from the last completed scan.</summary>
public sealed record MetadataCatalogSnapshotLibrary(
    Guid LibraryId,
    string LibraryName,
    IReadOnlyList<MetadataCatalogItem> Items,
    IReadOnlyList<string> Columns);

/// <summary>Small dashboard response identifying the libraries and timestamp of the last available scan.</summary>
public sealed record MetadataCatalogAvailability(
    DateTime? LastCompletedUtc,
    IReadOnlyList<MetadataCatalogScanLibrary> Libraries);

/// <summary>One library available in the last completed metadata scan.</summary>
public sealed record MetadataCatalogScanLibrary(Guid LibraryId, string LibraryName, int ItemCount);

/// <summary>One media item and the existing metadata values found for it during a catalog scan.</summary>
public sealed record MetadataCatalogItem(
    Guid Id,
    string Title,
    Guid LibraryId,
    string LibraryName,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Metadata);

/// <summary>One page from a selected library's local metadata catalog.</summary>
public sealed record MetadataCatalogPage(
    Guid LibraryId,
    string LibraryName,
    int Page,
    int PageSize,
    int TotalItems,
    IReadOnlyList<string> Columns,
    IReadOnlyList<MetadataCatalogItem> Items);

/// <summary>One available metadata type and its number of distinct values in a library catalog.</summary>
public sealed record MetadataCatalogType(string Name, int ValueCount);

/// <summary>One distinct metadata value with its matching media count.</summary>
public sealed record MetadataCatalogValue(string Value, int MatchingItems, string? PersonImageUrl);

/// <summary>A bounded page of metadata values for one library and metadata type.</summary>
public sealed record MetadataCatalogValuePage(
    Guid LibraryId,
    string MetadataType,
    int Page,
    int PageSize,
    int TotalValues,
    IReadOnlyList<MetadataCatalogValue> Values);

/// <summary>Preview of the current media items that a collection draft would include.</summary>
public sealed record CatalogPreviewItem(Guid Id, string Title, Guid LibraryId, string LibraryName);

/// <summary>Preview of the current catalog media that a draft would include, grouped by library in the dashboard.</summary>
public sealed record IndividualCollectionDraftPreview(int MatchingItems, IReadOnlyList<CatalogPreviewItem> Items);

/// <summary>Conflict information found before an individual collection draft is created.</summary>
public sealed record IndividualCollectionDraftConflict(string CollectionTitle, bool ExistingCollectionFound);

/// <summary>The completed outcome for one individual collection draft.</summary>
public sealed record IndividualCollectionDraftResult(string CollectionTitle, string Outcome, string Message);
