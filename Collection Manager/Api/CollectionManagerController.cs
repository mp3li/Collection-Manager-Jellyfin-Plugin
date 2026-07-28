using Jellyfin.Plugin.CollectionManager.Configuration;
using Jellyfin.Plugin.CollectionManager.Models;
using Jellyfin.Plugin.CollectionManager.Services;
using Jellyfin.Plugin.CollectionManager.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.CollectionManager.Api;

/// <summary>Administrator API used by the Collection Manager dashboard page.</summary>
[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("CollectionManager")]
public sealed class CollectionManagerController : ControllerBase
{
    private readonly CollectionReconciler _reconciler;
    private readonly MetadataCatalogService _metadataCatalog;
    private readonly ILibraryManager _libraryManager;
    private readonly ManualReconciliationRequestQueue _requests;
    private readonly ITaskManager _taskManager;

    /// <summary>Initializes a new instance of the <see cref="CollectionManagerController"/> class.</summary>
    public CollectionManagerController(
        CollectionReconciler reconciler,
        MetadataCatalogService metadataCatalog,
        ILibraryManager libraryManager,
        ManualReconciliationRequestQueue requests,
        ITaskManager taskManager)
    {
        _reconciler = reconciler;
        _metadataCatalog = metadataCatalog;
        _libraryManager = libraryManager;
        _requests = requests;
        _taskManager = taskManager;
    }

    /// <summary>Returns settings and selectable Jellyfin libraries for the dashboard's main settings tab.</summary>
    [HttpGet("settings/main")]
    public IActionResult GetSettings()
    {
        var plugin = RequirePlugin();
        return Ok(new
        {
            Configuration = plugin.Configuration,
            Libraries = _libraryManager.GetVirtualFolders(true).Select(folder => new { folder.ItemId, folder.Name }),
        });
    }

    /// <summary>Saves main settings through the plugin dashboard API.</summary>
    [HttpPost("settings/main")]
    public IActionResult UpdateSettings([FromBody] MainSettingsRequest request)
    {
        if (!HasOnlyKnownLibraries(request.LibraryIds))
        {
            return BadRequest("One or more selected libraries are no longer available on this server.");
        }

        var configuration = RequirePlugin().UpdateMainSettings(request);
        ReloadScheduledTaskTriggers();
        return Ok(new
        {
            Configuration = configuration,
            Libraries = _libraryManager.GetVirtualFolders(true).Select(folder => new { folder.ItemId, folder.Name }),
        });
    }

    /// <summary>Saves only the selected library roots from the Main Settings page.</summary>
    [HttpPost("settings/libraries")]
    public IActionResult UpdateLibraries([FromBody] LibrarySelectionRequest request)
    {
        if (!HasOnlyKnownLibraries(request.LibraryIds))
        {
            return BadRequest("One or more selected libraries are no longer available on this server.");
        }

        return Ok(RequirePlugin().UpdateSelectedLibraries(request.LibraryIds));
    }

