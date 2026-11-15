// Sweep мёртвых v4-параметров/компонентов FluentUI по .razor/.razor.cs/.cs.
// Список паттернов: fenced-блок ```json с маркером "sweep-dead-params" из ai/FluentV5Reference.md;
// если блока нет — встроенный список (синхронизирован с находками FluentV5MigrationPlan.md).
// Консервативен: только репорт file:line для ручной/агентской проверки, ничего не правит.
// Запуск: node sweep.js [--root ../..] [--ref ai/FluentV5Reference.md]
const fs = require('fs');
const path = require('path');
const { parseArgs } = require('./ui-lib');

const args = parseArgs(process.argv);
const ROOT = path.resolve(__dirname, String(args.root || '../..'));
const REF = path.resolve(ROOT, String(args.ref || 'ai/FluentV5Reference.md'));

const BUILTIN = {
  patterns: [
    { re: '\\bUseMenuService\\b', note: 'v4 FluentMenu-параметр (мёртв)' },
    { re: '\\bTypo\\s*=', note: 'v4 FluentLabel.Typo (мёртв; v5 — FluentText Size)' },
    { re: '\\bFill\\s*=', note: 'v4 FluentBadge.Fill (мёртв; v5 — BadgeColor)' },
    { re: '\\bThresholds\\s*=', note: 'v4-параметр (мёртв)' },
    { re: '\\bEnctype\\s*=', note: 'v4 FluentButton.Enctype → FormEncType' },
    { re: '\\bColumnOptionsLabels', note: 'v4 DataGrid → ColumnOptionsUISettings' },
    { re: '\\bColumnResizeLabels', note: 'v4 DataGrid → ColumnResizeUISettings' },
    { re: '\\bListItemFilteredColor', note: 'v4 FluentSortableList (мёртв)' },
    { re: '\\bListBorderWidth', note: 'v4 FluentSortableList (мёртв)' },
    { re: '\\bListItemHeight\\s*=', note: 'v4 FluentSortableList → Style --fluent-sortable-list-item-height' },
    { re: '\\bFluentNavMenu|FluentNavGroup|FluentNavLink\\b', note: 'удалены → FluentNav/FluentNavCategory/FluentNavItem' },
    { re: '\\bFluentToolbar\\b', note: 'удалён → div/кастом' },
    { re: '\\bFluentSplitter\\b', note: 'удалён → FluentMultiSplitter' },
    { re: '\\bFluentValidationSummary\\b', note: 'проверить: в v5 паттерн FluentField' },
    { re: '\\bFluentDesignSystemProvider\\b', note: 'удалён → IThemeService' },
    { re: '\\bFluentBodyContent\\b', note: 'удалён → обычный div' },
  ],
  excludeFiles: ['Mars.Admin.Framework/Dialogs/'],
  // Заведомо живое в v5 (не включать в паттерны): Anchor= у FluentTooltip, Visible= у
  // FluentMessageBar/FluentOverlay, Autofocus у FluentInputBase, TrapFocus/PreventScroll — свойства шима Dialogs.
};

function loadConfig() {
  try {
    const text = fs.readFileSync(REF, 'utf8');
    const blocks = [...text.matchAll(/```json\s*\n([\s\S]*?)```/g)];
    for (const b of blocks) {
      if (b[1].includes('sweep-dead-params')) {
        const jsonText = b[1].replace(/^\s*"sweep-dead-params"\s*,?\n?/, '');
        const cfg = JSON.parse(jsonText);
        return { ...cfg, source: REF };
      }
    }
  } catch { /* fallback */ }
  return { ...BUILTIN, source: 'builtin' };
}

function* walk(dir) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    if (e.name === 'bin' || e.name === 'obj' || e.name === 'node_modules' || e.name.startsWith('.')) continue;
    const p = path.join(dir, e.name);
    if (e.isDirectory()) yield* walk(p);
    else if (/\.(razor|cs|less|css)$/.test(e.name) && !e.name.endsWith('.min.css') && !e.name.endsWith('.min.js')) yield p;
  }
}

const cfg = loadConfig();
const patterns = cfg.patterns.map((p) => ({ ...p, rx: new RegExp(p.re) }));
const exclude = cfg.excludeFiles || [];
const hits = [];
for (const scope of ['src', 'docs', 'devstands']) {
  const root = path.join(ROOT, scope);
  if (!fs.existsSync(root)) continue;
  for (const file of walk(root)) {
    const rel = path.relative(ROOT, file).replace(/\\/g, '/');
    if (exclude.some((x) => rel.includes(x))) continue;
    const isRazor = file.endsWith('.razor');
    const lines = fs.readFileSync(file, 'utf8').split('\n');
    let inRazorComment = false;
    let inBlockComment = false;
    lines.forEach((rawLine, i) => {
      let line = rawLine;
      if (inBlockComment) {
        const end = line.indexOf('*/');
        if (end === -1) return;
        inBlockComment = false;
        line = line.slice(end + 2);
      }
      if (isRazor) {
        if (inRazorComment) {
          if (line.includes('*@')) inRazorComment = false;
          return;
        }
        const open = line.indexOf('@*');
        if (open !== -1) {
          const close = line.indexOf('*@', open + 2);
          if (close === -1) { inRazorComment = true; line = line.slice(0, open); }
          else line = line.slice(0, open) + line.slice(close + 2);
        }
      }
      const bo = line.indexOf('/*');
      if (bo !== -1) {
        const bc = line.indexOf('*/', bo + 2);
        if (bc === -1) { inBlockComment = true; line = line.slice(0, bo); }
        else line = line.slice(0, bo) + line.slice(bc + 2);
      }
      const trimmed = line.trimStart();
      if (!trimmed || trimmed.startsWith('//') || trimmed.startsWith('*')) return;
      for (const p of patterns) {
        if (p.rx.test(line)) hits.push({ file: rel, line: i + 1, note: p.note, text: trimmed.slice(0, 160) });
      }
    });
  }
}
console.log(JSON.stringify({ source: cfg.source, patterns: patterns.length, hits, hitCount: hits.length }, null, 2));
process.exit(hits.length ? 1 : 0);
