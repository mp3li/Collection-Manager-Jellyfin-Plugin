<p align="center">
  <img src="Assets/Branding/collection-manager-icon.png" alt="Collection Manager icon: a Jellyfin-style television containing an open collection box" width="180" />
</p>

<h1 align="center">Collection Manager Jellyfin Plugin</h1>

<p align="center">
  A complete native-collection system for Jellyfin: discover every useful value already in your libraries; build collections from selected media, metadata logic, or your existing folder organization; preview and create them in bulk; edit or recreate existing collections; keep metadata-driven membership synchronized; back up, restore, and clean up collection changes; and design coordinated artwork for collections and libraries—all from the Jellyfin administrator dashboard.
</p>

<p align="center">
  <img src="Assets/Badges/target.svg" alt="Target: Jellyfin 10.11.11" />
  <img src="Assets/Badges/interface.svg" alt="Interface: Jellyfin Server Dashboard" />
  <img src="Assets/Badges/collections.svg" alt="Create and Manage Jellyfin Collections" />
  <img src="Assets/Badges/collection-art.svg" alt="Create, Edit and Apply Collection Art" />
  <img src="Assets/Badges/bulk-tools.svg" alt="Bulk Tools: Create, Add, and Remove" />
  <img src="Assets/Badges/automation.svg" alt="Automation: Optional Metadata Sync" />
</p>

## Why This Plugin Was Created

I created Collection Manager after many years as a Jellyfin user, for fellow
Jellyfin users. Once a library has useful metadata, the next challenge is
turning all of that information into collections that make the library easier
to browse, understand, and enjoy—without manually maintaining every title
forever.

Metadata is not the only meaningful way people organize their media. Some
collections should come from one exact hand-picked group, some from a provider,
network, genre, person, year, language, or another metadata value, and others
from combinations of several values. Existing folder structures can already
represent years of careful organization. I wanted one plugin that respected
all of those approaches instead of forcing every library into a single kind of
rule.

I also wanted the result to remain completely native to Jellyfin. Collections
made here use Jellyfin's ordinary collection format and continue to work with
its normal detail pages, clients, image controls, menus, and APIs. Collection
Manager adds the planning, bulk creation, saved rules, synchronization,
recovery tools, and artwork workflows around those collections without
replacing Jellyfin's own collection experience.

