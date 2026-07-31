using System.Text.Json;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.CollectionManager.Models;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CollectionManager.Services;

/// <summary>Creates, stores, and restores complete native Jellyfin collection snapshots.</summary>
public sealed class CollectionBackupManager
{
    private const int MaximumImagesPerType = 100;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly ICollectionManager _collectionManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly ILogger<CollectionBackupManager> _logger;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="CollectionBackupManager"/> class.</summary>
    public CollectionBackupManager(
        ICollectionManager collectionManager,
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        ILogger<CollectionBackupManager> logger)
    {
        _collectionManager = collectionManager;
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _logger = logger;
    }

    /// <summary>Creates an optional on-disk snapshot of every current native Jellyfin collection.</summary>
    public async Task<CollectionBackupSummary> CreateAsync(string? name, CancellationToken cancellationToken)
    {
        var document = new CollectionBackupDocument
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
            CreatedUtc = DateTimeOffset.UtcNow,
        };
        var temporaryDirectory = Path.Combine(BackupDirectory, $".creating-{document.Id:N}");
        var finalDirectory = BackupDirectoryFor(document.Id);

        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(BackupDirectory);
            Directory.CreateDirectory(temporaryDirectory);
            foreach (var collection in GetCollections())
            {
                cancellationToken.ThrowIfCancellationRequested();
                document.Collections.Add(await CaptureAsync(collection, temporaryDirectory, cancellationToken).ConfigureAwait(false));
            }

