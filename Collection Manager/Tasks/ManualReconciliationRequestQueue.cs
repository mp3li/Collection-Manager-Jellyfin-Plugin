using System.Collections.Concurrent;

namespace Jellyfin.Plugin.CollectionManager.Tasks;

/// <summary>Shares short dashboard and metadata-event reconciliation requests with Jellyfin's native task.</summary>
public sealed class ManualReconciliationRequestQueue
{
    private readonly ConcurrentQueue<Guid> _ruleIds = new();
    private readonly ConcurrentDictionary<Guid, byte> _changedItemIds = new();
    private int _allRulesRequested;
    private int _targetedMetadataTaskQueuedOrRunning;

    /// <summary>Requests reconciliation of every enabled rule.</summary>
    public void EnqueueAllEnabledRules() => Interlocked.Exchange(ref _allRulesRequested, 1);

    /// <summary>Requests reconciliation of a single rule.</summary>
    public void EnqueueRule(Guid ruleId) => _ruleIds.Enqueue(ruleId);

    /// <summary>Returns whether an all-enabled-rules request is waiting.</summary>
    public bool TryTakeAllEnabledRulesRequest() => Interlocked.Exchange(ref _allRulesRequested, 0) == 1;

    /// <summary>Drains individual rule requests currently waiting.</summary>
    public IReadOnlyList<Guid> DrainRuleIds()
    {
        var ids = new HashSet<Guid>();
        while (_ruleIds.TryDequeue(out var ruleId))
        {
            ids.Add(ruleId);
        }

        return ids.ToArray();
    }

    /// <summary>Adds one changed Jellyfin item to the next targeted metadata reconciliation batch.</summary>
    public void EnqueueChangedItem(Guid itemId)
    {
        if (itemId != Guid.Empty)
        {
            _changedItemIds.TryAdd(itemId, 0);
        }
    }

    /// <summary>Starts one queued targeted metadata task when a changed-item batch is waiting.</summary>
    public bool TryQueueTargetedMetadataReconciliation() =>
        !_changedItemIds.IsEmpty && Interlocked.CompareExchange(ref _targetedMetadataTaskQueuedOrRunning, 1, 0) == 0;

    /// <summary>Drains the currently batched changed Jellyfin item ids.</summary>
    public IReadOnlyList<Guid> DrainChangedItemIds()
    {
        var ids = new List<Guid>();
        foreach (var itemId in _changedItemIds.Keys)
        {
            if (_changedItemIds.TryRemove(itemId, out _))
            {
                ids.Add(itemId);
            }
        }

        return ids;
    }

    /// <summary>Marks the active targeted metadata task complete.</summary>
    public void CompleteTargetedMetadataReconciliation() =>
        Interlocked.Exchange(ref _targetedMetadataTaskQueuedOrRunning, 0);
}
