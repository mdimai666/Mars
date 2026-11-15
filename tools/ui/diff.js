// Pixel-diff двух папок скриншотов (v4 baseline vs v5). Числовой отчёт, агент картинки не смотрит.
// Запуск: node diff.js --a out-v4 --b out-v5 [--threshold 0.5] [--out diff]
// threshold — процент расхождения на страницу, выше которого страница флагуется.
// Предварительно: crawl.js --shots --out out-v4 --host http://localhost:5003 (v4-база)
//                 crawl.js --shots --out out-v5 --host http://localhost:5005
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const pixelmatch = require('pixelmatch');
const { parseArgs } = require('./ui-lib');

const args = parseArgs(process.argv);
if (!args.a || !args.b) { console.error('нужны --a и --b (папки png)'); process.exit(2); }
const dirA = path.resolve(__dirname, String(args.a));
const dirB = path.resolve(__dirname, String(args.b));
const threshold = parseFloat(args.threshold || '0.5');
const outDir = path.resolve(__dirname, String(args.out || 'diff'));
fs.mkdirSync(outDir, { recursive: true });

const pngs = (d) => new Set(fs.readdirSync(d).filter((f) => f.endsWith('.png')));
const a = pngs(dirA), b = pngs(dirB);
const common = [...a].filter((f) => b.has(f)).sort();
const onlyA = [...a].filter((f) => !b.has(f));
const onlyB = [...b].filter((f) => !a.has(f));

const rows = [];
for (const f of common) {
  const imgA = PNG.sync.read(fs.readFileSync(path.join(dirA, f)));
  const imgB = PNG.sync.read(fs.readFileSync(path.join(dirB, f)));
  if (imgA.width !== imgB.width || imgA.height !== imgB.height) {
    rows.push({ file: f, status: 'SIZE_DIFF', a: `${imgA.width}x${imgA.height}`, b: `${imgB.width}x${imgB.height}` });
    continue;
  }
  const diff = new PNG({ width: imgA.width, height: imgA.height });
  const mismatched = pixelmatch(imgA.data, imgB.data, diff.data, imgA.width, imgA.height, { threshold: 0.1 });
  const pct = (mismatched / (imgA.width * imgA.height)) * 100;
  const row = { file: f, status: pct > threshold ? 'DIFF' : 'ok', mismatchPct: +pct.toFixed(2) };
  if (row.status === 'DIFF') fs.writeFileSync(path.join(outDir, f), PNG.sync.write(diff));
  rows.push(row);
}

const flagged = rows.filter((r) => r.status !== 'ok');
console.log(JSON.stringify({ common: common.length, onlyA, onlyB, threshold, flaggedCount: flagged.length, rows }, null, 2));
process.exit(flagged.length || onlyA.length || onlyB.length ? 1 : 0);
