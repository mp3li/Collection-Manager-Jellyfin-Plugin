# Changelog

## [Unreleased]

### Fixed

- Removed request-time person-image discovery from Crew and other person tag
  pages. It previously ran while the metadata catalog was locked, so opening a
  large person category could block every dashboard tag picker. Person image
  IDs are now captured during metadata scans instead.
- Ordered metadata types as requested: types without a colon first, then
  `NFO: …` types, followed by `Jellyfin: …` types.
- Made metadata tag text inherit Jellyfin's active theme colors rather than a
  hard-coded cyan color, so tags and matching counts remain readable across
  dashboard themes.
- Restored resilient, visible handling for metadata catalog responses in the
  Individual, Combined, and Multi-Match pickers. A failed or delayed request
  now displays its error instead of leaving a permanent loading message.
- Kept the dashboard at its current position when moving between metadata-tag
  pages, and retained the active value-search text while paging.
- Sorted every `Jellyfin: …` metadata type after the regular and NFO metadata
  types in each metadata-type list.
- Pre-indexed every metadata type's tag values and matching-media counts when
  the catalog is saved. Opening a category now uses that saved index instead of
  repeatedly walking every title in the library.
- Replaced the incorrect per-title **My Metadata Tags** renderer with the
  actual metadata-value picker. Each library now loads its tag categories, and
  every expanded category shows only its real tags, matching-media counts,
  search, alphabetical results, and fifty-tag pagination.
- Saved the last completed metadata catalog in Collection Manager's local
  Jellyfin plugin data. **My Metadata Tags** now shows its local completion
  date and time and reuses that scan after dashboard reloads and server
  restarts, rather than requiring a fresh scan.
- Replaced the unreadable all-tag-type metadata table in **My Metadata Tags**.
  Every selected library now has separate, independently expandable metadata
  type sections; each section shows only actual metadata tags, their matching
  media counts, search, and fifty-tag pagination.
- Corrected the repository manifest for Jellyfin 10.11.11: it now uses the
  exact `10.11.11.0` target ABI and Jellyfin's required MD5 release-package
  checksum format.
- Pointed the repository card at the full Collection Manager promotional image
  instead of the small transparent icon.
- Reworked the Collection Manager icon and matching promotional graphic with
  smooth, anti-aliased edges while retaining the shared Media Tagging Manager
  visual system and background treatment.

### Changed

- Added separate metadata-type and metadata-tag searches above the saved-scan
  library overview, changed collection pickers to a two-column layout where
  there is room, and made the Individual draft's other-library scope a proper
  Yes/No section with its Select All and Select None controls.
- Renamed the plugin, project, assembly, dashboard route, manifest, branding,
  license, and public repository to Collection Manager.

### Added

- Completed the current four-tab dashboard release: Main Settings, Individual
  Tag Collections, Combined Tag Collections, and Multi-Match Tag Collections.
- Added the full lazy metadata picker to Combined and Multi-Match: collapsed
  libraries and metadata types, on-demand values, search, alphabetical order,
  fifty-value pagination, matching counts, and Jellyfin person images.
- Added live combined union and multi-match intersection previews grouped by
  library, with a shared source-plus-additional-library scope.
- Added in-page same-title conflict choices for all collection-creation
  workflows; existing collections are never silently changed or duplicated.
- Added progress, current-action status, and completion outcomes to the
  one-draft collection workflows.

- Updated the product goals and private testing tracker to make the current
  four-tab release boundary explicit.
- Kept metadata cataloging and collection matching server-side, bounded, and
  based on the most recent completed scan.
- Updated package wording to describe the current release rather than planned
  automatic-collection areas.
