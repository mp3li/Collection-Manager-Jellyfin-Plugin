namespace Jellyfin.Plugin.CollectionManager.Configuration;

/// <summary>A persisted local snapshot of the selected-library collection overview.</summary>
public sealed class CollectionOverviewSnapshot
{
    /// <summary>Gets or sets when the scan completed.</summary>
    public DateTime CompletedUtc { get; set; }

    /// <summary>Gets or sets collection entries grouped by selected library.</summary>
    public List<CollectionOverviewLibrarySnapshot> Libraries { get; set; } = [];
}

/// <summary>One selected library's saved collection overview entries.</summary>
public sealed class CollectionOverviewLibrarySnapshot
{
    public Guid LibraryId { get; set; }
    public string LibraryName { get; set; } = string.Empty;
    public List<CollectionOverviewCollectionSnapshot> Collections { get; set; } = [];
}

/// <summary>A saved collection and its selected-library media items.</summary>
public sealed class CollectionOverviewCollectionSnapshot
{
    public Guid CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool MadeByPlugin { get; set; }
    public bool Exists { get; set; } = true;
    public bool NewlyAdded { get; set; }
    public bool NewlyRemoved { get; set; }
    public List<CollectionOverviewItemSnapshot> Items { get; set; } = [];
}

/// <summary>One media item captured in a collection overview scan.</summary>
public sealed class CollectionOverviewItemSnapshot
{
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? ProductionYear { get; set; }
    public string Overview { get; set; } = string.Empty;
    public bool HasPrimaryImage { get; set; }
    public bool NewlyAdded { get; set; }
    public bool NewlyRemoved { get; set; }
}

/// <summary>A reversible collection action initiated through Collection Manager.</summary>
public sealed class CollectionActionRecord
{
    public string Action { get; set; } = string.Empty;
    public Guid CollectionId { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public string? PreviousCollectionName { get; set; }
    public List<Guid> ItemIds { get; set; } = [];
    public DateTime OccurredUtc { get; set; }
}
