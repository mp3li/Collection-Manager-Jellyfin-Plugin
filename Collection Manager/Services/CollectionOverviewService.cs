using System.Text.Json;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.CollectionManager.Configuration;
using Jellyfin.Plugin.CollectionManager.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionManager.Services;

/// <summary>Scans and persists a read-only selected-library view of native Jellyfin collections.</summary>
public sealed class CollectionOverviewService
{
    private const string SnapshotFileName = "collection-overview.json";
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<CollectionOverviewService> _logger;
    private readonly string _snapshotPath;
    private readonly object _sync = new();
    private CollectionOverviewSnapshot? _snapshot;
    private CollectionOverviewScanStatus _status = new(false, 0, 0, null, "No collection overview scan has been completed yet.");

    /// <summary>Initializes the persisted collection overview state.</summary>
    public CollectionOverviewService(ILibraryManager libraryManager, ILogger<CollectionOverviewService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _snapshotPath = Plugin.Instance is { } plugin ? Path.Combine(plugin.DataFolderPath, SnapshotFileName) : string.Empty;
        Load();
    }

    /// <summary>Returns the current collection overview scan status.</summary>
    public CollectionOverviewScanStatus GetStatus()
    {
        lock (_sync) return _status;
    }

    /// <summary>Returns the last successfully saved collection overview.</summary>
    public CollectionOverviewSnapshot? GetSnapshot()
    {
        lock (_sync) return _snapshot;
    }

    /// <summary>Scans all native collections once and persists the selected-library overview.</summary>
    public CollectionOverviewScanStatus Scan(PluginConfiguration configuration)
    {
        var phase = "preparing the collection scan";
        try
        {
            phase = "reading native Jellyfin collections";
            var allCollections = _libraryManager.GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.BoxSet] }).OfType<BoxSet>().ToArray();
            phase = "reading the previous saved collection overview";
            var prior = GetSnapshot();
            // The first scan establishes the baseline. It must not paint every
            // collection and linked item as newly added.
            var hasPriorSnapshot = prior is not null;
            var priorCollections = prior?.Libraries.SelectMany(library => library.Collections).GroupBy(collection => collection.CollectionId).ToDictionary(group => group.Key, group => group.First()) ?? [];
            var managed = configuration.PluginManagedCollectionIds.Concat(configuration.Rules.Where(rule => rule.CollectionId.HasValue).Select(rule => rule.CollectionId!.Value)).ToHashSet();
            var nextCollections = new List<CollectionOverviewCollectionSnapshot>();

            foreach (var collection in allCollections)
            {
                BaseItem[] children;
                try { children = collection.GetLinkedChildren().ToArray(); }
                catch (Exception exception) { _logger.LogWarning(exception, "Skipping unreadable native collection {CollectionId} during collection overview scan.", collection.Id); continue; }
                var oldCollection = priorCollections.TryGetValue(collection.Id, out var priorCollection) ? priorCollection : null;
                var oldIds = oldCollection?.Items.Select(item => item.ItemId).ToHashSet() ?? [];
                nextCollections.Add(new CollectionOverviewCollectionSnapshot
                {
                    CollectionId = collection.Id,
                    Name = collection.Name,
                    MadeByPlugin = managed.Contains(collection.Id),
                    Exists = true,
                    NewlyAdded = hasPriorSnapshot && oldCollection is null,
                    Items = children.Select(item => new CollectionOverviewItemSnapshot
                    {
                        ItemId = item.Id,
                        Name = item.Name,
                        Type = item.GetType().Name,
                        ProductionYear = item.ProductionYear,
                        Overview = item.Overview,
                        NewlyAdded = hasPriorSnapshot && !oldIds.Contains(item.Id),
                    }).Concat((oldCollection?.Items ?? []).Where(item => !children.Any(current => current.Id == item.ItemId)).Select(item => new CollectionOverviewItemSnapshot
                    {
                        ItemId = item.ItemId,
                        Name = item.Name,
                        Type = item.Type,
                        ProductionYear = item.ProductionYear,
                        Overview = item.Overview,
                        NewlyRemoved = true,
                    })).ToList(),
                });
            }

            foreach (var removed in priorCollections.Values.Where(old => !nextCollections.Any(current => current.CollectionId == old.CollectionId)))
            {
                nextCollections.Add(new CollectionOverviewCollectionSnapshot { CollectionId = removed.CollectionId, Name = removed.Name, MadeByPlugin = removed.MadeByPlugin, Exists = false, NewlyRemoved = true, Items = removed.Items.Select(item => new CollectionOverviewItemSnapshot { ItemId = item.ItemId, Name = item.Name, Type = item.Type, ProductionYear = item.ProductionYear, Overview = item.Overview, NewlyRemoved = true }).ToList() });
            }

            phase = "saving the collection overview";
            var snapshot = new CollectionOverviewSnapshot { CompletedUtc = DateTime.UtcNow, Libraries = [new CollectionOverviewLibrarySnapshot { LibraryId = Guid.Empty, LibraryName = "All Collections", Collections = nextCollections.OrderBy(collection => collection.Name, StringComparer.OrdinalIgnoreCase).ToList() }] };
            Persist(snapshot);
            var count = snapshot.Libraries.SelectMany(library => library.Collections).Where(collection => collection.Exists).Select(collection => collection.CollectionId).Distinct().Count();
            lock (_sync)
            {
                _snapshot = snapshot;
                _status = new CollectionOverviewScanStatus(false, count, count, snapshot.CompletedUtc, $"Collection scan complete. Found {count} collection(s).");
                return _status;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Collection Manager could not scan native collections for the collection overview.");
            lock (_sync)
            {
                _status = new CollectionOverviewScanStatus(false, 0, 0, _snapshot?.CompletedUtc, $"Collection scan could not be completed while {phase} ({exception.GetType().Name}).");
                return _status;
            }
        }
    }

    private void Load()
    {
        if (string.IsNullOrWhiteSpace(_snapshotPath) || !File.Exists(_snapshotPath)) return;
        try
        {
            var snapshot = JsonSerializer.Deserialize<CollectionOverviewSnapshot>(File.ReadAllText(_snapshotPath));
            if (snapshot is null || snapshot.CompletedUtc == default) return;
            // Older private-testing builds marked every entry in their initial
            // snapshot as new. Treat that unmistakable all-new state as the
            // baseline so it is not shown as a false change set after upgrade.
            var existing = snapshot.Libraries.SelectMany(library => library.Collections).Where(collection => collection.Exists).ToArray();
            if (existing.Length > 0 && existing.All(collection => collection.NewlyAdded))
            {
                foreach (var collection in existing)
                {
                    collection.NewlyAdded = false;
                    foreach (var item in collection.Items.Where(item => !item.NewlyRemoved)) item.NewlyAdded = false;
                }

                Persist(snapshot);
            }
            var count = snapshot.Libraries.SelectMany(library => library.Collections).Where(collection => collection.Exists).Select(collection => collection.CollectionId).Distinct().Count();
            _snapshot = snapshot;
            _status = new CollectionOverviewScanStatus(false, count, count, snapshot.CompletedUtc, $"Showing the last available collection scan. Found {count} collection(s).");
        }
        catch (Exception exception) { _logger.LogWarning(exception, "Collection Manager could not load its saved collection overview."); }
    }

    private void Persist(CollectionOverviewSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(_snapshotPath)) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_snapshotPath)!);
            var temporary = _snapshotPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot));
            File.Move(temporary, _snapshotPath, true);
        }
        catch (Exception exception) { _logger.LogWarning(exception, "Collection Manager could not save its collection overview."); }
    }
}
