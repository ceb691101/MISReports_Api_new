import assert from 'node:assert/strict';
import { spawn, spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { dirname, join, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const baseUri = (process.argv[2] || 'http://localhost:44381').replace(/\/$/, '');
const buildArea = resolve(dirname(fileURLToPath(import.meta.url)), '../.build-area');
mkdirSync(buildArea, { recursive: true });
const profile = mkdtempSync(join(buildArea, 'solar-journals-swagger-'));
const browser = spawn(process.env.SWAGGER_TEST_BROWSER || 'C:/Program Files/Google/Chrome/Application/chrome.exe', [
    '--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
    '--disable-background-networking', '--remote-debugging-port=0', `--user-data-dir=${profile}`, 'about:blank'
], { stdio: 'ignore', windowsHide: true });
let socket;
let command;
let browserError;
browser.on('error', error => { browserError = error; });
const pause = ms => new Promise(resolve => setTimeout(resolve, ms));
async function until(callback, description, timeout = 60000) {
    const deadline = Date.now() + timeout;
    while (Date.now() < deadline) {
        if (browserError) throw browserError;
        const result = await callback();
        if (result) return result;
        await pause(100);
    }
    throw new Error(`Timed out waiting for ${description}.`);
}

try {
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
    const requests = [];
    const responses = new Map();
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
            const { request, requestId } = message.params;
            if (new URL(request.url).pathname.endsWith('/api/pucsl/solarJournalsRetail')) {
                requests.push({ id: requestId, method: request.method, body: JSON.parse(request.postData || '{}') });
            }
        }
        if (message.method === 'Network.responseReceived') responses.set(message.params.requestId, message.params.response.status);
        if (message.method === 'Runtime.exceptionThrown') errors.push(message.params.exceptionDetails.text);
    });
    command = (method, params = {}) => new Promise((resolve, reject) => {
        const id = ++sequence;
        const timer = setTimeout(() => { pending.delete(id); reject(new Error(`CDP timeout: ${method}`)); }, 15000);
        pending.set(id, { resolve, reject, timer });
        socket.send(JSON.stringify({ id, method, params }));
    });
    async function evaluate(expression) {
        const result = await command('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
        if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description || result.exceptionDetails.text);
        return result.result.value;
    }
    await command('Page.enable');
    await command('Network.enable');
    await command('Runtime.enable');
    await command('Network.setBlockedURLs', { urls: ['*://validator.swagger.io/*'] });
    await command('Page.navigate', { url: `${baseUri}/swagger/ui/index#!/PUCSL/PUCSL_GetSolarJournalsRetail` });
    await until(() => evaluate(`Boolean(window.journalForm = document.querySelector('form[data-solar-journals-retail]'))`), 'Solar Journals input fields');
    await until(() => evaluate(`!journalForm.querySelector('[name=billCycle]').disabled && !journalForm.querySelector('[name=locationCode]').disabled`), 'initial bill cycles and areas');
    const initial = await evaluate(`({
        types: Array.from(journalForm.querySelector('[name=areaType]').options).map(option => option.value),
        cycles: journalForm.querySelector('[name=billCycle]').options.length,
        cycle: journalForm.querySelector('[name=billCycle]').value,
        firstCycle: journalForm.querySelector('[name=billCycle]').options[0].value,
        bodyHidden: getComputedStyle(journalForm.querySelector('textarea[name=request]').closest('tr')).display === 'none'
    })`);
    assert.deepEqual(initial.types, ['Area', 'Province', 'Division', 'Entire CEB']);
    assert.ok(initial.cycles > 0);
    assert.equal(initial.cycle, initial.firstCycle);
    assert.equal(initial.bodyHidden, true);
    await evaluate(`journalForm.querySelector('.submit').click()`);
    await pause(200);
    assert.equal(requests.length, 0, 'A missing area must block the report request.');

    for (const type of ['Area', 'Province', 'Division', 'Entire CEB']) {
        await evaluate(`(() => {
            const select = journalForm.querySelector('[name=areaType]');
            select.value = ${JSON.stringify(type)};
            select.dispatchEvent(new Event('change', { bubbles: true }));
        })()`);
        if (type !== 'Entire CEB') {
            await until(() => evaluate(`!journalForm.querySelector('[name=locationCode]').disabled && journalForm.querySelector('[name=locationCode]').options.length > 1`), `${type} choices`);
        }
        assert.equal(await evaluate(`journalForm.querySelector('[name=locationCode]').value`), '', 'Changing area type must clear the previous location.');
        if (type === 'Entire CEB') assert.equal(await evaluate(`journalForm.querySelector('[name=locationCode]').disabled`), true);
        const location = await evaluate(`(() => {
            const select = journalForm.querySelector('[name=locationCode]');
            if (!select.disabled) select.selectedIndex = 1;
            select.dispatchEvent(new Event('change', { bubbles: true }));
            return select.value;
        })()`);
        const before = requests.length;
        await evaluate(`journalForm.querySelector('.submit').click()`);
        await until(() => requests.length > before, `${type} POST request`);
        const request = requests.at(-1);
        assert.equal(request.method, 'POST');
        assert.deepEqual(request.body, { AreaType: type, LocationCode: location, BillCycle: initial.cycle });
        await until(() => responses.has(request.id), `${type} report response`);
        assert.equal(responses.get(request.id), 200, `${type} report failed.`);
        console.log(`PASS: ${type} fields send the correct JSON POST and receive HTTP 200.`);
    }
    assert.deepEqual(errors, [], 'Swagger has JavaScript runtime errors.');
    console.log('PASS: Latest bill cycle selected, raw JSON hidden, missing location blocked, and dependent selections reset.');
} finally {
    if (socket?.readyState === WebSocket.OPEN && command) {
        await command('Browser.close').catch(() => {});
        await until(() => browser.exitCode !== null, 'browser shutdown', 10000).catch(() => {});
    }
    socket?.close();
    if (browser.pid && browser.exitCode === null) {
        spawnSync('taskkill', ['/pid', String(browser.pid), '/T', '/F'], { stdio: 'ignore', windowsHide: true });
        await until(() => browser.exitCode !== null, 'browser exit', 10000).catch(() => {});
    }
    if (profile.startsWith(buildArea + sep) && profile.includes('solar-journals-swagger-')) {
        rmSync(profile, { recursive: true, force: true, maxRetries: 10, retryDelay: 250 });
    }
}
