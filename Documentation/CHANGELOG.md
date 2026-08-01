# Changelog

## [0.1.0.66] - 2026-08-01

### Added

- Saves every new Collection Manager collection's creation-tab settings, then
  shows that exact editable tab under its current contents in the collection
  editor.
- Adds text-focused and poster-focused artwork application for Jellyfin
  libraries.

### Changed

- The existing Collection Manager reconciliation task now fully synchronizes
  saved creation settings against live Jellyfin and NFO metadata: it adds new
  matches and removes media that no longer matches.
- Newly Added Media Settings now controls automatic event and scheduled
  synchronization for saved creation settings.

## [0.1.0.65] - 2026-07-31

### Fixed

- Persists Jellyfin's image-update state after Collection Manager applies a
  collection image, so its saved image dimensions update with the artwork.
- Restores the visible native checkbox state for tag search results.
- Normalizes standard Jellyfin and NFO metadata field names consistently in
  every collection-creation tag picker.

### Added

- Adds **Repair Primary Image Metadata** under Clean Up Settings. It resaves
  each existing collection's exact current Primary image solely to make
  Jellyfin recalculate and persist its dimensions.

## [0.1.0.64] - 2026-07-31

### Fixed

- Defers the expensive collection-art list load and native checkbox rendering
  until the corresponding art tab is opened.
- Makes Multi-Collection/Library Gradient Art preview tiles match the selected
  Jellyfin art type's aspect ratio.

### Added

- Lets you drag gradient preview tiles to set the applied gradient order.

## [0.1.0.63] - 2026-07-31

### Fixed

- Keeps searched metadata-tag checkbox rows stable until their complete result
  list is ready, so they can be selected normally.
- Lets Multi-Collection/Library Gradient Art checkboxes update immediately and
  consolidates rapid gradient-preview refreshes instead of rebuilding the
  preview on every click.

## [0.1.0.62] - 2026-07-31

### Fixed

- Restores normal native checkbox selection for tags shown by the collection
  picker search.
- Shows each uploaded logo as a selectable imported-logo row for every chosen
  collection, so it is used in the preview and when applying logo art.

## [0.1.0.61] - 2026-07-31

### Fixed

- Keeps library groups automatically shown in collection-creation pickers, but
  restores the existing collapsed metadata tag types. The shared search box
  searches both tag type names and tag values.

## [0.1.0.60] - 2026-07-31

### Fixed

- Replaces the single oversized all-tags request in collection-creation tabs
  with the existing small metadata-value pages. Tag type sections appear first
  and all tag values fill in progressively without requiring expansion.

## [0.1.0.59] - 2026-07-31

### Changed

- Makes every Collection Overview input match the Collection Title input and
  places it immediately below Collection Title.

## [0.1.0.58] - 2026-07-31

### Fixed

- Refreshes the Logo Focused tab's collection picker every time the tab opens,
  while retaining checks for collections that still exist.

## [0.1.0.57] - 2026-07-31

### Added

- Adds an Overview field to manual, individual-tag, combined-tag, and
  multi-match collection drafts, and to the existing Collection Overview &
  Editor dialog.
- Shows all scanned tags from saved libraries in one searchable picker on each
  tag-collection creation tab while retaining the existing per-draft
  other-library choices.

### Changed

- Treats episodes and seasons as their parent Series during metadata scans, so
  tag-driven collection previews and created collections use the Series item
  instead of individual episodes.

## [0.1.0.56] - 2026-07-31

### Added

- Adds optional complete Collection Backup Settings, including backup creation,
  selection, renaming, deletion, and restore.
- Captures every native collection's members, restorable metadata and lock
  settings, provider IDs, display and metadata preferences, and all readable
  locally stored collection image files.
- Adds an explicit restore toggle for deleting current collections that are not
  present in the selected backup. It remains off unless the administrator turns
  it on for that restore.

## [0.1.0.55] - 2026-07-31

### Fixed

- Uses the same inline logo placement as the Media Tagging Manager picker, so
  logos stay in their own native checkbox row and cannot overlap at the right.

## [0.1.0.54] - 2026-07-30

### Fixed

- Restores the exact native Jellyfin checkbox spacing for logo rows so the
  checkbox outline cannot overlap the selected logo.

