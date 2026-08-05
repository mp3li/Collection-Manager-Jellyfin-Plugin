using System.Collections.Concurrent;
using Jellyfin.Plugin.CollectionManager.Models;

namespace Jellyfin.Plugin.CollectionManager.Tasks;

/// <summary>Shares short dashboard and metadata-event reconciliation requests with Jellyfin's native task.</summary>
public sealed class ManualReconciliationRequestQueue
{
    private readonly ConcurrentQueue<Guid> _ruleIds = new();
    private readonly ConcurrentQueue<Guid> _savedRecipeRecreationIds = new();
    private readonly ConcurrentDictionary<Guid, byte> _changedItemIds = new();
    private readonly ConcurrentDictionary<Guid, byte> _savedRecipeRecreationsQueuedOrRunning = new();
    private readonly ConcurrentDictionary<Guid, CollectionRecreationStatus> _savedRecipeRecreationStatuses = new();
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

    /// <summary>Queues one saved creation recipe to rebuild its collection membership through Jellyfin's native task runner.</summary>
    public CollectionRecreationStatus EnqueueSavedRecipeRecreation(Guid collectionId, string collectionTitle)
    {
        if (_savedRecipeRecreationsQueuedOrRunning.TryAdd(collectionId, 0))
        {
            var queued = new CollectionRecreationStatus(
                collectionId,
                "Queued",
                $"Recreation of {collectionTitle} is queued.",
                DateTime.UtcNow);
            _savedRecipeRecreationStatuses[collectionId] = queued;
            _savedRecipeRecreationIds.Enqueue(collectionId);
            return queued;
        }

        return GetSavedRecipeRecreationStatus(collectionId);
    }

    /// <summary>Returns the next queued saved-recipe recreation and marks it as running.</summary>
    public bool TryTakeSavedRecipeRecreation(out Guid collectionId)
    {
        if (_savedRecipeRecreationIds.TryDequeue(out collectionId))
        {
            var previous = GetSavedRecipeRecreationStatus(collectionId);
            _savedRecipeRecreationStatuses[collectionId] = previous with
            {
                State = "Running",
                Message = "Recreating this collection from its saved settings.",
                UpdatedUtc = DateTime.UtcNow,
            };
            return true;
        }

        collectionId = Guid.Empty;
        return false;
    }

    /// <summary>Records a completed saved-recipe recreation.</summary>
    public void CompleteSavedRecipeRecreation(SavedRecipeReconciliationResult result) =>
        CompleteSavedRecipeRecreation(result.CollectionId, "Completed", $"Recreated {result.CollectionName}.", result);

    /// <summary>Records a failed saved-recipe recreation.</summary>
    public void FailSavedRecipeRecreation(Guid collectionId, string message) =>
        CompleteSavedRecipeRecreation(collectionId, "Failed", message, null);

    /// <summary>Gets the last known state for one saved-recipe recreation.</summary>
    public CollectionRecreationStatus GetSavedRecipeRecreationStatus(Guid collectionId) =>
        _savedRecipeRecreationStatuses.TryGetValue(collectionId, out var status)
            ? status
            : new CollectionRecreationStatus(collectionId, "Idle", "No recreation is queued or running for this collection.", DateTime.UtcNow);

    private void CompleteSavedRecipeRecreation(
        Guid collectionId,
        string state,
        string message,
        SavedRecipeReconciliationResult? reconciliation)
    {
        _savedRecipeRecreationsQueuedOrRunning.TryRemove(collectionId, out _);
        _savedRecipeRecreationStatuses[collectionId] = new CollectionRecreationStatus(
            collectionId,
            state,
            message,
            DateTime.UtcNow,
            reconciliation);
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
