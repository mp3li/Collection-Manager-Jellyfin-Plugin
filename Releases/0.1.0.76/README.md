# 0.1.0.76

This Jellyfin `10.11.11` private-testing release contains only
`Jellyfin.Plugin.CollectionManager.dll` and the license—never Jellyfin
configuration, collection backups, logs, cached data, NFO files, or media.

It separates saving collection creation settings from recreating collection
membership, queues recreation through Jellyfin's native task runner with live
progress, and retains the full editor tabs for recreated external collections.

Use the repository's `manifest.json` URL in **Dashboard → Plugins →
Repositories** to install or update the plugin.
