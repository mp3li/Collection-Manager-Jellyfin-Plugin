using Jellyfin.Plugin.MediaCollectionManager.Configuration;
using Jellyfin.Plugin.MediaCollectionManager.Models;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;

namespace Jellyfin.Plugin.MediaCollectionManager.Services;

/// <summary>Builds, updates, and reconciles standard Jellyfin collections from local metadata.</summary>
public sealed class CollectionReconciler
{
    private const string ProviderTagPrefix = "Provider: ";
    private const string NetworkTagPrefix = "Network: ";
    private static readonly HashSet<string> ExcludedGenericJellyfinFields = new(StringComparer.Ordinal)
    {
        "Id", "Path", "InternalId", "ServerId", "FileNameWithoutExtension", "DateCreated",
        "DateLastSaved", "DateLastRefreshed", "DateModified", "IsFolder", "IsVirtualItem",
        "SupportsPeople", "SupportsAddingTo", "SupportsPositionTicksResume", "IsLocked",
    };
    private static readonly SemaphoreSlim ReconciliationLock = new(1, 1);
    private readonly ICollectionManager _collectionManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<CollectionReconciler> _logger;

    /// <summary>Initializes a new instance of the <see cref="CollectionReconciler"/> class.</summary>
    public CollectionReconciler(
        ICollectionManager collectionManager,
        ILibraryManager libraryManager,
        ILogger<CollectionReconciler> logger)
    {
        _collectionManager = collectionManager;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>Gets all library metadata facets available for rules and bulk creation.</summary>
    public MetadataFacets GetFacets()
    {
        var items = GetMediaItems().Select(item => (Item: item, Nfo: NfoMetadataReader.Read(item))).ToArray();
        return new MetadataFacets
        {
            Providers = Values(SourceValues(items.SelectMany(value => TaggedValues(value.Item.Tags, ProviderTagPrefix)), items.SelectMany(value => value.Nfo.Providers))),
            Networks = Values(SourceValues(items.SelectMany(value => TaggedValues(value.Item.Tags, NetworkTagPrefix)), items.SelectMany(value => value.Nfo.Networks))),
            Tags = Values(SourceValues(items.SelectMany(value => value.Item.Tags), items.SelectMany(value => value.Nfo.Tags))),
            Genres = Values(SourceValues(items.SelectMany(value => value.Item.Genres), items.SelectMany(value => value.Nfo.Genres))),
            Studios = Values(SourceValues(items.SelectMany(value => value.Item.Studios), items.SelectMany(value => value.Nfo.Studios))),
            Actors = Values(SourceValues(PersonValues(items.Select(value => value.Item), "Actor"), items.SelectMany(value => value.Nfo.Actors))),
            Directors = Values(SourceValues(PersonValues(items.Select(value => value.Item), "Director"), items.SelectMany(value => value.Nfo.Directors))),
            Composers = Values(SourceValues([], items.SelectMany(value => value.Nfo.Composers))),
            Writers = Values(SourceValues(items.SelectMany(value => _libraryManager.GetPeople(value.Item)
                .Where(person => string.Equals(person.Type.ToString(), "Writer", StringComparison.OrdinalIgnoreCase))
                .Select(person => person.Name)), items.SelectMany(value => value.Nfo.Writers))),
            Producers = Values(SourceValues([], items.SelectMany(value => value.Nfo.Producers))),
            Countries = Values(SourceValues(items.SelectMany(value => value.Item.ProductionLocations), items.SelectMany(value => value.Nfo.Countries))),
            Languages = Values(SourceValues(items.Select(value => value.Item.PreferredMetadataLanguage), items.SelectMany(value => value.Nfo.Languages))),
            ContentRatings = Values(SourceValues(items.Select(value => value.Item.OfficialRating), items.SelectMany(value => value.Nfo.ContentRatings))),
            ProductionYears = Values(SourceValues(items.Where(value => value.Item.ProductionYear.HasValue)
                .Select(value => value.Item.ProductionYear!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), items.SelectMany(value => value.Nfo.ProductionYears))),
            JellyfinFields = FieldFacets(items.SelectMany(value => JellyfinFieldValues(value.Item)), useNfoValues: false),
            NfoFields = FieldFacets(items.SelectMany(value => value.Nfo.Fields
                .SelectMany(field => field.Value.Select(itemValue => new KeyValuePair<string, string>(field.Key, itemValue)))
                .AsEnumerable()), useNfoValues: true),
        };
    }

    /// <summary>Runs every enabled plugin-managed collection rule.</summary>
    public async Task<IReadOnlyList<ReconciliationResult>> ReconcileEnabledRulesAsync(CancellationToken cancellationToken)
    {
        var rules = Plugin.Instance?.Configuration.Rules.Where(rule => rule.Enabled).ToArray() ?? [];
        var results = new List<ReconciliationResult>(rules.Length);
        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ReconcileRuleAsync(rule.Id, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    /// <summary>Reconciles exactly one stored rule.</summary>
    public async Task<ReconciliationResult> ReconcileRuleAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        await ReconciliationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var configuration = RequireConfiguration();
            var rule = configuration.Rules.SingleOrDefault(candidate => candidate.Id == ruleId)
                ?? throw new KeyNotFoundException("The requested collection rule no longer exists.");
            var matchingItems = GetMediaItems(rule.LibraryIds)
                .Where(item => Matches(rule, item))
                .Select(item => item.Id)
                .Distinct()
                .ToArray();

            var collection = rule.CollectionId.HasValue
                ? _libraryManager.GetItemById<BoxSet>(rule.CollectionId.Value)
                : null;
            if (collection is null)
            {
                collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                {
                    Name = rule.Name,
                    ItemIdList = matchingItems.Select(itemId => itemId.ToString("N", System.Globalization.CultureInfo.InvariantCulture)).ToArray(),
                }).ConfigureAwait(false);
                rule.CollectionId = collection.Id;
                rule.LastRunUtc = DateTime.UtcNow;
                Save(configuration);
                return new ReconciliationResult(rule.Id, collection.Id, matchingItems.Length, matchingItems.Length, 0, rule.Name);
            }

            var currentIds = collection.GetLinkedChildren().Select(item => item.Id).ToHashSet();
            var desiredIds = matchingItems.ToHashSet();
            var additions = desiredIds.Except(currentIds).ToArray();
            var removals = rule.RemoveItemsNoLongerMatching
                ? currentIds.Except(desiredIds).ToArray()
                : [];

            if (additions.Length > 0)
            {
                await _collectionManager.AddToCollectionAsync(collection.Id, additions).ConfigureAwait(false);
            }

            if (removals.Length > 0)
            {
                await _collectionManager.RemoveFromCollectionAsync(collection.Id, removals).ConfigureAwait(false);
            }

            rule.LastRunUtc = DateTime.UtcNow;
            Save(configuration);
            _logger.LogInformation(
                "Reconciled collection {CollectionName}: {Matching} matching, {Added} added, {Removed} removed.",
                rule.Name,
                matchingItems.Length,
                additions.Length,
                removals.Length);
            return new ReconciliationResult(rule.Id, collection.Id, matchingItems.Length, additions.Length, removals.Length, rule.Name);
        }
        finally
        {
            ReconciliationLock.Release();
        }
    }

