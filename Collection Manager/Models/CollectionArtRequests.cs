using Jellyfin.Plugin.CollectionManager.Configuration;

namespace Jellyfin.Plugin.CollectionManager.Models;

/// <summary>Saves the default generated-art choice.</summary>
public sealed class DefaultArtPreferenceRequest
{
    public CollectionArtPreference? Preference { get; set; }
}

/// <summary>Saves text-focused or poster-focused artwork settings.</summary>
public sealed class TextFocusedArtSettingsRequest
{
    public string PreviewText { get; set; } = "Collection Name";
    public string? FontAssetId { get; set; }
    public string? FontFileName { get; set; }
    public int TextSize { get; set; } = 100;
    public string TextColor { get; set; } = "#FFFFFF";
    public string TextShadowColor { get; set; } = "#000000";
    public CollectionArtBackgroundStyle BackgroundStyle { get; set; }
    public string BackgroundColor { get; set; } = "#000000";
    public string GradientColorOne { get; set; } = "#000000";
    public string GradientColorTwo { get; set; } = "#333333";
    public CollectionArtGradientDirection GradientDirection { get; set; }
    public string? BackgroundAssetId { get; set; }
    public string? BackgroundFileName { get; set; }
    public CollectionArtImageType ArtType { get; set; } = CollectionArtImageType.Poster;
    public string? PosterStyle { get; set; }
}

/// <summary>Saves logo-focused artwork settings.</summary>
public sealed class LogoFocusedArtSettingsRequest
{
    public CollectionArtBackgroundStyle BackgroundStyle { get; set; }
    public string BackgroundColor { get; set; } = "#000000";
    public string GradientColorOne { get; set; } = "#000000";
    public string GradientColorTwo { get; set; } = "#333333";
    public CollectionArtGradientDirection GradientDirection { get; set; }
    public string? BackgroundAssetId { get; set; }
    public string? BackgroundFileName { get; set; }
    public CollectionArtImageType ArtType { get; set; } = CollectionArtImageType.Poster;
}

/// <summary>Saves multi-collection gradient artwork settings.</summary>
public sealed class MultiCollectionGradientArtSettingsRequest
{
    public string GradientColorOne { get; set; } = "#000000";
    public string GradientColorTwo { get; set; } = "#333333";
    public string GradientColorThree { get; set; } = "#666666";
    public CollectionArtGradientDirection GradientDirection { get; set; }
    public CollectionArtImageType ArtType { get; set; } = CollectionArtImageType.Poster;
}

/// <summary>Saves one selected logo for a collection.</summary>
public sealed class CollectionLogoSelectionRequest
{
    public Guid CollectionId { get; set; }
    public string? LogoKind { get; set; }
    public string? LogoName { get; set; }
    public string? ImportedLogoAssetId { get; set; }
    public string? ImportedLogoFileName { get; set; }
    public bool RoundCorners { get; set; }
}
