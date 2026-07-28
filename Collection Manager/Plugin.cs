using System.Globalization;
using Jellyfin.Plugin.CollectionManager.Configuration;
using Jellyfin.Plugin.CollectionManager.Models;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.CollectionManager;

/// <summary>The Collection Manager plugin entry point.</summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly object _configurationLock = new();
    /// <summary>Initializes a new instance of the <see cref="Plugin"/> class.</summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>Gets the active plugin instance.</summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Collection Manager";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("5e9d033a-41dd-4c93-b53f-aa94f8e2f7e9");

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            DisplayName = "Collection Manager",
            EnableInMainMenu = true,
            EmbeddedResourcePath = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.Configuration.configPage.html",
                GetType().Namespace),
        };
    }

    /// <summary>Applies a narrow configuration change to a cloned server configuration.</summary>
    public T UpdateConfigurationSafely<T>(Func<PluginConfiguration, T> update)
    {
        lock (_configurationLock)
        {
            var updated = CloneConfiguration(Configuration);
            var result = update(updated);
            base.UpdateConfiguration(updated);
            return result;
        }
    }

    /// <summary>Updates only the values owned by the dashboard Main Settings tab.</summary>
    public PluginConfiguration UpdateMainSettings(MainSettingsRequest request) =>
        UpdateConfigurationSafely(configuration =>
        {
            configuration.UseAllLibraries = false;
            configuration.LibraryIds = request.LibraryIds.Distinct().ToList();
            configuration.AutomaticallyAddNewMediaToApplicableCollections = request.AutomaticallyAddNewMediaToApplicableCollections;
            return CloneConfiguration(configuration);
        });

    /// <summary>Saves only the selected library roots without changing other settings.</summary>
    public PluginConfiguration UpdateSelectedLibraries(IEnumerable<Guid> libraryIds) =>
        UpdateConfigurationSafely(configuration =>
        {
            configuration.UseAllLibraries = false;
            configuration.LibraryIds = libraryIds.Distinct().ToList();
            return CloneConfiguration(configuration);
        });

    /// <summary>Saves the administrator-selected overview metadata text color.</summary>
    public PluginConfiguration UpdateMetadataTagOverviewColor(string color) =>
        UpdateConfigurationSafely(configuration =>
        {
            configuration.MetadataTagOverviewColor = color;
            return CloneConfiguration(configuration);
        });

    /// <summary>Saves the collection-overview filters selected by the administrator.</summary>
    public PluginConfiguration UpdateCollectionOverviewSettings(bool showPluginMade, bool showNonPluginMade) =>
        UpdateConfigurationSafely(configuration =>
        {
            configuration.ShowPluginMadeCollections = showPluginMade;
            configuration.ShowNonPluginMadeCollections = showNonPluginMade;
            return CloneConfiguration(configuration);
        });

    /// <summary>Saves the collection-overview change colors selected by the administrator.</summary>
    public PluginConfiguration UpdateCollectionOverviewColors(string addedColor, string removedColor) =>
        UpdateConfigurationSafely(configuration =>
        {
            configuration.CollectionOverviewAddedColor = addedColor;
            configuration.CollectionOverviewRemovedColor = removedColor;
            return CloneConfiguration(configuration);
        });

    /// <summary>Records a collection created by Collection Manager.</summary>
    public void MarkCollectionManaged(Guid collectionId) => UpdateConfigurationSafely(configuration =>
    {
        if (!configuration.PluginManagedCollectionIds.Contains(collectionId))
        {
            configuration.PluginManagedCollectionIds.Add(collectionId);
        }

        return 0;
    });

    /// <summary>Records a reversible dashboard collection action.</summary>
    public void RecordCollectionAction(CollectionActionRecord record) => UpdateConfigurationSafely(configuration =>
    {
        configuration.CollectionActionHistory.Add(CloneAction(record));
        if (configuration.CollectionActionHistory.Count > 100)
        {
            configuration.CollectionActionHistory.RemoveRange(0, configuration.CollectionActionHistory.Count - 100);
        }

        return 0;
    });

    /// <summary>Removes a managed collection identifier after its collection is deleted.</summary>
    public void ForgetManagedCollection(Guid collectionId) => UpdateConfigurationSafely(configuration =>
    {
        configuration.PluginManagedCollectionIds.RemoveAll(id => id == collectionId);
        return 0;
    });

    /// <summary>Removes the most recent action after it has been undone.</summary>
    public void RemoveLastCollectionAction() => UpdateConfigurationSafely(configuration =>
    {
        if (configuration.CollectionActionHistory.Count > 0)
        {
            configuration.CollectionActionHistory.RemoveAt(configuration.CollectionActionHistory.Count - 1);
        }

        return 0;
    });

    /// <summary>Saves the latest collection overview snapshot.</summary>
    public void SaveCollectionOverviewSnapshot(CollectionOverviewSnapshot snapshot) => UpdateConfigurationSafely(configuration =>
    {
        configuration.CollectionOverviewSnapshot = CloneSnapshot(snapshot);
        return 0;
    });

    /// <summary>Gets an isolated snapshot of all persisted collection rules.</summary>
    public IReadOnlyList<CollectionRule> GetRulesSnapshot()
    {
        lock (_configurationLock)
        {
            return Configuration.Rules.Select(CloneRule).ToArray();
        }
    }

    /// <summary>Gets an isolated snapshot of one persisted collection rule.</summary>
    public CollectionRule? GetRuleSnapshot(Guid ruleId)
    {
        lock (_configurationLock)
        {
            var rule = Configuration.Rules.SingleOrDefault(candidate => candidate.Id == ruleId);
            return rule is null ? null : CloneRule(rule);
        }
    }

    private static PluginConfiguration CloneConfiguration(PluginConfiguration source) => new()
    {
        MetadataSourceMode = source.MetadataSourceMode,
        MetadataSourcePriority = source.MetadataSourcePriority,
        UseAllLibraries = source.UseAllLibraries,
        LibraryIds = source.LibraryIds.Distinct().ToList(),
        AutomaticallyAddNewMediaToApplicableCollections = source.AutomaticallyAddNewMediaToApplicableCollections,
        MetadataTagOverviewColor = source.MetadataTagOverviewColor,
        ShowPluginMadeCollections = source.ShowPluginMadeCollections,
        ShowNonPluginMadeCollections = source.ShowNonPluginMadeCollections,
        CollectionOverviewAddedColor = source.CollectionOverviewAddedColor,
        CollectionOverviewRemovedColor = source.CollectionOverviewRemovedColor,
        PluginManagedCollectionIds = source.PluginManagedCollectionIds.Distinct().ToList(),
        CollectionOverviewSnapshot = source.CollectionOverviewSnapshot is null ? null : CloneSnapshot(source.CollectionOverviewSnapshot),
        CollectionActionHistory = source.CollectionActionHistory.Select(CloneAction).ToList(),
        WatchMetadataChanges = source.WatchMetadataChanges,
        ScheduledReconciliationEnabled = source.ScheduledReconciliationEnabled,
        ScheduledReconciliationMinutes = source.ScheduledReconciliationMinutes,
        LastScheduledReconciliationUtc = source.LastScheduledReconciliationUtc,
        Rules = source.Rules.Select(CloneRule).ToList(),
    };

    private static CollectionRule CloneRule(CollectionRule source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Field = source.Field,
        MetadataFieldName = source.MetadataFieldName,
        Values = source.Values.ToList(),
        Enabled = source.Enabled,
        RemoveItemsNoLongerMatching = source.RemoveItemsNoLongerMatching,
        CollectionId = source.CollectionId,
        LastRunUtc = source.LastRunUtc,
    };

    private static CollectionActionRecord CloneAction(CollectionActionRecord source) => new()
    {
        Action = source.Action,
        CollectionId = source.CollectionId,
        CollectionName = source.CollectionName,
        PreviousCollectionName = source.PreviousCollectionName,
        ItemIds = source.ItemIds.Distinct().ToList(),
        OccurredUtc = source.OccurredUtc,
    };

    private static CollectionOverviewSnapshot CloneSnapshot(CollectionOverviewSnapshot source) => new()
    {
        CompletedUtc = source.CompletedUtc,
        Libraries = source.Libraries.Select(library => new CollectionOverviewLibrarySnapshot
        {
            LibraryId = library.LibraryId,
            LibraryName = library.LibraryName,
            Collections = library.Collections.Select(collection => new CollectionOverviewCollectionSnapshot
            {
                CollectionId = collection.CollectionId,
                Name = collection.Name,
                MadeByPlugin = collection.MadeByPlugin,
                Exists = collection.Exists,
                NewlyAdded = collection.NewlyAdded,
                NewlyRemoved = collection.NewlyRemoved,
                Items = collection.Items.Select(item => new CollectionOverviewItemSnapshot
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    NewlyAdded = item.NewlyAdded,
                    NewlyRemoved = item.NewlyRemoved,
                }).ToList(),
            }).ToList(),
        }).ToList(),
    };
}
