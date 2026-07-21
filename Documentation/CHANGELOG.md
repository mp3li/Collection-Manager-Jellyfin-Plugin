# Changelog

All notable user-visible changes to Media Collection Manager are documented in
this file. The project follows a Keep a Changelog-style structure and uses
unreleased entries until a version is packaged for distribution.

## [Unreleased]

### Added

- Renamed the project and Jellyfin plugin to **Media Collection Manager**.
- Added a Jellyfin dashboard configuration page for automatic collection rules,
  bulk tools, manual multi-item collection creation, and membership changes.
- Added automatic collection rules for provider tags, network tags, generic
  tags, genres, actors, studios, directors, composers, and production years.
- Added library-facet discovery and custom comma-separated values for rules.
- Added editable rule and collection names, enabled/paused rules, and optional
  removal of items that no longer match.
- Added bulk creation of one managed collection per selected metadata value.
- Added local media search with multi-select manual collection creation.
- Added bulk add/remove actions for selected media in an existing collection.
- Added real Jellyfin collection-manager integration so plugin-created
  collections retain native Jellyfin controls, including artwork and image
  management.
- Added a debounced watcher for library item additions, metadata updates, and
  removals.
- Added a scheduled reconciliation task, configurable watcher/schedule toggles,
  and a configurable minimum scheduled interval.
- Added read-only matching of adjacent NFO sidecars for existing provider source,
  Composer, Music & Lyrics, Producer, and Executive Producer values.
- Added collection sources for writers, producers, countries/production
  locations, languages, and content ratings, all read from existing metadata.
- Added exact compatibility with Media Tagging Manager's existing `Provider: `
  and `Network: ` tag prefixes; the dashboard presents the clean provider or
  network name in its separate source picker.
- Added visual source settings for Jellyfin metadata only, local NFO sidecars
  only, or both, including source seniority and selected-library scope.
- Added generic usable Jellyfin scalar-field and local-NFO-field collection
  sources, without modifying either metadata source.
- Added project package metadata in `Jellyfin Package/build.yaml`.
- Added the public README plus separate goals and private-results documents.
- Added the Media Collection Manager Non-Commercial Source-Available License.

### Changed

- Replaced the README's flat-color badges with locally rendered Jellyfin-gradient
  badges using `#AA5CC3` through `#00A4DC`.
- Clarified the product direction: Provider and Network remain separate rule
  types and future home-screen sections; they are not a combined-source feature.
- Clarified that additional sources mean organization from existing local
  Jellyfin/NFO metadata only. Media Collection Manager does not fetch, add, or
  alter tags or metadata.
- Clarified that provider/network metadata is already written by the user's
  downloader and tagging tools. The collection plugin must read and organize
  that existing metadata only; it must not create a new tagging convention,
  fetch metadata, or alter tags.