## [0.1.0.53] - 2026-07-30

### Fixed

- Corrects the release asset URL, checksum, and plugin DLL version so Jellyfin
  installs the exact package described by the repository manifest.
- Shows saved Primary collection art in Collections Overview & Editor and uses
  Jellyfin's native checkbox structure for logo selections.
- Makes dashboard borders and dividers visible in light mode using the
  description-text color, and removes the primary tab-strip divider.

## [0.1.0.51] - 2026-07-30

### Fixed

- Sends font imports and rendered collection art as real authenticated multipart
  Jellyfin uploads, rather than empty form requests.
- Returns a JSON acknowledgement when a logo selection is cleared, so Unselect
  Logo refreshes the draft correctly.
- Redraws selected-logo previews when their background style changes and adds
  more space between the logo picker and preview.

### Added

- Added Save Gradient Settings, which saves only the gradient tool's settings
  without applying artwork.

## [0.1.0.50] - 2026-07-30

### Fixed

- Shows the actual HTTP, validation, and server exception details when a font
  import or generated-art application fails.
- Rebuilt each logo draft as a non-overlapping two-column layout with a compact
  search field and preview beside the picker.
- Added per-logo rounded corners, reflected immediately in the preview and in
  the art generated by the existing apply action.
- Capitalizes one-word metadata labels, labels metadata-type counts as tags,
  and places Select Gradient above the gradient collection picker.

## [0.1.0.49] - 2026-07-30

### Fixed

- Restored the original artwork-preview size.
- Corrected form submission for Jellyfin's successful empty image-save response
  and supplied the authenticated asset URLs required by browser-loaded fonts.
- Rendered cached provider, network, and production-company logos using the
  same authenticated URL pattern as Media Tagging Manager, and added a small
  artwork preview beside each selected logo collection.
- Grouped metadata type pickers with the requested source headings and divider;
  NFO and Jellyfin fields now sort together alphabetically and compact names
  such as `releasedate` and `dateadded` display as `Release Date` and
  `Date Added`.

## [0.1.0.48] - 2026-07-30

### Fixed

- Stopped combined and multi-match tag selection from rebuilding the tab,
  collapsing selectors, flashing content, or moving the dashboard scroll.
- Added default-font fallback, saved text-size controls, clearer upload errors,
  numeric native Jellyfin image-type submission, and normalized metadata-type
  display labels.
- Removed Image as an artwork background-style choice and reduced artwork
  preview sizes by half.

## [0.1.0.47] - 2026-07-30

### Added

- Implemented the New Collection Art Settings, Text Focused Collection Art,
  Poster Focused Collection Art, Logo Focused Collection Art, and
  Multi-Collection/Library Gradient Art tabs.
- Added Collection Art Preferences to every new collection draft and native
  Jellyfin image application for the selected artwork type.
- Added Delete Collection controls to Collections Overview & Editor.

### Fixed

- Preserved expanded combined-tag selectors and dashboard scroll position when
  selecting metadata tags.
- Moved the combined-tag additional-library controls below the complete
  library list.

## [0.1.0.46] - 2026-07-29

### Fixed

- Allowed each native Jellyfin manual-media checkbox label to grow to its
  actual poster-and-description height. This preserves the supported checkbox
  initialization from 0.1.0.45 without overlapping media rows or artwork.

## [0.1.0.45] - 2026-07-29

### Fixed

- Corrected manual-media picker markup to follow Jellyfin 10.11.11's required
  `emby-checkbox` sibling order: checkbox input, label-text span, then the
  component-generated outline. This fixes the abnormal first-item checkbox on
  every manual-library page.

## [0.1.0.44] - 2026-07-29

### Fixed

- Rebuilt manual-media selection rows with the standard single Jellyfin
  `checkboxContainer` and `emby-checkbox` structure. Every item, including the
  first item in each manual-library page, now has the same normal-size
  selectable square.

## [0.1.0.43] - 2026-07-29

### Fixed

- Gave the Create Manual Collections Previous Page and Next Page controls
  explicit references after their requested relocation above the media results.
  Next Page works again, and the small Page _ out of _ text remains directly
  to its right.
- Made manual-media overviews reserve five lines, clipping longer descriptions
  with an ellipsis so they cannot overlap the next item.

## [0.1.0.42] - 2026-07-29

