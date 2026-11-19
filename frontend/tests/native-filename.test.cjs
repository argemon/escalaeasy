const { test } = require('node:test');
const assert = require('node:assert/strict');
const load = require('./source-loader.cjs')();
const native = load('lib/nativeDocument.ts');
const { sampleDocument } = load('data/sample.ts');

test('native export names the company and selected period even without shifts', () => {
  const document = structuredClone(sampleDocument);
  document.company.name = 'Comércio São João';
  document.planningPeriod = { start: '2026-12-28', end: '2027-01-03' };
  document.assignments = [];
  let downloaded;
  const original = native.downloadText;
  native.downloadText = (contents, filename) => { downloaded = { contents, filename }; };
  try {
    load('lib/api.ts').exportNative(document);
  } finally {
    native.downloadText = original;
  }
  assert.equal(downloaded.filename, 'Comércio São João - 28-12-2026 a 03-01-2027.escalalivre');
  const restored = native.parseNativeText(downloaded.contents);
  assert.equal(restored.company.name, document.company.name);
  assert.deepEqual(JSON.parse(JSON.stringify(restored.planningPeriod)), document.planningPeriod);
});

test('native filenames preserve accents and replace characters forbidden in Windows', () => {
  const document = structuredClone(sampleDocument);
  document.planningPeriod = { start: '2026-11-19', end: '2026-11-19' };
  document.company.name = '  Ação <Matriz>: "Sul"/Norte\\Loja|Rede?*\u0000  ';
  const filename = native.nativeFilename(document);
  assert.ok(filename.startsWith('Ação -Matriz-'));
  assert.ok(filename.endsWith(' - 19-11-2026 a 19-11-2026.escalalivre'));
  assert.equal(/[<>:"/\\|?*]/.test(filename), false);
  assert.ok(Array.from(filename).every(char => char >= ' '));
  document.company.name = ' ... ';
  assert.equal(native.nativeFilename(document), 'Empresa - 19-11-2026 a 19-11-2026.escalalivre');
  document.company.name = 'Á'.repeat(300);
  assert.ok(native.nativeFilename(document).length < 200);
});
