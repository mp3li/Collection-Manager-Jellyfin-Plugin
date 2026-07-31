using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.CollectionManager.Configuration;

/// <summary>Describes the Collection Manager art choice attached to a new collection draft.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CollectionArtPreference
{
    /// <summary>Leave artwork to Jellyfin.</summary>
    JellyfinDefault,

    /// <summary>Use the saved Collection Manager default choice.</summary>
    PluginDefault,

    /// <summary>Use the saved text-focused settings.</summary>
    TextFocused,

    /// <summary>Use the saved poster-focused settings.</summary>
    PosterFocused,
}

/// <summary>Jellyfin collection image types exposed by the art dashboard.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CollectionArtImageType
{
    /// <summary>Jellyfin primary artwork, displayed as a poster.</summary>
    Poster,

    /// <summary>Jellyfin backdrop artwork.</summary>
    Backdrop,

    /// <summary>Jellyfin banner artwork.</summary>
    Banner,

    /// <summary>Jellyfin thumb artwork.</summary>
    Thumbnail,

    /// <summary>Jellyfin logo artwork.</summary>
    Logo,
}

/// <summary>Shared background choices for generated collection art.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CollectionArtBackgroundStyle
{
    /// <summary>One solid color.</summary>
    Solid,

    /// <summary>No background.</summary>
    Transparent,

    /// <summary>A color gradient.</summary>
    Gradient,

    /// <summary>An administrator-imported image.</summary>
    Image,
}

/// <summary>Supported directions for generated gradients.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CollectionArtGradientDirection
{
    /// <summary>Top to bottom.</summary>
    Vertical,

    /// <summary>Left to right.</summary>
    Horizontal,

    /// <summary>Top-left to bottom-right.</summary>
    Diagonal,

    /// <summary>From the center outward.</summary>
    Center,
}

/// <summary>Persisted text-focused artwork settings.</summary>
public class TextFocusedCollectionArtSettings
{
    public string PreviewText { get; set; } = "Collection Name";
    public string? FontAssetId { get; set; }
    public string? FontFileName { get; set; }
    public int TextSize { get; set; } = 100;
    public string TextColor { get; set; } = "#FFFFFF";
    public string TextShadowColor { get; set; } = "#000000";
    public CollectionArtBackgroundStyle BackgroundStyle { get; set; } = CollectionArtBackgroundStyle.Solid;
    public string BackgroundColor { get; set; } = "#000000";
    public string GradientColorOne { get; set; } = "#000000";
    public string GradientColorTwo { get; set; } = "#333333";
    public CollectionArtGradientDirection GradientDirection { get; set; } = CollectionArtGradientDirection.Vertical;
    public string? BackgroundAssetId { get; set; }
    public string? BackgroundFileName { get; set; }
    public CollectionArtImageType ArtType { get; set; } = CollectionArtImageType.Poster;
}

/// <summary>Persisted poster-focused artwork settings.</summary>
public sealed class PosterFocusedCollectionArtSettings : TextFocusedCollectionArtSettings
{
    public string PosterStyle { get; set; } = "OnePoster";
}

/// <summary>Persisted logo-focused artwork settings.</summary>
public sealed class LogoFocusedCollectionArtSettings
{
    public CollectionArtBackgroundStyle BackgroundStyle { get; set; } = CollectionArtBackgroundStyle.Solid;
    public string BackgroundColor { get; set; } = "#000000";
    public string GradientColorOne { get; set; } = "#000000";
    public string GradientColorTwo { get; set; } = "#333333";
    public CollectionArtGradientDirection GradientDirection { get; set; } = CollectionArtGradientDirection.Vertical;
    public string? BackgroundAssetId { get; set; }
    public string? BackgroundFileName { get; set; }
    public CollectionArtImageType ArtType { get; set; } = CollectionArtImageType.Poster;
}

/// <summary>Persisted multi-collection or library gradient-art settings.</summary>
public sealed class MultiCollectionGradientArtSettings
{
    public string GradientColorOne { get; set; } = "#000000";
    public string GradientColorTwo { get; set; } = "#333333";
    public string GradientColorThree { get; set; } = "#666666";
    public CollectionArtGradientDirection GradientDirection { get; set; } = CollectionArtGradientDirection.Horizontal;
    public CollectionArtImageType ArtType { get; set; } = CollectionArtImageType.Poster;
}

/// <summary>One saved logo selected for a collection in the logo-focused art tab.</summary>
public sealed class CollectionLogoSelection
{
    public Guid CollectionId { get; set; }
    public string? LogoKind { get; set; }
    public string? LogoName { get; set; }
    public string? ImportedLogoAssetId { get; set; }
    public string? ImportedLogoFileName { get; set; }
}
