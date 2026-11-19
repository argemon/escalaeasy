const { test } = require('node:test');
const assert = require('node:assert/strict');
const load = require('./source-loader.cjs')();
const display = load('lib/displayTime.ts');

test('civil dates and overnight windows display without timezone conversion or seconds', () => {
  assert.equal(display.displayDate('2026-11-04'), '04/11/2026');
  assert.equal(display.displayDateTime('2026-12-31T23:59:37'), '31/12/2026 23:59');
  assert.equal(display.displayTemporalText('2026-12-31T23:59:37 – 2027-01-01T08:00:12; carga 120:30:15'), '31/12/2026 23:59 – 01/01/2027 08:00; carga 120:30');
});

test('duration presentation supports weekly totals while source precision remains intact', () => {
  const seconds = 158401;
  assert.equal(display.displayDuration(seconds), '44:00');
  assert.equal(seconds, 158401);
  assert.equal(display.displayDuration(0), '00:00');
  assert.equal(display.displayDuration(100 * 3600 + 59 * 60 + 59), '100:59');
});
