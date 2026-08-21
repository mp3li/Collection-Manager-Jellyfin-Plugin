# 0.1.0.81

This Jellyfin `10.11.11` private-testing release contains only
`Jellyfin.Plugin.CollectionManager.dll` and the license—never Jellyfin
configuration, collection backups, logs, cached data, NFO files, or media.

It waits for Media Tagging Manager and Jellyfin library scans to finish, then
waits for three quiet minutes without another metadata change before starting
collection reconciliation. Reconciliation scans and writes are batched, while
dashboard scans and requests are cancellable, bounded, cached, or lazy-loaded
to reduce server pressure without changing collection ownership or recipe
behavior.

Use the repository's `manifest.json` URL in **Dashboard → Plugins →
Repositories** to install or update the plugin.
