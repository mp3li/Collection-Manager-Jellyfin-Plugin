import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

const html = fs.readFileSync(new URL('../Collection Manager/Configuration/configPage.html', import.meta.url), 'utf8');
const scriptMatch = html.match(/<script>([\s\S]*?)<\/script>/);
assert.ok(scriptMatch, 'The embedded dashboard script was not found.');
const helperEnd = scriptMatch[1].indexOf('function normalize(raw)');
assert.ok(helperEnd > 0, 'The dashboard helper boundary was not found.');

const context = {
    ApiClient: {},
    Promise,
    Response,
    console,
    document: {
        querySelector: () => ({ querySelector: () => null }),
    },
};
vm.createContext(context);
vm.runInContext(`${scriptMatch[1].slice(0, helperEnd)}
globalThis.__mcmHelpers = { normalizeApiError, err };
}());`, context);

const helpers = context.__mcmHelpers;
const normalizedStringError = await helpers.normalizeApiError(new Response(
    JSON.stringify('One or more selected libraries are no longer available on this server.'),
    { status: 400, statusText: 'Bad Request', headers: { 'Content-Type': 'application/json' } },
)).catch(error => error);
assert.equal(
    helpers.err(normalizedStringError),
    'HTTP 400: One or more selected libraries are no longer available on this server.',
);

const normalizedValidationError = await helpers.normalizeApiError(new Response(
    JSON.stringify({ errors: { ManualItemIds: ['Select one or more media items.'] } }),
    { status: 400, statusText: 'Bad Request', headers: { 'Content-Type': 'application/problem+json' } },
)).catch(error => error);
assert.equal(helpers.err(normalizedValidationError), 'HTTP 400: Select one or more media items.');

const catalogNameMatch = scriptMatch[1].match(/function catalogLibraryName\(id\) \{[^\n]+\}/);
assert.ok(catalogNameMatch, 'The current-library name resolver was not found.');
const nameContext = {
    libraries: [{ ItemId: 'stable-id', Name: 'Movies Library', DisplayName: 'Renamed Movies' }],
    prop: (object, name) => object && (object[name] !== undefined ? object[name] : object[name[0].toLowerCase() + name.slice(1)]),
};
vm.createContext(nameContext);
vm.runInContext(`${catalogNameMatch[0]}; globalThis.__catalogLibraryName = catalogLibraryName;`, nameContext);
assert.equal(nameContext.__catalogLibraryName('stable-id'), 'Renamed Movies');

console.log('Collection Manager dashboard regression tests passed.');
