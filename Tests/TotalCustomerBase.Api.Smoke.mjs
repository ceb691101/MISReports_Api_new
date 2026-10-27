import assert from 'node:assert/strict';

const base = (process.argv[2] || 'http://localhost:5173').replace(/\/$/, '');
const endpoint = '/misapi/api/general/total-customer-base';
async function read(path) {
    const response = await fetch(base + path, { signal: AbortSignal.timeout(60000) });
    const result = await response.json();
    assert.equal(response.status, 200, JSON.stringify(result));
    assert.ok(!result.errorMessage, result.errorMessage);
    return result.data;
}

const report = await read(endpoint);
const cycles = await read(endpoint + '/bill-cycles');
assert.equal(report.CurrentCycle, Number(cycles[0].Code));
assert.equal(report.CurrentCycleLabel, cycles[0].Label);
assert.equal(report.PreviousCycle, report.CurrentCycle - 1);
assert.ok(report.PreviousCycleLabel);
assert.ok(report.Rows.length > 0);
assert.equal(new Set(report.Rows.map(row => row.ProvinceName + '|' + row.AreaCode)).size, report.Rows.length);
assert.ok(report.Rows.every(row => Number.isInteger(row.AgTotal) && Number.isInteger(row.FTotal)));
const explicit = await read(endpoint + '?' + new URLSearchParams({ billCycle: String(report.CurrentCycle) }));
assert.deepEqual(explicit, report);
console.log(`PASS: No-parameter request detects ${report.CurrentCycleLabel} and returns ${report.Rows.length} areas, matching an explicit cycle.`);

const areas = await read('/misapi/api/ordinary/areas');
for (const [areaType, field] of [['Province', 'ProvCode'], ['Division', 'Region']]) {
    const codes = [...new Set(areas.map(area => area[field]).filter(Boolean))];
    const locationCode = codes.find(code => report.Rows.some(row => areas.some(area => area[field] === code && area.AreaCode === row.AreaCode)));
    assert.ok(locationCode);
    const expected = report.Rows.filter(row => areas.some(area => area[field] === locationCode && area.AreaCode === row.AreaCode));
    const scoped = await read(endpoint + '?' + new URLSearchParams({ areaType, locationCode }));
    assert.equal(scoped.CurrentCycle, report.CurrentCycle);
    assert.deepEqual(scoped.Rows, expected);
    console.log(`PASS: ${areaType} ${locationCode}, ${scoped.Rows.length} rows match the all-area report with automatic cycle detection.`);
}
for (const query of [
    { billCycle: '0' }, { billCycle: 'invalid' },
    { billCycle: String(report.CurrentCycle), areaType: 'invalid' },
    { billCycle: String(report.CurrentCycle), areaType: 'Province' }
]) {
    const response = await fetch(base + endpoint + '?' + new URLSearchParams(query));
    assert.equal(response.status, 400);
}
console.log('PASS: Explicit invalid bill cycles, invalid area types, and missing scoped locations are rejected.');
