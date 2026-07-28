using System.Collections.Concurrent;

namespace Jellyfin.Plugin.MediaCollectionManager.Tasks;

/// <summary>Shares short dashboard and metadata-event reconciliation requests with Jellyfin's native task.</summary>
public sealed class ManualReconciliationRequestQueue
{
    private readonly ConcurrentQueue<Guid> _ruleIds = new();
    private int _allRulesRequested;

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
}
