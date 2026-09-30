// Обход страниц админки: консоль-ошибки + DOM-assertions из assertions.json.
// Скриншоты НЕ снимаются по умолчанию (только --shots, для пользователя).
// Запуск: node crawl.js [--host http://localhost:5005] [--pages /dev,/dev/Post]
//                      [--shots] [--out out] [--skip-nodes] [--no-assert]
// Exit code: 1 если есть упавшие assertions / console-ошибки / навигационные сбои.
const fs = require('fs');
const path = require('path');
const { parseArgs, launch, newPage } = require('./ui-lib');

const args = parseArgs(process.argv);
const HOST = args.host || 'http://localhost:5005';
const OUT = path.resolve(__dirname, args.out || 'out');
const SHOTS = !!args.shots;
if (SHOTS) fs.mkdirSync(OUT, { recursive: true });

const PAGES = args.pages
  ? String(args.pages).split(',')
  : [
      '/dev', '/dev/Post', '/dev/PostType', '/dev/PostCategory', '/dev/PostCategoryType',
      '/dev/Media', '/dev/Users', '/dev/UserType', '/dev/ApiKeys', '/dev/Passkeys',
      '/dev/NavMenu', '/dev/Plugins', '/dev/FeedbackList', '/dev/Settings',
      '/dev/Settings/HostCache', '/dev/Settings/About', '/dev/Settings/Front',
      '/dev/builder', '/dev/builder/settings', '/dev/builder/models',
      '/dev/builder/models/classmodels', '/dev/builder/scheduler', '/dev/builder/com/list',
      '/dev/builder/playbooks', '/dev/builder/docker', '/dev/builder/debug',
      '/dev/builder/styledesigner', '/dev/builder/style-gallery',
      '/dev/Nodes', '/dev/nodered', '/dev/forms', '/dev/front',
      '/dev/datasource', '/dev/datasource/query', '/dev/datasource/config', '/dev/datasource/actions',
    ];

const IGNORE_CONSOLE = ['favicon', 'blazor.webassembly.js', 'The resource https://'];

function loadAssertions() {
  const f = path.join(__dirname, 'assertions.json');
  return fs.existsSync(f) ? JSON.parse(fs.readFileSync(f, 'utf8')) : { all: [], pages: {} };
}

function matchGlob(pattern, p) {
  return pattern === p || (pattern.endsWith('*') && p.startsWith(pattern.slice(0, -1)));
}

function assertionsFor(a, p) {
  return [...(a.all || []), ...Object.entries(a.pages || {}).filter(([k]) => matchGlob(k, p)).flatMap(([, v]) => v)];
}

const normColor = (s) => String(s).replace(/\s+/g, '').toLowerCase();

async function runAssertion(page, a) {
  const fail = (detail) => ({ name: a.name, ok: false, detail });
  const ok = (detail) => ({ name: a.name, ok: true, detail });
  try {
    if (a.absent) {
      const n = await page.locator(a.absent).count();
      return n === 0 ? ok(`count=0`) : fail(`${a.absent}: found ${n}`);
    }
    if (a.exists) {
      const n = await page.locator(a.selector).count();
      const min = a.exists.min ?? 1;
      return n >= min ? ok(`count=${n}`) : fail(`${a.selector}: count=${n} < ${min}`);
    }
    if (a.each) {
      const loc = page.locator(a.each);
      const n = Math.min(await loc.count(), 50);
      if (n === 0) return ok('no elements');
      const bad = [];
      for (let i = 0; i < n; i++) {
        const has = await loc.nth(i).evaluate((el, childSel) => {
          const root = el.shadowRoot || el;
          return !!(el.querySelector(childSel) || root.querySelector(childSel));
        }, a.hasChild);
        if (!has) bad.push(i);
      }
      return bad.length === 0 ? ok(`all ${n} ok`) : fail(`${a.each}: no ${a.hasChild} in #${bad.join(',#')} (${n} total)`);
    }
    // selector + css / box
    const loc = page.locator(a.selector);
    const n = await loc.count();
    if (n === 0) return a.optional ? ok('absent (optional)') : fail(`${a.selector}: not found`);
    const el = loc.first();
    if (a.css) {
      const actual = await el.evaluate((e, props) => {
        const cs = getComputedStyle(e);
        const out = {};
        for (const p of Object.keys(props)) out[p] = cs.getPropertyValue(p).trim();
        return out;
      }, a.css);
      for (const [prop, expected] of Object.entries(a.css)) {
        let got = actual[prop];
        if (/^(rgb|hsl)/.test(got)) got = normColor(got);
        const exp = /^(rgb|hsl)/.test(expected) ? normColor(expected) : expected;
        const m = /^\/(.*)\/$/.exec(exp);
        const pass = m ? new RegExp(m[1]).test(got) : got === exp;
        if (!pass) return fail(`${a.selector} ${prop}: "${got}" != "${exp}"`);
      }
      return ok(a.selector);
    }
    if (a.box) {
      const b = await el.boundingBox();
      if (!b) return fail(`${a.selector}: no box`);
      for (const [k, v] of Object.entries(a.box)) {
        const dim = k.toLowerCase().includes('height') ? b.height : b.width;
        const val = Math.round(dim);
        if (k.startsWith('max') && val > v) return fail(`${a.selector} ${k}: ${val} > ${v}`);
        if (k.startsWith('min') && val < v) return fail(`${a.selector} ${k}: ${val} < ${v}`);
      }
      return ok(a.selector);
    }
    return fail('unknown assertion shape');
  } catch (e) {
    return fail(String(e).slice(0, 200));
  }
}

