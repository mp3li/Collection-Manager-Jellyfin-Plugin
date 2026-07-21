using Jellyfin.Plugin.MediaCollectionManager.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaCollectionManager.Services;

/// <summary>Debounces local item metadata changes and reconciles active rules.</summary>
public sealed class MetadataChangeListener : IHostedService, IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly CollectionReconciler _reconciler;
    private readonly ILogger<MetadataChangeListener> _logger;
    private readonly object _timerLock = new();
    private Timer? _timer;

    /// <summary>Initializes a new instance of the <see cref="MetadataChangeListener"/> class.</summary>
    public MetadataChangeListener(
        ILibraryManager libraryManager,
        CollectionReconciler reconciler,
        ILogger<MetadataChangeListener> logger)
    {
        _libraryManager = libraryManager;
        _reconciler = reconciler;
        _logger = logger;
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
    public void Dispose() => _timer?.Dispose();

    private void OnItemChanged(object? sender, ItemChangeEventArgs eventArgs)
    {
        if (eventArgs.Item is BoxSet || Plugin.Instance?.Configuration.WatchMetadataChanges != true)
        {
            return;
        }

        lock (_timerLock)
        {
            _timer ??= new Timer(_ => _ = ReconcileAfterQuietPeriodAsync(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _timer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
        }
    }

    private async Task ReconcileAfterQuietPeriodAsync()
    {
        try
        {
            await _reconciler.ReconcileEnabledRulesAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Media Collection Manager could not reconcile after a metadata change.");
        }
    }
}