    /// <summary>Saves the metadata overview's selected text color.</summary>
    [HttpPost("settings/metadata-overview-color")]
    public IActionResult UpdateMetadataOverviewColor([FromBody] MetadataOverviewColorRequest request)
    {
        var color = request.Color?.Trim() ?? string.Empty;
        if (!System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$"))
        {
            return BadRequest("Choose a valid six-digit hexadecimal color.");
        }

        return Ok(RequirePlugin().UpdateMetadataTagOverviewColor(color));
    }

    /// <summary>Starts a background scan that builds the local read-only metadata catalog.</summary>
    [HttpPost("metadata-catalog/scan")]
    public ActionResult<MetadataCatalogStatus> ScanMetadataCatalog()
    {
        if (RequirePlugin().Configuration.LibraryIds.Count == 0)
        {
            return BadRequest("Save one or more libraries before scanning metadata tags.");
        }

        return Accepted(_metadataCatalog.StartScan());
    }

    /// <summary>Returns the current local metadata-catalog scan progress.</summary>
    [HttpGet("metadata-catalog/status")]
    public ActionResult<MetadataCatalogStatus> GetMetadataCatalogStatus() => Ok(_metadataCatalog.GetStatus());

    /// <summary>Returns one bounded page from a saved library's metadata catalog.</summary>
    [HttpGet("metadata-catalog")]
    public ActionResult<MetadataCatalogPage> GetMetadataCatalogPage([FromQuery] Guid libraryId, [FromQuery] int page = 1) =>
        Ok(_metadataCatalog.GetPage(libraryId, page, 10));

    /// <summary>Gets metadata types for one saved library after the most recent catalog scan.</summary>
    [HttpGet("metadata-catalog/types")]
    public ActionResult<IReadOnlyList<MetadataCatalogType>> GetMetadataCatalogTypes([FromQuery] Guid libraryId) =>
        Ok(_metadataCatalog.GetTypes(libraryId));

    /// <summary>Gets a lazy, searchable page of values for one scanned metadata type.</summary>
    [HttpGet("metadata-catalog/values")]
    public ActionResult<MetadataCatalogValuePage> GetMetadataCatalogValues(
        [FromQuery] Guid libraryId,
        [FromQuery] string metadataType,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1) =>
        Ok(_metadataCatalog.GetValues(libraryId, metadataType, searchTerm, page));

    /// <summary>Previews the current catalog media a single individual collection draft would include.</summary>
    [HttpPost("individual-collection-drafts/preview")]
    public ActionResult<IndividualCollectionDraftPreview> PreviewIndividualCollectionDraft([FromBody] IndividualCollectionDraftRequest draft) =>
        Ok(_metadataCatalog.PreviewDraft(draft));

    /// <summary>Checks selected draft titles for existing native Jellyfin collection conflicts.</summary>
    [HttpPost("individual-collection-drafts/conflicts")]
    public ActionResult<IReadOnlyList<IndividualCollectionDraftConflict>> FindIndividualCollectionDraftConflicts([FromBody] List<IndividualCollectionDraftRequest> drafts) =>
        Ok(drafts.Where(IsCompletedDraft)
            .Select(draft => new IndividualCollectionDraftConflict(draft.CollectionTitle.Trim(), FindCollectionByName(draft.CollectionTitle) is not null))
            .ToArray());

    /// <summary>Creates one reviewed individual collection draft, or records its administrator-selected conflict outcome.</summary>
    [HttpPost("individual-collection-drafts/create")]
    public async Task<ActionResult<IndividualCollectionDraftResult>> CreateIndividualCollectionDraft([FromBody] IndividualCollectionDraftRequest draft)
    {
        if (!IsCompletedDraft(draft))
        {
            return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle?.Trim() ?? string.Empty, "Skipped", "The draft is incomplete or has no collection title."));
        }

