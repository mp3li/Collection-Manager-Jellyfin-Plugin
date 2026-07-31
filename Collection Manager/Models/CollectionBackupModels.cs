namespace Jellyfin.Plugin.CollectionManager.Models;

/// <summary>One selectable collection backup stored by Collection Manager.</summary>
public sealed class CollectionBackupSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public int CollectionCount { get; set; }
    public int ImageCount { get; set; }
}

/// <summary>The persisted document for one complete collection backup.</summary>
public sealed class CollectionBackupDocument
{
    public int FormatVersion { get; set; } = 1;
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public List<CollectionBackupItem> Collections { get; set; } = [];
}

/// <summary>All restorable native Jellyfin collection information captured by a backup.</summary>
public sealed class CollectionBackupItem
{
    public Guid OriginalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? ForcedSortName { get; set; }
    public string? SortName { get; set; }
    public DateTime? PremiereDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? OfficialRating { get; set; }
    public float? CriticRating { get; set; }
    public string? CustomRating { get; set; }
    public string? Overview { get; set; }
    public string? Tagline { get; set; }
    public List<string> Studios { get; set; } = [];
    public List<string> Genres { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<string> ProductionLocations { get; set; } = [];
    public string? HomePageUrl { get; set; }
    public float? CommunityRating { get; set; }
    public long? RunTimeTicks { get; set; }
    public int? ProductionYear { get; set; }
    public Dictionary<string, string> ProviderIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> LockedFields { get; set; } = [];
    public string? DisplayOrder { get; set; }
    public string? PreferredMetadataLanguage { get; set; }
    public string? PreferredMetadataCountryCode { get; set; }
    public bool IsLocked { get; set; }
    public DateTime DateCreated { get; set; }
    public List<Guid> MemberIds { get; set; } = [];
    public List<CollectionBackupImage> Images { get; set; } = [];
}

/// <summary>One collection image file stored alongside the backup document.</summary>
public sealed class CollectionBackupImage
{
    public string ImageType { get; set; } = string.Empty;
    public int ImageIndex { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/octet-stream";
}

/// <summary>The optional name supplied when creating a backup.</summary>
public sealed class CollectionBackupCreateRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>The optional name supplied when renaming a backup.</summary>
public sealed class CollectionBackupRenameRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>Controls whether restore also deletes collections not present in the selected backup.</summary>
public sealed class CollectionBackupRestoreRequest
{
    public bool DeleteCollectionsMissingFromBackup { get; set; }
}

/// <summary>Counts returned after a collection backup restore.</summary>
public sealed class CollectionBackupRestoreResult
{
    public int RestoredCollections { get; set; }
    public int RecreatedCollections { get; set; }
    public int DeletedCollections { get; set; }
    public int RestoredImages { get; set; }
    public int SkippedMissingMedia { get; set; }
}
