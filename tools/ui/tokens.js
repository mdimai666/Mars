// Дамп CSS-переменных темы v5: inline на <html> (стиль-атрибут), computed на <html>,
// adoptedStyleSheets (:root/html-правила). Для снапшотов/diff тем и проверки моста.
// Запуск: node tokens.js [--host http://localhost:5005] [--page /dev] [--out tokens.json] [--filter "mars|color"]
const fs = require('fs');
const path = require('path');
const { parseArgs, launch, newPage } = require('./ui-lib');

const args = parseArgs(process.argv);
const HOST = args.host || 'http://localhost:5005';
const PAGE_PATH = args.page || '/dev';

(async () => {
  const browser = await launch(true);
  const { page } = await newPage(browser, HOST);
  await page.goto(`${HOST}${PAGE_PATH}`, { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(2000);

  const dump = await page.evaluate(() => {
    const html = document.documentElement;
    const cs = getComputedStyle(html);
    const from = (src, into, tag) => {
      for (let i = 0; i < src.length; i++) {
        const name = src[i];
        if (name.startsWith('--') && !(name in into)) into[name] = { value: cs.getPropertyValue(name).trim(), source: tag };
      }
    };
    const tokens = {};
    from(html.style, tokens, 'inline');
    for (let i = 0; i < cs.length; i++) {
      const name = cs[i];
      if (name.startsWith('--') && !(name in tokens)) tokens[name] = { value: cs.getPropertyValue(name).trim(), source: 'computed' };
    }
    for (const sheet of document.adoptedStyleSheets || []) {
      let rules; try { rules = sheet.cssRules; } catch { continue; }
      for (const rule of rules) {
        if (!rule.selectorText || !/^(html|:root)/.test(rule.selectorText)) continue;
        for (const prop of rule.style || []) {
          if (prop.startsWith('--') && !(prop in tokens)) tokens[prop] = { value: rule.style.getPropertyValue(prop).trim(), source: `adopted:${rule.selectorText}` };
        }
      }
    }
    const bodyTheme = document.body ? document.body.getAttribute('data-theme') : null;
    return { bodyTheme, tokens };
  });

  let entries = Object.entries(dump.tokens).sort();
  if (args.filter) {
    const re = new RegExp(String(args.filter), 'i');
    entries = entries.filter(([k]) => re.test(k));
  }
  const out = { host: HOST, page: PAGE_PATH, bodyTheme: dump.bodyTheme, count: entries.length, tokens: Object.fromEntries(entries) };
  const text = JSON.stringify(out, null, 2);
  if (args.out) {
    const f = path.resolve(__dirname, String(args.out));
    fs.mkdirSync(path.dirname(f), { recursive: true });
    fs.writeFileSync(f, text);
    console.log(`written ${f} (${out.count} tokens, bodyTheme=${dump.bodyTheme})`);
  } else console.log(text);
  await browser.close();
})().catch((e) => { console.error('FATAL', e.message || e); process.exit(2); });
