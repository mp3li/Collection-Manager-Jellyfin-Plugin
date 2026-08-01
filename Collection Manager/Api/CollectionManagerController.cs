using Jellyfin.Plugin.CollectionManager.Configuration;
using Jellyfin.Plugin.CollectionManager.Models;
using Jellyfin.Plugin.CollectionManager.Services;
using Jellyfin.Plugin.CollectionManager.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    private readonly CollectionOverviewService _collectionOverview;
    private readonly ILibraryManager _libraryManager;
    private readonly ManualReconciliationRequestQueue _requests;
    private readonly ITaskManager _taskManager;
    private readonly IProviderManager _providerManager;
    private readonly CollectionArtAssetStore _artAssets;
    private readonly CollectionBackupManager _collectionBackups;

    /// <summary>Initializes a new instance of the <see cref="CollectionManagerController"/> class.</summary>
    public CollectionManagerController(
        CollectionReconciler reconciler,
        MetadataCatalogService metadataCatalog,
        CollectionOverviewService collectionOverview,
        ILibraryManager libraryManager,
        ManualReconciliationRequestQueue requests,
        ITaskManager taskManager,
        IProviderManager providerManager,
        CollectionArtAssetStore artAssets,
        CollectionBackupManager collectionBackups)
    {
        _reconciler = reconciler;
        _metadataCatalog = metadataCatalog;
        _collectionOverview = collectionOverview;
        _libraryManager = libraryManager;
        _requests = requests;
        _taskManager = taskManager;
        _providerManager = providerManager;
        _artAssets = artAssets;
        _collectionBackups = collectionBackups;
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

    /// <summary>Returns every optional complete collection backup available to this plugin.</summary>
    [HttpGet("collection-backups")]
    public async Task<ActionResult<IReadOnlyList<CollectionBackupSummary>>> GetCollectionBackups(CancellationToken cancellationToken) =>
        Ok(await _collectionBackups.GetAllAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Creates an optional complete native Jellyfin collection backup.</summary>
    [HttpPost("collection-backups")]
    public async Task<ActionResult<CollectionBackupSummary>> CreateCollectionBackup([FromBody] CollectionBackupCreateRequest request, CancellationToken cancellationToken) =>
        Ok(await _collectionBackups.CreateAsync(request.Name, cancellationToken).ConfigureAwait(false));

    /// <summary>Renames one stored backup without changing any native Jellyfin collection.</summary>
    [HttpPost("collection-backups/{backupId:guid}/rename")]
    public async Task<IActionResult> RenameCollectionBackup(Guid backupId, [FromBody] CollectionBackupRenameRequest request, CancellationToken cancellationToken)
    {
        await _collectionBackups.RenameAsync(backupId, request.Name, cancellationToken).ConfigureAwait(false);
        return Ok(new { });
    }

    /// <summary>Deletes one stored backup without changing any native Jellyfin collection.</summary>
    [HttpDelete("collection-backups/{backupId:guid}")]
    public async Task<IActionResult> DeleteCollectionBackup(Guid backupId, CancellationToken cancellationToken)
    {
        await _collectionBackups.DeleteAsync(backupId, cancellationToken).ConfigureAwait(false);
        return Ok(new { });
    }

    /// <summary>Restores a complete collection backup and optionally removes collections absent from it.</summary>
    [HttpPost("collection-backups/{backupId:guid}/restore")]
    public async Task<ActionResult<CollectionBackupRestoreResult>> RestoreCollectionBackup(Guid backupId, [FromBody] CollectionBackupRestoreRequest request, CancellationToken cancellationToken) =>
        Ok(await _collectionBackups.RestoreAsync(backupId, request.DeleteCollectionsMissingFromBackup, cancellationToken).ConfigureAwait(false));

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

    /// <summary>Returns the local last-completed scan timestamp and its captured libraries.</summary>
    [HttpGet("metadata-catalog/last-available-scan")]
    public ActionResult<MetadataCatalogAvailability> GetLastAvailableMetadataCatalog() => Ok(_metadataCatalog.GetLastAvailableCatalog());

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
    public async Task<ActionResult<IndividualCollectionDraftResult>> CreateIndividualCollectionDraft([FromBody] IndividualCollectionDraftRequest draft, CancellationToken cancellationToken)
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

        var collection = await _reconciler.CreateCollectionAsync(draft.CollectionTitle, itemIds).ConfigureAwait(false);
        await UpdateCollectionOverviewAsync(collection, draft.Overview, cancellationToken).ConfigureAwait(false);
        return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Created", $"Created with {itemIds.Count} matching media item(s).", collection.Id));
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
    public async Task<ActionResult<IndividualCollectionDraftResult>> CreateTagCollectionDraft([FromBody] TagCollectionDraftRequest draft, CancellationToken cancellationToken)
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
        var collection = await _reconciler.CreateCollectionAsync(draft.CollectionTitle, itemIds).ConfigureAwait(false);
        await UpdateCollectionOverviewAsync(collection, draft.Overview, cancellationToken).ConfigureAwait(false);
        return Ok(new IndividualCollectionDraftResult(draft.CollectionTitle.Trim(), "Created", $"Created with {itemIds.Count} unique media item(s).", collection.Id));
    }

    /// <summary>Returns metadata values currently present in the server libraries.</summary>
    [HttpGet("facets")]
    public ActionResult<MetadataFacets> GetFacets() => Ok(_reconciler.GetFacets());

    /// <summary>Searches media within the dashboard's configured library scope.</summary>
    [HttpGet("items")]
    public ActionResult<IReadOnlyList<MediaSearchResult>> SearchItems([FromQuery] string? searchTerm) =>
        Ok(_reconciler.SearchMedia(searchTerm));

    /// <summary>Returns all persisted collection-art settings for the five Collection Art tabs.</summary>
    [HttpGet("art/settings")]
    public IActionResult GetArtSettings()
    {
        var configuration = RequirePlugin().Configuration;
        return Ok(new
        {
            configuration.DefaultArtPreference,
            configuration.TextFocusedArt,
            configuration.PosterFocusedArt,
            configuration.LogoFocusedArt,
            configuration.MultiCollectionGradientArt,
            configuration.CollectionLogoSelections,
        });
    }

    /// <summary>Saves the default artwork choice for new collection drafts.</summary>
    [HttpPost("art/settings/default")]
    public IActionResult SaveDefaultArtPreference([FromBody] DefaultArtPreferenceRequest request) =>
        Ok(RequirePlugin().UpdateConfigurationSafely(configuration =>
        {
            configuration.DefaultArtPreference = request.Preference;
            return configuration.DefaultArtPreference;
        }));

    /// <summary>Saves text-focused collection-art settings.</summary>
    [HttpPost("art/settings/text-focused")]
    public IActionResult SaveTextFocusedArtSettings([FromBody] TextFocusedArtSettingsRequest request) =>
        Ok(RequirePlugin().UpdateConfigurationSafely(configuration =>
        {
            configuration.TextFocusedArt = CreateTextFocusedSettings(request);
            return configuration.TextFocusedArt;
        }));

    /// <summary>Saves poster-focused collection-art settings.</summary>
    [HttpPost("art/settings/poster-focused")]
    public IActionResult SavePosterFocusedArtSettings([FromBody] TextFocusedArtSettingsRequest request) =>
        Ok(RequirePlugin().UpdateConfigurationSafely(configuration =>
        {
            configuration.PosterFocusedArt = CreatePosterFocusedSettings(request);
            return configuration.PosterFocusedArt;
        }));

    /// <summary>Saves logo-focused collection-art settings.</summary>
    [HttpPost("art/settings/logo-focused")]
    public IActionResult SaveLogoFocusedArtSettings([FromBody] LogoFocusedArtSettingsRequest request) =>
        Ok(RequirePlugin().UpdateConfigurationSafely(configuration =>
        {
            configuration.LogoFocusedArt = new LogoFocusedCollectionArtSettings
            {
                BackgroundStyle = NormalizeBackgroundStyle(request.BackgroundStyle),
                BackgroundColor = request.BackgroundColor,
                GradientColorOne = request.GradientColorOne,
                GradientColorTwo = request.GradientColorTwo,
                GradientDirection = request.GradientDirection,
                BackgroundAssetId = request.BackgroundAssetId,
                BackgroundFileName = request.BackgroundFileName,
                ArtType = request.ArtType,
            };
            return configuration.LogoFocusedArt;
        }));

    /// <summary>Saves multi-collection or library gradient-art settings.</summary>
    [HttpPost("art/settings/multi-gradient")]
    public IActionResult SaveMultiCollectionGradientArtSettings([FromBody] MultiCollectionGradientArtSettingsRequest request) =>
        Ok(RequirePlugin().UpdateConfigurationSafely(configuration =>
        {
            configuration.MultiCollectionGradientArt = new MultiCollectionGradientArtSettings
            {
                GradientColorOne = request.GradientColorOne,
                GradientColorTwo = request.GradientColorTwo,
                GradientColorThree = request.GradientColorThree,
                GradientDirection = request.GradientDirection,
                ArtType = request.ArtType,
            };
            return configuration.MultiCollectionGradientArt;
        }));

    /// <summary>Saves the one selected logo for a collection in logo-focused art.</summary>
    [HttpPost("art/logo-selections")]
    public IActionResult SaveCollectionLogoSelection([FromBody] CollectionLogoSelectionRequest request)
    {
        if (_libraryManager.GetItemById<BoxSet>(request.CollectionId) is null)
        {
            return NotFound("The selected collection is no longer available on this server.");
        }

        var selection = RequirePlugin().UpdateConfigurationSafely(configuration =>
        {
            configuration.CollectionLogoSelections.RemoveAll(selection => selection.CollectionId == request.CollectionId);
            if (!string.IsNullOrWhiteSpace(request.LogoKind)
                || !string.IsNullOrWhiteSpace(request.LogoName)
                || !string.IsNullOrWhiteSpace(request.ImportedLogoAssetId))
            {
                configuration.CollectionLogoSelections.Add(new CollectionLogoSelection
                {
                    CollectionId = request.CollectionId,
                    LogoKind = request.LogoKind?.Trim(),
                    LogoName = request.LogoName?.Trim(),
                    ImportedLogoAssetId = request.ImportedLogoAssetId?.Trim(),
                    ImportedLogoFileName = request.ImportedLogoFileName?.Trim(),
                    RoundCorners = request.RoundCorners,
                });
            }

            return configuration.CollectionLogoSelections.Where(selection => selection.CollectionId == request.CollectionId).FirstOrDefault();
        });

        return selection is null ? Ok(new { Cleared = true }) : Ok(selection);
    }

    /// <summary>Saves an imported TTF/OTF font or image in this plugin's Jellyfin data folder.</summary>
    [HttpPost("art-assets/{kind}")]
    public async Task<ActionResult<CollectionArtAsset>> UploadCollectionArtAsset(string kind, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryParseAssetKind(kind, out var assetKind))
        {
            return BadRequest("The collection-art asset kind must be font or image.");
        }

        try
        {
            return Ok(await _artAssets.SaveAsync(file, assetKind, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception)
        {
            return BadRequest($"Could not save the {assetKind.ToString().ToLowerInvariant()} file: {exception.GetType().Name}: {exception.Message}");
        }
    }

    /// <summary>Serves an imported collection-art asset to the dashboard preview.</summary>
    [HttpGet("art-assets/{kind}/{id}")]
    public IActionResult GetCollectionArtAsset(string kind, string id)
    {
        if (!TryParseAssetKind(kind, out var assetKind))
        {
            return NotFound();
        }

        var asset = _artAssets.Open(id, assetKind);
        return asset is null ? NotFound() : File(asset.Stream, asset.ContentType);
    }

    /// <summary>Returns every native Jellyfin collection for the collection-art pickers.</summary>
    [HttpGet("art/collections")]
    public ActionResult<IReadOnlyList<object>> GetArtCollections() => Ok(_libraryManager
        .GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.BoxSet] })
        .OfType<BoxSet>()
        .OrderBy(collection => collection.SortName ?? collection.Name, StringComparer.OrdinalIgnoreCase)
        .Select(collection => new { collection.Id, collection.Name, MediaItems = collection.GetLinkedChildren().Count })
        .ToArray());

    /// <summary>Returns the collection's actual primary-image URLs for poster-focused previews.</summary>
    [HttpGet("art/collections/{collectionId:guid}/poster-examples")]
    public ActionResult<IReadOnlyList<object>> GetCollectionPosterExamples(Guid collectionId)
    {
        var collection = _libraryManager.GetItemById<BoxSet>(collectionId);
        if (collection is null)
        {
            return NotFound();
        }

        return Ok(collection.GetLinkedChildren()
            .Where(item => item.HasImage(ImageType.Primary, 0))
            .Select(item => new { item.Id, item.Name })
            .ToArray());
    }

    /// <summary>Returns the native top-level media posters in one Jellyfin library for poster-focused library artwork.</summary>
    [HttpGet("art/libraries/{libraryId:guid}/poster-examples")]
    public ActionResult<IReadOnlyList<object>> GetLibraryPosterExamples(Guid libraryId)
    {
        if (!HasOnlyKnownLibraries([libraryId]))
        {
            return NotFound("The selected Jellyfin library is no longer available on this server.");
        }

        var library = _libraryManager.GetItemById(libraryId);
        var collectionType = library is null ? CollectionType.unknown : _libraryManager.GetConfiguredContentType(library);

        return Ok(_libraryManager.GetItemList(new InternalItemsQuery
        {
            ParentId = libraryId,
            Recursive = true,
            IncludeItemTypes = GetManualCollectionItemTypes(collectionType),
        })
            .Where(item => item.HasImage(ImageType.Primary, 0))
            .Select(item => new { item.Id, item.Name })
            .ToArray());
    }

    /// <summary>Saves one browser-rendered PNG as the selected native Jellyfin collection image type.</summary>
    [HttpPost("art/apply")]
    public async Task<IActionResult> ApplyCollectionArt(
        [FromForm] Guid collectionId,
        [FromForm] CollectionArtImageType artType,
        [FromForm] IFormFile image,
        CancellationToken cancellationToken)
    {
        var collection = _libraryManager.GetItemById(collectionId);
        if (collection is null)
        {
            return NotFound("The selected collection is no longer available on this server.");
        }

        if (image is null || image.Length <= 0)
        {
            return BadRequest("A rendered PNG is required.");
        }

        try
        {
            await using var stream = image.OpenReadStream();
            await _providerManager.SaveImage(collection, stream, "image/png", ToJellyfinImageType(artType), artType == CollectionArtImageType.Backdrop ? 0 : null, cancellationToken).ConfigureAwait(false);
            await collection.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }
        catch (Exception exception)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Could not apply the rendered collection art.",
                detail: $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    /// <summary>Resaves existing primary collection images so Jellyfin recalculates and persists their dimensions.</summary>
    [HttpPost("art/repair-primary-image-metadata")]
    public async Task<ActionResult<object>> RepairPrimaryImageMetadata(CancellationToken cancellationToken)
    {
        var collections = _libraryManager
            .GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.BoxSet] })
            .OfType<BoxSet>()
            .OrderBy(collection => collection.SortName ?? collection.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var repaired = 0;
        var skipped = 0;
        var failures = new List<string>();

        foreach (var collection in collections)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var image = collection.GetImageInfo(ImageType.Primary, 0);
            if (string.IsNullOrWhiteSpace(image?.Path) || !System.IO.File.Exists(image.Path))
            {
                skipped++;
                continue;
            }

            var mimeType = MimeTypes.GetMimeType(image.Path);
            if (!mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                skipped++;
                continue;
            }

            try
            {
                // Read before saving: SaveImage may write back to this same path.
                var imageBytes = await System.IO.File.ReadAllBytesAsync(image.Path, cancellationToken).ConfigureAwait(false);
                var stream = new MemoryStream(imageBytes, writable: false);
                await _providerManager.SaveImage(collection, stream, mimeType, ImageType.Primary, 0, cancellationToken).ConfigureAwait(false);
                await collection.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
                repaired++;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                failures.Add($"{collection.Name}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        return Ok(new
        {
            TotalCollections = collections.Length,
            RepairedCollections = repaired,
            SkippedCollections = skipped,
            FailedCollections = failures.Count,
            Failures = failures,
        });
    }

    /// <summary>Returns the selectable media in one Jellyfin library for the manual collection picker.</summary>
    [HttpGet("manual-collections/library-items")]
    public IActionResult GetManualCollectionLibraryItems([FromQuery] Guid libraryId)
    {
        if (!HasOnlyKnownLibraries([libraryId]))
        {
            return BadRequest("The selected Jellyfin library is no longer available on this server.");
        }

        var library = _libraryManager.GetItemById(libraryId);
        var collectionType = library is null ? CollectionType.unknown : _libraryManager.GetConfiguredContentType(library);
        // Match Jellyfin's normal library browsing query: scope it to the
        // library, search recursively, and request only the item kinds that
        // belong in that library's configured collection type.
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            ParentId = libraryId,
            Recursive = true,
            IncludeItemTypes = GetManualCollectionItemTypes(collectionType),
        })
            .Where(item => item is not Video { ExtraType: not null })
            .OrderBy(item => item.SortName ?? item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new
            {
                item.Id,
                item.Name,
                Type = GetManualCollectionDisplayType(item.GetBaseItemKind()),
                item.ProductionYear,
                item.Overview,
                HasPrimaryImage = item.HasImage(ImageType.Primary, 0),
            })
            .ToArray();

        return Ok(new { LibraryId = libraryId, Items = items });
    }

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
    public async Task<ActionResult<object>> CreateCollection([FromBody] CreateCollectionRequest request, CancellationToken cancellationToken)
    {
        var collection = await _reconciler.CreateCollectionAsync(request.Name, request.ItemIds).ConfigureAwait(false);
        await UpdateCollectionOverviewAsync(collection, request.Overview, cancellationToken).ConfigureAwait(false);
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

    /// <summary>Saves the collection types the administrator wants shown in the overview.</summary>
    [HttpPost("settings/collection-overview")]
    public IActionResult UpdateCollectionOverviewSettings([FromBody] CollectionOverviewSettingsRequest request) =>
        Ok(RequirePlugin().UpdateCollectionOverviewSettings(request.ShowPluginMadeCollections, request.ShowNonPluginMadeCollections));

    /// <summary>Saves the collection overview's added and removed colors.</summary>
    [HttpPost("settings/collection-overview-colors")]
    public IActionResult UpdateCollectionOverviewColors([FromBody] CollectionOverviewColorsRequest request)
    {
        if (!IsColor(request.AddedColor) || !IsColor(request.RemovedColor))
        {
            return BadRequest("Choose valid six-digit hexadecimal colors.");
        }

        return Ok(RequirePlugin().UpdateCollectionOverviewColors(request.AddedColor.Trim(), request.RemovedColor.Trim()));
    }

    /// <summary>Scans current standard Jellyfin collections and saves a selected-library overview snapshot.</summary>
    [HttpPost("collection-overview/scan")]
    public IActionResult ScanCollectionOverview()
    {
        try
        {
            var plugin = RequirePlugin();
            if (plugin.Configuration.LibraryIds.Count == 0)
            {
                return BadRequest("Save one or more libraries in Main Settings before scanning collections.");
            }

            return Ok(_collectionOverview.Scan(plugin.Configuration));
        }
        catch (Exception)
        {
            return Problem(title: "Collection scan could not be completed.", statusCode: 500);
        }
    }

    /// <summary>Returns the saved collection overview scan status and last-completed time.</summary>
    [HttpGet("collection-overview/status")]
    public IActionResult GetCollectionOverviewStatus()
    {
        return Ok(_collectionOverview.GetStatus());
    }

    /// <summary>Returns saved per-library collection counts for the expandable overview.</summary>
    [HttpGet("collection-overview/counts")]
    public IActionResult GetCollectionOverviewCounts()
    {
        var collections = _collectionOverview.GetSnapshot()?.Libraries.SelectMany(library => library.Collections).ToArray() ?? [];
        return Ok(new[] { new { LibraryId = Guid.Empty, TotalItems = FilterCollections(collections).Count } });
    }

    /// <summary>Returns every saved collection-overview entry for the collapsed overview list.</summary>
    [HttpGet("collection-overview/all")]
    public IActionResult GetAllCollectionOverview()
    {
        var values = FilterCollections(_collectionOverview.GetSnapshot()?.Libraries.SelectMany(library => library.Collections) ?? [])
            .OrderBy(collection => collection.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return Ok(new { Items = values, TotalItems = values.Length });
    }

    /// <summary>Returns a bounded saved collection-overview page for one selected library.</summary>
    [HttpGet("collection-overview/page")]
    public IActionResult GetCollectionOverviewPage([FromQuery] Guid libraryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var size = Math.Clamp(pageSize, 1, 50);
        var values = FilterCollections(_collectionOverview.GetSnapshot()?.Libraries.SelectMany(library => library.Collections) ?? []).OrderBy(collection => collection.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var currentPage = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(values.Length / (double)size)));
        return Ok(new { Items = values.Skip((currentPage - 1) * size).Take(size).ToArray(), TotalItems = values.Length, Page = currentPage, PageSize = size });
    }

    /// <summary>Returns a lazy page of current media members for a collection editor dialog.</summary>
    [HttpGet("collection-overview/collection")]
    public IActionResult GetCollectionEditor([FromQuery] Guid collectionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var collection = _libraryManager.GetItemById<BoxSet>(collectionId);
        if (collection is null)
        {
            return NotFound("This collection no longer exists.");
        }

        var items = collection.GetLinkedChildren().OrderBy(item => item.SortName ?? item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var size = Math.Clamp(pageSize, 1, 100);
        var currentPage = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(items.Length / (double)size)));
        return Ok(new
        {
            CollectionId = collection.Id,
            Name = collection.Name,
            Overview = collection.Overview,
            TotalItems = items.Length,
            Page = currentPage,
            PageSize = size,
            Items = items.Skip((currentPage - 1) * size).Take(size).Select(item => new { item.Id, item.Name, Type = item.GetType().Name, item.ProductionYear }).ToArray(),
        });
    }

    /// <summary>Returns a bounded poster-capable collection picker page for the editor dialog.</summary>
    [HttpGet("collection-overview/targets")]
    public IActionResult GetCollectionTargets([FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] string? searchTerm = null)
    {
        var values = _libraryManager.GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.BoxSet] })
            .OfType<BoxSet>()
            .Where(collection => string.IsNullOrWhiteSpace(searchTerm) || collection.Name.Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(collection => collection.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var size = Math.Clamp(pageSize, 1, 50);
        var currentPage = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(values.Length / (double)size)));
        return Ok(new
        {
            Items = values.Skip((currentPage - 1) * size).Take(size).Select(collection => new { collection.Id, collection.Name, PosterItemId = collection.Id }).ToArray(),
            TotalItems = values.Length,
            Page = currentPage,
            PageSize = size,
        });
    }

    /// <summary>Renames an existing native Jellyfin collection from its editor dialog.</summary>
    [HttpPost("collections/rename")]
    public async Task<IActionResult> RenameCollection([FromBody] CollectionRenameRequest request, CancellationToken cancellationToken)
    {
        if (request.CollectionId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("A collection and collection title are required.");
        }

        var collection = _libraryManager.GetItemById<BoxSet>(request.CollectionId);
        if (collection is null)
        {
            return NotFound("This collection no longer exists.");
        }

        if (!string.Equals(collection.Name, request.Name.Trim(), StringComparison.Ordinal))
        {
            await _reconciler.RenameCollectionAsync(request.CollectionId, request.Name.Trim(), cancellationToken).ConfigureAwait(false);
        }

        if (request.Overview is not null)
        {
            await UpdateCollectionOverviewAsync(collection, request.Overview, cancellationToken).ConfigureAwait(false);
        }
        return NoContent();
    }

    /// <summary>Deletes one existing native Jellyfin collection selected from the collection overview.</summary>
    [HttpPost("collections/delete")]
    public IActionResult DeleteCollection([FromBody] CollectionDeleteRequest request)
    {
        if (request.CollectionId == Guid.Empty)
        {
            return BadRequest("A collection is required.");
        }

        var collection = _libraryManager.GetItemById<BoxSet>(request.CollectionId);
        if (collection is null)
        {
            return NotFound("This collection no longer exists.");
        }

        _libraryManager.DeleteItemsUnsafeFast([collection]);
        return NoContent();
    }

    /// <summary>Applies selected reversible cleanup actions to Collection Manager collections.</summary>
    [HttpPost("collections/cleanup")]
    public async Task<IActionResult> CleanUpCollections([FromBody] CollectionCleanupRequest request, CancellationToken cancellationToken)
    {
        var plugin = RequirePlugin();
        var messages = new List<string>();
        if (request.UndoLastCollectionAction && plugin.Configuration.CollectionActionHistory.LastOrDefault() is { } action)
        {
            await UndoCollectionAction(action, cancellationToken).ConfigureAwait(false);
            plugin.RemoveLastCollectionAction();
            messages.Add("Undid the last Collection Manager action.");
        }

        if (request.RemoveAllPluginMadeCollections)
        {
            var managedIds = plugin.Configuration.PluginManagedCollectionIds.Concat(plugin.Configuration.Rules.Where(rule => rule.CollectionId.HasValue).Select(rule => rule.CollectionId!.Value)).Distinct().ToArray();
            var collections = managedIds.Select(id => _libraryManager.GetItemById<BoxSet>(id)).Where(collection => collection is not null).Cast<BoxSet>().ToArray();
            if (collections.Length > 0)
            {
                _libraryManager.DeleteItemsUnsafeFast(collections);
            }

            foreach (var collection in collections)
            {
                plugin.ForgetManagedCollection(collection.Id);
            }

            messages.Add($"Removed {collections.Length} collection(s) made by this plugin.");
        }

        if (request.RemoveAllMediaAdditionsToExternalCollections)
        {
            var managed = plugin.Configuration.PluginManagedCollectionIds.Concat(plugin.Configuration.Rules.Where(rule => rule.CollectionId.HasValue).Select(rule => rule.CollectionId!.Value)).ToHashSet();
            var additions = plugin.Configuration.CollectionActionHistory.Where(action => string.Equals(action.Action, "Add", StringComparison.Ordinal) && !managed.Contains(action.CollectionId))
                .SelectMany(action => action.ItemIds.Select(itemId => (action.CollectionId, ItemId: itemId))).Distinct().ToArray();
            foreach (var group in additions.GroupBy(value => value.CollectionId))
            {
                await _reconciler.RemoveFromCollectionAsync(group.Key, group.Select(value => value.ItemId)).ConfigureAwait(false);
            }

            messages.Add($"Removed {additions.Length} media addition(s) from existing collections not made by this plugin.");
        }

        return Ok(new { Message = messages.Count == 0 ? "Select one or more cleanup actions." : string.Join(" ", messages) });
    }

    private CollectionOverviewSnapshot BuildCollectionOverviewSnapshot(PluginConfiguration configuration)
    {
        var selectedLibraries = _libraryManager.GetVirtualFolders(true)
            .Select(folder => new { Id = Guid.TryParse(folder.ItemId, out var id) ? id : Guid.Empty, folder.Name })
            .Where(folder => folder.Id != Guid.Empty && configuration.LibraryIds.Contains(folder.Id))
            .GroupBy(folder => folder.Id).Select(group => group.First()).ToArray();
        var itemLibraries = selectedLibraries.SelectMany(library => _libraryManager.GetItemList(new InternalItemsQuery { ParentId = library.Id, Recursive = true })
            .Where(item => item is not BoxSet).Select(item => new { item.Id, LibraryId = library.Id })).GroupBy(value => value.Id).ToDictionary(group => group.Key, group => group.First().LibraryId);
        var previous = configuration.CollectionOverviewSnapshot;
        var previousLibraries = previous?.Libraries.ToDictionary(library => library.LibraryId) ?? [];
        var managed = configuration.PluginManagedCollectionIds.Concat(configuration.Rules.Where(rule => rule.CollectionId.HasValue).Select(rule => rule.CollectionId!.Value)).ToHashSet();
        var current = selectedLibraries.ToDictionary(library => library.Id, library => new CollectionOverviewLibrarySnapshot { LibraryId = library.Id, LibraryName = library.Name });

        var allCollections = _libraryManager.GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.BoxSet] }).OfType<BoxSet>().ToArray();
        foreach (var collection in allCollections)
        {
            BaseItem[] children;
            try
            {
                children = collection.GetLinkedChildren().Where(child => itemLibraries.ContainsKey(child.Id)).ToArray();
            }
            catch
            {
                // A broken linked item must not prevent all other native collections from being scanned.
                continue;
            }
            foreach (var group in children.GroupBy(child => itemLibraries[child.Id]))
            {
                var prior = previousLibraries.TryGetValue(group.Key, out var previousLibrary)
                    ? previousLibrary.Collections.FirstOrDefault(candidate => candidate.CollectionId == collection.Id)
                    : null;
                var priorItems = prior?.Items.Select(item => item.ItemId).ToHashSet() ?? [];
                current[group.Key].Collections.Add(new CollectionOverviewCollectionSnapshot
                {
                    CollectionId = collection.Id,
                    Name = collection.Name,
                    MadeByPlugin = managed.Contains(collection.Id),
                    Exists = true,
                    NewlyAdded = prior is null,
                    Items = group.Select(child => new CollectionOverviewItemSnapshot
                    {
                        ItemId = child.Id,
                        Name = child.Name,
                        NewlyAdded = !priorItems.Contains(child.Id),
                    }).Concat((prior?.Items ?? []).Where(item => !group.Any(child => child.Id == item.ItemId)).Select(item => new CollectionOverviewItemSnapshot
                    {
                        ItemId = item.ItemId,
                        Name = item.Name,
                        NewlyRemoved = true,
                    })).ToList(),
                });
            }
        }

        foreach (var library in current.Values)
        {
            if (!previousLibraries.TryGetValue(library.LibraryId, out var previousLibrary))
            {
                continue;
            }

            foreach (var removed in previousLibrary.Collections.Where(previousCollection => !library.Collections.Any(currentCollection => currentCollection.CollectionId == previousCollection.CollectionId)))
            {
                library.Collections.Add(new CollectionOverviewCollectionSnapshot
                {
                    CollectionId = removed.CollectionId,
                    Name = removed.Name,
                    MadeByPlugin = removed.MadeByPlugin,
                    Exists = false,
                    NewlyRemoved = true,
                    Items = removed.Items.Select(item => new CollectionOverviewItemSnapshot { ItemId = item.ItemId, Name = item.Name, NewlyRemoved = true }).ToList(),
                });
            }
        }

        return new CollectionOverviewSnapshot { CompletedUtc = DateTime.UtcNow, Libraries = current.Values.OrderBy(library => library.LibraryName, StringComparer.OrdinalIgnoreCase).ToList() };
    }

    private List<CollectionOverviewCollectionSnapshot> FilterCollections(IEnumerable<CollectionOverviewCollectionSnapshot> collections)
    {
        var configuration = RequirePlugin().Configuration;
        return collections.Where(collection => (collection.MadeByPlugin && configuration.ShowPluginMadeCollections) || (!collection.MadeByPlugin && configuration.ShowNonPluginMadeCollections)).ToList();
    }

    private async Task UndoCollectionAction(CollectionActionRecord action, CancellationToken cancellationToken)
    {
        switch (action.Action)
        {
            case "Create":
                if (_libraryManager.GetItemById<BoxSet>(action.CollectionId) is { } created)
                {
                    _libraryManager.DeleteItemsUnsafeFast([created]);
                    RequirePlugin().ForgetManagedCollection(created.Id);
                }
                break;
            case "Add":
                await _reconciler.RemoveFromCollectionAsync(action.CollectionId, action.ItemIds, recordAction: false).ConfigureAwait(false);
                break;
            case "Remove":
                await _reconciler.AddToCollectionAsync(action.CollectionId, action.ItemIds, recordAction: false).ConfigureAwait(false);
                break;
            case "Rename" when !string.IsNullOrWhiteSpace(action.PreviousCollectionName):
                await _reconciler.RenameCollectionAsync(action.CollectionId, action.PreviousCollectionName, cancellationToken, recordAction: false).ConfigureAwait(false);
                break;
        }
    }

    private static bool IsColor(string? color) =>
        !string.IsNullOrWhiteSpace(color) && System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$");

    private static TextFocusedCollectionArtSettings CreateTextFocusedSettings(TextFocusedArtSettingsRequest request) => new()
    {
        PreviewText = request.PreviewText ?? string.Empty,
        FontAssetId = request.FontAssetId?.Trim(),
        FontFileName = request.FontFileName?.Trim(),
        TextSize = Math.Clamp(request.TextSize, 10, 250),
        TextColor = request.TextColor,
        TextShadowColor = request.TextShadowColor,
        BackgroundStyle = NormalizeBackgroundStyle(request.BackgroundStyle),
        BackgroundColor = request.BackgroundColor,
        GradientColorOne = request.GradientColorOne,
        GradientColorTwo = request.GradientColorTwo,
        GradientDirection = request.GradientDirection,
        BackgroundAssetId = request.BackgroundAssetId?.Trim(),
        BackgroundFileName = request.BackgroundFileName?.Trim(),
        ArtType = request.ArtType,
    };

    private static PosterFocusedCollectionArtSettings CreatePosterFocusedSettings(TextFocusedArtSettingsRequest request) => new()
    {
        PreviewText = request.PreviewText ?? string.Empty,
        FontAssetId = request.FontAssetId?.Trim(),
        FontFileName = request.FontFileName?.Trim(),
        TextSize = Math.Clamp(request.TextSize, 10, 250),
        TextColor = request.TextColor,
        TextShadowColor = request.TextShadowColor,
        BackgroundStyle = NormalizeBackgroundStyle(request.BackgroundStyle),
        BackgroundColor = request.BackgroundColor,
        GradientColorOne = request.GradientColorOne,
        GradientColorTwo = request.GradientColorTwo,
        GradientDirection = request.GradientDirection,
        BackgroundAssetId = request.BackgroundAssetId?.Trim(),
        BackgroundFileName = request.BackgroundFileName?.Trim(),
        ArtType = request.ArtType,
        PosterStyle = request.PosterStyle is "FourPosters" or "NinePosters" ? request.PosterStyle : "OnePoster",
    };

    private static bool TryParseAssetKind(string value, out CollectionArtAssetKind kind)
    {
        if (string.Equals(value, "font", StringComparison.OrdinalIgnoreCase))
        {
            kind = CollectionArtAssetKind.Font;
            return true;
        }

        if (string.Equals(value, "image", StringComparison.OrdinalIgnoreCase))
        {
            kind = CollectionArtAssetKind.Image;
            return true;
        }

        kind = default;
        return false;
    }

    private static CollectionArtBackgroundStyle NormalizeBackgroundStyle(CollectionArtBackgroundStyle style) =>
        style == CollectionArtBackgroundStyle.Image ? CollectionArtBackgroundStyle.Solid : style;

    private static ImageType ToJellyfinImageType(CollectionArtImageType artType) => artType switch
    {
        CollectionArtImageType.Poster => ImageType.Primary,
        CollectionArtImageType.Backdrop => ImageType.Backdrop,
        CollectionArtImageType.Banner => ImageType.Banner,
        CollectionArtImageType.Thumbnail => ImageType.Thumb,
        CollectionArtImageType.Logo => ImageType.Logo,
        _ => throw new ArgumentOutOfRangeException(nameof(artType)),
    };

    private async Task UpdateCollectionOverviewAsync(BoxSet collection, string? overview, CancellationToken cancellationToken)
    {
        collection.Overview = string.IsNullOrWhiteSpace(overview) ? null : overview.Trim();
        await _libraryManager.UpdateItemAsync(collection, collection, ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
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

    private static BaseItemKind[] GetManualCollectionItemTypes(CollectionType? collectionType) => collectionType switch
    {
        CollectionType.tvshows => [BaseItemKind.Series],
        CollectionType.movies => [BaseItemKind.Movie],
        CollectionType.music => [BaseItemKind.Audio, BaseItemKind.MusicAlbum],
        CollectionType.musicvideos => [BaseItemKind.MusicVideo],
        CollectionType.books => [BaseItemKind.Book, BaseItemKind.AudioBook],
        CollectionType.homevideos => [BaseItemKind.Movie, BaseItemKind.Video],
        CollectionType.trailers => [BaseItemKind.Trailer],
        _ => [BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.Audio, BaseItemKind.MusicAlbum, BaseItemKind.AudioBook, BaseItemKind.Book, BaseItemKind.MusicVideo, BaseItemKind.Video],
    };

    private static string GetManualCollectionDisplayType(BaseItemKind itemType) => itemType switch
    {
        BaseItemKind.Audio => "Audio",
        BaseItemKind.MusicAlbum => "Audio Album",
        BaseItemKind.AudioBook => "Audiobook",
        BaseItemKind.MusicVideo => "Music Video",
        _ => itemType.ToString(),
    };

    private bool IsCompletedDraft(IndividualCollectionDraftRequest draft) =>
        draft.SourceLibraryId != Guid.Empty &&
        !string.IsNullOrWhiteSpace(draft.MetadataType) &&
        !string.IsNullOrWhiteSpace(draft.MetadataValue) &&
        !string.IsNullOrWhiteSpace(draft.CollectionTitle);

    private BoxSet? FindCollectionByName(string title) =>
        _libraryManager.GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.BoxSet] })
            .OfType<BoxSet>()
            .FirstOrDefault(collection => string.Equals(collection.Name, title.Trim(), StringComparison.OrdinalIgnoreCase));
}
