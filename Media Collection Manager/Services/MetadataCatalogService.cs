using System.Globalization;
using System.Reflection;
using Jellyfin.Plugin.MediaCollectionManager.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaCollectionManager.Services;

/// <summary>Builds an in-server, read-only catalog of existing metadata for the dashboard overview.</summary>
public sealed class MetadataCatalogService
{
    private static readonly HashSet<string> ExcludedScalarFields = new(StringComparer.Ordinal)
    {
        "Id", "Path", "InternalId", "ServerId", "FileNameWithoutExtension", "DateCreated",
        "DateLastSaved", "DateLastRefreshed", "DateModified", "IsFolder", "IsVirtualItem",
        "SupportsPeople", "SupportsAddingTo", "SupportsPositionTicksResume", "IsLocked",
    };

    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<MetadataCatalogService> _logger;
    private readonly object _sync = new();
    private Dictionary<Guid, CatalogLibrary> _catalogs = [];
    private MetadataCatalogStatus _status = new(false, 0, 0, null, "Save one or more libraries, then scan metadata tags.");

    /// <summary>Initializes a new instance of the <see cref="MetadataCatalogService"/> class.</summary>
    public MetadataCatalogService(ILibraryManager libraryManager, ILogger<MetadataCatalogService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>Gets the current scan status without exposing any media paths or private file information.</summary>
    public MetadataCatalogStatus GetStatus()
    {
        lock (_sync)
        {
            return _status;
        }
    }

    /// <summary>Starts a fresh scan of the saved library selections when another scan is not already running.</summary>
    public MetadataCatalogStatus StartScan()
    {
        lock (_sync)
        {
            if (_status.IsScanning)
            {
                return _status;
            }

            var savedLibraryIds = Plugin.Instance?.Configuration.LibraryIds.Distinct().ToArray() ?? [];
            if (savedLibraryIds.Length == 0)
            {
                _status = new MetadataCatalogStatus(false, 0, 0, null, "Save one or more libraries before scanning metadata tags.");
                return _status;
            }

            _status = new MetadataCatalogStatus(true, 0, 0, _status.LastCompletedUtc, "Preparing the metadata tag scan…");
            _ = Task.Run(ScanAsync);
            return _status;
        }
    }

    /// <summary>Gets one ten-item page for a saved library from the most recent completed scan.</summary>
    public MetadataCatalogPage GetPage(Guid libraryId, int page, int pageSize)
    {
        lock (_sync)
        {
            if (!_catalogs.TryGetValue(libraryId, out var catalog))
            {
                return new MetadataCatalogPage(libraryId, "Selected library", Math.Max(page, 1), pageSize, 0, [], []);
            }

            var normalizedPageSize = Math.Clamp(pageSize, 1, 10);
            var totalItems = catalog.Items.Count;
            var pageCount = Math.Max(1, (int)Math.Ceiling(totalItems / (double)normalizedPageSize));
            var normalizedPage = Math.Clamp(page, 1, pageCount);
            var items = catalog.Items.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray();
            return new MetadataCatalogPage(
                catalog.LibraryId,
                catalog.LibraryName,
                normalizedPage,
                normalizedPageSize,
                totalItems,
                catalog.Columns,
                items);
        }
    }

    /// <summary>Gets metadata types with values in one saved library after a completed scan.</summary>
    public IReadOnlyList<MetadataCatalogType> GetTypes(Guid libraryId)
    {
        lock (_sync)
        {
            if (!_catalogs.TryGetValue(libraryId, out var catalog))
            {
                return [];
            }

            return catalog.Columns.Select(column => new MetadataCatalogType(
                    column,
                    catalog.Items.SelectMany(item => item.Metadata.TryGetValue(column, out var values) ? values : [])
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count()))
                .Where(type => type.ValueCount > 0)
                .OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    /// <summary>Gets a lazy, fifty-value page for a selected catalog metadata type.</summary>
    public MetadataCatalogValuePage GetValues(Guid libraryId, string metadataType, string? searchTerm, int page)
    {
        lock (_sync)
        {
            var normalizedType = metadataType?.Trim() ?? string.Empty;
            if (!_catalogs.TryGetValue(libraryId, out var catalog) || string.IsNullOrWhiteSpace(normalizedType))
            {
                return new MetadataCatalogValuePage(libraryId, normalizedType, 1, 50, 0, []);
            }

            var term = searchTerm?.Trim();
            var values = catalog.Items.SelectMany(item => item.Metadata.TryGetValue(normalizedType, out var itemValues)
                    ? itemValues.Select(value => new { Value = value, ItemId = item.Id })
                    : [])
                .Where(value => string.IsNullOrWhiteSpace(term) || value.Value.Contains(term, StringComparison.OrdinalIgnoreCase))
                .GroupBy(value => value.Value, StringComparer.OrdinalIgnoreCase)
                .Select(group => new MetadataCatalogValue(group.Key, group.Select(value => value.ItemId).Distinct().Count(),
                    IsPersonType(normalizedType) ? FindPersonImageId(catalog, normalizedType, group.Key) : null))
                .OrderBy(value => value.Value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            const int pageSize = 50;
            var pageCount = Math.Max(1, (int)Math.Ceiling(values.Length / (double)pageSize));
            var normalizedPage = Math.Clamp(page, 1, pageCount);
            return new MetadataCatalogValuePage(
                libraryId,
                normalizedType,
                normalizedPage,
                pageSize,
                values.Length,
                values.Skip((normalizedPage - 1) * pageSize).Take(pageSize).ToArray());
        }
    }

    /// <summary>Previews every current catalog item that matches a draft across its chosen libraries.</summary>
    public IndividualCollectionDraftPreview PreviewDraft(IndividualCollectionDraftRequest draft)
    {
        var items = MatchingItems(draft).OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ToArray();
        return new IndividualCollectionDraftPreview(items.Length, items.Select(ToPreviewItem).ToArray());
    }

    /// <summary>Gets the current catalog item ids that match a draft across its source and additional libraries.</summary>
    public IReadOnlyList<Guid> GetMatchingItemIds(IndividualCollectionDraftRequest draft) =>
        MatchingItems(draft).Select(item => item.Id).Distinct().ToArray();

    /// <summary>Previews a union or intersection of selected catalog tag values.</summary>
    public IndividualCollectionDraftPreview PreviewTagCollection(TagCollectionDraftRequest draft)
    {
        var items = MatchingTagCollectionItems(draft).OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ToArray();
        return new IndividualCollectionDraftPreview(items.Length, items.Select(ToPreviewItem).ToArray());
    }

    /// <summary>Gets the current unique media item ids for a union or intersection draft.</summary>
    public IReadOnlyList<Guid> GetMatchingItemIds(TagCollectionDraftRequest draft) => MatchingTagCollectionItems(draft).Select(item => item.Id).ToArray();

    private async Task ScanAsync()
    {
        try
        {
            var selectedIds = Plugin.Instance?.Configuration.LibraryIds.Distinct().ToHashSet() ?? [];
            var folders = _libraryManager.GetVirtualFolders(true)
                .Select(folder => new { Folder = folder, LibraryId = Guid.TryParse(folder.ItemId, out var id) ? id : Guid.Empty })
                .Where(value => value.LibraryId != Guid.Empty && selectedIds.Contains(value.LibraryId))
                .Select(value => new ScanLibrary(
                    value.LibraryId,
                    value.Folder.Name,
                    _libraryManager.GetItemList(new InternalItemsQuery { ParentId = value.LibraryId, Recursive = true })
                        .Where(item => item is not BoxSet)
                        .GroupBy(item => item.Id)
                        .Select(group => group.First())
                        .ToArray()))
                .ToArray();
            var totalItems = folders.Sum(folder => folder.Items.Length);
            UpdateStatus(true, 0, totalItems, "Scanning metadata tags…");

            var nextCatalogs = new Dictionary<Guid, CatalogLibrary>();
            var processed = 0;
            foreach (var folder in folders)
            {
                var rows = new List<MetadataCatalogItem>(folder.Items.Length);
                foreach (var item in folder.Items)
                {
                    rows.Add(CreateCatalogItem(item, folder.Id, folder.Name));
                    processed++;
                    if (processed % 10 == 0 || processed == totalItems)
                    {
                        UpdateStatus(true, processed, totalItems, "Scanning metadata tags…");
                        await Task.Yield();
                    }
                }

                var columns = rows.SelectMany(row => row.Metadata.Keys)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(column => column, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                nextCatalogs[folder.Id] = new CatalogLibrary(
                    folder.Id,
                    folder.Name,
                    rows.OrderBy(row => row.Title, StringComparer.OrdinalIgnoreCase).ToArray(),
                    columns);
            }

            lock (_sync)
            {
                _catalogs = nextCatalogs;
                _status = new MetadataCatalogStatus(false, processed, totalItems, DateTime.UtcNow, "Metadata tag scan complete. Your overview has been updated.");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Media Collection Manager could not scan the saved libraries for metadata tags.");
            lock (_sync)
            {
                _status = new MetadataCatalogStatus(false, 0, 0, _status.LastCompletedUtc, "Metadata tag scan could not be completed. Check the Jellyfin server log and try again.");
            }
        }
    }

    private MetadataCatalogItem CreateCatalogItem(BaseItem item, Guid libraryId, string libraryName)
    {
        var values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var nfo = NfoMetadataReader.Read(item);

        Add(values, "Tags", item.Tags);
        Add(values, "Tags", nfo.Tags);
        Add(values, "Providers", TaggedValues(item.Tags, "Provider: "));
        Add(values, "Providers", nfo.Providers);
        Add(values, "Networks", TaggedValues(item.Tags, "Network: "));
        Add(values, "Networks", nfo.Networks);
        Add(values, "Keywords", TaggedValues(item.Tags, "Keyword: "));
        Add(values, "Existing Collection Metadata", TaggedValues(item.Tags, "Collection: "));
        Add(values, "Genres", item.Genres);
        Add(values, "Genres", nfo.Genres);
        Add(values, "Genres", TaggedValues(item.Tags, "Genre: "));
        Add(values, "Studios", item.Studios);
        Add(values, "Studios", nfo.Studios);
        Add(values, "Production Companies", item.Studios);
        Add(values, "Production Companies", nfo.Studios);
        Add(values, "Cast", People(item, "Actor"));
        Add(values, "Cast", nfo.Actors);
        Add(values, "Crew", Crew(item));
        Add(values, "Directors", People(item, "Director"));
        Add(values, "Directors", nfo.Directors);
        Add(values, "Writers", People(item, "Writer"));
        Add(values, "Writers", nfo.Writers);
        Add(values, "Producers", People(item, "Producer"));
        Add(values, "Producers", nfo.Producers);
        Add(values, "Composers", nfo.Composers);
        Add(values, "Countries", item.ProductionLocations);
        Add(values, "Countries", nfo.Countries);
        Add(values, "Production Countries", item.ProductionLocations);
        Add(values, "Production Countries", nfo.Countries);
        Add(values, "Languages", [item.PreferredMetadataLanguage]);
        Add(values, "Languages", nfo.Languages);
        Add(values, "Content Ratings", [item.OfficialRating]);
        Add(values, "Content Ratings", nfo.ContentRatings);
        Add(values, "Ratings and Classifications", [item.OfficialRating]);
        Add(values, "Ratings and Classifications", nfo.ContentRatings);
        Add(values, "Production Years", item.ProductionYear.HasValue ? [item.ProductionYear.Value.ToString(CultureInfo.InvariantCulture)] : []);
        Add(values, "Production Years", nfo.ProductionYears);

        foreach (var field in GetJellyfinScalarFields(item))
        {
            Add(values, "Jellyfin: " + field.Key, [field.Value]);
        }

        foreach (var field in nfo.Fields)
        {
            Add(values, "NFO: " + field.Key, field.Value);
        }

        var readOnlyValues = values.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
        return new MetadataCatalogItem(item.Id, item.Name, libraryId, libraryName, readOnlyValues);
    }

    private IEnumerable<string> People(BaseItem item, string type) =>
        _libraryManager.GetPeople(item)
            .Where(person => string.Equals(person.Type.ToString(), type, StringComparison.OrdinalIgnoreCase))
            .Select(person => person.Name);

    private IEnumerable<string> Crew(BaseItem item) =>
        _libraryManager.GetPeople(item)
            .Where(person => !string.Equals(person.Type.ToString(), "Actor", StringComparison.OrdinalIgnoreCase))
            .Select(person => person.Name);

    private static IEnumerable<string> TaggedValues(IEnumerable<string> tags, string prefix) =>
        tags.Where(tag => tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(tag => tag[prefix.Length..])
            .Where(value => !string.IsNullOrWhiteSpace(value));

    private static bool IsPersonType(string metadataType) =>
        metadataType is "Cast" or "Crew" or "Directors" or "Writers" or "Producers" or "Composers";

    private string? FindPersonImageId(CatalogLibrary catalog, string metadataType, string personName)
    {
        foreach (var row in catalog.Items.Where(item => item.Metadata.TryGetValue(metadataType, out var values) && values.Contains(personName, StringComparer.OrdinalIgnoreCase)))
        {
            var item = _libraryManager.GetItemById<BaseItem>(row.Id);
            if (item is null)
            {
                continue;
            }

            var person = _libraryManager.GetPeople(item).FirstOrDefault(candidate =>
                string.Equals(candidate.Name, personName, StringComparison.OrdinalIgnoreCase) && candidate.Id != Guid.Empty);
            if (person is not null && _libraryManager.GetItemById<BaseItem>(person.Id) is { } personItem && personItem.HasImage(ImageType.Primary, 0))
            {
                return person.Id.ToString("N", CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private IReadOnlyList<MetadataCatalogItem> MatchingItems(IndividualCollectionDraftRequest draft)
    {
        var type = draft.MetadataType?.Trim() ?? string.Empty;
        var value = draft.MetadataValue?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var libraryIds = draft.AdditionalLibraryIds.Append(draft.SourceLibraryId).Distinct().ToArray();
        lock (_sync)
        {
            return libraryIds.Where(_catalogs.ContainsKey)
                .SelectMany(id => _catalogs[id].Items)
                .Where(item => item.Metadata.TryGetValue(type, out var values) && values.Contains(value, StringComparer.OrdinalIgnoreCase))
                .GroupBy(item => item.Id)
                .Select(group => group.First())
                .ToArray();
        }
    }

    private IReadOnlyList<MetadataCatalogItem> MatchingTagCollectionItems(TagCollectionDraftRequest draft)
    {
        var tags = draft.SelectedTags.Where(tag => !string.IsNullOrWhiteSpace(tag.MetadataType) && !string.IsNullOrWhiteSpace(tag.MetadataValue)).ToArray();
        if (tags.Length == 0) return [];
        // A combined/multi-match draft has one shared scope. Every source library is always
        // included, and the dashboard may add more libraries to that same scope. This lets a
        // multi-match draft selected from different libraries meaningfully compare each tag
        // across all of the administrator's chosen libraries.
        var sharedLibraryIds = tags.Select(tag => tag.SourceLibraryId)
            .Concat(draft.AdditionalLibraryIds)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var sets = tags.Select(tag => MatchingItems(new IndividualCollectionDraftRequest
        {
            SourceLibraryId = sharedLibraryIds.FirstOrDefault(),
            MetadataType = tag.MetadataType,
            MetadataValue = tag.MetadataValue,
            AdditionalLibraryIds = sharedLibraryIds.Skip(1).ToList(),
        }).ToDictionary(item => item.Id)).ToArray();
        var ids = draft.RequireAllTags
            ? sets.Skip(1).Aggregate(new HashSet<Guid>(sets[0].Keys), (current, next) => { current.IntersectWith(next.Keys); return current; })
            : sets.SelectMany(set => set.Keys).ToHashSet();
        return sets.SelectMany(set => set.Values).Where(item => ids.Contains(item.Id)).GroupBy(item => item.Id).Select(group => group.First()).ToArray();
    }

    private static CatalogPreviewItem ToPreviewItem(MetadataCatalogItem item) => new(item.Id, item.Title, item.LibraryId, item.LibraryName);

    private static void Add(Dictionary<string, List<string>> values, string column, IEnumerable<string?> additions)
    {
        var usable = additions.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).ToArray();
        if (usable.Length == 0)
        {
            return;
        }

        if (!values.TryGetValue(column, out var existing))
        {
            existing = [];
            values[column] = existing;
        }

        existing.AddRange(usable);
    }

    private static IEnumerable<KeyValuePair<string, string>> GetJellyfinScalarFields(BaseItem item)
    {
        foreach (var property in item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || ExcludedScalarFields.Contains(property.Name))
            {
                continue;
            }

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type != typeof(string) && !type.IsEnum && !type.IsPrimitive && type != typeof(decimal) && type != typeof(DateTime) && type != typeof(DateTimeOffset) && type != typeof(Guid))
            {
                continue;
            }

            object? raw;
            try
            {
                raw = property.GetValue(item);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (raw is null)
            {
                continue;
            }

            var value = raw is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : raw.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return new KeyValuePair<string, string>(property.Name, value.Trim());
            }
        }
    }

    private void UpdateStatus(bool isScanning, int processedItems, int totalItems, string message)
    {
        lock (_sync)
        {
            _status = new MetadataCatalogStatus(isScanning, processedItems, totalItems, _status.LastCompletedUtc, message);
        }
    }

    private sealed record ScanLibrary(Guid Id, string Name, BaseItem[] Items);
    private sealed record CatalogLibrary(Guid LibraryId, string LibraryName, IReadOnlyList<MetadataCatalogItem> Items, IReadOnlyList<string> Columns);
}