        var existing = FindCollectionByName(draft.CollectionTitle);
        if (existing is not null)
        {
            if (string.Equals(draft.ExistingCollectionAction, "UseExisting", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Matched an existing collection", "The existing Jellyfin collection was left unchanged."));
            }

            if (string.Equals(draft.ExistingCollectionAction, "Skip", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Skipped", "The draft was skipped because a collection with this title already exists."));
            }

            return Conflict("A Jellyfin collection with this title already exists. Choose whether to use the existing collection or skip this draft.");
        }

        var itemIds = _metadataCatalog.GetMatchingItemIds(draft);
        if (itemIds.Count == 0)
        {
            return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Skipped", "No current catalog media matches this draft."));
        }

        await _reconciler.CreateCollectionAsync(draft.CollectionTitle, itemIds).ConfigureAwait(false);
        return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Created", $"Created with {itemIds.Count} matching media item(s)."));
    }

    /// <summary>Previews a combined or multi-match draft from multiple selected metadata tags.</summary>
    [HttpPost("tag-collection-drafts/preview")]
    public ActionResult<IndividualCollectionDraftPreview> PreviewTagCollectionDraft([FromBody] TagCollectionDraftRequest draft) => Ok(_metadataCatalog.PreviewTagCollection(draft));

    /// <summary>Checks one combined or multi-match collection title for a native Jellyfin conflict.</summary>
    [HttpPost("tag-collection-drafts/conflict")]
    public ActionResult<IndividualCollectionDraftConflict> FindTagCollectionDraftConflict([FromBody] TagCollectionDraftRequest draft) =>
        Ok(new IndividualCollectionDraftConflict(draft.CollectionTitle?.Trim() ?? string.Empty, FindCollectionByName(draft.CollectionTitle ?? string.Empty) is not null));

    /// <summary>Creates one reviewed combined or multi-match collection draft.</summary>
    [HttpPost("tag-collection-drafts/create")]
    public async Task<ActionResult<IndividualCollectionDraftResult>> CreateTagCollectionDraft([FromBody] TagCollectionDraftRequest draft)
    {
        if (draft.SelectedTags.Count < 2 || string.IsNullOrWhiteSpace(draft.CollectionTitle)) return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle?.Trim() ?? string.Empty, "Skipped", "At least two selected metadata tags and a collection title are required."));
        var existing = FindCollectionByName(draft.CollectionTitle);
        if (existing is not null)
        {
            if (string.Equals(draft.ExistingCollectionAction, "UseExisting", StringComparison.OrdinalIgnoreCase)) return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Matched an existing collection", "The existing Jellyfin collection was left unchanged."));
            if (string.Equals(draft.ExistingCollectionAction, "Skip", StringComparison.OrdinalIgnoreCase)) return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Skipped", "The draft was skipped because a collection with this title already exists."));
            return Conflict("A Jellyfin collection with this title already exists. Choose whether to use it or skip this draft.");
        }
        var itemIds = _metadataCatalog.GetMatchingItemIds(draft);
        if (itemIds.Count == 0) return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Skipped", draft.RequireAllTags ? "No media currently matches every selected metadata tag." : "No current catalog media matches the selected metadata tags."));
        await _reconciler.CreateCollectionAsync(draft.CollectionTitle, itemIds).ConfigureAwait(false);
        return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Created", $"Created with {itemIds.Count} unique media item(s)."));
    }

    /// <summary>Returns metadata values currently present in the server libraries.</summary>
    [HttpGet("facets")]
    public ActionResult<MetadataFacets> GetFacets() => Ok(_reconciler.GetFacets());

    /// <summary>Searches media within the dashboard's configured library scope.</summary>
    [HttpGet("items")]
    public ActionResult<IReadOnlyList<MediaSearchResult>> SearchItems([FromQuery] string? searchTerm) =>
        Ok(_reconciler.SearchMedia(searchTerm));

    /// <summary>Returns persisted automatic collection rules.</summary>
    [HttpGet("rules")]
    public ActionResult<IReadOnlyList<CollectionRule>> GetRules() =>
        Ok(RequirePlugin().GetRulesSnapshot());

    /// <summary>Creates or updates an automatic collection rule.</summary>
    [HttpPost("rules")]
    public async Task<ActionResult<CollectionRule>> SaveRule([FromBody] SaveRuleRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("A collection name is required.");
        }

        var values = request.Values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (values.Count == 0)
        {
            return BadRequest("Choose or enter at least one metadata value.");
        }

        if ((request.Field == CollectionRuleField.JellyfinField || request.Field == CollectionRuleField.NfoField) && string.IsNullOrWhiteSpace(request.MetadataFieldName))
        {
            return BadRequest("Choose the metadata field to use.");
        }

        var plugin = RequirePlugin();
        var existing = request.Id.HasValue ? plugin.GetRuleSnapshot(request.Id.Value) : null;
        if (request.Id.HasValue && existing is null)
        {
            return NotFound();
        }

        var renamedCollectionId = existing?.CollectionId;
        var renamed = existing is not null && !string.Equals(existing.Name, request.Name.Trim(), StringComparison.Ordinal);
        var rule = plugin.UpdateConfigurationSafely(configuration =>
        {
            var updated = request.Id.HasValue
                ? configuration.Rules.Single(candidate => candidate.Id == request.Id.Value)
                : new CollectionRule();
            updated.Name = request.Name.Trim();
            updated.Field = request.Field;
            updated.MetadataFieldName = string.IsNullOrWhiteSpace(request.MetadataFieldName) ? null : request.MetadataFieldName.Trim();
            updated.Values = values;
            // Library scope belongs to the Main Settings tab for every rule.
            updated.Enabled = request.Enabled;
            updated.RemoveItemsNoLongerMatching = request.RemoveItemsNoLongerMatching;
            if (!configuration.Rules.Contains(updated))
            {
                configuration.Rules.Add(updated);
            }

            return new CollectionRule
            {
                Id = updated.Id,
                Name = updated.Name,
                Field = updated.Field,
                MetadataFieldName = updated.MetadataFieldName,
                Values = updated.Values.ToList(),
                Enabled = updated.Enabled,
                RemoveItemsNoLongerMatching = updated.RemoveItemsNoLongerMatching,
                CollectionId = updated.CollectionId,
                LastRunUtc = updated.LastRunUtc,
            };
        });
        if (renamed && renamedCollectionId.HasValue)
        {
            await _reconciler.RenameCollectionAsync(renamedCollectionId.Value, rule.Name, cancellationToken).ConfigureAwait(false);
        }

        return Ok(rule);
    }

    /// <summary>Creates one rule per selected metadata value, ready for a bulk reconciliation.</summary>
    [HttpPost("rules/bulk")]
    public ActionResult<IReadOnlyList<CollectionRule>> CreateRulesInBulk([FromBody] BulkCreateRulesRequest request)
    {
        var values = request.Values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (values.Length == 0)
        {
            return BadRequest("Choose at least one metadata value.");
        }

        if ((request.Field == CollectionRuleField.JellyfinField || request.Field == CollectionRuleField.NfoField) && string.IsNullOrWhiteSpace(request.MetadataFieldName))
        {
            return BadRequest("Choose the metadata field to use.");
        }

        var created = RequirePlugin().UpdateConfigurationSafely(configuration =>
        {
            var createdRules = new List<CollectionRule>(values.Length);
            foreach (var value in values)
            {
                var name = string.Concat(request.NamePrefix?.Trim(), request.NamePrefix?.Length > 0 ? " " : string.Empty, value);
                var duplicate = configuration.Rules.Any(rule =>
                    rule.Field == request.Field &&
                    rule.Values.Count == 1 &&
                    string.Equals(rule.Values[0], value, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(rule.Name, name, StringComparison.OrdinalIgnoreCase));
                if (duplicate)
                {
                    continue;
                }

                var rule = new CollectionRule
                {
                    Name = name,
                    Field = request.Field,
                    MetadataFieldName = string.IsNullOrWhiteSpace(request.MetadataFieldName) ? null : request.MetadataFieldName.Trim(),
                    Values = [value],
                };
                configuration.Rules.Add(rule);
                createdRules.Add(rule);
            }

            return createdRules;
        });
        foreach (var rule in created)
        {
            _requests.EnqueueRule(rule.Id);
        }

        if (created.Count > 0)
        {
            _taskManager.QueueScheduledTask<ReconcileCollectionsTask>();
        }

        return Ok(created);
    }

    /// <summary>Removes a rule without deleting its normal Jellyfin collection.</summary>
    [HttpDelete("rules/{ruleId:guid}")]
    public IActionResult DeleteRule(Guid ruleId)
    {
        if (RequirePlugin().GetRuleSnapshot(ruleId) is null)
        {
            return NotFound();
        }

        RequirePlugin().UpdateConfigurationSafely(configuration =>
        {
            configuration.Rules.RemoveAll(candidate => candidate.Id == ruleId);
            return 0;
        });
        return NoContent();
    }

    /// <summary>Runs one rule now, including its add and optional remove actions.</summary>
    [HttpPost("rules/{ruleId:guid}/reconcile")]
    public IActionResult ReconcileRule(Guid ruleId)
    {
        if (RequirePlugin().GetRuleSnapshot(ruleId) is null)
        {
            return NotFound();
        }

        _requests.EnqueueRule(ruleId);
        _taskManager.QueueScheduledTask<ReconcileCollectionsTask>();
        return Accepted();
    }

    /// <summary>Runs every enabled automatic collection rule now.</summary>
    [HttpPost("reconcile")]
    public IActionResult ReconcileAll()
    {
        _requests.EnqueueAllEnabledRules();
        _taskManager.QueueScheduledTask<ReconcileCollectionsTask>();
        return Accepted();
    }

    /// <summary>Creates a standard Jellyfin collection with the selected items in one action.</summary>
    [HttpPost("collections")]
    public async Task<ActionResult<object>> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        var collection = await _reconciler.CreateCollectionAsync(request.Name, request.ItemIds).ConfigureAwait(false);
        return Ok(new { collection.Id, collection.Name });
    }

    /// <summary>Adds selected media items to a standard Jellyfin collection.</summary>
    [HttpPost("collections/add")]
    public async Task<IActionResult> AddToCollection([FromBody] CollectionMembershipRequest request)
    {
        await _reconciler.AddToCollectionAsync(request.CollectionId, request.ItemIds).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Removes selected media items from a standard Jellyfin collection.</summary>
    [HttpPost("collections/remove")]
    public async Task<IActionResult> RemoveFromCollection([FromBody] CollectionMembershipRequest request)
    {
        await _reconciler.RemoveFromCollectionAsync(request.CollectionId, request.ItemIds).ConfigureAwait(false);
        return NoContent();
    }

    private static Plugin RequirePlugin() =>
        Plugin.Instance ?? throw new InvalidOperationException("Collection Manager has not finished initializing.");

    private void ReloadScheduledTaskTriggers()
    {
        var worker = _taskManager.ScheduledTasks
            .FirstOrDefault(candidate => candidate.ScheduledTask is ReconcileCollectionsTask);
        if (worker is null)
        {
            return;
        }

        worker.Triggers = [.. ((ReconcileCollectionsTask)worker.ScheduledTask).GetDefaultTriggers()];
        worker.ReloadTriggerEvents();
    }

    private bool HasOnlyKnownLibraries(IEnumerable<Guid> libraryIds)
    {
        var known = _libraryManager.GetVirtualFolders(true)
            .Select(folder => Guid.TryParse(folder.ItemId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
        return libraryIds.Distinct().All(id => known.Contains(id));
    }

    private bool IsCompletedDraft(IndividualCollectionDraftRequest draft) =>
        draft.SourceLibraryId != Guid.Empty &&
        !string.IsNullOrWhiteSpace(draft.MetadataType) &&
        !string.IsNullOrWhiteSpace(draft.MetadataValue) &&
        !string.IsNullOrWhiteSpace(draft.CollectionTitle);

    private BoxSet? FindCollectionByName(string title) =>
        _libraryManager.GetItemList(new InternalItemsQuery { Recursive = true })
            .OfType<BoxSet>()
            .FirstOrDefault(collection => string.Equals(collection.Name, title.Trim(), StringComparison.OrdinalIgnoreCase));
}
