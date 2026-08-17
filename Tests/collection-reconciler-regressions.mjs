import assert from 'node:assert/strict';
import fs from 'node:fs';

const source = fs.readFileSync(new URL('../Collection Manager/Services/CollectionReconciler.cs', import.meta.url), 'utf8');
const targetedStart = source.indexOf('public async Task ReconcileChangedItemsAsync');
const targetedEnd = source.indexOf('/// <summary>Reconciles exactly one stored rule.</summary>', targetedStart);

assert.ok(targetedStart >= 0 && targetedEnd > targetedStart, 'The targeted reconciliation implementation was not found.');

const targetedSource = source.slice(targetedStart, targetedEnd);
assert.match(
    targetedSource,
    /var memberships = LoadAutomaticCollectionMemberships\(rules, recipes\);/,
    'Targeted reconciliation must load one membership snapshot for the whole changed-item batch.',
);
assert.equal(
    (targetedSource.match(/GetLinkedChildren\(\)/g) || []).length,
    1,
    'Automatic collection links must be resolved only once in the batch membership loader.',
);
assert.match(targetedSource, /membership\.ItemIds\.Add\(/, 'Successful additions must update the batch membership snapshot.');
assert.match(targetedSource, /membership\.ItemIds\.Remove\(/, 'Successful removals must update the batch membership snapshot.');

console.log('Collection Manager reconciliation regression tests passed.');
