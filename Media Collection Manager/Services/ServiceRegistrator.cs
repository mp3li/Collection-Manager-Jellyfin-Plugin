using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;

namespace Jellyfin.Plugin.MediaCollectionManager.Services;

/// <summary>Registers collection services and the metadata watcher with Jellyfin's DI container.</summary>
public sealed class ServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<CollectionReconciler>();
        serviceCollection.AddHostedService<MetadataChangeListener>();
    }
}
