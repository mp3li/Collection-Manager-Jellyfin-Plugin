using Jellyfin.Plugin.CollectionManager.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionManager.Services;

/// <summary>Coalesces reconciliation requests behind a metadata quiet period and optional companion-plugin work.</summary>
public sealed class ReconciliationScheduler : IDisposable
{
    internal static readonly TimeSpan MetadataQuietPeriod = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan BusyRecheckInterval = TimeSpan.FromSeconds(15);
    private readonly ManualReconciliationRequestQueue _requests;
    private readonly ITaskManager _taskManager;
    private readonly MediaTaggingManagerActivityGate _mediaTaggingManager;
    private readonly ILogger<ReconciliationScheduler> _logger;
    private readonly object _sync = new();
    private Timer? _timer;
    private DateTime? _lastMetadataChangeUtc;
    private DateTime? _lastBusyObservationUtc;
    private bool _runRequested;
    private bool _forceTaskRun;
    private bool _waitingForBusyWorkToEnd;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="ReconciliationScheduler"/> class.</summary>
    public ReconciliationScheduler(
        ManualReconciliationRequestQueue requests,
        ITaskManager taskManager,
        MediaTaggingManagerActivityGate mediaTaggingManager,
        ILogger<ReconciliationScheduler> logger)
    {
        _requests = requests;
        _taskManager = taskManager;
        _mediaTaggingManager = mediaTaggingManager;
        _logger = logger;
    }

    /// <summary>Records a metadata event and restarts the trailing quiet period.</summary>
    public void NotifyMetadataChange()
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            _lastMetadataChangeUtc = now;
            _runRequested = true;
            if (_mediaTaggingManager.IsBusy)
            {
                _lastBusyObservationUtc = now;
                _waitingForBusyWorkToEnd = true;
                ArmTimerLocked(BusyRecheckInterval);
            }
            else
            {
                ArmTimerLocked(MetadataQuietPeriod);
            }
        }
    }

    /// <summary>Requests a task run, optionally preserving a native scheduled run that had to defer.</summary>
    public void RequestReconciliation(bool forceTaskRun = false)
    {
        lock (_sync)
        {
            _runRequested = true;
            _forceTaskRun |= forceTaskRun;
            ArmForCurrentStateLocked(DateTime.UtcNow);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
        }
    }

    private void ArmForCurrentStateLocked(DateTime now)
    {
        if (_mediaTaggingManager.IsBusy)
        {
            _lastBusyObservationUtc = now;
            _waitingForBusyWorkToEnd = true;
            ArmTimerLocked(BusyRecheckInterval);
            return;
        }

        if (_waitingForBusyWorkToEnd)
        {
            _lastBusyObservationUtc = now;
            _waitingForBusyWorkToEnd = false;
        }

        var quietSince = Later(_lastMetadataChangeUtc, _lastBusyObservationUtc);
        var remaining = quietSince.HasValue ? quietSince.Value + MetadataQuietPeriod - now : TimeSpan.Zero;
        ArmTimerLocked(remaining > TimeSpan.Zero ? remaining : TimeSpan.FromMilliseconds(1));
    }

    private void ArmTimerLocked(TimeSpan delay)
    {
        if (_disposed)
        {
            return;
        }

        _timer ??= new Timer(_ => TryQueueReconciliation(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _timer.Change(delay, Timeout.InfiniteTimeSpan);
    }

    private void TryQueueReconciliation()
    {
        var shouldQueue = false;
        try
        {
            lock (_sync)
            {
                if (_disposed || !_runRequested)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                if (_mediaTaggingManager.IsBusy)
                {
                    _lastBusyObservationUtc = now;
                    _waitingForBusyWorkToEnd = true;
                    ArmTimerLocked(BusyRecheckInterval);
                    return;
                }

                if (_waitingForBusyWorkToEnd)
                {
                    _lastBusyObservationUtc = now;
                    _waitingForBusyWorkToEnd = false;
                }

                var quietSince = Later(_lastMetadataChangeUtc, _lastBusyObservationUtc);
                if (quietSince.HasValue && now - quietSince.Value < MetadataQuietPeriod)
                {
                    ArmTimerLocked(MetadataQuietPeriod - (now - quietSince.Value));
                    return;
                }

                var targetedReserved = _requests.TryQueueTargetedMetadataReconciliation();
                shouldQueue = _forceTaskRun || _requests.HasExplicitRequests || targetedReserved;
                _runRequested = false;
                _forceTaskRun = false;
            }

            if (shouldQueue)
            {
                _taskManager.QueueScheduledTask<ReconcileCollectionsTask>();
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Collection Manager could not queue reconciliation after its quiet period.");
            lock (_sync)
            {
                _runRequested = true;
                ArmTimerLocked(BusyRecheckInterval);
            }
        }
    }

    private static DateTime? Later(DateTime? first, DateTime? second) =>
        !first.HasValue ? second : !second.HasValue ? first : first.Value >= second.Value ? first : second;
}
