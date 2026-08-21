import assert from 'node:assert/strict';
import fs from 'node:fs';

const read = (path) => fs.readFileSync(new URL(`../${path}`, import.meta.url), 'utf8');
const scheduler = read('Collection Manager/Services/ReconciliationScheduler.cs');
const gate = read('Collection Manager/Services/MediaTaggingManagerActivityGate.cs');
const catalog = read('Collection Manager/Services/MetadataCatalogService.cs');
const overview = read('Collection Manager/Services/CollectionOverviewService.cs');
const services = read('Collection Manager/Services/ServiceRegistrator.cs');
const dashboard = read('Collection Manager/Configuration/configPage.html');
const reconciler = read('Collection Manager/Services/CollectionReconciler.cs');

assert.match(scheduler, /TimeSpan\.FromMinutes\(3\)/, 'Metadata reconciliation must retain a three-minute trailing quiet period.');
assert.match(scheduler, /_lastBusyObservationUtc/, 'The quiet period must restart after companion-plugin work is last observed.');
assert.match(gate, /StartsWith\(TaskKeyPrefix/, 'Media Tagging Manager task keys must be detected without an assembly dependency.');
assert.match(gate, /RefreshLibrary/, 'Jellyfin library scans must remain gated because Media Tagging Manager has a post-scan task.');

assert.doesNotMatch(catalog, /Task\.Run\(ScanAsync/, 'Metadata catalog scans must not use unmanaged background tasks.');
assert.match(services, /IScheduledTask, MetadataCatalogScanTask/, 'Metadata catalog scans must run as native Jellyfin tasks.');
assert.match(services, /IScheduledTask, CollectionOverviewScanTask/, 'Collection overview scans must run as native Jellyfin tasks.');
assert.match(catalog, /CancellationToken cancellationToken/, 'Metadata catalog scans must be cancellable.');
assert.match(overview, /CancellationToken cancellationToken/, 'Collection overview scans must be cancellable.');

assert.doesNotMatch(dashboard, /setInterval\(refreshStatus/, 'The dashboard must not poll the server forever while idle.');
assert.match(dashboard, /metadata-catalog\/search\?libraryIds=/, 'Flat tag search must use one bounded server search endpoint.');
assert.doesNotMatch(dashboard, /750\)/, 'Recreation status must not poll every 750 milliseconds.');
assert.match(dashboard, /collection-overview\/members\?collectionId=/, 'Collection member payloads must load only when expanded.');

assert.match(reconciler, /var items = GetMediaItems\(\)\.ToArray\(\);[\s\S]*foreach \(var rule in rules\)/, 'Enabled rules must share one media snapshot.');
assert.match(reconciler, /GetLiveMatchingItemIds\(recipes, cancellationToken\)/, 'Saved recipes must share one live library snapshot.');
assert.match(reconciler, /additionsByCollection/, 'Targeted updates must batch membership additions by collection.');

console.log('Collection Manager load-shedding regression tests passed.');