    /// <summary>Creates an ordinary Jellyfin collection with a bulk initial item set.</summary>
    public Task<BoxSet> CreateCollectionAsync(string name, IEnumerable<Guid> itemIds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A collection name is required.", nameof(name));
        }

        return _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
        {
            Name = name.Trim(),
            ItemIdList = itemIds.Select(itemId => itemId.ToString("N", System.Globalization.CultureInfo.InvariantCulture)).Distinct().ToArray(),
        });
    }

    /// <summary>Adds many selected items to an existing standard Jellyfin collection.</summary>
    public Task AddToCollectionAsync(Guid collectionId, IEnumerable<Guid> itemIds) =>
        _collectionManager.AddToCollectionAsync(collectionId, itemIds.Distinct());

    /// <summary>Removes many selected items from an existing standard Jellyfin collection.</summary>
    public Task RemoveFromCollectionAsync(Guid collectionId, IEnumerable<Guid> itemIds) =>
        _collectionManager.RemoveFromCollectionAsync(collectionId, itemIds.Distinct());

    /// <summary>Searches media within the libraries selected for this plugin.</summary>
    public IReadOnlyList<MediaSearchResult> SearchMedia(string? searchTerm)
    {
        var term = searchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            return [];
        }

        return GetMediaItems()
            .Where(item => item.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.SortName ?? item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .Select(item => new MediaSearchResult(item.Id, item.Name, item.GetType().Name, item.ProductionYear))
            .ToArray();
    }

    /// <summary>Renames the standard Jellyfin collection attached to an edited rule.</summary>
    public async Task RenameCollectionAsync(Guid collectionId, string name, CancellationToken cancellationToken)
    {
        var collection = _libraryManager.GetItemById<BoxSet>(collectionId)
            ?? throw new KeyNotFoundException("The collection attached to this rule no longer exists.");
        if (string.Equals(collection.Name, name, StringComparison.Ordinal))
        {
            return;
        }

        collection.Name = name;
        await _libraryManager.UpdateItemAsync(collection, collection, ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
    }

    private static PluginConfiguration RequireConfiguration() =>
        Plugin.Instance?.Configuration ?? throw new InvalidOperationException("Media Collection Manager is not initialized.");

    private static void Save(PluginConfiguration configuration) =>
        Plugin.Instance?.SaveConfiguration(configuration);

    private static IReadOnlyList<string> Values(IEnumerable<string?> values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IEnumerable<string> PersonValues(IEnumerable<BaseItem> items, string personType) =>
        items.SelectMany(item => _libraryManager.GetPeople(item))
            .Where(person => string.Equals(person.Type.ToString(), personType, StringComparison.OrdinalIgnoreCase))
            .Select(person => person.Name);

    private IEnumerable<BaseItem> GetMediaItems(IEnumerable<Guid>? libraryIds = null)
    {
        var requestedLibraries = libraryIds?.Distinct().ToArray() ?? [];
        var configuration = RequireConfiguration();
        var selectedLibraries = configuration.UseAllLibraries
            ? requestedLibraries
            : (requestedLibraries.Length == 0
                ? configuration.LibraryIds.Distinct().ToArray()
                : requestedLibraries.Intersect(configuration.LibraryIds).ToArray());
        if (selectedLibraries.Length == 0)
        {
            if (!configuration.UseAllLibraries)
            {
                return [];
            }

            return _libraryManager.GetItemList(new InternalItemsQuery { Recursive = true })
                .Where(item => item is not BoxSet);
        }

        return selectedLibraries.SelectMany(libraryId => _libraryManager.GetItemList(new InternalItemsQuery
            {
                ParentId = libraryId,
                Recursive = true,
            }))
            .Where(item => item is not BoxSet)
            .GroupBy(item => item.Id)
            .Select(group => group.First());
    }

    private bool Matches(CollectionRule rule, BaseItem item)
    {
        var desired = rule.Values.Where(value => !string.IsNullOrWhiteSpace(value)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (desired.Count == 0)
        {
            return false;
        }

        return GetValues(rule, item).Any(desired.Contains);
    }

    private IEnumerable<string> GetValues(CollectionRule rule, BaseItem item) => rule.Field switch
    {
        CollectionRuleField.Provider => SourceValues(TaggedValues(item.Tags, ProviderTagPrefix), NfoMetadataReader.Read(item).Providers),
        CollectionRuleField.Network => SourceValues(TaggedValues(item.Tags, NetworkTagPrefix), NfoMetadataReader.Read(item).Networks),
        CollectionRuleField.Tag => SourceValues(item.Tags, NfoMetadataReader.Read(item).Tags),
        CollectionRuleField.Genre => SourceValues(item.Genres, NfoMetadataReader.Read(item).Genres),
        CollectionRuleField.Studio => SourceValues(item.Studios, NfoMetadataReader.Read(item).Studios),
        CollectionRuleField.Actor => SourceValues(PersonValues([item], "Actor"), NfoMetadataReader.Read(item).Actors),
        CollectionRuleField.Director => SourceValues(PersonValues([item], "Director"), NfoMetadataReader.Read(item).Directors),
        CollectionRuleField.Composer => SourceValues([], NfoMetadataReader.Read(item).Composers),
        CollectionRuleField.Writer => SourceValues(PersonValues([item], "Writer"), NfoMetadataReader.Read(item).Writers),
        CollectionRuleField.Producer => SourceValues([], NfoMetadataReader.Read(item).Producers),
        CollectionRuleField.Country => SourceValues(item.ProductionLocations, NfoMetadataReader.Read(item).Countries),
        CollectionRuleField.Language => SourceValues([item.PreferredMetadataLanguage], NfoMetadataReader.Read(item).Languages),
        CollectionRuleField.ContentRating => SourceValues([item.OfficialRating], NfoMetadataReader.Read(item).ContentRatings),
        CollectionRuleField.ProductionYear => SourceValues(
            item.ProductionYear.HasValue ? [item.ProductionYear.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)] : [],
            NfoMetadataReader.Read(item).ProductionYears),
        CollectionRuleField.JellyfinField => SourceValues(JellyfinFieldValues(item, rule.MetadataFieldName).Select(value => value.Value), []),
        CollectionRuleField.NfoField => NfoFieldValues(item, rule.MetadataFieldName),
        _ => [],
    };

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> FieldFacets(
        IEnumerable<KeyValuePair<string, string>> values,
        bool useNfoValues)
    {
        return values.GroupBy(value => value.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new KeyValuePair<string, IReadOnlyList<string>>(
                group.Key,
                Values(useNfoValues
                    ? SourceValues([], group.Select(value => value.Value))
                    : SourceValues(group.Select(value => value.Value), []))))
            .Where(pair => pair.Value.Count > 0)
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> NfoFieldValues(BaseItem item, string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return [];
        }

        var nfo = NfoMetadataReader.Read(item);
        return nfo.Fields.TryGetValue(fieldName.Trim(), out var values)
            ? SourceValues([], values)
            : [];
    }

    private static IEnumerable<KeyValuePair<string, string>> JellyfinFieldValues(BaseItem item, string? requestedFieldName = null)
    {
        foreach (var property in item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || ExcludedGenericJellyfinFields.Contains(property.Name))
            {
                continue;
            }

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type != typeof(string) && !type.IsEnum && !type.IsPrimitive && type != typeof(decimal) && type != typeof(DateTime) && type != typeof(DateTimeOffset) && type != typeof(Guid))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(requestedFieldName) && !string.Equals(property.Name, requestedFieldName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            object? rawValue;
            try
            {
                rawValue = property.GetValue(item);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (rawValue is null)
            {
                continue;
            }

            var value = rawValue is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : rawValue.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return new KeyValuePair<string, string>(property.Name, value.Trim());
            }
        }
    }

    private static IEnumerable<string> TaggedValues(IEnumerable<string> tags, string prefix) =>
        tags.Where(tag => tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(tag => tag[prefix.Length..])
            .Where(value => !string.IsNullOrWhiteSpace(value));

    private static IEnumerable<string> SourceValues(IEnumerable<string?> jellyfinValues, IEnumerable<string?> nfoValues)
    {
        var jellyfin = jellyfinValues.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim());
        var nfo = nfoValues.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim());
        var configuration = RequireConfiguration();
        return configuration.MetadataSourceMode switch
        {
            MetadataSourceMode.JellyfinOnly => jellyfin,
            MetadataSourceMode.NfoOnly => nfo,
            _ when configuration.MetadataSourcePriority == MetadataSourcePriority.LocalNfo => nfo.Concat(jellyfin).Distinct(StringComparer.OrdinalIgnoreCase),
            _ => jellyfin.Concat(nfo).Distinct(StringComparer.OrdinalIgnoreCase),
        };
    }
}
