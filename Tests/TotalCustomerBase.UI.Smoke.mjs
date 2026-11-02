import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { dirname, join, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

// Mount the actual report in Vite so checks do not depend on login or sidebar state.
const base = (process.argv[2] || 'http://localhost:5173').replace(/\/$/, '');
const buildRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../.build-area');
mkdirSync(buildRoot, { recursive: true });
const profile = mkdtempSync(join(buildRoot, 'customer-base-ui-'));
const browser = spawn(process.env.SWAGGER_TEST_BROWSER || 'C:/Program Files/Google/Chrome/Application/chrome.exe', [
    '--headless=new', '--disable-gpu', '--no-first-run', '--no-default-browser-check',
    '--disable-background-networking', '--remote-debugging-port=0', `--user-data-dir=${profile}`, 'about:blank'
], { stdio: 'ignore', windowsHide: true });
let socket, browserError;
browser.on('error', error => { browserError = error; });
const pause = ms => new Promise(resolve => setTimeout(resolve, ms));
async function until(callback, label) {
    const deadline = Date.now() + 45000;
    while (Date.now() < deadline) {
        if (browserError) throw browserError;
        const result = await callback();
        if (result) return result;
        await pause(100);
    }
    throw new Error(`Timeout: ${label}`);
}

try {
    const portFile = join(profile, 'DevToolsActivePort');
    await until(() => existsSync(portFile), 'Chrome startup');
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
    socket.addEventListener('message', event => {
        const message = JSON.parse(event.data);
        if (message.method === 'Network.requestWillBeSent') {
            const url = new URL(message.params.request.url);
            if (url.pathname === '/misapi/api/general/total-customer-base') reportRequests.push(url);
        }
        const handler = pending.get(message.id);
        if (!handler) return;
        pending.delete(message.id);
        clearTimeout(handler.timer);
        message.error ? handler.reject(new Error(message.error.message)) : handler.resolve(message.result);
    });
    const command = (method, params = {}) => new Promise((resolve, reject) => {
        const id = ++sequence;
        const timer = setTimeout(() => { pending.delete(id); reject(new Error(`${method} timed out`)); }, 45000);
        pending.set(id, { resolve, reject, timer });
        socket.send(JSON.stringify({ id, method, params }));
    });
    async function evaluate(expression) {
        const result = await command('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
        if (result.exceptionDetails) throw new Error(result.exceptionDetails.exception?.description || result.exceptionDetails.text);
        return result.result.value;
    }
    await command('Page.enable');
    await command('Runtime.enable');
    await command('Network.enable');
    await command('Page.navigate', { url: base });
    await until(() => evaluate('Boolean(window.$RefreshReg$)'), 'Vite React preamble');
    await evaluate(`(async () => {
        const [react, dom, report] = await Promise.all([
            import('/node_modules/.vite/deps/react.js'),
            import('/node_modules/.vite/deps/react-dom_client.js'),
            import('/src/mainTopics/general/TotalCustomerBase.tsx')
        ]);
        const React = react.default ?? react;
        const ReactDOM = dom.default ?? dom;
        const host = document.createElement('div');
        host.id = 'customer-base-check';
        document.body.append(host);
        ReactDOM.createRoot(host).render(React.createElement(report.default));
    })()`);
    await until(() => evaluate(`Boolean(document.querySelector('#customer-base-check tbody tr'))`), 'automatic report load');
    assert.equal(await evaluate(`document.querySelectorAll('#customer-base-check select').length`), 0);
    {
        const result = await evaluate(`(() => {
            const root = document.getElementById('customer-base-check');
            const counts = Array.from(root.querySelectorAll('tbody tr'), row =>
                Array.from(row.querySelectorAll('td')).slice(3).map(cell => Number(cell.textContent.replaceAll(',', ''))));
            return {
                rows: counts.length,
                headers: root.querySelectorAll('thead th').length,
                totals: Array.from(root.querySelectorAll('tfoot td'), cell => Number(cell.textContent.replaceAll(',', ''))),
                sums: counts.reduce((sum, row) => [sum[0] + row[0], sum[1] + row[1]], [0, 0]),
                alerts: Array.from(root.querySelectorAll('[role="alert"]'), alert => alert.textContent)
            };
        })()`);
        assert.ok(result.rows > 0);
        assert.equal(result.headers, 5);
        assert.deepEqual(result.totals, result.sums);
        assert.deepEqual(result.alerts, []);
        assert.equal(reportRequests.length, 1);
        assert.equal(reportRequests[0].search, '');
        const response = await fetch(reportRequests[0], { signal: AbortSignal.timeout(60000) });
        assert.equal(response.status, 200);
        const expected = await response.json();
        assert.ok(!expected.errorMessage);
        assert.equal(result.rows, expected.data.Rows.length);
        assert.deepEqual(result.totals, expected.data.Rows.reduce((sum, row) => [sum[0] + row.AgTotal, sum[1] + row.FTotal], [0, 0]));
        const text = await evaluate(`document.getElementById('customer-base-check').textContent`);
        assert.ok(text.includes(expected.data.CurrentCycleLabel));
        assert.ok(text.includes(expected.data.PreviousCycleLabel));
        assert.equal(expected.data.PreviousCycle, expected.data.CurrentCycle - 1);
        console.log(`PASS: Automatic load without dropdowns; all ${result.rows} areas, cycle labels, and footer totals match the API.`);
        await evaluate(`document.querySelector('#customer-base-check button').click()`);
        await until(() => reportRequests.length === 2, 'Refresh request');
        await until(() => evaluate(`Boolean(document.querySelector('#customer-base-check tbody tr')) && !document.querySelector('#customer-base-check button').disabled`), 'refreshed report');
        assert.equal(reportRequests[1].search, '');
        console.log('PASS: Refresh re-detects the cycle and reloads all areas.');
    }
} finally {
    if (socket?.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ id: 99999, method: 'Browser.close' }));
        socket.close();
    }
    if (browser.exitCode === null)
        await Promise.race([new Promise(resolve => browser.once('exit', resolve)), pause(3000)]);
    if (browser.exitCode === null) browser.kill();
    const target = resolve(profile);
    assert.ok(target.startsWith(buildRoot + sep), 'Browser profile must stay within the build directory.');
    try { rmSync(target, { recursive: true, force: true, maxRetries: 3, retryDelay: 200 }); }
    catch (error) { console.log('Temporary browser profile retained:', error.code); }
}
