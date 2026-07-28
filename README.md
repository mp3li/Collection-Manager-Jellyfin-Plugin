<p align="center">
  <img src="Assets/Branding/collection-manager-icon.png" alt="Collection Manager icon: a Jellyfin-style television containing an open collection box" width="180" />
</p>

<h1 align="center">Collection Manager</h1>

<p align="center">
  <img src="Assets/Badges/status.svg" alt="Early testing build" />
  <img src="Assets/Badges/target.svg" alt="Jellyfin 10.11.11" />
  <img src="Assets/Badges/collections.svg" alt="Native collections" />
  <img src="Assets/Badges/bulk-tools.svg" alt="Bulk tools" />
</p>

<p align="center"><strong>⚠️ Early testing build:</strong> this plugin is for private testing on Jellyfin 10.11.11. Keep ordinary server backups and do not treat an untested build as a production release.</p>

<p align="center">A companion to <a href="https://github.com/mp3li/Media-Tagging-Manager-Jellyfin-Plugin">Media Tagging Manager</a>, built to turn the metadata already in your Jellyfin libraries into normal Jellyfin collections.</p>

## What it is for

Building a useful collection in Jellyfin should not mean adding titles one at a
time. Collection Manager scans the metadata that already exists in the
libraries you choose and gives you reviewed, bulk collection drafts instead.

Metadata is descriptive information already connected to a media item: genres,
providers, networks, studios, cast, crew, directors, composers, years,
languages, ratings, custom tags, fields in local NFO files, and more. Provider
and Network are intentionally separate metadata types. A library can therefore
have a `Provider: MarqueeTV` tag, a `Network: HBO` tag, an `Opera` genre, a
production company, and any number of other values without this plugin treating
them as the same thing.

[Media Tagging Manager](https://github.com/mp3li/Media-Tagging-Manager-Jellyfin-Plugin)
can assign many useful existing tags, including provider and network tags. Media
Collection Manager works especially well with those values, but it is not
limited to them: it reads every available metadata value from the selected
libraries.

## Current four-tab build

| Dashboard tab | What it does |
| --- | --- |
| **Main Settings** | Select libraries, save the newly-added-media preference, scan the local read-only metadata catalog, and inspect the saved most-recent scan in a paginated library overview. |
| **Individual Tag Collections** | Select values such as `Horror`, `Netflix`, or `A24` and make one independently titled, reviewed native collection draft for each value. |
| **Combined Tag Collections** | Select two or more values and make one collection containing the unique union of media matching any selected value. |
| **Multi-Match Tag Collections** | Select two or more values and make one collection containing only media matching every selected value. |

All collection workflows provide matching counts, grouped previews, editable
titles, additional-library scope, visible same-title conflict choices, and
creation outcomes. Collections remain standard Jellyfin collections, so their
normal Jellyfin artwork, image, and three-dot-menu controls remain available.

## Metadata and privacy boundary

Collection Manager is read-only with respect to your library metadata. It
does not fetch metadata from the internet, create tags, modify tags, rewrite NFO
files, rename media, move files, or access streaming services. It stores its
latest completed catalog only in Collection Manager's local Jellyfin plugin
data and does all matching on the server. The catalog and dashboard previews do
not expose media paths.

The plugin does not silently duplicate or alter a same-title collection. When a
title already exists, the dashboard shows an in-page choice to use the existing
collection unchanged, skip the draft, or revise the draft title.

## Requirements

- Jellyfin Server **10.11.11**
- Administrator access
- Existing local library metadata
- .NET SDK 9.0 to build from source

The project references Jellyfin.Controller and Jellyfin.Model **10.11.11** and
targets the Jellyfin 10.11 ABI.

## Build and private installation

Build the plugin:

```bash
dotnet build "Collection Manager/CollectionManager.csproj" --configuration Release
```

For a direct private DLL test, copy
`Collection Manager/bin/Release/net9.0/Jellyfin.Plugin.CollectionManager.dll`
to a dedicated Collection Manager directory inside your Jellyfin plugins
directory, restart Jellyfin, then open **Dashboard → Collection Manager**.

For repository-manifest testing, first make a release ZIP containing the exact
Release DLL, publish that ZIP as a GitHub Release asset, calculate its MD5,
and create a manifest whose version, target ABI, framework, source URL,
checksum, timestamp, branding image, and changelog all describe that exact
asset. The current private-testing release is available through this repository
manifest:

```text
https://raw.githubusercontent.com/mp3li/Collection-Manager-Jellyfin-Plugin/main/manifest.json
```

In Jellyfin, open **Dashboard → Plugins → Repositories**, add that URL, then
install the **0.1.0.15 Private Testing** prerelease from the catalog. Restart
Jellyfin and record the real server results in the testing tracker.

## Private test tracking

The four-tab implementation has automated build and JavaScript syntax checks.
Private Jellyfin installation and behavior must still be recorded before calling
the runtime behavior complete. Use the [testing tracker](Documentation/goal-testing.txt)
for the actual server results.

## Project documents

- [Current product goals](Documentation/project-goals.txt)
- [Private testing tracker](Documentation/goal-testing.txt)
- [Changelog](Documentation/CHANGELOG.md)

## License

Collection Manager is available under the [Collection Manager
Noncommercial License 1.0](LICENSE).
