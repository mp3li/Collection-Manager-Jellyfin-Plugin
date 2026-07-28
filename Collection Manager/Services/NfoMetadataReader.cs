using System.Xml.Linq;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.CollectionManager.Services;

/// <summary>Reads selected existing values from a media item's adjacent Jellyfin NFO sidecar.</summary>
/// <remarks>
/// Jellyfin imports standard NFO fields into <see cref="BaseItem"/>, but niche
/// fields such as source-site and custom Composer/Producer fields remain in the
/// NFO. This reader is deliberately read-only and never writes sidecars.
/// </remarks>
public static class NfoMetadataReader
{
    /// <summary>Reads an item's matching NFO sidecar when one exists and is valid XML.</summary>
    public static NfoMetadata Read(BaseItem item)
    {
        var nfoPath = FindNfoPath(item);
        if (nfoPath is null)
        {
            return NfoMetadata.Empty;
        }

        try
        {
            var root = XDocument.Load(nfoPath, LoadOptions.None).Root;
            if (root is null)
            {
                return NfoMetadata.Empty;
            }

            var metadata = new NfoMetadata();
            foreach (var element in root.Elements())
            {
                var value = Clean(element.Value);
                switch (element.Name.LocalName.ToLowerInvariant())
                {
                    case "actor":
                        var actorName = Clean(element.Elements().FirstOrDefault(child => child.Name.LocalName == "name")?.Value);
                        metadata.Actors.Add(actorName);
                        metadata.AddField("Actor", actorName);
                        break;
                    case "source":
                    case "source_site":
                        metadata.Providers.Add(value);
                        break;
                    case "tag":
                        metadata.Tags.Add(value);
                        break;
                    case "genre":
                        metadata.Genres.Add(value);
                        break;
                    case "studio":
                        metadata.Studios.Add(value);
                        break;
                    case "director":
                        metadata.Directors.Add(value);
                        break;
                    case "writer":
                    case "credits":
                        metadata.Writers.Add(value);
                        break;
                    case "country":
                        metadata.Countries.Add(value);
                        break;
                    case "language":
                        metadata.Languages.Add(value);
                        break;
                    case "mpaa":
                        metadata.ContentRatings.Add(value);
                        break;
                    case "year":
                        metadata.ProductionYears.Add(value);
                        break;
                    case "customfield":
                        ReadCustomField(element, metadata);
                        break;
                    default:
                        break;
                }

                if (element.Name.LocalName is not "actor" and not "customfield" && !element.HasElements)
                {
                    metadata.AddField(element.Name.LocalName, value);
                }
            }

            return metadata.Cleaned();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return NfoMetadata.Empty;
        }
    }

    private static void ReadCustomField(XElement element, NfoMetadata metadata)
    {
        var label = Clean(element.Elements().FirstOrDefault(child => child.Name.LocalName == "label")?.Value)
            .ToLowerInvariant();
        var value = Clean(element.Elements().FirstOrDefault(child => child.Name.LocalName == "value")?.Value);
        metadata.AddField(label, value);
        switch (label)
        {
            case "provider":
            case "source site":
                metadata.Providers.Add(value);
                break;
            case "network":
                metadata.Networks.Add(value);
                break;
            case "composer":
            case "music & lyrics":
                metadata.Composers.Add(value);
                break;
            case "producer":
            case "executive producer":
                metadata.Producers.Add(value);
                break;
        }
    }

    private static string? FindNfoPath(BaseItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Path))
        {
            return null;
        }

        var matchingSidecar = System.IO.Path.ChangeExtension(item.Path, ".nfo");
        return File.Exists(matchingSidecar) ? matchingSidecar : null;
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;
}

/// <summary>Read-only values found in a local NFO sidecar.</summary>
public sealed class NfoMetadata
{
    /// <summary>Gets the empty metadata value.</summary>
    public static NfoMetadata Empty { get; } = new();

    /// <summary>Gets provider values.</summary>
    public List<string> Providers { get; } = [];

    /// <summary>Gets network values.</summary>
    public List<string> Networks { get; } = [];

    /// <summary>Gets tags.</summary>
    public List<string> Tags { get; } = [];

    /// <summary>Gets actor names.</summary>
    public List<string> Actors { get; } = [];

    /// <summary>Gets genres.</summary>
    public List<string> Genres { get; } = [];

    /// <summary>Gets studios.</summary>
    public List<string> Studios { get; } = [];

    /// <summary>Gets directors.</summary>
    public List<string> Directors { get; } = [];

    /// <summary>Gets writers.</summary>
    public List<string> Writers { get; } = [];

    /// <summary>Gets composers.</summary>
    public List<string> Composers { get; } = [];

    /// <summary>Gets producers.</summary>
    public List<string> Producers { get; } = [];

    /// <summary>Gets countries.</summary>
    public List<string> Countries { get; } = [];

    /// <summary>Gets languages.</summary>
    public List<string> Languages { get; } = [];

    /// <summary>Gets content ratings.</summary>
    public List<string> ContentRatings { get; } = [];

    /// <summary>Gets production years.</summary>
    public List<string> ProductionYears { get; } = [];

    /// <summary>Gets every usable direct field found in the NFO sidecar.</summary>
    public Dictionary<string, List<string>> Fields { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Adds a direct NFO field value without changing the sidecar.</summary>
    public void AddField(string? fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var name = fieldName.Trim();
        if (!Fields.TryGetValue(name, out var values))
        {
            values = [];
            Fields[name] = values;
        }

        values.Add(value.Trim());
    }

    /// <summary>Returns de-duplicated values.</summary>
    public NfoMetadata Cleaned()
    {
        Clean(Providers);
        Clean(Networks);
        Clean(Tags);
        Clean(Actors);
        Clean(Genres);
        Clean(Studios);
        Clean(Directors);
        Clean(Writers);
        Clean(Composers);
        Clean(Producers);
        Clean(Countries);
        Clean(Languages);
        Clean(ContentRatings);
        Clean(ProductionYears);
        foreach (var values in Fields.Values)
        {
            Clean(values);
        }

        foreach (var fieldName in Fields.Where(pair => pair.Value.Count == 0).Select(pair => pair.Key).ToArray())
        {
            Fields.Remove(fieldName);
        }
        return this;
    }

    private static void Clean(List<string> values)
    {
        var deduplicated = values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        values.Clear();
        values.AddRange(deduplicated);
    }
}
