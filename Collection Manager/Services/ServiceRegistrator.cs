using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Jellyfin.Plugin.CollectionManager.Tasks;

namespace Jellyfin.Plugin.CollectionManager.Services;

/// <summary>Registers collection services and the metadata watcher with Jellyfin's DI container.</summary>
public sealed class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<CollectionReconciler>();
        serviceCollection.AddSingleton<MetadataCatalogService>();
        serviceCollection.AddSingleton<ManualReconciliationRequestQueue>();
        serviceCollection.AddSingleton<IScheduledTask, ReconcileCollectionsTask>();
        serviceCollection.AddHostedService<MetadataChangeListener>();
    }
}