            await File.WriteAllTextAsync(DocumentPath(temporaryDirectory), JsonSerializer.Serialize(document, JsonOptions), cancellationToken).ConfigureAwait(false);
            Directory.Move(temporaryDirectory, finalDirectory);
        }
        catch
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }

            throw;
        }
        finally
        {
            _fileLock.Release();
        }

        return ToSummary(document);
    }

    /// <summary>Gets all available backups, newest first.</summary>
    public async Task<IReadOnlyList<CollectionBackupSummary>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(BackupDirectory))
        {
            return [];
        }

        var summaries = new List<CollectionBackupSummary>();
        foreach (var directory in Directory.EnumerateDirectories(BackupDirectory, "collection-backup-*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                summaries.Add(ToSummary(await ReadAsync(directory, cancellationToken).ConfigureAwait(false)));
            }
            catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
            {
                _logger.LogWarning(exception, "Skipping unreadable collection backup directory {BackupDirectory}.", directory);
            }
        }

        return summaries.OrderByDescending(backup => backup.CreatedUtc).ToArray();
    }

    /// <summary>Renames a stored backup without changing Jellyfin collections.</summary>
    public async Task RenameAsync(Guid backupId, string? name, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = BackupDirectoryFor(backupId);
            var document = await ReadAsync(directory, cancellationToken).ConfigureAwait(false);
            document.Name = NormalizeName(name);
            await File.WriteAllTextAsync(DocumentPath(directory), JsonSerializer.Serialize(document, JsonOptions), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>Deletes only the selected on-disk backup, never a Jellyfin collection.</summary>
    public async Task DeleteAsync(Guid backupId, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = BackupDirectoryFor(backupId);
            if (!Directory.Exists(directory))
            {
                throw new KeyNotFoundException("The requested collection backup does not exist.");
            }

            Directory.Delete(directory, recursive: true);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>Restores a saved collection snapshot, including membership, metadata, and image files.</summary>
    public async Task<CollectionBackupRestoreResult> RestoreAsync(Guid backupId, bool deleteCollectionsMissingFromBackup, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var backupDirectory = BackupDirectoryFor(backupId);
            var document = await ReadAsync(backupDirectory, cancellationToken).ConfigureAwait(false);
            var result = new CollectionBackupRestoreResult();
            var currentCollections = GetCollections().ToDictionary(collection => collection.Id);
            var savedIds = document.Collections.Select(collection => collection.OriginalId).ToHashSet();

            if (deleteCollectionsMissingFromBackup)
            {
                var extras = currentCollections.Values.Where(collection => !savedIds.Contains(collection.Id)).ToArray();
                if (extras.Length > 0)
                {
                    _libraryManager.DeleteItemsUnsafeFast(extras);
                    result.DeletedCollections = extras.Length;
                    foreach (var extra in extras)
                    {
                        currentCollections.Remove(extra.Id);
                        Plugin.Instance?.ForgetManagedCollection(extra.Id);
                    }
                }
            }

            foreach (var saved in document.Collections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!currentCollections.TryGetValue(saved.OriginalId, out var collection))
                {
                    var survivingMemberIds = saved.MemberIds.Where(id => _libraryManager.GetItemById(id) is not null).Distinct().ToArray();
                    collection = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                    {
                        Name = saved.Name,
                        ItemIdList = survivingMemberIds.Select(itemId => itemId.ToString("N", System.Globalization.CultureInfo.InvariantCulture)).ToArray(),
                    }).ConfigureAwait(false);
                    currentCollections[collection.Id] = collection;
                    result.RecreatedCollections++;
                }

                ApplyMetadata(collection, saved);
                await _libraryManager.UpdateItemAsync(collection, collection, ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                await RestoreMembershipAsync(collection, saved, result, cancellationToken).ConfigureAwait(false);
                result.RestoredImages += await RestoreImagesAsync(collection, saved.Images, backupDirectory, cancellationToken).ConfigureAwait(false);
                result.RestoredCollections++;
            }

            return result;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<CollectionBackupItem> CaptureAsync(BoxSet collection, string temporaryDirectory, CancellationToken cancellationToken)
    {
        var saved = new CollectionBackupItem
        {
            OriginalId = collection.Id,
            Name = collection.Name,
            OriginalTitle = collection.OriginalTitle,
            ForcedSortName = collection.ForcedSortName,
            SortName = collection.SortName,
            PremiereDate = collection.PremiereDate,
            EndDate = collection.EndDate,
            OfficialRating = collection.OfficialRating,
            CriticRating = collection.CriticRating,
            CustomRating = collection.CustomRating,
            Overview = collection.Overview,
            Tagline = collection.Tagline,
            Studios = collection.Studios?.ToList() ?? [],
            Genres = collection.Genres?.ToList() ?? [],
            Tags = collection.Tags?.ToList() ?? [],
            ProductionLocations = collection.ProductionLocations?.ToList() ?? [],
            HomePageUrl = collection.HomePageUrl,
            CommunityRating = collection.CommunityRating,
            RunTimeTicks = collection.RunTimeTicks,
            ProductionYear = collection.ProductionYear,
            ProviderIds = collection.ProviderIds?.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase) ?? new(StringComparer.OrdinalIgnoreCase),
            LockedFields = collection.LockedFields?.Select(field => field.ToString()).ToList() ?? [],
            DisplayOrder = collection.DisplayOrder?.ToString(),
            PreferredMetadataLanguage = collection.PreferredMetadataLanguage,
            PreferredMetadataCountryCode = collection.PreferredMetadataCountryCode,
            IsLocked = collection.IsLocked,
            DateCreated = collection.DateCreated,
            MemberIds = collection.GetLinkedChildren().Select(item => item.Id).Distinct().ToList(),
        };

        var collectionImageDirectory = Path.Combine(temporaryDirectory, "images", collection.Id.ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        foreach (var type in Enum.GetValues<ImageType>())
        {
            for (var index = 0; index < MaximumImagesPerType; index++)
            {
                var image = collection.GetImageInfo(type, index);
                var sourcePath = image?.Path;
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    break;
                }

                if (!File.Exists(sourcePath))
                {
                    _logger.LogWarning("Collection image {ImageType} index {ImageIndex} for {CollectionName} has no readable file at {ImagePath}.", type, index, collection.Name, sourcePath);
                    continue;
                }

                var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
                var mimeType = GetImageMimeType(extension);
                if (mimeType is null)
                {
                    _logger.LogWarning("Skipping collection image {ImagePath} because extension {Extension} is not supported for backup restore.", sourcePath, extension);
                    continue;
                }

                Directory.CreateDirectory(collectionImageDirectory);
                var fileName = $"{type}-{index}{extension}";
                var destination = Path.Combine(collectionImageDirectory, fileName);
                await using (var input = File.OpenRead(sourcePath))
                await using (var output = File.Create(destination))
                {
                    await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                }

                saved.Images.Add(new CollectionBackupImage
                {
                    ImageType = type.ToString(),
                    ImageIndex = index,
                    RelativePath = Path.Combine("images", collection.Id.ToString("N", System.Globalization.CultureInfo.InvariantCulture), fileName),
                    MimeType = mimeType,
                });
            }
        }

        return saved;
    }

    private async Task RestoreMembershipAsync(BoxSet collection, CollectionBackupItem saved, CollectionBackupRestoreResult result, CancellationToken cancellationToken)
    {
        var currentIds = collection.GetLinkedChildren().Select(item => item.Id).ToHashSet();
        var availableSavedIds = saved.MemberIds.Where(id => _libraryManager.GetItemById(id) is not null).Distinct().ToHashSet();
        result.SkippedMissingMedia += saved.MemberIds.Count - availableSavedIds.Count;
        var additions = availableSavedIds.Except(currentIds).ToArray();
        var removals = currentIds.Except(availableSavedIds).ToArray();

        if (additions.Length > 0)
        {
            await _collectionManager.AddToCollectionAsync(collection.Id, additions).ConfigureAwait(false);
        }

        if (removals.Length > 0)
        {
            await _collectionManager.RemoveFromCollectionAsync(collection.Id, removals).ConfigureAwait(false);
        }
    }

    private async Task<int> RestoreImagesAsync(BoxSet collection, IEnumerable<CollectionBackupImage> savedImages, string backupDirectory, CancellationToken cancellationToken)
    {
        foreach (var imageType in Enum.GetValues<ImageType>())
        {
            for (var count = 0; count < MaximumImagesPerType; count++)
            {
                var image = collection.GetImageInfo(imageType, 0);
                if (string.IsNullOrWhiteSpace(image?.Path))
                {
                    break;
                }

                await collection.DeleteImageAsync(imageType, 0).ConfigureAwait(false);
            }
        }

        var restored = 0;
        foreach (var savedImage in savedImages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Enum.TryParse<ImageType>(savedImage.ImageType, ignoreCase: true, out var imageType))
            {
                _logger.LogWarning("Skipping an unknown saved collection image type {ImageType}.", savedImage.ImageType);
                continue;
            }

            var sourcePath = Path.GetFullPath(Path.Combine(backupDirectory, savedImage.RelativePath));
            if (!sourcePath.StartsWith(Path.GetFullPath(backupDirectory) + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || !File.Exists(sourcePath))
            {
                _logger.LogWarning("Collection backup image {ImagePath} is missing or outside its backup directory.", savedImage.RelativePath);
                continue;
            }

            await using var stream = File.OpenRead(sourcePath);
            await _providerManager.SaveImage(collection, stream, savedImage.MimeType, imageType, savedImage.ImageIndex, cancellationToken).ConfigureAwait(false);
            restored++;
        }

        return restored;
    }

    private static void ApplyMetadata(BoxSet collection, CollectionBackupItem saved)
    {
        collection.Name = saved.Name;
        collection.OriginalTitle = saved.OriginalTitle;
        collection.ForcedSortName = saved.ForcedSortName;
        collection.SortName = saved.SortName;
        collection.PremiereDate = saved.PremiereDate;
        collection.EndDate = saved.EndDate;
        collection.OfficialRating = saved.OfficialRating;
        collection.CriticRating = saved.CriticRating;
        collection.CustomRating = saved.CustomRating;
        collection.Overview = saved.Overview;
        collection.Tagline = saved.Tagline;
        collection.Studios = saved.Studios.ToArray();
        collection.Genres = saved.Genres.ToArray();
        collection.Tags = saved.Tags.ToArray();
        collection.ProductionLocations = saved.ProductionLocations.ToArray();
        collection.HomePageUrl = saved.HomePageUrl;
        collection.CommunityRating = saved.CommunityRating;
        collection.RunTimeTicks = saved.RunTimeTicks;
        collection.ProductionYear = saved.ProductionYear;
        collection.ProviderIds = saved.ProviderIds;
        collection.LockedFields = saved.LockedFields.Select(ParseMetadataField).Where(field => field.HasValue).Select(field => field!.Value).ToArray();
        collection.DisplayOrder = saved.DisplayOrder;
        collection.PreferredMetadataLanguage = saved.PreferredMetadataLanguage;
        collection.PreferredMetadataCountryCode = saved.PreferredMetadataCountryCode;
        collection.IsLocked = saved.IsLocked;
        collection.DateCreated = saved.DateCreated;
    }

    private static MetadataField? ParseMetadataField(string value) =>
        Enum.TryParse<MetadataField>(value, ignoreCase: true, out var field) ? field : null;

    private IEnumerable<BoxSet> GetCollections() => _libraryManager
        .GetItemList(new InternalItemsQuery { IncludeItemTypes = [BaseItemKind.BoxSet] })
        .OfType<BoxSet>();

    private static string NormalizeName(string? name) => string.IsNullOrWhiteSpace(name)
        ? $"Collections Backup {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}"
        : name.Trim();

    private static CollectionBackupSummary ToSummary(CollectionBackupDocument document) => new()
    {
        Id = document.Id,
        Name = document.Name,
        CreatedUtc = document.CreatedUtc,
        CollectionCount = document.Collections.Count,
        ImageCount = document.Collections.Sum(collection => collection.Images.Count),
    };

    private static async Task<CollectionBackupDocument> ReadAsync(string directory, CancellationToken cancellationToken)
    {
        var path = DocumentPath(directory);
        if (!File.Exists(path))
        {
            throw new KeyNotFoundException("The requested collection backup does not exist.");
        }

        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<CollectionBackupDocument>(stream, JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The collection backup is empty or invalid.");
        if (document.FormatVersion != 1 || document.Id == Guid.Empty)
        {
            throw new InvalidDataException("The collection backup format is not supported.");
        }

        return document;
    }

    private static string BackupDirectory => Path.Combine(Plugin.Instance?.DataFolderPath ?? throw new InvalidOperationException("Collection Manager's plugin data folder is unavailable."), "collection-backups");
    private static string BackupDirectoryFor(Guid id) => Path.Combine(BackupDirectory, $"collection-backup-{id:N}");
    private static string DocumentPath(string directory) => Path.Combine(directory, "backup.json");

    private static string? GetImageMimeType(string extension) => extension switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        _ => null,
    };
}
