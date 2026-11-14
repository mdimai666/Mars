// Обход страниц админки: консоль-ошибки + DOM-assertions из assertions.json.
// Скриншоты НЕ снимаются по умолчанию (только --shots, для пользователя).
// Запуск: node crawl.js [--host http://localhost:5005] [--pages /dev,/dev/Post]
//                      [--shots] [--out out] [--skip-nodes] [--skip-front-menu] [--skip-navmenu] [--no-assert]
// box-assertions: min/max-размеры + геометрия — withinViewport (бокс целиком в
// viewport, допуск 1px) и noOverlap (нет пересечения с другими селекторами).
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
      '/dev/Media', '/dev/Users', '/dev/UserType',
      '/dev/Profile', '/dev/Profile/Edit', '/dev/Profile/ApiKeys', '/dev/Profile/Passkeys',
      '/dev/NavMenu', '/dev/FeedbackList', '/dev/Settings',
      '/dev/Settings/HostCache', '/dev/Settings/About', '/dev/Settings/Front',
      '/dev/tech', '/dev/tech/models',
      '/dev/tech/models/classmodels', '/dev/tech/scheduler', '/dev/tech/components',
      '/dev/tech/playbooks', '/dev/tech/docker', '/dev/tech/devtest',
      '/dev/tech/styledesigner', '/dev/tech/style-gallery',
      '/dev/tech/nodes', '/dev/tech/automation', '/dev/tech/forms', '/dev/tech/frontend',
      '/dev/tech/frontend/editor/admin', '/dev/tech/plugins',
      '/dev/tech/datasource', '/dev/tech/datasource/query', '/dev/tech/datasource/config', '/dev/tech/datasource/actions',
    ];