(async () => {
  const assertions = args['no-assert'] ? { all: [], pages: {} } : loadAssertions();
  const browser = await launch(true);
  const { context, page } = await newPage(browser, HOST);

  const consoleErrors = [];
  page.on('console', (m) => {
    if ((m.type() === 'error' || m.type() === 'warning') && !IGNORE_CONSOLE.some((x) => m.text().includes(x))) {
      consoleErrors.push({ url: page.url(), type: m.type(), text: m.text().slice(0, 300) });
    }
  });
  page.on('pageerror', (e) => consoleErrors.push({ url: page.url(), type: 'pageerror', text: String(e).slice(0, 300) }));

  const report = [];
  let failed = 0;

  for (const p of PAGES) {
    const entry = { page: p, status: 'ok', assertions: [] };
    const errBefore = consoleErrors.length;
    try {
      await page.goto(`${HOST}${p}`, { waitUntil: 'domcontentloaded', timeout: 90000 });
      await page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {});
      await page.waitForSelector('.admin-layout, fluent-nav', { timeout: 60000 }).catch(() => {});
      await page.waitForTimeout(1200);
      const blazorErr = await page.$('#blazor-error-ui');
      if (blazorErr && (await blazorErr.isVisible().catch(() => false))) entry.status = 'BLAZOR_ERROR_UI';
      for (const a of assertionsFor(assertions, p)) {
        const r = await runAssertion(page, a);
        entry.assertions.push(r);
        if (!r.ok) failed++;
      }
      if (SHOTS) {
        fs.mkdirSync(OUT, { recursive: true });
        await page.screenshot({ path: path.join(OUT, `${p.replace(/\//g, '_') || '_root'}.png`), fullPage: true });
      }
      entry.consoleErrors = consoleErrors.slice(errBefore).map((e) => `${e.type}: ${e.text}`);
      if (entry.consoleErrors.length) failed += entry.consoleErrors.length;
    } catch (e) {
      entry.status = 'NAV_ERROR';
      entry.error = String(e).slice(0, 200);
      failed++;
    }
    const bad = entry.assertions.filter((r) => !r.ok);
    console.log(`${p} -> ${entry.status}${bad.length ? ` | FAIL: ${bad.map((b) => `${b.name} (${b.detail})`).join('; ')}` : ''}${entry.consoleErrors?.length ? ` | +${entry.consoleErrors.length} console` : ''}`);
    report.push(entry);
  }

  // Диалог редактора ноды (NodeEditContainer1): dblclick по ноде, ширина, закрытие по ESC.
  if (!args['skip-nodes']) {
    const entry = { page: '/dev/nodered#node-dialog', status: 'ok', assertions: [] };
    const errBefore = consoleErrors.length;
    try {
      await page.goto(`${HOST}/dev/nodered`, { waitUntil: 'domcontentloaded', timeout: 90000 });
      await page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {});
      await page.waitForSelector('.red-ui-flow-node__body', { timeout: 30000 });
      await page.waitForTimeout(800);
      await page.locator('.red-ui-flow-node__body').first().dblclick();
      await page.waitForTimeout(2000);
      const info = await page.evaluate(() => {
        const host = document.querySelector('fluent-dialog.NodeEditDialog');
        if (!host) return { visible: false };
        const inner = host.shadowRoot ? host.shadowRoot.querySelector('dialog') : null;
        const el = inner || host;
        const r = el.getBoundingClientRect();
        return { visible: r.height > 0 && (inner ? inner.open !== false : true), box: { width: Math.round(r.width), height: Math.round(r.height), bottom: Math.round(r.bottom) } };
      });
      entry.dialog = info;
      if (!info.visible) { entry.status = 'DIALOG_NOT_OPEN'; failed++; }
      else if (info.box.width < 1200) { entry.assertions.push({ name: 'node dialog wide', ok: false, detail: `width=${info.box.width} < 1200` }); failed++; }
      else entry.assertions.push({ name: 'node dialog wide', ok: true, detail: `width=${info.box.width}` });
      await page.keyboard.press('Escape');
      await page.waitForTimeout(1500);
      entry.closedAfterEsc = await page.evaluate(() => {
        const host = document.querySelector('fluent-dialog.NodeEditDialog');
        if (!host) return true;
        const inner = host.shadowRoot ? host.shadowRoot.querySelector('dialog') : null;
        if (inner && inner.open !== undefined) return !inner.open;
        return inner ? inner.getBoundingClientRect().height === 0 : host.getBoundingClientRect().height === 0;
      });
      if (!entry.closedAfterEsc) failed++;
      entry.consoleErrors = consoleErrors.slice(errBefore).map((e) => `${e.type}: ${e.text}`);
    } catch (e) {
      entry.status = 'NODE_DIALOG_ERROR';
      entry.error = String(e).slice(0, 300);
      failed++;
    }
    console.log('node dialog:', entry.status, JSON.stringify(entry.dialog || {}), 'esc-closed:', entry.closedAfterEsc);
    report.push(entry);
  }

  fs.mkdirSync(OUT, { recursive: true });
  const reportFile = path.join(OUT, 'crawl-report.json');
  fs.writeFileSync(reportFile, JSON.stringify({ host: HOST, report, consoleErrors, failed }, null, 2));
  console.log(`\nDONE. failed=${failed}. Report: ${reportFile}`);
  await browser.close();
  process.exit(failed > 0 ? 1 : 0);
})().catch((e) => { console.error('FATAL', e.message || e); process.exit(2); });
