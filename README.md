<h1 align="center">Media Collection Manager</h1>

<p align="center">
  A Jellyfin server plugin for creating, filling, and keeping ordinary media collections organized from the metadata already in your own library.
</p>

<p align="center">
  <img alt="Status: Initial Development" src="Assets/Badges/status.svg" />
  <img alt="Interface: Jellyfin Server Dashboard" src="Assets/Badges/interface.svg" />
  <img alt="Collections: Standard Jellyfin Collections" src="Assets/Badges/collections.svg" />
  <img alt="Automation: Optional Metadata Sync" src="Assets/Badges/automation.svg" />
  <img alt="Bulk Tools: Create, Add and Remove" src="Assets/Badges/bulk-tools.svg" />
  <img alt="Target: Jellyfin 10.11.x" src="Assets/Badges/target.svg" />
</p>

## Table of Contents

<details>
<summary>Open Table of Contents</summary>

<br />

- [About the Project](#about-the-project)
- [What the Plugin Does](#what-the-plugin-does)
- [Collection Sources](#collection-sources)
- [Visual Dashboard Workflow](#visual-dashboard-workflow)
- [Metadata Watching and Scheduling](#metadata-watching-and-scheduling)
- [Using It with Media Tagging Manager](#using-it-with-media-tagging-manager)
- [Normal Jellyfin Collection Controls](#normal-jellyfin-collection-controls)
- [Requirements](#requirements)
- [Build and Install](#build-and-install)
- [Settings](#settings)
- [How Rule Matching Works](#how-rule-matching-works)
- [Safe Removal and Editing](#safe-removal-and-editing)
- [Project Structure](#project-structure)
- [Project Tracking](#project-tracking)
- [Known Limitations](#known-limitations)
- [Responsible Library Use](#responsible-library-use)
- [License](#license)

</details>

## About the Project

Media Collection Manager makes one of the least pleasant parts of maintaining a personal Jellyfin library much less manual: turning the tags and metadata already attached to media into collections that are actually useful for rediscovery.

It is built around a simple principle. The plugin creates **normal Jellyfin collections**, not a separate collection format or a look-alike screen. After a collection has been made, it appears with the rest of the server's collections and keeps its regular Jellyfin image, metadata, sharing, delete, and three-dot-menu behavior.

You can use it entirely on its own. It also pairs naturally with Media Tagging Manager: that plugin can find and write provider, network, and other tags; this plugin can turn those tags into one or many maintained collections.

## What the Plugin Does

- Adds a visual **Media Collection Manager** page to the Jellyfin server dashboard.
- Creates one automatic collection from one or more selected metadata values.
- Bulk-creates a separate collection for every selected provider, network, genre, tag, cast member, studio, director, composer, or production year.
- Lets you choose values found in the actual current server library or type custom values yourself.
- Lets you edit, pause, run, or stop managing saved rules.
- Searches the local library, lets you select many media items, and creates a normal manual collection in one action.
- Adds or removes the selected media items from an existing collection in one action.
- Creates the collection if an automatic rule has not created it yet.
- Adds newly matching media and, when enabled, removes media that no longer matches.
- Watches Jellyfin library item additions, updates, and removals, including metadata loaded from changed NFO files after Jellyfin processes them.
- Provides a manual **Run all enabled rules now** button and a dashboard Scheduled Task.

## Collection Sources

The dashboard can make collections from these local Jellyfin metadata fields:

| Source | What it uses | Typical use |
| --- | --- | --- |
| Provider | Existing NFO provider source fields and `Provider: ` Jellyfin tags | A `BroadwayHD`, `MarqueeTV`, `OperaVision`, `Netflix`, or other provider value already in the library. |
| Network | Existing NFO network fields and `Network: ` Jellyfin tags | A `BBC`, `HBO`, or other existing network value. |
| Any tag | Jellyfin tags | Your own discovery tags, moods, formats, themes, or labels. |
| Genre | Jellyfin genres | Genre shelves such as `Opera`, `Crime`, or `Documentary`. |
| Cast member | Local people credits with the Actor role | An actor-focused collection. |
| Studio | Local studio metadata | Studio or production-company collections. |
| Director | Local people credits with the Director role | Director collections. |
| Composer | Existing NFO `Composer` or `Music & Lyrics` custom fields | Composer collections, including performance libraries. |
| Writer | Local Writer credits and existing NFO writer/credits fields | Writer or librettist collections. |
| Producer | Existing NFO `Producer` and `Executive Producer` custom fields | Producer collections. |
| Country / production location | Existing Jellyfin/NFO country metadata | Country or production-location collections. |
| Language | Existing Jellyfin/NFO language metadata | Original-language collections. |
| Content rating | Existing Jellyfin/NFO content rating | Certification-based collections. |
| Production year | Local production year | A year-based shelf. |
| Other Jellyfin field | Existing usable scalar fields assigned to the item | A title, original title, rating, date, or another local scalar field shown by the dashboard. |
| Other local NFO field | Existing direct NFO elements and custom fields | A niche sidecar field that Jellyfin does not normally surface. |

Provider and Network are deliberately separate collection sources. Media Collection Manager reads the provider and network metadata that already exists in the user's library, including provider values such as OperaVision, BroadwayHD, and MarqueeTV written by the user's downloader and tagging tools. It recognizes Media Tagging Manager's established `Provider: <name>` and `Network: <name>` tags, shows the clean `<name>` value in the correct separate picker, and does not fetch, create, alter, or guess metadata categories.

For the Media Metadata and Extras Getter and Live Performance Metadata and Extras Getter specifically, the plugin also reads the matching adjacent NFO sidecar **without writing to it**. This preserves useful existing fields that Jellyfin does not always expose as a normal library property, including `source`/`source_site`, Composer, Music & Lyrics, Producer, and Executive Producer.

For a field outside the named sources, choose **Other Jellyfin metadata field** or **Other local NFO field**. The dashboard first asks for the existing field name, then shows the values found for that field. This is still read-only organization: it never imports, changes, or creates the metadata.

## Visual Dashboard Workflow

Open **Dashboard → Plugins → Media Collection Manager** after installing and restarting Jellyfin.

### Make one maintained collection

1. Enter the collection name you want to see in Jellyfin.
2. Choose a source such as **Provider**, **Network**, **Genre**, or **Cast member**.
3. Select one or more values from the library list, or enter comma-separated custom values.
4. Leave **Keep this collection in sync automatically** enabled if it should follow later metadata changes.
5. Choose whether the plugin should remove media after it no longer matches.
6. Save the rule, then run it now or let the watcher/scheduled task do it.

Multiple values in one rule use **OR** matching: a collection with `Drama` and `Mystery` contains items with either genre.

### Make many collections at once

Use **Bulk create by metadata** when you want one collection for every selected value. For example, select five genres to make five genre collections, or select every provider value to make a full provider shelf. The plugin saves a rule for each selected value and fills it immediately.

### Make or change a manual collection in bulk

Use the manual search area to find titles, tick as many as needed, and either:

- create a new normal Jellyfin collection from the selection; or
- select an existing collection and bulk add or remove the selection.

This supplements the normal one-at-a-time add-to-collection flow; it does not remove or override it.

## Metadata Watching and Scheduling

The plugin has two optional paths for keeping enabled rules current.

### Watch library changes

When **Watch library additions and metadata edits** is on, Media Collection Manager subscribes to Jellyfin library item added, updated, and removed events. It waits ten seconds after the last event in a burst, then reconciles enabled rules once. This avoids repeatedly rebuilding the same collection while a library scan or tagger updates many items.

It reacts after Jellyfin has processed the library update, so it works with changed tags, genres, people, studios, production years, and NFO-backed metadata. Collection events themselves are ignored, preventing the plugin from creating a loop when it updates collection membership.

### Scheduled reconciliation

The plugin also registers **Reconcile Media Collection Manager rules** under Dashboard → Scheduled Tasks. Jellyfin checks it hourly; the dashboard's **Minimum minutes between scheduled checks** setting controls how often it actually performs a reconciliation. Set the interval from 15 minutes up to seven days, or turn scheduled reconciliation off entirely.

The saved rule list and the Scheduled Tasks page both provide a manual way to run now.

## Using It with Media Tagging Manager

The two plugins have separate jobs:

```text
Media Tagging Manager
    discovers or refreshes metadata and writes Jellyfin tags
                         ↓
Media Collection Manager
    watches the library update and reconciles matching collections
                         ↓
Normal Jellyfin collections
    stay available for browsing and ordinary three-dot-menu edits
```

No special integration switch, API key, or shared database is required. Existing provider/network metadata from Media Tagging Manager, the user's downloader, or local NFO sidecars appears as collection source values here. You can also use the collection plugin with ordinary existing Jellyfin tags.

## Normal Jellyfin Collection Controls

Every collection made by this plugin is created through Jellyfin's standard collection manager. Therefore Jellyfin's native collection controls continue to be the source of truth for collection artwork, images, metadata editing, user-facing presentation, sharing, and deletion.

The plugin focuses on the missing bulk and metadata-driven workflow. It deliberately does not replace Jellyfin's collection detail page or three-dot menu with a fragile duplicate UI.

Editing a saved rule's collection name also renames its tracked Jellyfin collection. If you rename, delete, or otherwise manually change a managed collection through Jellyfin, review its saved rule before the next reconciliation. Stopping management removes only the rule; it intentionally leaves the existing normal Jellyfin collection untouched.

## Requirements

- Jellyfin Server **10.11.x** with the matching plugin ABI.
- The `.NET 9` runtime supplied by a compatible Jellyfin Server installation.
- Administrator access to install plugins and use the dashboard page.
- Local metadata in the library for the fields you want to use. A source cannot create a useful collection if none of the library items have that source value.

The project uses `Jellyfin.Controller` and `Jellyfin.Model` version `10.11.10`. When packaging for a different Jellyfin server version, update both package references and [`Jellyfin Package/build.yaml`](Jellyfin%20Package/build.yaml)'s `targetAbi` to match that server.

## Build and Install

Build the plugin from this repository:

```bash
cd "/path/to/Media Collection Manager"
dotnet restore "Media Collection Manager/MediaCollectionManager.csproj"
dotnet build "Media Collection Manager/MediaCollectionManager.csproj" --configuration Release
```

Copy the published DLL into a dedicated Jellyfin plugin folder, restart the server, then open the dashboard page:

```text
Media Collection Manager/bin/Release/net9.0/Jellyfin.Plugin.MediaCollectionManager.dll
```

For distribution, package the DLL with the matching [`build.yaml`](Jellyfin%20Package/build.yaml) metadata. Jellyfin plugins must be compiled against the same compatible server packages; a package mismatch can make Jellyfin mark a plugin unsupported.

## Settings

| Setting | Default | Effect |
| --- | --- | --- |
| Watch library additions and metadata edits | On | Reconciles enabled rules after item changes settle. |
| Also check on a schedule | On | Allows the Scheduled Task to reconcile enabled rules. |
| Minimum minutes between scheduled checks | 360 | Limits real reconciliation frequency from 15 to 10,080 minutes. |
| Keep this collection in sync automatically | On per rule | Includes a saved rule in watcher and scheduled runs. |
| Remove items that no longer match | On per rule | Removes outdated memberships during reconciliation. Turn it off for add-only rules. |
| Use metadata from | Both | Limits matching and discovery to existing Jellyfin metadata, matching local NFO sidecars, or both. |
| Source seniority | Jellyfin | In both mode, orders overlapping source values with Jellyfin or local NFO first while retaining values available from only one enabled source. |
| Use all libraries | On | Uses every library by default; turn it off to select specific library roots for discovery, rules, and manual search. |

## How Rule Matching Works

- Matching is case-insensitive and uses exact local metadata values.
- Multiple values inside one rule are combined with **OR**.
- An empty value list never matches every item by mistake; the dashboard rejects it.
- A rule creates or claims one tracked standard collection and remembers its Jellyfin id.
- Existing membership is compared against the desired item ids on every run.
- The plugin adds only missing matches and removes only items that no longer match when the removal setting is on.
- The plugin does not modify the media files, NFO files, tags, genres, people, or artwork. It only manages collection membership.

## Safe Removal and Editing

There are two intentionally separate operations:

- **Edit rule** changes the collection name, which local metadata the automation follows, whether it runs, and whether it removes stale matches.
- **Stop managing** deletes only the plugin rule. It does **not** delete the Jellyfin collection, media, local metadata, images, or any manually created collection.

Use Jellyfin's normal collection controls for artwork, images, metadata, and final presentation changes. This keeps the plugin's job limited to rules and bulk membership rather than trying to copy every native collection feature.

## Project Structure

```text
Media Collection Manager/
├── Api/
│   └── MediaCollectionManagerController.cs  # dashboard API for rules and bulk actions
├── Configuration/
│   ├── CollectionRule.cs                    # persistent rule model
│   ├── PluginConfiguration.cs               # dashboard/watch/schedule settings
│   └── configPage.html                      # embedded visual Jellyfin dashboard page
├── Models/                                  # API request and response types
├── Services/
│   ├── CollectionReconciler.cs              # real Jellyfin collection creation and sync
│   ├── MetadataChangeListener.cs            # debounced library-item watcher
│   └── ServiceRegistrator.cs                # Jellyfin dependency registration
├── Tasks/
│   └── ReconcileCollectionsTask.cs          # Dashboard Scheduled Task
├── Plugin.cs                                # plugin entry point and configuration page
└── MediaCollectionManager.csproj

Assets/
└── Badges/                                  # locally rendered Jellyfin-gradient README badges
Documentation/
├── CHANGELOG.md                            # user-visible changes
├── project-goals.txt                       # product-delivery checklist
└── goal-testing.txt                        # private test results tracker
Jellyfin Package/
└── build.yaml                              # Jellyfin package metadata
```

## Project Tracking

The project documents are deliberately separated by purpose:

- [`Documentation/project-goals.txt`](Documentation/project-goals.txt) records the features and boundaries the plugin is meant to deliver.
- [`Documentation/goal-testing.txt`](Documentation/goal-testing.txt) records private installation, behavior, safety, and release checks, including the results you report while testing.
- [`Documentation/CHANGELOG.md`](Documentation/CHANGELOG.md) records user-visible changes over time.

## Known Limitations

- This first implementation targets Jellyfin 10.11.x. It must be rebuilt against the exact compatible server packages before using it on another ABI.
- Provider rules read existing provider source metadata and `Provider: ` tags, not remote-provider ids such as TMDb or IMDb ids. Network rules read existing network metadata and `Network: ` tags. This is intentional: both are local-library organization values, not external lookup data.
- Rules use all server libraries by default. The dashboard can instead restrict the plugin to selected library roots; that scope applies to source discovery, rules, and manual-media search.
- Provider and Network are intentionally separate collection sources.
- Niche NFO fields are read only from the matching sidecar whose basename matches the media filename. They are not written, renamed, or normalized by this plugin.
- The dashboard can read Jellyfin metadata only, matching local NFO sidecars only, or both. In both mode, it lets the administrator choose which source is presented first while retaining values that appear only in the other enabled source.
- A metadata edit made completely outside Jellyfin is visible to the watcher only after Jellyfin scans or otherwise loads that changed item.
- The plugin does not download metadata, decide what a provider/network should be, or invent missing values. Media Tagging Manager or your own local metadata should handle that separate task.

## Responsible Library Use

Media Collection Manager works only with the Jellyfin library and metadata already under your server's control. It does not access streaming services, scrape provider sites, download media, or transmit library metadata to an outside service. Review automatic rules before enabling removal: a broad tag or a typo can legitimately cause a rule to change many memberships.

## License

Media Collection Manager is available under the [Media Collection Manager
Non-Commercial Source-Available License](LICENSE). It may be studied, adapted,
and shared for non-commercial use with attribution to mp3li. Commercial rights
are reserved.