// 'HotReload.BrowserTools' — 404 конфига browser-tools при serve.ps1 -Watch (артефакт watch, не продукта).
const IGNORE_CONSOLE = ['favicon', 'blazor.webassembly.js', 'The resource https://', 'HotReload.BrowserTools'];

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
      if (n >= min) return ok(`count=${n}`);
      return a.optional ? ok(`count=${n} (optional)`) : fail(`${a.selector}: count=${n} < ${min}`);
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
    // selector + css / box / text
    const loc = page.locator(a.selector);
    const n = await loc.count();
    if (n === 0) return a.optional ? ok('absent (optional)') : fail(`${a.selector}: not found`);
    const el = loc.first();
    if (a.text) {
      const t = ((await el.textContent()) || '').trim();
      if (a.text.contains && !t.includes(a.text.contains)) return fail(`${a.selector}: text "${t.slice(0, 40)}" !contains "${a.text.contains}"`);
      if (a.text.equals && t !== a.text.equals) return fail(`${a.selector}: text "${t.slice(0, 40)}" != "${a.text.equals}"`);
      return ok(a.selector);
    }
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
      if (a.box.withinViewport) {
        const vp = page.viewportSize();
        const box = { x: Math.round(b.x), y: Math.round(b.y), w: Math.round(b.width), h: Math.round(b.height) };
        if (b.x < -1 || b.y < -1 || b.x + b.width > vp.width + 1 || b.y + b.height > vp.height + 1) {
          return fail(`${a.selector} outside viewport: box=${JSON.stringify(box)} viewport=${vp.width}x${vp.height}`);
        }
      }
      if (a.box.noOverlap) {
        for (const otherSel of [].concat(a.box.noOverlap)) {
          const others = page.locator(otherSel);
          const m = Math.min(await others.count(), 50);
          for (let i = 0; i < m; i++) {
            const o = others.nth(i);
            const handle = await o.elementHandle();
            if (!handle) continue;
            // сам элемент и его предки/потомки пересекаются тривиально — пропускаем
            const related = await el.evaluate((e, other) => e === other || e.contains(other) || other.contains(e), handle);
            if (related) continue;
            const ob = await o.boundingBox();
            if (!ob) continue;
            const ix = Math.min(b.x + b.width, ob.x + ob.width) - Math.max(b.x, ob.x);
            const iy = Math.min(b.y + b.height, ob.y + ob.height) - Math.max(b.y, ob.y);
            if (ix > 1 && iy > 1) {
              return fail(`${a.selector} overlaps ${otherSel}[${i}] by ${Math.round(ix)}x${Math.round(iy)}px`);
            }
          }
        }
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
    if (m.type() !== 'error' && m.type() !== 'warning') return;
    // у resource-ошибок URL живёт в location, а не в text — иначе ignore-лист не работает
    const loc = m.location()?.url || '';
    const text = `${m.text()}${loc ? ` @ ${loc}` : ''}`;
    if (!IGNORE_CONSOLE.some((x) => text.includes(x))) {
      consoleErrors.push({ url: page.url(), type: m.type(), text: text.slice(0, 300) });
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
      await page.waitForSelector('.admin-layout, .cloudy-layout, .cloudy-tech, fluent-nav', { timeout: 60000 }).catch(() => {});
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

  // Инлайн-панель редактора ноды (NodeEditContainer1 в правой панели NodeEditor1):
  // dblclick по ноде открывает .node-edit-panel, крестик закрывает, консоль возвращается.
  if (!args['skip-nodes']) {
    const entry = { page: '/dev/tech/automation#node-panel', status: 'ok', assertions: [] };
    const errBefore = consoleErrors.length;
    try {
      await page.goto(`${HOST}/dev/tech/automation`, { waitUntil: 'domcontentloaded', timeout: 90000 });
      await page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {});
      await page.waitForSelector('.red-ui-flow-node__body', { timeout: 30000 });
      await page.waitForTimeout(800);
      await page.locator('.red-ui-flow-node__body').first().dblclick();
      await page.waitForTimeout(1200);
      const info = await page.evaluate(() => {
        const side = document.querySelector('.ne-side');
        const panel = document.querySelector('.ne-side .node-edit-panel');
        if (!panel) return { visible: false, editingClass: side ? side.classList.contains('ne-side--editing') : false };
        const r = panel.getBoundingClientRect();
        return { visible: r.height > 0, box: { x: Math.round(r.x), y: Math.round(r.y), width: Math.round(r.width), height: Math.round(r.height) }, editingClass: side ? side.classList.contains('ne-side--editing') : false };
      });
      entry.panel = info;
      if (!info.visible || !info.editingClass) { entry.status = 'NODE_PANEL_NOT_OPEN'; failed++; }
      else if (info.box.width < 300) { entry.assertions.push({ name: 'node panel width', ok: false, detail: `width=${info.box.width} < 300` }); failed++; }
      else entry.assertions.push({ name: 'node panel width', ok: true, detail: `width=${info.box.width}` });
      if (info.visible) {
        const vp = page.viewportSize();
        const b = info.box;
        const inside = b.x >= -1 && b.y >= -1 && b.x + b.width <= vp.width + 1 && b.y + b.height <= vp.height + 1;
        entry.assertions.push({ name: 'node panel within viewport', ok: inside, detail: `box=${JSON.stringify(b)} viewport=${vp.width}x${vp.height}` });
        if (!inside) failed++;
      }
      await page.locator('.node-edit-panel .nep-close').click();
      await page.waitForTimeout(800);
      entry.closedAfterX = await page.evaluate(() => {
        const panel = document.querySelector('.ne-side .node-edit-panel');
        const consoleVisible = document.querySelector('.ne-side .DebugMessagesConsole');
        return !panel && !!consoleVisible;
      });
      if (!entry.closedAfterX) failed++;
      entry.consoleErrors = consoleErrors.slice(errBefore).map((e) => `${e.type}: ${e.text}`);
    } catch (e) {
      entry.status = 'NODE_PANEL_ERROR';
      entry.error = String(e).slice(0, 300);
      failed++;
    }
    console.log('node panel:', entry.status, JSON.stringify(entry.panel || {}), 'closed-after-x:', entry.closedAfterX);
    report.push(entry);
  }

  // Контекстное меню дерева файлов front editor (регрессия позиционирования
  // OpenMenuAsync: меню открывается в курсоре и целиком помещается в viewport;
  // баг-образец — меню уезжало на top=1252 при viewport 1000px).
  if (!args['skip-front-menu']) {
    const entry = { page: '/dev/tech/frontend/editor/admin#tree-context-menu', status: 'ok', assertions: [] };
    const errBefore = consoleErrors.length;
    try {
      await page.goto(`${HOST}/dev/tech/frontend/editor/admin`, { waitUntil: 'domcontentloaded', timeout: 90000 });
      await page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {});
      await page.waitForSelector('.tree-item-content', { timeout: 60000 });
      await page.waitForTimeout(800);
      const item = page.locator('.tree-item-content').last();
      await item.scrollIntoViewIfNeeded();
      await item.click({ button: 'right' });
      await page.waitForTimeout(1000);
      const info = await page.evaluate(() => {
        // открытый popup FluentMenu: slotted fluent-menu-list с ненулевым боксом
        const lists = [...document.querySelectorAll('.files-panel fluent-menu fluent-menu-list')];
        const open = lists.map((l) => ({ l, r: l.getBoundingClientRect() })).find((x) => x.r.width > 0 && x.r.height > 0);
        if (!open) return { visible: false };
        const r = open.r;
        return { visible: true, box: { x: Math.round(r.x), y: Math.round(r.y), width: Math.round(r.width), height: Math.round(r.height) } };
      });
      entry.menu = info;
      const vp = page.viewportSize();
      if (!info.visible) { entry.status = 'MENU_NOT_OPEN'; failed++; }
      else {
        const b = info.box;
        const inside = b.x >= -1 && b.y >= -1 && b.x + b.width <= vp.width + 1 && b.y + b.height <= vp.height + 1;
        entry.assertions.push({ name: 'context menu within viewport', ok: inside, detail: `box=${JSON.stringify(b)} viewport=${vp.width}x${vp.height}` });
        if (!inside) failed++;
      }
      await page.keyboard.press('Escape');
      await page.waitForTimeout(600);
      entry.closedAfterEsc = await page.evaluate(() => {
        const lists = [...document.querySelectorAll('.files-panel fluent-menu fluent-menu-list')];
        return !lists.some((l) => { const r = l.getBoundingClientRect(); return r.width > 0 && r.height > 0; });
      });
      if (!entry.closedAfterEsc) failed++;
      entry.consoleErrors = consoleErrors.slice(errBefore).map((e) => `${e.type}: ${e.text}`);
      if (entry.consoleErrors.length) failed += entry.consoleErrors.length;
    } catch (e) {
      entry.status = 'FRONT_MENU_ERROR';
      entry.error = String(e).slice(0, 300);
      failed++;
    }
    console.log('front tree menu:', entry.status, JSON.stringify(entry.menu || {}), 'closed-after-esc:', entry.closedAfterEsc);
    report.push(entry);
  }

  // Дерево структуры меню навигации (регрессия «комка текста»: строки не
  // перекрываются, выбор открывает панель свойств, меню пункта в viewport и
  // закрывается Escape, drag&drop не ломает геометрию). Данные не сохраняются.
  if (!args['skip-navmenu']) {
    const entry = { page: '/dev/NavMenu#tree', status: 'ok', assertions: [] };
    const errBefore = consoleErrors.length;
    const rowGeo = () => page.evaluate(() => {
      const rows = [...document.querySelectorAll('.nmt-row')].map((r) => r.getBoundingClientRect());
      let overlap = 0;
      for (let i = 0; i < rows.length; i++)
        for (let j = i + 1; j < rows.length; j++) {
          const a = rows[i], b = rows[j];
          const ix = Math.min(a.right, b.right) - Math.max(a.left, b.left);
          const iy = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
          if (ix > 1 && iy > 1) overlap++;
        }
      return { count: rows.length, overlap, minH: rows.length ? Math.round(Math.min(...rows.map((r) => r.height))) : 0 };
    });
    try {
      await page.goto(`${HOST}/dev/NavMenu`, { waitUntil: 'domcontentloaded', timeout: 90000 });
      await page.waitForSelector('a[href*="/EditNavMenu/"]', { timeout: 60000 });
      const href = await page.locator('a[href*="/EditNavMenu/"]').first().getAttribute('href');
      await page.goto(`${HOST}${href}`, { waitUntil: 'domcontentloaded', timeout: 90000 });
      await page.waitForSelector('.nmt-row', { timeout: 60000 });
      await page.waitForTimeout(800);

      const geo = await rowGeo();
      entry.tree = geo;
      const push = (name, ok, detail) => { entry.assertions.push({ name, ok, detail }); if (!ok) failed++; };
      push('tree rows rendered', geo.count > 0, `count=${geo.count}`);
      push('tree rows do not overlap', geo.overlap === 0, `overlap=${geo.overlap}`);
      push('tree rows not collapsed', geo.minH >= 32, `minHeight=${geo.minH}`);

      await page.locator('.nmt-row').first().click();
      await page.waitForSelector('.nmt-props__form', { timeout: 10000 });
      push('selection opens props form', true, 'ok');

      await page.locator('.nmt-row').first().hover();
      await page.locator('.nmt-row__tools .nmt__iconbtn').first().click();
      await page.waitForSelector('.nmt-menu', { timeout: 10000 });
      await page.waitForTimeout(300);
      const mb = await page.locator('.nmt-menu').boundingBox();
      const vp = page.viewportSize();
      const inside = mb && mb.x >= -1 && mb.y >= -1 && mb.x + mb.width <= vp.width + 1 && mb.y + mb.height <= vp.height + 1;
      push('row menu within viewport', !!inside, `box=${JSON.stringify(mb && { x: Math.round(mb.x), y: Math.round(mb.y), w: Math.round(mb.width), h: Math.round(mb.height) })}`);
      await page.keyboard.press('Escape');
      await page.waitForTimeout(500);
      push('row menu closes on Escape', (await page.locator('.nmt-menu').count()) === 0, 'closed');

      const before = await page.locator('.nmt-row').count();
      await page.locator('.nmt-row').nth(1).dragTo(page.locator('.nmt-row').first());
      await page.waitForTimeout(800);
      const after = await rowGeo();
      push('drag&drop keeps rows intact', after.count === before && after.overlap === 0, `before=${before} after=${after.count} overlap=${after.overlap}`);
      entry.consoleErrors = consoleErrors.slice(errBefore).map((e) => `${e.type}: ${e.text}`);
      if (entry.consoleErrors.length) failed += entry.consoleErrors.length;
    } catch (e) {
      entry.status = 'NAVMENU_TREE_ERROR';
      entry.error = String(e).slice(0, 300);
      failed++;
    }
    console.log('navmenu tree:', entry.status, JSON.stringify(entry.tree || {}), entry.assertions.filter((a) => !a.ok).map((a) => `${a.name} (${a.detail})`).join('; '));
    report.push(entry);
  }

  fs.mkdirSync(OUT, { recursive: true });
  const reportFile = path.join(OUT, 'crawl-report.json');
  fs.writeFileSync(reportFile, JSON.stringify({ host: HOST, report, consoleErrors, failed }, null, 2));
  console.log(`\nDONE. failed=${failed}. Report: ${reportFile}`);
  await browser.close();
  process.exit(failed > 0 ? 1 : 0);
})().catch((e) => { console.error('FATAL', e.message || e); process.exit(2); });