### Changed

- Moved the Create Manual Collections library controls directly below Search
  media, before the media results, in this order: Previous Page, Select All,
  Select None, Next Page.
- Made those four controls use the same `mcm-library-heading` layout and
  ordinary `raised` button markup as the manual-library expand/collapse button.
- Removed scroll-position manipulation from those four control handlers.

## [0.1.0.41] - 2026-07-29

### Fixed

- Made the Create Manual Collections Previous Page, Select All, Select None,
  and Next Page controls use the same shared ordinary `raised` button renderer
  as the Select Metadata Tags Previous Page and Next Page controls.
- Replaced the manual picker’s separate scroll-container handling with the
  dashboard page-position restore routine already used by metadata-tag paging.

## [0.1.0.40] - 2026-07-29

### Fixed

- Replaced the manual picker pager controls with the exact existing Select
  Metadata Tags pagination markup: the shared `mcm-pagination` container and
  ordinary `raised` buttons, with no manual-only classes, inline styles, or
  color overrides.
- Stopped recreating the manual picker search field and pager during page or
  selection changes. Only the ten-item media-results area now redraws, while
  the existing pager remains in the page.

## [0.1.0.39] - 2026-07-29

### Fixed

- Force all four Create Manual Collections pager controls to neutral gray in
  normal, hover, focus, and active states rather than allowing Jellyfin's blue
  interaction state to override them.
- Replaced pager-position offset scrolling with a literal save-and-restore of
  Jellyfin's active scroll container position for Previous Page, Next Page,
  Select All, and Select None.

## [0.1.0.38] - 2026-07-29

### Fixed

- Applied an explicit normal gray button treatment to the Create Manual
  Collections pager controls.
- Kept the manual picker pager below the media list and preserve its visible
  screen position when a page redraw changes the heights of the ten displayed
  media entries. Paging now uses Jellyfin's actual scrolling container instead
  of the browser window.

## [0.1.0.37] - 2026-07-29

### Fixed

- Manual collection Select All and Select None now operate on the current
  ten-item page only, including its first media picker. The controls use the
  existing standard page-button wording and do not restore the browser window
  scroll position.
- Moved Logo Focused Art Settings to the second tab row; Multi-Collection
  Gradient Art remains on the third row.

## [0.1.0.36] - 2026-07-29

### Added

- Added five intentionally blank dashboard-tab placeholders for the next art
  planning round: Default Art Settings, Text Focused Art Settings, Poster
  Focused Art Settings, Logo Focused Art Settings, and Multi-Collection
  Gradient Art. Logo Focused Art Settings is on the second tab row and
  Multi-Collection Gradient Art occupies the third tab row.

### Changed

- Create Manual Collections now pages each expanded library picker ten items at
  a time, adds a library search field, and provides Select All and Select None
  controls between the previous and next page controls.
- Manual-library Select All and Select None now apply only to the ten visible
  choices on the current page. Paging uses the standard gray Previous Page and
  Next Page controls and does not issue a window scroll restoration that can
  move the dashboard to an unrelated position.
- Renamed manual music picker labels from MP3 and MP3 Album to **Audio** and
  **Audio Album**.

## [0.1.0.35] - 2026-07-29

### Fixed

- Removed the redundant introductory sentence from the Collection Overview &
  Editor tab; its first visible section is now Selected Collections Settings.
- Changed Create Manual Collections to query each library through Jellyfin's
  typed item query with the configured library content type. The picker now
  requests collection-level items only: Series for TV, Movies for movie
  libraries, Books and Audiobooks for book libraries, and the appropriate
  music or video item kinds for their libraries. Folders, seasons, episodes,
  and video extras are not selectable.
- Renamed music picker types to **MP3** and **MP3 Album**, and decode rich-text
  library descriptions to readable plain text before displaying them.

## [0.1.0.34] - 2026-07-29

### Fixed

- Moved Collection Overview & Editor into the same dedicated secondary-tab row
  styling used by Media Tagging Manager, below the primary row rather than
  merely placing it directly after Main Settings.
- Restored the reliable recursive library query after direct virtual-library
  queries returned no selectable media. It now explicitly filters Episodes,
  Seasons, and Jellyfin video extras while retaining each Series as one
  selectable item.
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
