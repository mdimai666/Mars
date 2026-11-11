// Прогон страниц по viewport'ам: нет горизонтального overflow, ключевые контролы
// досягаемы (scrollIntoView + hit-test центра), iframe ненулевой. Конфиг контролов —
// responsive.json (all + pages с glob-ключами, как в assertions.json).
// Запуск: node responsive.js [--host http://localhost:5005] [--pages /dev,/dev/Post]
//                            [--widths 1600,1200,900,768,390] [--height 1000]
//                            [--shots] [--out out]
// Высота: --height для width>=600, 844 для мобильных (<600). Скриншоты только --shots.
// Exit code: 1 если есть падения. Отчёт: out/responsive-report.json.
const fs = require('fs');
const path = require('path');
const { parseArgs, launch, newPage } = require('./ui-lib');

const args = parseArgs(process.argv);
const HOST = args.host || 'http://localhost:5005';
const OUT = path.resolve(__dirname, args.out || 'out');
const SHOTS = !!args.shots;
const WIDTHS = String(args.widths || '1600,1200,900,768,390').split(',').map((w) => parseInt(w, 10));
const HEIGHT = parseInt(args.height || '1000', 10);
const MOBILE_HEIGHT = 844;

// Репрезентативный набор: облачный шелл, списки, профиль, tech-зона, front editor,
// галерея, datasource. Полный список страниц — в crawl.js; сюда добавлять точечно.
const DEFAULT_PAGES = [
  '/dev', '/dev/Post', '/dev/Users', '/dev/Profile', '/dev/Settings',
  '/dev/tech', '/dev/tech/automation', '/dev/tech/frontend',
  '/dev/tech/frontend/editor/admin', '/dev/tech/style-gallery', '/dev/tech/datasource',
];
const PAGES = args.pages ? String(args.pages).split(',') : DEFAULT_PAGES;

function loadConfig() {
  const f = path.join(__dirname, 'responsive.json');
  return fs.existsSync(f) ? JSON.parse(fs.readFileSync(f, 'utf8')) : { all: {}, pages: {} };
}

function matchGlob(pattern, p) {
  return pattern === p || (pattern.endsWith('*') && p.startsWith(pattern.slice(0, -1)));
}

function configFor(cfg, p) {
  const parts = [cfg.all || {}, ...Object.entries(cfg.pages || {}).filter(([k]) => matchGlob(k, p)).map(([, v]) => v)];
  return {
    controls: parts.flatMap((x) => x.controls || []),
    iframes: parts.flatMap((x) => x.iframes || []),
  };
}

async function checkOverflow(page) {
  const ov = await page.evaluate(() => {
    const vw = window.innerWidth;
    const de = document.documentElement.scrollWidth;
    const body = document.body ? document.body.scrollWidth : 0;
    return { vw, scrollWidth: Math.max(de, body) };
  });
  return { ok: ov.scrollWidth <= ov.vw + 1, detail: `scrollWidth=${ov.scrollWidth} vw=${ov.vw}` };
}

// Досягаемость: видимость, скролл в центр viewport, бокс в горизонтальных границах,
// hit-test центра (elementFromPoint с пробитием shadow DOM) — не перекрыт другим элементом.
async function checkControl(page, sel) {
  const loc = page.locator(sel);
  if ((await loc.count()) === 0) return { ok: false, detail: 'not found' };
  const el = loc.first();
  if (!(await el.isVisible().catch(() => false))) return { ok: false, detail: 'not visible' };
  await el.evaluate((e) => e.scrollIntoView({ block: 'center', inline: 'center' })).catch(() => {});
  await page.waitForTimeout(150);
  const info = await el.evaluate((e) => {
    const r = e.getBoundingClientRect();
    if (r.width <= 0 || r.height <= 0) return { zero: true };
    const hit = document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2);
    let reachable = false;
    if (hit) {
      let node = hit;
      while (node) {
        if (node === e) { reachable = true; break; }
        const root = node.getRootNode();
        node = node.parentElement || (root instanceof ShadowRoot ? root.host : null);
      }
      if (!reachable) reachable = e.contains(hit);
    }
    return {
      zero: false,
      rect: { x: Math.round(r.x), y: Math.round(r.y), w: Math.round(r.width), h: Math.round(r.height) },
      vw: window.innerWidth,
      hit: hit ? hit.tagName.toLowerCase() : null,
      reachable,
    };
  });
  if (info.zero) return { ok: false, detail: 'zero box' };
  if (info.rect.x < -1 || info.rect.x + info.rect.w > info.vw + 1) {
    return { ok: false, detail: `outside viewport horizontally: ${JSON.stringify(info.rect)} vw=${info.vw}` };
  }
  if (!info.reachable) return { ok: false, detail: `covered at center (hit=${info.hit}) rect=${JSON.stringify(info.rect)}` };
  return { ok: true, detail: `${info.rect.w}x${info.rect.h} @(${info.rect.x},${info.rect.y})` };
}

