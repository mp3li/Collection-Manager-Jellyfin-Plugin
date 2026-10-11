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

const textSizeControlStart = scriptMatch[1].indexOf('function addTextSizeControl');
const textSizeControlEnd = scriptMatch[1].indexOf('function removeImageBackgroundChoice', textSizeControlStart);
assert.ok(textSizeControlStart > 0 && textSizeControlEnd > textSizeControlStart, 'The text-size controls were not found.');
const textSizeControls = scriptMatch[1].slice(textSizeControlStart, textSizeControlEnd);
assert.match(
    textSizeControls,
    /\$\('#textArtTextSize'\)\.closest\('\.inputContainer'\)\.parentNode\.insertAdjacentHTML\('afterend', '[^']*Text line spacing/,
    'Text line spacing should be inserted directly below text size.',
);

const drawCanvasTextMatch = scriptMatch[1].match(/function drawCanvasText\([^\n]+/);
assert.ok(drawCanvasTextMatch, 'The collection-art text renderer was not found.');
const drawContext = {};
vm.createContext(drawContext);
vm.runInContext(`${drawCanvasTextMatch[0]}; globalThis.__drawCanvasText = drawCanvasText;`, drawContext);
const drawnLines = [];
const canvasContext = {
    measureText: value => ({ width: value.length * 100 }),
    fillText: (value, x, y) => drawnLines.push({ value, x, y }),
};
drawContext.__drawCanvasText(canvasContext, 'One Two Three', 1000, 1500, '#ffffff', '#000000', 100, 150);
assert.equal(drawnLines.length, 2);
assert.equal(drawnLines[1].y - drawnLines[0].y, 157.5, 'Text line spacing should change the distance between wrapped lines.');
assert.match(scriptMatch[1], /TextLineSpacing:Number\(\$\('#textArtTextLineSpacing'\)\.value \|\| 112\)/);
assert.match(scriptMatch[1], /#textArtTextLineSpacing/);

const catalogNameMatch = scriptMatch[1].match(/function catalogLibraryName\(id\) \{[^\n]+\}/);
assert.ok(catalogNameMatch, 'The current-library name resolver was not found.');
const nameContext = {
    libraries: [{ ItemId: 'stable-id', Name: 'Movies Library', DisplayName: 'Renamed Movies' }],
    prop: (object, name) => object && (object[name] !== undefined ? object[name] : object[name[0].toLowerCase() + name.slice(1)]),
};
vm.createContext(nameContext);
vm.runInContext(`${catalogNameMatch[0]}; globalThis.__catalogLibraryName = catalogLibraryName;`, nameContext);
assert.equal(nameContext.__catalogLibraryName('stable-id'), 'Renamed Movies');

const recreationProgressMatch = scriptMatch[1].match(/function reportRecreationProgress\(context, working, message, result\) \{[\s\S]*?\n        \}/);
assert.ok(recreationProgressMatch, 'The recreation progress updater was not found.');
assert.doesNotMatch(
    recreationProgressMatch[0],
    /renderMode\(mode\)/,
    'Recreation status polling must not rerender the tag editor and relaunch its matching-media preview.',
);
assert.match(
    recreationProgressMatch[0],
    /data-mode-status/,
    'Recreation status polling should update the existing status controls directly.',
);

console.log('Collection Manager dashboard regression tests passed.');
