import assert from 'node:assert/strict';
import { spawn, spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { dirname, join, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const baseUri = (process.argv[2] || 'http://localhost:44381').replace(/\/$/, '');
const buildArea = resolve(dirname(fileURLToPath(import.meta.url)), '../.build-area');
mkdirSync(buildArea, { recursive: true });
const profile = mkdtempSync(join(buildArea, 'swagger-ui-test-'));
const browserPath = process.env.SWAGGER_TEST_BROWSER || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const browser = spawn(browserPath, [
    '--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
    '--disable-background-networking', '--remote-debugging-port=0', `--user-data-dir=${profile}`, 'about:blank'
], { stdio: 'ignore', windowsHide: true });
let socket;
let cdpCommand;
const pause = ms => new Promise(resolve => setTimeout(resolve, ms));

async function until(callback, description, timeout = 60000) {
    const deadline = Date.now() + timeout;
    while (Date.now() < deadline) {
        const result = await callback();
        if (result) return result;
        await pause(100);
    }
    throw new Error(`Timed out waiting for ${description}.`);
}

try {
    browser.on('error', error => { throw error; });
    const portFile = join(profile, 'DevToolsActivePort');
    await until(() => existsSync(portFile), 'headless browser');
    const port = readFileSync(portFile, 'utf8').split('\n')[0].trim();
    const pages = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
    socket = new WebSocket(pages.find(page => page.type === 'page').webSocketDebuggerUrl);
    await new Promise((resolve, reject) => {
        socket.addEventListener('open', resolve, { once: true });
        socket.addEventListener('error', reject, { once: true });
    });
    let sequence = 0;
    const pending = new Map();
    const reportRequests = [];
    const errors = [];
    socket.addEventListener('message', event => {
        const message = JSON.parse(event.data);
        if (message.id) {
            const handler = pending.get(message.id);
            if (handler) {
                pending.delete(message.id);
                clearTimeout(handler.timer);
                message.error ? handler.reject(new Error(message.error.message)) : handler.resolve(message.result);
            }
        }
        if (message.method === 'Network.requestWillBeSent') {
            const url = new URL(message.params.request.url);
            if (url.pathname.endsWith('/api/debtors/bulk/government-age-analysis')) reportRequests.push(url);
        }
        if (message.method === 'Runtime.exceptionThrown') errors.push(message.params.exceptionDetails.text);
    });
    function command(method, params = {}) {
        return new Promise((resolve, reject) => {
            const id = ++sequence;
            const timer = setTimeout(() => { pending.delete(id); reject(new Error(`CDP timeout: ${method}`)); }, 15000);
            pending.set(id, { resolve, reject, timer });
            socket.send(JSON.stringify({ id, method, params }));
        });
    }
    cdpCommand = command;
    async function evaluate(expression) {
        const result = await command('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
        if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description || result.exceptionDetails.text);
        return result.result.value;
    }
    await command('Page.enable');
    await command('Network.enable');
    await command('Runtime.enable');
    await command('Network.setBlockedURLs', { urls: ['*://validator.swagger.io/*'] });
    await command('Page.navigate', { url: `${baseUri}/swagger/ui/index#!/GovernmentAgeAnalysis/GovernmentAgeAnalysis_GetGovernmentAgeAnalysis` });
    await until(() => evaluate(`Boolean(window.govForm = Array.from(document.querySelectorAll('form.sandbox')).find(form => form.dataset.governmentAgeLocations))`), 'government Swagger form');
    const initial = await evaluate(`({
        choices: Array.from(govForm.querySelector('[name=areaType]').options).map(option => option.value).filter(Boolean),
        locationTag: govForm.querySelector('[name=locationCode]').tagName,
        disabled: govForm.querySelector('[name=locationCode]').disabled
    })`);
    assert.deepEqual(initial.choices, ['Province', 'Division', 'Entire CEB']);
    assert.equal(initial.locationTag, 'SELECT');
    assert.equal(initial.disabled, true);

    async function selectArea(type) {
        await evaluate(`(() => { const select = govForm.querySelector('[name=areaType]'); select.value = ${JSON.stringify(type)}; select.dispatchEvent(new Event('change', { bubbles: true })); })()`);
        if (type !== 'Entire CEB') await until(() => evaluate(`!govForm.querySelector('[name=locationCode]').disabled && govForm.querySelector('[name=locationCode]').options.length > 1`), `${type} choices`);
    }
    await selectArea('Province');
    const province = await evaluate(`govForm.querySelector('[name=locationCode]').options[1].value`);
    const requestCount = reportRequests.length;
    await evaluate(`govForm.querySelector('.submit').click()`);
    await pause(150);
    assert.equal(reportRequests.length, requestCount, 'Missing province selection should block submission.');
    assert.match(await evaluate(`govForm.querySelector('[role=status]').textContent`), /Select a province/);
    const filters = await (await fetch(`${baseUri}/api/debtors/bulk/sectors`)).json();
    const cycle = filters.data.billCycles[0];
    await evaluate(`(() => {
        govForm.querySelector('[name=billCycle]').value = ${JSON.stringify(cycle)};
        govForm.querySelector('[name=reportType]').value = 'Summary';
        const location = govForm.querySelector('[name=locationCode]'); location.value = ${JSON.stringify(province)};
        location.dispatchEvent(new Event('change', { bubbles: true }));
        govForm.querySelector('.submit').click();
    })()`);
    await until(() => reportRequests.length > requestCount, 'province report request');
    assert.equal(reportRequests.at(-1).searchParams.get('areaType'), 'Province');
    assert.equal(reportRequests.at(-1).searchParams.get('locationCode'), province);

    await selectArea('Division');
    const divisions = await evaluate(`({ value: govForm.querySelector('[name=locationCode]').value, codes: Array.from(govForm.querySelector('[name=locationCode]').options).map(option => option.value).filter(Boolean) })`);
    assert.equal(divisions.value, '', 'Switching Area Type must clear the previous province.');
    assert.deepEqual(divisions.codes, ['R1', 'R2', 'R3', 'R4']);
    await evaluate(`govForm.querySelector('[name=locationCode]').value = 'R1'; govForm.querySelector('.submit').click()`);
    await until(() => reportRequests.at(-1)?.searchParams.get('areaType') === 'Division', 'division report request');
    assert.equal(reportRequests.at(-1).searchParams.get('locationCode'), 'R1');

    await selectArea('Entire CEB');
    assert.equal(await evaluate(`govForm.querySelector('[name=locationCode]').disabled && govForm.querySelector('[name=locationCode]').value === ''`), true);
    await evaluate(`govForm.querySelector('.submit').click()`);
    await until(() => reportRequests.at(-1)?.searchParams.get('areaType') === 'Entire CEB', 'Entire CEB report request');
    assert.equal(reportRequests.at(-1).searchParams.has('locationCode'), false);
    assert.deepEqual(errors, [], 'Swagger has JavaScript runtime errors.');
    console.log('PASS: Swagger dependent dropdowns, missing-location validation, selection reset, and Province/Division/Entire CEB request parameters.');
} finally {
    // Ask Chrome to close normally so its profile files are released before deletion.
    if (socket && socket.readyState === WebSocket.OPEN && cdpCommand) {
        await cdpCommand('Browser.close').catch(() => {});
        await until(() => browser.exitCode !== null, 'browser shutdown', 10000).catch(() => {});
    }
    if (socket) socket.close();
    if (browser.pid && browser.exitCode === null) {
        spawnSync('taskkill', ['/pid', String(browser.pid), '/T', '/F'], { stdio: 'ignore', windowsHide: true });
        await until(() => browser.exitCode !== null, 'browser process exit', 10000).catch(() => {});
    }
    // Only remove the temporary profile created under this repository's build directory.
    if (profile.startsWith(buildArea + sep) && profile.includes('swagger-ui-test-')) {
        rmSync(profile, { recursive: true, force: true, maxRetries: 10, retryDelay: 250 });
    }
}
