using Jellyfin.Plugin.CollectionManager.Configuration;
using Jellyfin.Plugin.CollectionManager.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.Plugin.CollectionManager.Services;

/// <summary>Debounces local item metadata changes and reconciles active rules.</summary>
public sealed class MetadataChangeListener : IHostedService, IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly ManualReconciliationRequestQueue _requests;
    private readonly ReconciliationScheduler _scheduler;

    /// <summary>Initializes a new instance of the <see cref="MetadataChangeListener"/> class.</summary>
    public MetadataChangeListener(
        ILibraryManager libraryManager,
        ManualReconciliationRequestQueue requests,
        ReconciliationScheduler scheduler)
    {
        _libraryManager = libraryManager;
        _requests = requests;
        _scheduler = scheduler;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnItemChanged;
        _libraryManager.ItemUpdated += OnItemChanged;
        _libraryManager.ItemRemoved += OnItemChanged;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnItemChanged;
        _libraryManager.ItemUpdated -= OnItemChanged;
        _libraryManager.ItemRemoved -= OnItemChanged;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private void OnItemChanged(object? sender, ItemChangeEventArgs eventArgs)
    {
        var configuration = Plugin.Instance?.Configuration;
        if (eventArgs.Item is BoxSet
            || configuration?.WatchMetadataChanges != true
            || configuration.AutomaticallyAddNewMediaToApplicableCollections != true)
        {
            return;
        }

        _requests.EnqueueChangedItem(eventArgs.Item.Id);
        _scheduler.NotifyMetadataChange();
    }
}
