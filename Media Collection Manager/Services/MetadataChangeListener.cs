using Jellyfin.Plugin.MediaCollectionManager.Configuration;
using Jellyfin.Plugin.MediaCollectionManager.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaCollectionManager.Services;

/// <summary>Debounces local item metadata changes and reconciles active rules.</summary>
public sealed class MetadataChangeListener : IHostedService, IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly ManualReconciliationRequestQueue _requests;
    private readonly ITaskManager _taskManager;
    private readonly ILogger<MetadataChangeListener> _logger;
    private readonly object _timerLock = new();
    private Timer? _timer;

    /// <summary>Initializes a new instance of the <see cref="MetadataChangeListener"/> class.</summary>
    public MetadataChangeListener(
        ILibraryManager libraryManager,
        ManualReconciliationRequestQueue requests,
        ITaskManager taskManager,
        ILogger<MetadataChangeListener> logger)
    {
        _libraryManager = libraryManager;
        _requests = requests;
        _taskManager = taskManager;
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
            _timer ??= new Timer(_ => QueueReconciliationAfterQuietPeriod(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _timer.Change(TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
        }
    }

    private void QueueReconciliationAfterQuietPeriod()
    {
        try
        {
            _requests.EnqueueAllEnabledRules();
            _taskManager.QueueScheduledTask<ReconcileCollectionsTask>();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Media Collection Manager could not reconcile after a metadata change.");
        }
    }
}
