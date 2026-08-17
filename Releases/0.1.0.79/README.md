# 0.1.0.79

This Jellyfin `10.11.11` private-testing release contains only
`Jellyfin.Plugin.CollectionManager.dll` and the license—never Jellyfin
configuration, collection backups, logs, cached data, NFO files, or media.

It stops Combined Tag and Multi-Match Tag recreation status checks from
repeatedly reloading the matching-media preview while the collection is being
recreated.

Use the repository's `manifest.json` URL in **Dashboard → Plugins →
Repositories** to install or update the plugin.
