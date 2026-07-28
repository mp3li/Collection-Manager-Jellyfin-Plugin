using System.Globalization;
using Jellyfin.Plugin.MediaCollectionManager.Configuration;
using Jellyfin.Plugin.MediaCollectionManager.Models;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.MediaCollectionManager;

/// <summary>The Media Collection Manager plugin entry point.</summary>
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
    public override string Name => "Media Collection Manager";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("5e9d033a-41dd-4c93-b53f-aa94f8e2f7e9");

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            DisplayName = "Media Collection Manager",
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
}
