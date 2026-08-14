using Jellyfin.Plugin.CollectionManager.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionManager.Services;

/// <summary>Keeps saved library references attached to the same configured locations after a Jellyfin library rename.</summary>
public sealed class LibraryIdentityService
{
    private readonly ILibraryManager _libraryManager;
    private readonly MetadataCatalogService _metadataCatalog;
    private readonly ILogger<LibraryIdentityService> _logger;

    /// <summary>Initializes a new instance of the <see cref="LibraryIdentityService"/> class.</summary>
    public LibraryIdentityService(
        ILibraryManager libraryManager,
        MetadataCatalogService metadataCatalog,
        ILogger<LibraryIdentityService> logger)
    {
        _libraryManager = libraryManager;
        _metadataCatalog = metadataCatalog;
        _logger = logger;
    }

    /// <summary>Resolves renamed library ids, updates saved recipes, and records current stable locations.</summary>
    public PluginConfiguration Reconcile()
    {
        var plugin = Plugin.Instance ?? throw new InvalidOperationException("Collection Manager has not finished initializing.");
        var currentLibraries = _libraryManager.GetVirtualFolders(true)
            .Select(folder => new CurrentLibrary(
                Guid.TryParse(folder.ItemId, out var id) ? id : Guid.Empty,
                _libraryManager.GetItemById(Guid.TryParse(folder.ItemId, out var itemId) ? itemId : Guid.Empty)?.Name ?? folder.Name,
                NormalizeLocations(folder.Locations ?? [])))
            .Where(library => library.Id != Guid.Empty)
            .ToArray();
        var currentIds = currentLibraries.Select(library => library.Id).ToHashSet();
        var snapshot = plugin.Configuration;
        var referencedIds = snapshot.LibraryIds
            .Concat(snapshot.CollectionCreationRecipes.SelectMany(recipe => recipe.SelectedTags.Select(tag => tag.SourceLibraryId)
                .Concat(recipe.AdditionalLibraryIds)))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var mappings = new Dictionary<Guid, Guid>();
        foreach (var formerId in referencedIds.Where(id => !currentIds.Contains(id)))
        {
            var storedIdentity = (snapshot.LibraryIdentities ?? []).FirstOrDefault(identity => identity.LibraryId == formerId);
            var storedLocations = storedIdentity is null ? [] : NormalizeLocations(storedIdentity.Locations);
            var locationMatches = storedLocations.Count == 0
                ? []
                : currentLibraries.Where(library => library.Locations.Count > 0 && SameLocations(storedLocations, library.Locations)).ToArray();
            var replacement = locationMatches.Length == 1
                ? locationMatches[0].Id
                : _metadataCatalog.InferCurrentLibraryId(formerId, currentIds);
            if (replacement.HasValue)
            {
                mappings[formerId] = replacement.Value;
            }
        }

        Guid Map(Guid id) => mappings.TryGetValue(id, out var replacement) ? replacement : id;
        var mappedReferences = referencedIds.Select(Map).Where(currentIds.Contains).Distinct().ToHashSet();
        var identities = currentLibraries
            .Where(library => mappedReferences.Contains(library.Id))
            .OrderBy(library => library.Id)
            .Select(library => new ConfiguredLibraryIdentity
            {
                LibraryId = library.Id,
                Locations = library.Locations.ToList(),
            })
            .ToList();
        var identitiesChanged = !SameIdentities(snapshot.LibraryIdentities ?? [], identities);
        if (mappings.Count == 0 && !identitiesChanged)
        {
            return snapshot;
        }

        var updated = plugin.UpdateConfigurationSafely(configuration =>
        {
            configuration.LibraryIds = configuration.LibraryIds.Select(Map).Distinct().ToList();
            foreach (var recipe in configuration.CollectionCreationRecipes)
            {
                foreach (var tag in recipe.SelectedTags)
                {
                    tag.SourceLibraryId = Map(tag.SourceLibraryId);
                }

                recipe.AdditionalLibraryIds = recipe.AdditionalLibraryIds.Select(Map).Distinct().ToList();
            }

            configuration.LibraryIdentities = identities;
            return configuration;
        });
        if (mappings.Count > 0)
        {
            var names = currentLibraries.ToDictionary(library => library.Id, library => library.Name);
            _metadataCatalog.RemapLibraries(mappings, names);
            foreach (var mapping in mappings)
            {
                _logger.LogInformation(
                    "Updated saved Collection Manager library reference {FormerLibraryId} to renamed library {CurrentLibraryId}.",
                    mapping.Key,
                    mapping.Value);
            }
        }

        return updated;
    }

    private static IReadOnlyList<string> NormalizeLocations(IEnumerable<string> locations) =>
        locations.Where(location => !string.IsNullOrWhiteSpace(location))
            .Select(location => Path.TrimEndingDirectorySeparator(location.Trim()))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(location => location, StringComparer.Ordinal)
            .ToArray();

    private static bool SameLocations(IEnumerable<string> left, IEnumerable<string> right) =>
        NormalizeLocations(left).SequenceEqual(NormalizeLocations(right), StringComparer.Ordinal);

    private static bool SameIdentities(IEnumerable<ConfiguredLibraryIdentity> left, IEnumerable<ConfiguredLibraryIdentity> right)
    {
        var leftValues = left.OrderBy(identity => identity.LibraryId).ToArray();
        var rightValues = right.OrderBy(identity => identity.LibraryId).ToArray();
        return leftValues.Length == rightValues.Length
            && leftValues.Zip(rightValues).All(pair => pair.First.LibraryId == pair.Second.LibraryId
                && SameLocations(pair.First.Locations, pair.Second.Locations));
    }

    private sealed record CurrentLibrary(Guid Id, string Name, IReadOnlyList<string> Locations);
}
