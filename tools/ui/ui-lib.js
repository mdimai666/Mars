// Общие хелперы tools/ui: аргументы, браузер (msedge), auth через storageState.
// Учётка для автологина: tools/ui/auth.local.json {"user":"...","pass":"..."} (не в git)
// или env MARS_UI_USER / MARS_UI_PASS. После первого логина сохраняется auth.json.
const fs = require('fs');
const path = require('path');

const AUTH_FILE = path.join(__dirname, 'auth.json');
const CREDS_FILE = path.join(__dirname, 'auth.local.json');

function parseArgs(argv) {
  const args = { _: [] };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a.startsWith('--')) {
      const key = a.slice(2);
      const next = argv[i + 1];
      let val;
      if (next === undefined || next.startsWith('--')) val = true;
      else { val = next; i++; }
      if (key in args) args[key] = [].concat(args[key], val);
      else args[key] = val;
    } else args._.push(a);
  }
  return args;
}

async function launch(headless = true) {
  const { chromium } = require('playwright-core');
  return chromium.launch({ channel: 'msedge', headless });
}

function creds() {
  if (fs.existsSync(CREDS_FILE)) return JSON.parse(fs.readFileSync(CREDS_FILE, 'utf8'));
  const user = process.env.MARS_UI_USER;
  const pass = process.env.MARS_UI_PASS;
  if (user && pass) return { user, pass };
  return null;
}

async function login(page, { user, pass }) {
  const fill = async (name, value) => {
    for (const sel of [`[name='${name}'] input`, `input[name='${name}']`]) {
      const el = page.locator(sel).first();
      if (await el.count()) { await el.fill(value); return; }
    }
    throw new Error(`login field ${name} not found`);
  };
  await fill('login-email', user);
  await fill('password', pass);
  const btn = page.locator("fluent-button[type='submit']").first();
  if (await btn.count()) await btn.click({ timeout: 30000 }).catch(() => {});
  else await page.keyboard.press('Enter');
}

// Открывает браузер+страницу, логинится при необходимости, сохраняет auth.json.
// ВАЖНО: админка — Blazor WASM, решение «логин/шелл» принимается на клиенте ПОСЛЕ старта
// приложения, поэтому ждём появления шела (.admin-layout/fluent-nav) или формы логина.
const SHELL_SEL = '.admin-layout, .cloudy-layout, fluent-nav';
const LOGIN_SEL = "[name='login-email']";

async function newPage(browser, host, { viewport = { width: 1600, height: 1000 } } = {}) {
  const context = await browser.newContext({
    viewport,
    storageState: fs.existsSync(AUTH_FILE) ? AUTH_FILE : undefined,
  });
  const page = await context.newPage();
  await page.goto(`${host}/dev`, { waitUntil: 'domcontentloaded', timeout: 180000 });
  await page.waitForFunction(
    ({ shellSel, loginSel }) => !!(document.querySelector(shellSel) || document.querySelector(loginSel)),
    { shellSel: SHELL_SEL, loginSel: LOGIN_SEL },
    { timeout: 180000 },
  );
  if (!(await page.locator(SHELL_SEL).count())) {
    const c = creds();
    if (!c) throw new Error('Нет auth: создайте tools/ui/auth.local.json {"user","pass"} или задайте MARS_UI_USER/MARS_UI_PASS (см. README.md)');
    await login(page, c);
    await page.waitForSelector(SHELL_SEL, { timeout: 120000 });
  }
  await context.storageState({ path: AUTH_FILE });
  return { context, page };
}

// computed-стили + бокс первого элемента (locator пробивает open shadow DOM).
async function measure(page, sel, props, { limit = 3, varsFilter = null } = {}) {
  const loc = page.locator(sel);
  const n = Math.min(await loc.count(), limit);
  const out = [];
  for (let i = 0; i < n; i++) {
    out.push(await loc.nth(i).evaluate((el, { props, varsFilter }) => {
      const cs = getComputedStyle(el);
      const style = {};
      for (const p of props) style[p] = cs.getPropertyValue(p).trim();
      if (varsFilter) {
        const re = new RegExp(varsFilter, 'i');
        const vars = {};
        const seen = new Set();
        for (const src of [el.style, cs]) {
          for (let j = 0; j < src.length; j++) {
            const name = src[j];
            if (name.startsWith('--') && re.test(name) && !seen.has(name)) {
              seen.add(name);
              vars[name] = cs.getPropertyValue(name).trim();
            }
          }
        }
        style.__vars = vars;
      }
      const r = el.getBoundingClientRect();
      return {
        tag: el.tagName.toLowerCase(),
        cls: (el.getAttribute('class') || '').slice(0, 120),
        rect: { w: Math.round(r.width), h: Math.round(r.height), top: Math.round(r.top), left: Math.round(r.left) },
        style,
      };
    }, { props, varsFilter }));
  }
  return { sel, count: await loc.count(), measured: out };
}

const DEFAULT_PROPS = [
  'background-color', 'color', 'height', 'width', 'min-height',
  'padding-top', 'padding-bottom', 'padding-left', 'padding-right',
  'font-size', 'font-family', 'font-weight', 'line-height',
  'border-radius', 'text-decoration-line', 'display',
];

module.exports = { parseArgs, launch, newPage, measure, DEFAULT_PROPS, AUTH_FILE };
