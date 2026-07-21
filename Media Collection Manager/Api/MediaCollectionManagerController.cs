using Jellyfin.Plugin.MediaCollectionManager.Configuration;
using Jellyfin.Plugin.MediaCollectionManager.Models;
using Jellyfin.Plugin.MediaCollectionManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.MediaCollectionManager.Api;

/// <summary>Administrator API used by the Media Collection Manager dashboard page.</summary>
[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("MediaCollectionManager")]
public sealed class MediaCollectionManagerController : ControllerBase
{
    private readonly CollectionReconciler _reconciler;

    /// <summary>Initializes a new instance of the <see cref="MediaCollectionManagerController"/> class.</summary>
    public MediaCollectionManagerController(CollectionReconciler reconciler) => _reconciler = reconciler;

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
        Ok(Plugin.Instance?.Configuration.Rules ?? []);

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

        var configuration = RequireConfiguration();
        var rule = request.Id.HasValue
            ? configuration.Rules.SingleOrDefault(candidate => candidate.Id == request.Id.Value)
            : null;
        if (request.Id.HasValue && rule is null)
        {
            return NotFound();
        }

        rule ??= new CollectionRule();
        var renamedCollectionId = rule.CollectionId;
        var renamed = !string.Equals(rule.Name, request.Name.Trim(), StringComparison.Ordinal);
        rule.Name = request.Name.Trim();
        rule.Field = request.Field;
        rule.MetadataFieldName = string.IsNullOrWhiteSpace(request.MetadataFieldName) ? null : request.MetadataFieldName.Trim();
        rule.Values = values;
        rule.LibraryIds = request.LibraryIds.Distinct().ToList();
        rule.Enabled = request.Enabled;
        rule.RemoveItemsNoLongerMatching = request.RemoveItemsNoLongerMatching;
        if (!configuration.Rules.Contains(rule))
        {
            configuration.Rules.Add(rule);
        }

        Plugin.Instance?.SaveConfiguration(configuration);
        if (renamed && renamedCollectionId.HasValue)
        {
            await _reconciler.RenameCollectionAsync(renamedCollectionId.Value, rule.Name, cancellationToken).ConfigureAwait(false);
        }

        return Ok(rule);
    }

    /// <summary>Creates one rule per selected metadata value, ready for a bulk reconciliation.</summary>
    [HttpPost("rules/bulk")]
    public async Task<ActionResult<IReadOnlyList<CollectionRule>>> CreateRulesInBulk(
        [FromBody] BulkCreateRulesRequest request,
        CancellationToken cancellationToken)
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

        var configuration = RequireConfiguration();
        var created = new List<CollectionRule>(values.Length);
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
                LibraryIds = request.LibraryIds.Distinct().ToList(),
            };
            configuration.Rules.Add(rule);
            created.Add(rule);
        }

        Plugin.Instance?.SaveConfiguration(configuration);
        foreach (var rule in created)
        {
            await _reconciler.ReconcileRuleAsync(rule.Id, cancellationToken).ConfigureAwait(false);
        }

        return Ok(created);
    }

    /// <summary>Removes a rule without deleting its normal Jellyfin collection.</summary>
    [HttpDelete("rules/{ruleId:guid}")]
    public IActionResult DeleteRule(Guid ruleId)
    {
        var configuration = RequireConfiguration();
        var rule = configuration.Rules.SingleOrDefault(candidate => candidate.Id == ruleId);
        if (rule is null)
        {
            return NotFound();
        }

        configuration.Rules.Remove(rule);
        Plugin.Instance?.SaveConfiguration(configuration);
        return NoContent();
    }

    /// <summary>Runs one rule now, including its add and optional remove actions.</summary>
    [HttpPost("rules/{ruleId:guid}/reconcile")]
    public async Task<ActionResult<ReconciliationResult>> ReconcileRule(Guid ruleId, CancellationToken cancellationToken) =>
        Ok(await _reconciler.ReconcileRuleAsync(ruleId, cancellationToken).ConfigureAwait(false));

    /// <summary>Runs every enabled automatic collection rule now.</summary>
    [HttpPost("reconcile")]
    public async Task<ActionResult<IReadOnlyList<ReconciliationResult>>> ReconcileAll(CancellationToken cancellationToken) =>
        Ok(await _reconciler.ReconcileEnabledRulesAsync(cancellationToken).ConfigureAwait(false));

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

    private static PluginConfiguration RequireConfiguration() =>
        Plugin.Instance?.Configuration ?? throw new InvalidOperationException("Media Collection Manager is not initialized.");
}
