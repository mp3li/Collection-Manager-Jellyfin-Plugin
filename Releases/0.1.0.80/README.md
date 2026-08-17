# 0.1.0.80

This Jellyfin `10.11.11` private-testing release contains only
`Jellyfin.Plugin.CollectionManager.dll` and the license—never Jellyfin
configuration, collection backups, logs, cached data, NFO files, or media.

It stops recreation status checks from repeatedly reloading matching-media
previews, and stops targeted metadata reconciliation from resolving every
collection link again for every changed item in the same batch.

Use the repository's `manifest.json` URL in **Dashboard → Plugins →
Repositories** to install or update the plugin.