async function checkIframe(page, sel) {
  const loc = page.locator(sel);
  if ((await loc.count()) === 0) return { ok: false, detail: 'not found' };
  const b = await loc.first().boundingBox();
  if (!b || b.width < 1 || b.height < 1) return { ok: false, detail: `zero box: ${JSON.stringify(b)}` };
  return { ok: true, detail: `${Math.round(b.width)}x${Math.round(b.height)}` };
}

(async () => {
  const cfg = loadConfig();
  const browser = await launch(true);
  const { page } = await newPage(browser, HOST);

  const report = [];
  let failed = 0;

  for (const p of PAGES) {
    const { controls, iframes } = configFor(cfg, p);
    for (const w of WIDTHS) {
      const h = w < 600 ? MOBILE_HEIGHT : HEIGHT;
      const entry = { page: p, width: w, height: h, status: 'ok', checks: [] };
      try {
        await page.setViewportSize({ width: w, height: h });
        await page.goto(`${HOST}${p}`, { waitUntil: 'domcontentloaded', timeout: 90000 });
        await page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {});
        await page.waitForSelector('.admin-layout, .cloudy-layout, .cloudy-tech, fluent-nav', { timeout: 60000 }).catch(() => {});
        await page.waitForTimeout(1200);
        const blazorErr = await page.$('#blazor-error-ui');
        if (blazorErr && (await blazorErr.isVisible().catch(() => false))) { entry.status = 'BLAZOR_ERROR_UI'; failed++; }

        const ov = await checkOverflow(page);
        entry.checks.push({ name: 'no horizontal overflow', ...ov });
        if (!ov.ok) failed++;

        for (const c of controls) {
          const spec = typeof c === 'string' ? { sel: c } : c;
          if (spec.minWidth && w < spec.minWidth) continue;
          const r = await checkControl(page, spec.sel);
          if (!r.ok && spec.optional) { r.ok = true; r.detail += ' (optional)'; }
          entry.checks.push({ name: `control ${spec.sel}`, ...r });
          if (!r.ok) failed++;
        }
        for (const s of iframes) {
          const r = await checkIframe(page, s);
          entry.checks.push({ name: `iframe ${s}`, ...r });
          if (!r.ok) failed++;
        }
        if (SHOTS) {
          fs.mkdirSync(path.join(OUT, 'responsive'), { recursive: true });
          await page.screenshot({ path: path.join(OUT, 'responsive', `${p.replace(/\//g, '_') || '_root'}_${w}.png`), fullPage: true });
        }
      } catch (e) {
        entry.status = 'NAV_ERROR';
        entry.error = String(e).slice(0, 200);
        failed++;
      }
      const bad = entry.checks.filter((c) => !c.ok);
      console.log(`${p} @${w}x${h} -> ${entry.status}${bad.length ? ` | FAIL: ${bad.map((b) => `${b.name} (${b.detail})`).join('; ')}` : ''}`);
      report.push(entry);
    }
  }

  fs.mkdirSync(OUT, { recursive: true });
  const reportFile = path.join(OUT, 'responsive-report.json');
  fs.writeFileSync(reportFile, JSON.stringify({ host: HOST, widths: WIDTHS, report, failed }, null, 2));
  console.log(`\nDONE. failed=${failed}. Report: ${reportFile}`);
  await browser.close();
  process.exit(failed > 0 ? 1 : 0);
})().catch((e) => { console.error('FATAL', e.message || e); process.exit(2); });