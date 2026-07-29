# Changelog

## [0.1.0.33] - 2026-07-29

### Fixed

- Moved Collection Overview & Editor directly after Main Settings in the tab
  row to reduce tab wrapping.
- Changed Create Manual Collections to use each library's direct Jellyfin
  items instead of a recursive leaf-item query. Television libraries now show
  series as series rather than separate episodes, seasons, deleted scenes, or
  other extras.
- Renamed the single manual-draft section to **Individual Collection Draft**.

## [0.1.0.32] - 2026-07-29

### Added

- Added Create Manual Collections directly after Main Settings. It reuses the
  existing Collection Overview & Editor poster-led media presentation and native
  Jellyfin collection creator: expand a library, choose media with picker
  checkboxes, review one titled draft, remove selected items inline, and create
  a standard collection from any combination of Jellyfin libraries.

### Changed

- Restored the dashboard tab shape to the exact shared Media Tagging Manager
  tab rules. No other tab styling was changed.
- Updated the current goals, private-testing tracker, and README to describe
  the six-tab development scope, the unpaginated collapsed collection overview,
  and the currently published versus unreleased private-testing build boundary.

## [0.1.0.31] - 2026-07-29

### Changed

- Collections Overview & Editor now loads its complete saved collection list at
  once, with every collection collapsed by default. It no longer has overview
  pagination. The Collection Editor and Add to Collection pickers retain their
  independent pagination.

## [0.1.0.30] - 2026-07-29

### Changed

- Removed the unnecessary total-collection and page-size summary from the
  collapsed Collections Overview & Editor list.

## [0.1.0.29] - 2026-07-29

### Fixed

- Applied the compact picker CSS directly to native Jellyfin dialogs, which are
  outside the dashboard page element and therefore were unaffected by the prior
  page-scoped rules.

### Changed

- Collection Editor and Add to Collection pickers now show a two-column grid of
  small poster thumbnails, titles, and checkboxes/radio pickers only. Their
  item descriptions are omitted.

## [0.1.0.28] - 2026-07-29

### Changed

- Made posters in the Collection Editor and Add to Collection pickers compact
  thumbnails beside their selectable titles. Collection overview poster sizing
  is unchanged.

## [0.1.0.27] - 2026-07-29

### Fixed

- Restored the Collection Editor and Add to Collection pickers after a shared
  poster helper rename left their JavaScript rendering path undefined.

### Changed

- Added outlined styling to every dashboard tab for private visual testing.
- Put each collection’s expand/collapse control before Edit Collection beneath
  the collection name.

## [0.1.0.26] - 2026-07-29

### Changed

- Updated Collections Overview & Editor with individually collapsible collection
  sections, a two-column collection-member grid, compact matching color inputs,
  and no visible missing-poster text.

## [0.1.0.25] - 2026-07-29

### Changed

- Replaced the Collection Overview & Editor row renderer with the actual
  Media Tagging Manager Recommendations and Similar Titles renderer structure:
  poster fallback, title, secondary details, overview, and latest-scan
  addition/removal styling. The only collection-specific substitutions are
  **Collection Name**, **Media in Collection**, and **Edit Collection** beneath
  the collection name.
- Removed the unrequested fixed first-column width and action-wrapper spacing.
  The two-column table now uses the same base table sizing, padding, borders,
  poster sizing, and row spacing as the source overview.

## [0.1.0.24] - 2026-07-29

### Changed

- Corrected Collections Overview & Editor to its requested two-column layout:
  **Collection Name**, with Edit Collection directly beneath each name, and
  **Media in Collection**, containing the poster-led media list. Removed the
  unrequested collection-item count column.

### Fixed

- Replaced the custom Collection Overview editor and picker popups with the
  native Jellyfin Web 10.11.11 `Dashboard.dialogHelper` implementation,
  including Jellyfin's `ui-body-a`, `background-theme-a`, and `formDialog`
  classes. The dialog now receives the active Jellyfin theme from the web
  client rather than using custom modal background or text styling.

### Changed

- Used the exact Recommendations and Similar Titles overview table sizing,
  padding, borders, related-title presentation, and pager structure from Media
  Tagging Manager. Collection-specific names and columns remain the only data
  differences; the separately required flat all-collections scope is retained.

### Changed

- Replaced the Collection Overview & Editor table and page-control presentation
  with the actual shared overview CSS and rendering structure from Media Tagging
  Manager's Recommendations and Similar Titles Additions and Removals section.
  Only the collection-specific columns, collection data, and Edit Collection
  control differ.

### Fixed

- Corrected Collection Overview & Editor to use the same poster-led related-title
  presentation as the Media Tagging Manager overview. The saved overview remains
  one flat list of all native Jellyfin collections; it does not divide collections
  by library.
- Corrected the visible collection total to use the returned overview page total,
  rather than a separate count that could incorrectly show zero.
- A first collection scan now establishes a neutral baseline. It no longer marks
  every pre-existing collection and linked title as newly added, and snapshots
  produced by the affected private-testing build are normalized on load.
- Moved the collection editor dialogs into the same themed dashboard content
  container used by Media Tagging Manager, restoring ordinary Jellyfin theme
  background and text colors.

### Changed

- Collection editor media rows and the Add to Collection picker now use Jellyfin
  posters when available. The picker is paginated at 30 collections per page.
- Moved Select All and Select None beside the editor pagination controls.
- Replaced Save Collection Title with Save. Rename, selected removals, and queued
  additions are now applied together only when Save is clicked; Close discards
  unsaved changes.

### Added

- Added Collection Overview & Editor: saved selected-library collection scans,
  collection-type settings, last-scan status, change colors, expandable library
  pages, and native collection title and membership editing.
- Added Main Settings cleanup choices for the last recorded action,
  plugin-recorded collections, and plugin-recorded additions to external
  collections.
- Renamed the three collection-creation tabs to begin with Create and placed
  Select All, Select None, and Save Libraries together after the library picker.

### Fixed

- Replaced Collection Overview & Editor's library-grouped scan with one flat
  all-native-collections scan and overview, as requested.
- Collection scan failures now show only a safe scan stage and exception type,
  while the full exception remains in the Jellyfin server log.
- Replaced the unsupported root recursive item query with Jellyfin's typed
  `BoxSet` query when enumerating native collections on 10.11.11.
- One malformed collection link no longer prevents the complete collection
  overview scan.

### Fixed

- Kept the dashboard at its exact viewport position when using Previous Page
  or Next Page in Main Settings, Individual, Combined, and Multi-Match tag
  views. The controls are also blurred before their list is replaced, avoiding
  Jellyfin's focus-driven scroll jump.
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

- Completed the structural parity correction with Media Tagging Manager:
  removed Collection Manager-only repeated tab-title cards. Only the shared
  repository and Patreon line appears above the tabs, and each panel begins
  with its first functional section using the shared section-card heading.
- Made every section heading consistent and moved outer library
  expand/collapse controls to the left, while leaving nested metadata-type
  controls on the right.
- Added separate metadata-type and metadata-tag searches above the saved-scan
  library overview, changed collection pickers to a two-column layout where
  there is room, and made the Individual draft's other-library scope a proper
  Yes/No section with its Select All and Select None controls.
- Renamed the plugin, project, assembly, dashboard route, manifest, branding,
  license, and public repository to Collection Manager.

### Added

- Added the shared Media Tagging Manager repository and Patreon line above the
  dashboard tabs.
- Removed the unwanted introductory copy from Main Settings and changed its
  Yes/No inputs to Jellyfin's native checkbox control presentation.
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