Collection Manager is the organization layer within all of my Jellyfin
plugins. [Media Tagging Manager](https://github.com/mp3li/Media-Tagging-Manager-Jellyfin-Plugin)
can build the metadata foundation, Collection Manager can turn that metadata
or the organization already present in a library into native collections, and
[Home Screen Manager](https://github.com/mp3li/Home-Screen-Sections-Manager-Jellyfin-Plugin)
can present those collections as customized home-screen sections. Each plugin
remains fully useful on its own.

## About Collection Manager

Collection Manager gives you control over the complete native-collection
workflow inside the Jellyfin administrator dashboard. You choose the libraries
and values to review, decide whether to create one collection or many, select
which existing collections to edit or recreate, choose and save any
metadata-driven rules, explicitly run backup or cleanup actions, and decide
which artwork to design and apply. The plugin provides those tools and carries
out the choices you make; it does not create, recreate, clean up, restore, or
redesign collections simply because it is installed.

It does not require an external API key. It can use the metadata already stored
in Jellyfin, values read from local NFO files, metadata added by Media Tagging
Manager, exact media selections, or the folder organization already configured
for Jellyfin libraries.

Collection Manager can also be used with the two standalone companion plugins:
[Media Tagging Manager](https://github.com/mp3li/Media-Tagging-Manager-Jellyfin-Plugin)
and [Home Screen Manager](https://github.com/mp3li/Home-Screen-Sections-Manager-Jellyfin-Plugin).
When used together, the three plugins can carry enriched metadata through
collection organization and into a deeply customized Jellyfin home screen
without making that larger workflow mandatory.

## Five Ways to Create Collections

- **Manual** — select exact collection-level media across one or more
  libraries and place every selection into one collection.
- **Individual Tag** — turn each selected metadata value into its own
  independently editable collection draft, then create multiple drafts in one
  run.
- **Combined Tag** — create one union collection containing media that matches
  any selected metadata value.
- **Multi-Match Tag** — create one intersection collection containing only
  media that matches every selected metadata value.
- **Folder** — navigate the folder organization already configured in
  Jellyfin, select one folder or multiple folders, and create one collection
  from their combined recursive contents.

Every builder provides a reviewable draft with a title, optional overview,
current included-media count or preview, collection-art preference, progress,
and final result. Individual, Combined, Multi-Match, and Folder builders also
provide visible same-title handling instead of silently changing an existing
collection.

## Automatic and Scheduled Maintenance

- Save the complete creation recipe behind every collection made by the
  plugin.
- Optionally react to Jellyfin item-added, item-updated, and item-removed events
  for metadata-driven recipes.
- Add media that now matches and remove media that no longer matches without
  running a full-library reconciliation after every change.
- Batch rapid metadata changes into one targeted scheduled-task run.
- Optionally run a full reconciliation safety pass every one to 168 hours.
- Keep full reconciliation off by default unless the administrator enables it.

## Data and Safety Model

### Backups, Undo, and Scoped Cleanup

- Create named collection backups containing membership, restorable collection
  metadata and settings, provider IDs, and readable locally stored collection
  images.
- Select, rename, delete, and restore saved backups.
- Optionally delete collections absent from the selected backup during a
  restore; that destructive option remains off unless explicitly selected.
- Undo the last recorded collection action.
- Remove only collections recorded as created by Collection Manager.
- Remove only recorded Collection Manager additions from collections it did
  not create.
- Repair stale Jellyfin Primary-image dimensions without redesigning or
  replacing the existing image.

### Native Jellyfin Collections

Collection creation, title/overview edits, membership changes, deletion,
recreation, image application, backup restore, and cleanup are real native
Jellyfin collection operations. They never rename, move, or delete the media
files referenced by those collections.

### Read-Only Media Metadata

Collection Manager reads media metadata to build its catalog and evaluate
recipes. It does not:

- Fetch metadata from the internet
- Create, change, or remove media tags
- Rewrite NFO files
- Rename or move media files
- Alter video or audio streams
- Access streaming services
- Require or store third-party API credentials

If the media metadata itself needs to change, make that change through Jellyfin,
an NFO editor, Media Tagging Manager, or another intended metadata provider.

### Saved Plugin Data

The latest completed metadata catalog, collection-overview state, collection
creation recipes, action history, art preferences, imported fonts and logos,
per-collection logo selections, and collection backups live in Collection
Manager's local Jellyfin plugin-data folder.

The release archive never contains that server-specific data.

### Actions That Replace or Remove Collection Data

- **Save** in the normal editor does not recreate collection membership.
- **Recreate Collection** replaces membership with the saved recipe's result.
- Generated art replaces the selected native image type on selected
  collections or libraries.
- Backup restore replaces saved collection state and images where applicable.
- **Delete collections not in this backup** can delete current collections
  outside the selected backup.
- Cleanup and Delete Collection perform the exact collection changes named by
  their controls.

Create a fresh Collection Manager backup and keep your ordinary Jellyfin server
backup before broad restore, cleanup, recreation, deletion, or artwork work.
Collection Manager backups cover collections; use your normal server backup for
library artwork and all other Jellyfin data.

## Current Release

The current catalog build is **0.1.0.77**. It is the feature-complete candidate
for the first **1.0.0** release. The catalog version will change to 1.0.0 only
when the final v1 DLL, ZIP, manifest checksum, commit, and publication are made
together.

The latest build adds **Create Collections by Folder** and includes the
background collection-recreation fix. Recreation now returns immediately,
runs through Jellyfin's scheduled-task system, and reports its actual status
instead of leaving a web request stuck behind a proxy timeout.

## Requirements

- Jellyfin Server **10.11.11**
- Administrator access
- Existing Jellyfin libraries
- Existing Jellyfin or local-NFO metadata for metadata-driven collections
- A current browser supported by Jellyfin Web for dashboard artwork previews

No external API key is required. Media Tagging Manager and Home Screen Manager
are optional companions, not requirements for ordinary collection creation.

## Install

1. Open **Dashboard → Plugins → Repositories** in Jellyfin.
2. Add this repository manifest URL:

   ```text
   https://raw.githubusercontent.com/mp3li/Collection-Manager-Jellyfin-Plugin/main/manifest.json
   ```

3. Refresh the catalog and open **Collection Manager Jellyfin Plugin**.
4. Select **Install** or **Update** for the newest compatible version.
5. Restart Jellyfin when prompted.
6. Open **Dashboard → Collection Manager** from the Dashboard sidebar.

Future versions use the same plugin GUID and repository entry, so normal
releases appear as an **Update** on the existing plugin instead of as a
different plugin.

The published release ZIP contains only the compiled plugin DLL and
[LICENSE](LICENSE). It does not contain server configuration, collection
backups, plugin data, logs, imported art assets, media paths, NFO files, or
media.

## How to Use This Plugin

Collection Manager has twelve dashboard tabs arranged across three rows. This
section documents the options and actions available in each tab.

### Main Settings Tab

<details>
<summary><strong>Click here to expand Main Settings Tab content</strong></summary>

<br />

#### Collection Backup Settings

- **Available Backups** selects the backup used by Rename, Delete, or Restore.
  Each entry shows its name, creation time, collection count, and image count.
- **Delete collections not in this backup when restoring** is an optional
  destructive restore mode. Leave it unchecked to preserve other current
  collections.
- **Create Collections Backup** captures the current native collections,
  membership, restorable metadata/settings, provider IDs, and readable local
  collection images.
- **Rename Collections Backup** prompts for a new name for the selected backup.
- **Delete Collections Backup** permanently removes the selected saved backup
  after confirmation.
- **Restore from Collections Backup** restores saved collections and members,
  recreates missing collections when possible, restores saved images, skips
  media that no longer exists, and reports every result count.

#### Selected Libraries

- Every library checkbox controls whether that library participates in
  metadata scanning and metadata-driven collection creation.
- **Select All** checks every displayed Jellyfin library.
- **Select None** clears the displayed selection.
- **Save Libraries** saves the selected library scope without changing the
  other Main Settings choices.
- At least one library must be selected before library settings or Main
  Settings can be saved.

#### Newly Added Media Settings

- **Yes** enables targeted membership updates for applicable metadata-driven
  saved recipes when Jellyfin reports item changes.
- **No** disables that event-driven maintenance.
- **Save Main Settings** saves both the current library selection and the
  Yes/No preference.
- The setting does not turn Manual or Folder snapshots into automatic folder
  watchers.

#### Scan for Metadata Tags

- **Scan Metadata Tags** examines the saved libraries and builds the catalog
  used by every metadata-tag picker.
- The progress bar and status text report scan progress and completion.
- A second conflicting scan is not started while one is already active.
- The newest successful scan becomes the **Last Available Scan**; the prior
  saved scan remains usable until a new one completes.

#### Clean Up Settings

Choose one or more actions, then use **Clean Up**:

- **Undo Last Collection Action** reverses the most recent recorded create,
  membership, or supported collection-management action.
- **Remove All Collections Made By This Plugin** removes collections recorded
  as Collection Manager-created.
- **Remove All Media Additions To Existing Collections Not Made by This
  Plugin** removes only membership additions recorded by Collection Manager on
  external collections.
- The result appears below the button, and the selected cleanup checkboxes are
  cleared after completion.

#### Repair Primary Image Metadata

- **Repair Primary Image Metadata** reads each existing collection's exact
  current Primary image and saves that same image back through Jellyfin so its
  stored dimensions are recalculated.
- Collections without a readable Primary image are skipped.
- The result reports repaired, skipped, and failed collections.
- Reload the Jellyfin collections page afterward to display corrected card
  ratios.

#### Scheduled Full Collection Reconciliation

- **Enable Scheduled Full Collection Reconciliation** turns the optional full
  safety pass on or off. It is off by default.
- Enabling it reveals **Full reconciliation interval in hours**.
- The interval accepts a whole number from one through 168 hours.
- **Save Scheduled Full Collection Reconciliation Settings** saves the enabled
  state and interval and reports the resulting schedule.
- Targeted metadata-event updates remain separate from this full scheduled
  pass.

#### My Metadata Tags

- **Click here to expand/collapse** opens or closes the saved metadata overview.
- **Search metadata tag types** filters the displayed type sections.
- **Search metadata tags** supplies the value search used when a type is
  expanded.
- Every saved-scan library displays its item count and has an independent
  expand/collapse control.
- Types are separated between tags supplied by Media Tagging Manager and
  values discovered from Jellyfin or NFO metadata where applicable.
- Every metadata type displays its value count and expands independently.
- Expanded values are alphabetical, show current matching-media counts, and
  use **Previous Page** and **Next Page** controls for fifty-value pages.
- Person-based values display a Jellyfin person image when one is available.

</details>

### Create Manual Collections Tab

<details>
<summary><strong>Click here to expand Create Manual Collections Tab content</strong></summary>

<br />

#### Select Media

- Every Jellyfin library begins as an expandable section.
- Opening a library loads its current collection-level items and displays
  Jellyfin artwork, name, type, year, and readable overview.
- **Search media** filters that library's current item list.
- **Previous Page** and **Next Page** browse ten-item pages.
- **Select All** selects only the ten visible items on the current page.
- **Select None** clears only the ten visible items on the current page.
- Individual checkboxes add or remove media from the one shared draft.
- TV libraries offer Series; Movie libraries offer Movies; Music libraries
  offer Audio and Audio Albums; Book libraries offer Books and Audiobooks; and
  other supported libraries offer their appropriate collection-level media.
  Seasons, episodes, folders, deleted scenes, and other video extras are not
  separate manual choices.

#### Individual Collection Draft

- **Collection Title** names the one collection being created.
- **Overview** starts collapsed and can be expanded to add the collection
  description.
- **Collection Art Preferences** offers Jellyfin default art, Collection
  Manager's saved default, Text Focused art, or Poster Focused art.
- The included count and media cards show the exact current selection across
  every opened library.
- **Remove** beside a selected item removes it from the draft and unchecks it
  in the picker.

#### Create Manual Collections

- **Create Manual Collection** creates one native Jellyfin collection from the
  title, overview, art preference, and every selected item.
- Progress and final status appear beneath the button.
- After successful creation, the title, selection, and loaded picker state are
  cleared for the next manual collection.

</details>

### Create Individual Tag Collections Tab

<details>
<summary><strong>Click here to expand Create Individual Tag Collections Tab content</strong></summary>

<br />

#### Select Metadata Tags

- **Search tags** searches both metadata type names and values across the saved
  libraries.
- Each metadata type displays its total value count and has its own **Click
  here to expand/collapse** control.
- Opening a type loads its values from the latest completed metadata scan.
- Each value shows its matching count, source library, and person image when
  available.
- **Previous Page** and **Next Page** browse fifty-value pages inside the
  expanded metadata type.
- Checking a value immediately creates one independent draft; unchecking it
  removes that draft.

#### Individual Collection Drafts

Every selected value receives its own controls:

- **Create this draft** includes or excludes that draft from the next batch.
- **Collection Title** begins with the selected metadata value and remains
  editable.
- **Overview** starts collapsed and can be expanded and edited.
- **Collection Art Preferences** offers Jellyfin default, plugin default, Text
  Focused, or Poster Focused art.
- The source library, metadata type, metadata value, and current matching-media
  explanation remain visible.
- **Also Add Media With Selected Tag From Other Libraries — Yes/No** controls
  whether additional saved libraries are used.
- The source library is always included.
- **Select All** selects every other saved library; **Select None** returns to
  source-library-only matching; individual other-library checkboxes allow a
  custom scope.
- **Click here to expand/collapse included media** shows the current preview
  grouped by library.
- **Remove Collection Draft** removes the draft and its selected metadata
  value.

#### Create Collections

- **Create Selected Collections** processes every completed draft whose
  **Create this draft** option remains checked.
- Status and progress identify the current draft and completed total.
- Same-title results show **Use existing collection without changing it** and
  **Skip this draft** choices. Editing the title remains another resolution.
- Each draft reports Created, Matched an existing collection, Skipped, or
  Failed.

</details>

### Create Combined Tag Collections Tab

<details>
<summary><strong>Click here to expand Create Combined Tag Collections Tab content</strong></summary>

<br />

#### Select Metadata Tags

- Uses the same searchable, expandable metadata-type and value picker as the
  Individual Tag tab.
- Select at least two values. Every value is added to one shared collection
  draft.
- Matching uses union logic: an item is included when it matches any selected
  value and appears only once even if it matches several.

#### Combined Collection Draft

- Every selected-tag card shows its source library, metadata type, value,
  source-library match count, and **Remove Selected Tag** action.
- **Collection Title** and the collapsed **Overview** customize the result.
- **Collection Art Preferences** offers the four new-collection art choices.
- Libraries where tags were selected are shown checked and cannot be removed
  from the matching scope.
- Other saved libraries have individual checkboxes plus **Select All** and
  **Select None**.
- The matching-media line reports the current unique union count.
- **Click here to expand/collapse included media** opens the grouped preview.
- **Clear Collection Draft** removes all selected tags, title, overview,
  library scope, preview, conflict state, and art selection.

#### Create Collections

- **Create Collection** creates one native union collection and clears the
  successful draft while retaining its completion result.
- The button remains unavailable until at least two tags, a title, and a
  nonzero completed preview exist.
- Same-title choices can leave the existing collection unchanged or skip the
  draft.
- Progress and the final item count appear in the tab.

</details>

### Create Multi-Match Tag Collections Tab

<details>
<summary><strong>Click here to expand Create Multi-Match Tag Collections Tab content</strong></summary>

<br />

#### Select Metadata Tags

- Uses the same complete metadata picker and requires at least two values.
- Every selected value becomes a requirement in the one shared draft.
- Matching uses intersection logic: an item is included only when it matches
  every selected value within the shared library scope.

#### Multi-Match Collection Draft

- Selected-tag cards, **Remove Selected Tag**, Collection Title, collapsed
  Overview, Collection Art Preferences, fixed source libraries, optional other
  libraries, **Select All**, **Select None**, preview toggle, and **Clear
  Collection Draft** behave like the Combined tab.
- The matching line and preview contain only all-tag matches.
- A zero-match state explicitly explains that nothing currently matches every
  selected value.

#### Create Collections

- **Create Collection** remains disabled for fewer than two tags, a missing
  title, a loading preview, or a zero-match result.
- Same-title choices can leave the existing collection unchanged or skip the
  draft.
- Successful creation clears the draft and preserves the completion result.

</details>

### Create Collections by Folder Tab

<details>
<summary><strong>Click here to expand Create Collections by Folder Tab content</strong></summary>

<br />

#### Select Folders

- Each physical location already configured for a Jellyfin library appears as
  its own expandable root.
- Unavailable roots display an availability message instead of a browse
  button.
- **Click here to expand/collapse** opens or closes a configured root.
- **Current folder** shows the active location beneath that root.
- **Select this folder** adds or removes the current folder itself.
- **Open Folder** navigates into a child folder.
- **Up One Folder** returns to the parent without allowing navigation above the
  configured Jellyfin root.
- **Search folders** filters the active folder's direct children.
- **Previous Page** and **Next Page** browse ten-folder pages.
- **Select All** and **Select None** apply only to the ten visible child folders
  on the current page.
- Selections remain active while navigating other folders or other library
  roots.

#### Individual Collection Draft

- One draft combines every selected folder, including folders from different
  configured library locations.
- Overlapping parent and child selections are deduplicated by Jellyfin item ID.
- **Collection Title**, collapsed **Overview**, and **Collection Art
  Preferences** customize the one result.
- Every selected-folder card shows its library/location path and provides
  **Remove Folder**.
- The included count reports unique current Jellyfin media across all selected
  folders.
- **Click here to expand/collapse included media** shows up to the first fifty
  current preview items with artwork, library, type, and year.

#### Create Collections by Folder

- **Create Collection** creates one native collection from the combined
  recursive contents of all selected folders—not one collection per folder.
- The button requires selected folders, a title, a completed preview, and at
  least one recognized media item.
- A same-title result offers **Use existing collection without changing it** or
  **Skip this draft**.
- Progress and the final unique-item/folder count appear beneath the button.
- The successful folder selection becomes a fixed saved Manual recipe; it is
  not an automatic folder watcher.

</details>

### Collection Overview & Editor Tab

<details>
<summary><strong>Click here to expand Collection Overview &amp; Editor Tab content</strong></summary>

<br />

#### Selected Collections Settings

- **Show collections made by this plugin** includes Collection Manager-created
  collections.
- **Show collections not made by this plugin** includes other native Jellyfin
  collections.
- Either option or both may be enabled.
- **Save Selected Collections Settings** saves the overview filter and reloads
  the displayed results.

#### Scan for Collections

- **Scan for Collections** captures one new flat overview of current native
  collections and members.
- The progress bar and status show the collection count and local completion
  time.
- The latest saved overview remains available after leaving the dashboard or
  restarting Jellyfin.

#### Collections Overview & Editor

- **Click here to expand/collapse** opens or closes the complete overview.
- **Newly added color** and **Newly removed color** choose the comparison colors.
- **Save Colors** persists both choices.
- Every collection begins collapsed and shows its current Primary art, title,
  and saved item count.
- A removed collection remains identified as removed since the previous scan.
- Expanded membership uses the saved added and removed colors to distinguish
  titles that changed since the previous scan; removed titles remain visible
  in that comparison.
- Each current collection provides **Click here to expand/collapse**, **Edit
  Collection**, and **Delete Collection**. Delete requires confirmation.

Inside **Edit Collection**:

- **Collection title** changes the native Jellyfin collection name.
- **Overview** starts collapsed and edits the native collection description.
- Current membership is loaded in fifty-item pages with Jellyfin posters.
- **Previous Page** and **Next Page** navigate membership pages.
- **Select All** and **Select None** change the current page's checked items.
- **Remove from Collection** queues selected members for removal.
- **Add to Collection** opens a target selector for adding the selected members
  to another native collection.
- The target selector loads all collections and preserves the queued addition
  until the editor is saved.
- **Save** applies the pending title, overview, removals, and additions.
- **Close** discards unsaved editor changes.

For saved Collection Manager recipes:

- The exact Manual, Individual Tag, Combined Tag, or Multi-Match creation UI is
  shown beneath current membership with the saved selections, title, overview,
  library scope, preview, and art preference restored.
- **Save Collection Creation Settings** changes the saved future recipe without
  replacing current membership.
- **Recreate Collection** is the separate action that replaces membership with
  the recipe's current result.

For a collection without saved Collection Manager settings:

- **Recreate This Section With This Plugin Settings → Recreate** opens the full
  four-tab recipe editor, beginning on **Create Manual Collections**.
- The available editor tabs are Create Manual Collections, Create Individual
  Tag Collections, Create Combined Tag Collections, and Create Multi-Match Tag
  Collections.
- Switching tabs retains the in-progress state in each tab while the editor is
  open.
- Recreation is queued server-side; the dialog polls real queued, running,
  completed, or failed state and reports matching, added, removed, and current
  membership counts.
- A recreated external collection keeps the full creation-tab editor during
  later edits.

</details>

### New Collection Art Settings Tab

<details>
<summary><strong>Click here to expand New Collection Art Settings Tab content</strong></summary>

<br />

- **Text focused art** selects the saved Text Focused design as Collection
  Manager's default for future drafts that choose the plugin default.
- **Poster focused art** selects the saved Poster Focused design instead.
- The choices are mutually exclusive.
- **Save Default Art Preferences** persists the selected default.
- Logo Focused and Multi-Collection/Library Gradient art remain manual tools
  and cannot be the automatic new-collection default.

Every new collection draft independently offers:

- **Jellyfin default art** — leave automatic art handling to Jellyfin.
- **Collection Manager plugin's default art setting** — apply the Text or
  Poster default selected above.
- **Text focused art** — apply the saved Text Focused design directly.
- **Poster focused art** — apply the saved Poster Focused design directly.

Existing-collection creation editors additionally offer **Retain Current
Collection Art**.

</details>

### Text Focused Collection Art Tab

<details>
<summary><strong>Click here to expand Text Focused Collection Art Tab content</strong></summary>

<br />

#### Design and preview

- **Preview Collection Name Text** supplies the sample title shown in the live
  canvas.
- **Import Font** accepts a TTF or OTF file and stores it in Collection
  Manager's plugin-data folder.
- **Text size** accepts a value from 10% through 250%.
- **Select Text Color** and **Select Text Shadow Color** control the title.
- **Select Art Background Style** offers Solid, Transparent, or Gradient.
- Solid reveals one background-color picker.
- Transparent creates art with no background fill.
- Gradient reveals two color pickers and Vertical, Horizontal, Diagonal, or
  Center direction.
- **Collection Art Type** offers Primary, Backdrop, Banner, Thumbnail, or Logo
  and changes the preview aspect ratio.
- **Preview Art** updates as the controls change.
- **Save Text Focused Collection Art Preferences** stores the complete design.

#### Apply Text Focused Collection Art

- Select any number of native collections from the collection picker.
- The count line reports the chosen image type and selected collection total.
- **Apply Text Focused Collection Art** renders and replaces that image type on
  each selected collection, with per-item progress and a completed/failed
  total.

#### Apply Text Focused Library Art

- Select any number of Jellyfin libraries from the library picker.
- **Apply Text Focused Library Art** uses each library's displayed name and
  replaces the selected native image type on those libraries.

</details>

### Poster Focused Collection Art Tab

<details>
<summary><strong>Click here to expand Poster Focused Collection Art Tab content</strong></summary>

<br />

#### Design and preview

- **Preview Collection Name Text**, **Import Font**, **Text size**, **Select
  Text Color**, and **Select Text Shadow Color** control the overlaid title.
- **Select Poster Style** offers One poster, Four posters, or Nine posters.
- **Collection For Poster Examples** chooses which collection supplies real
  Jellyfin Primary images to the live preview.
- **Collection Art Type** offers Primary, Backdrop, Banner, Thumbnail, or Logo
  and changes the preview aspect ratio.
- **Save Poster Focused Collection Art Preferences** stores the complete
  design.

#### Apply Poster Focused Collection Art

- Select any number of collections.
- **Apply Poster Focused Collection Art** builds each target from real member
  posters and replaces the selected image type, with per-item progress.

#### Apply Poster Focused Library Art

- Select any number of Jellyfin libraries.
- **Apply Poster Focused Library Art** builds each target from current
  top-level media posters in that library and replaces the selected image type.

</details>

### Logo Focused Collection Art Tab

<details>
<summary><strong>Click here to expand Logo Focused Collection Art Tab content</strong></summary>

<br />

#### Background and image type

- **Select Background for Behind Logos** offers Solid, Transparent, or
  Gradient.
- Solid reveals one background color.
- Transparent keeps the logo background clear.
- Gradient provides two colors and Vertical, Horizontal, Diagonal, or Center
  direction.
- **Collection Art Type** offers Primary, Backdrop, Banner, Thumbnail, or Logo.
- **Save Logo Focused Collection Art Preferences** stores the background and
  image-type settings.

#### Collections and logos

- **Select Collections to Use Logos** chooses every collection that needs an
  individual logo assignment.
- Each selected collection receives its own card with media count, **Search
  logos**, available-logo list, saved current logo, and live preview.
- When Media Tagging Manager is installed, its saved Provider, Network, Genre,
  Keyword, Collection, and Production Company logos appear in the picker.
- **Import Your Own Logo** accepts PNG, JPG, JPEG, WEBP, GIF, or SVG.
- Checking one logo saves it for that collection and unchecks the other logo
  choices in that card.
- **Round the corners of this logo** controls the selected logo's rendered
  corners.
- **Unselect Logo** clears that collection's saved assignment.
- **Create Logo Based Collection Art** requires a logo for every selected
  collection, then renders and replaces the selected native image type with
  per-collection progress.

</details>

### Multi-Collection/Library Gradient Art Tab

<details>
<summary><strong>Click here to expand Multi-Collection/Library Gradient Art Tab content</strong></summary>

<br />

- **First gradient color**, **Second gradient color**, and **Third gradient
  color** define the shared color flow.
- **Gradient direction** offers Vertical, Horizontal, Diagonal, or Center.
- **Save Gradient Settings** stores the colors and direction.
- **Select Collections for Multi-Collection Gradient Art** chooses collection
  targets.
- **Select Libraries for Multi-Collection Gradient Art** chooses library
  targets.
- Collections and libraries cannot be mixed in one set; choosing one target
  kind clears the other.
- **Art Type** offers Primary, Backdrop, Banner, Thumbnail, or Logo.
- **Multi-Collection/Library Gradient Art Preview** divides the shared gradient
  across one preview tile per selected target.
- Preview tiles are draggable, and their order becomes the order used when the
  set is rendered.
- **Create Multi-Collection/Library Gradient Collection Art** saves the current
  settings, renders each target's segment, replaces the selected native image
  type, and reports per-target progress.

This workflow is designed for side-by-side rows such as custom home-screen
sections created with
[Home Screen Manager](https://github.com/mp3li/Home-Screen-Sections-Manager-Jellyfin-Plugin).

</details>

## Compatibility and Limits

- Target server: **Jellyfin 10.11.11**.
- Target ABI: **Jellyfin 10.11**.
- Runtime framework: **net9.0**.
- The dashboard and API require an elevated Jellyfin administrator.
- Metadata pickers represent the latest completed plugin scan, not an
  automatically live view of every later metadata edit.
- Folder creation uses media Jellyfin currently recognizes beneath the selected
  paths, not arbitrary unscanned files on disk.
- Folder collections are fixed snapshots and do not watch directories for new
  files.
- Targeted automatic changes apply to metadata-driven saved recipes, not fixed
  Manual or Folder snapshots.
- Full reconciliation follows each saved recipe exactly and may remove current
  members that are outside that recipe.
- Provider, network, person, NFO, and other metadata choices depend on what is
  actually present in the selected libraries during the scan.
- Generated artwork is rendered by the administrator's browser and uploaded as
  PNG data through Jellyfin's native image-save path.
- Media Tagging Manager is required only for its saved logo library or metadata
  that it adds. Existing Jellyfin and NFO metadata remains usable without it.

## Companion Plugins

- [Media Tagging Manager](https://github.com/mp3li/Media-Tagging-Manager-Jellyfin-Plugin) can enrich existing media with provider, network, genre, keyword, collection, people, production, rating, classification, and language metadata, and supplies a reusable logo library.
- [Home Screen Manager](https://github.com/mp3li/Home-Screen-Sections-Manager-Jellyfin-Plugin) can display custom home-screen sections built from collections, libraries, metadata, and manually selected media, including rows designed for coordinated gradient art.

Collection Manager remains fully usable on its own.

## Documentation

- [Changelog](Documentation/CHANGELOG.md)

The detailed testing tracker remains a separate development record rather than
being presented here as the plugin's feature list or release status.

## License

Collection Manager is available under the [Collection Manager Noncommercial
License 1.0](LICENSE). Read it before redistributing, modifying, or using the
project outside your own server.
