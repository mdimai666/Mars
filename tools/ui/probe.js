// Точечный замер computed-стилей и боксов — для диагностики «почему это выглядит не так».
// Запуск: node probe.js --page /dev/Post --sel body --sel ".fluent-data-grid th"
//         [--host http://localhost:5005] [--props background-color,color] [--vars --filter "mars|color"]
//         [--limit 3]
// Вывод: JSON в stdout. Скриншоты не делает.
const { parseArgs, launch, newPage, measure, DEFAULT_PROPS } = require('./ui-lib');

const args = parseArgs(process.argv);
const HOST = args.host || 'http://localhost:5005';
const PAGE_PATH = args.page || '/dev';
const sels = args.sel ? [].concat(args.sel) : [];
if (!sels.length) { console.error('нужен хотя бы один --sel'); process.exit(2); }
const props = args.props ? String(args.props).split(',') : DEFAULT_PROPS;
const varsFilter = args.vars ? String(args.filter || '--') : null;
const limit = parseInt(args.limit || '3', 10);

(async () => {
  const browser = await launch(true);
  const { page } = await newPage(browser, HOST);
  await page.goto(`${HOST}${PAGE_PATH}`, { waitUntil: 'domcontentloaded', timeout: 90000 });
  await page.waitForLoadState('networkidle', { timeout: 30000 }).catch(() => {});
  await page.waitForSelector('.admin-layout, fluent-nav', { timeout: 60000 }).catch(() => {});
  if (args.wait) await page.waitForSelector(String(args.wait), { timeout: 120000 });
  await page.waitForTimeout(1500);
  const out = [];
  for (const sel of sels) {
    out.push(await measure(page, sel, props, { limit, varsFilter }));
  }
  console.log(JSON.stringify({ host: HOST, page: PAGE_PATH, results: out }, null, 2));
  await browser.close();
})().catch((e) => { console.error('FATAL', e.message || e); process.exit(2); });
