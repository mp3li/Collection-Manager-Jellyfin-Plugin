# Changelog

## [Unreleased]

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

### Changed

- Updated the product goals and private testing tracker to make the current
  four-tab release boundary explicit.
- Kept metadata cataloging and collection matching server-side, bounded, and
  based on the most recent completed scan.
- Updated package wording to describe the current release rather than planned
  automatic-collection areas.
