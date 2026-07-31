using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.CollectionManager.Services;

/// <summary>Stores administrator-imported collection-art assets in this plugin's Jellyfin data folder.</summary>
public sealed class CollectionArtAssetStore
{
    private static readonly HashSet<string> FontExtensions = new(StringComparer.OrdinalIgnoreCase) { ".ttf", ".otf" };
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg" };

    /// <summary>Saves one approved dashboard upload and returns its stable asset identifier and original file name.</summary>
    public async Task<CollectionArtAsset> SaveAsync(IFormFile file, CollectionArtAssetKind kind, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Choose a non-empty file.");
        }

        var originalName = Path.GetFileName(file.FileName ?? string.Empty);
        var extension = Path.GetExtension(originalName);
        var allowed = kind == CollectionArtAssetKind.Font ? FontExtensions : ImageExtensions;
        if (!allowed.Contains(extension))
        {
            throw new InvalidOperationException(kind == CollectionArtAssetKind.Font
                ? "Fonts must be .ttf or .otf files."
                : "Images must be PNG, JPG, JPEG, WEBP, GIF, or SVG files.");
        }

        var id = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
        var directory = GetDirectory(kind);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, id + extension.ToLowerInvariant());
        await using var output = File.Create(path);
        await using var input = file.OpenReadStream();
        await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        return new CollectionArtAsset(id, originalName, GetMimeType(extension));
    }

    /// <summary>Gets a stored art asset, if it still exists.</summary>
    public CollectionArtAssetFile? Open(string id, CollectionArtAssetKind kind)
    {
        if (!Guid.TryParseExact(id, "N", out _))
        {
            return null;
        }

        var directory = GetDirectory(kind);
        if (!Directory.Exists(directory))
        {
            return null;
        }

        var path = Directory.EnumerateFiles(directory, id + ".*").FirstOrDefault();
        if (path is null)
        {
            return null;
        }

        return new CollectionArtAssetFile(File.OpenRead(path), GetMimeType(Path.GetExtension(path)));
    }

    private static string GetDirectory(CollectionArtAssetKind kind)
    {
        var dataFolder = Plugin.Instance?.DataFolderPath
            ?? throw new InvalidOperationException("Collection Manager's plugin data folder is unavailable.");
        return Path.Combine(dataFolder, "collection-art-assets", kind == CollectionArtAssetKind.Font ? "fonts" : "images");
    }

    private static string GetMimeType(string extension) => extension.ToLowerInvariant() switch
    {
        ".ttf" => "font/ttf",
        ".otf" => "font/otf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        _ => "application/octet-stream",
    };
}

/// <summary>The asset category persisted by <see cref="CollectionArtAssetStore"/>.</summary>
public enum CollectionArtAssetKind
{
    /// <summary>An imported TTF or OTF font.</summary>
    Font,

    /// <summary>An imported image or logo.</summary>
    Image,
}

/// <summary>Information returned after saving one dashboard asset.</summary>
public sealed record CollectionArtAsset(string Id, string FileName, string ContentType);

/// <summary>An opened art asset stream and its content type.</summary>
public sealed record CollectionArtAssetFile(Stream Stream, string ContentType);
