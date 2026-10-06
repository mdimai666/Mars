# FluentV5Reference — дистиллят фактов о FluentUI Blazor 5.0.0

Справочник для агентов: факты о пакете, снятые напрямую, чтобы НЕ раскапывать nuget/GitHub/MCP
повторно. MCP-доки `fluentui-blazor` описывают dev-ветку и ДВАЖДЫ разошлись со stable 5.0.0 —
источник истины только фактический пакет.

**Пути пакета (локальный nuget-кэш):**
- Components: `C:\Users\D\.nuget\packages\microsoft.fluentui.aspnetcore.components\5.0.0\`
  - JS-бандл (minified, ~458 КБ): `staticwebassets\Microsoft.FluentUI.AspNetCore.Components.lib.module.js`
  - CSS: `staticwebassets\css\reboot.css`, `staticwebassets\css\default-fuib.css`,
    scoped-бандл `staticwebassets\Microsoft.FluentUI.AspNetCore.Components.bundle.scp.css` (~172 КБ)
  - XML-доки: `lib\net10.0\Microsoft.FluentUI.AspNetCore.Components.xml` (~1.8 МБ; TFM: net8.0/9.0/10.0/11.0)
  - точечные JS: `staticwebassets\Components\{DataGrid,DateTime,Grid,InputFile,Nav,PullToRefresh,Tooltip,TreeView}\*.razor.js`
- Icons: `C:\Users\D\.nuget\packages\microsoft.fluentui.aspnetcore.components.icons\5.0.0\lib\net8.0\` —
  ОДИН TFM (net8.0), 5 сборок: `Icons.dll` (5 КБ, база), `Icons.Regular.dll` (10.2 МБ),
  `Icons.Filled.dll` (8.6 МБ), `Icons.Color.dll` (3.5 МБ), `Icons.Light.dll` (0.2 МБ).

Дата снятия фактов: 2026-10-01. Нарратив миграции — в `ai/FluentV5MigrationPlan.md`,
CSS-архитектура Mars (--mars-*, мост) — в `ai/CssRefactoringGuide.md`.

---

## 1. Живые CSS-токены v5

### Механика (бандл lib.module.js)

- Токены генерируются в рантайме JS: полный набор пишется **inline на `<html>`**
  (`style.setProperty`) и/или в `document.adoptedStyleSheets` (`:root`, без important).
  Функции бандла: светлая нейтраль — `cp(brandRamp)`, тёмная — `dp(brandRamp)`, полный набор —
  `bo(...)` (шрифты+геометрия+тени), дефолт — `xn = bo(Wi)`, где `Wi` — рампа Microsoft-синего
  (primary **#0f6cbd**).
- Статический compat-лист (`:root`) инжектируется через `applyStyles()` в adoptedStyleSheets —
  мост `--success/--warning/...` и spacing-фолбэки (см. ниже).
- `default-fuib.css` грузится ВСЕГДА (fetch → adoptedStyleSheets), отключается атрибутом
  `no-fuib-style` на body/html. `reboot.css` — ОПЦИОНАЛЕН, только при атрибуте `use-reboot`
  на body/html. **В Mars `use-reboot` нигде не установлен** (grep) — подчёркивания ссылок
  даёт `default-fuib.css` (`a { text-decoration: underline }`), не reboot (уточнение плана).
- Тёмная тема: `body[data-theme="dark"]` + CustomEvent `themeChanged` (функция `ku` в бандле);
  в light атрибут снимается. Нейтраль/бренд токены регенерируются (индексы другой рампы).

### Серая рампа `b` (источник нейтральных значений, бандл)

`2:#050505 4:#0a0a0a 6:#0f0f0f 8:#141414 10:#1a1a1a 12:#1f1f1f 14:#242424 16:#292929
18:#2e2e2e 20:#333333 22:#383838 24:#3d3d3d 26:#424242 28:#474747 30:#4d4d4d 32:#525252
34:#575757 36:#5c5c5c 38:#616161 40:#666666 42:#6b6b6b 44:#707070 46:#757575 48:#7a7a7a
50:#808080 52:#858585 54:#8a8a8a 56:#8f8f8f 58:#949494 60:#999999 62:#9e9e9e 64:#a3a3a3
66:#a8a8a8 68:#adadad 70:#b3b3b3 72:#b8b8b8 74:#bdbdbd 76:#c2c2c2 78:#c7c7c7 80:#cccccc
82:#d1d1d1 84:#d6d6d6 86:#dbdbdb 88:#e0e0e0 90:#e6e6e6 92:#ebebeb 94:#f0f0f0 96:#f5f5f5
98:#fafafa 99:#fcfcfc`; `M = #ffffff`; альфа-рампы: `Ne` = rgba(255,255,255,.05..0.9),
`rt` = rgba(0,0,0,.05..0.9).

### --colorNeutral* (ключевые; light → dark; индекс в рампе `b`)

| Токен | Light | Dark |
|---|---|---|
| Foreground1 (+Hover/Pressed/Selected) | b[14] #242424 | #ffffff |
| Foreground2 | b[26] #424242 | b[84] #d6d6d6 |
| Foreground3 | b[38] #616161 | b[68] #adadad |
| Foreground4 | b[44] #707070 | b[60] #999999 |
| ForegroundDisabled | b[74] #bdbdbd | b[36] #5c5c5c |
| ForegroundInverted / Inverted2 | #ffffff | b[14] #242424 |
| Background1 | #ffffff | b[16] #292929 |
| Background1Hover / Pressed / Selected | b[96] #f5f5f5 / b[88] #e0e0e0 / b[92] #ebebeb | b[24] #3d3d3d / b[12] #1f1f1f / b[22] #383838 |
| Background2 | b[98] #fafafa | b[14] #242424 |
| Background2Hover / Pressed / Selected | b[94] #f0f0f0 / b[86] #dbdbdb / b[90] #e6e6e6 | b[22] #383838 / b[10] #1a1a1a / b[20] #333333 |
| Background3 | b[96] #f5f5f5 | (генерится dp — индекс не снимался) |
| Background4 | b[94] #f0f0f0 | (同上) |
| Stroke1 (light) | b[82] #d1d1d1 | — |
| Stroke2 (light) | b[88] #e0e0e0 | — |
| Stroke3 / StrokeSubtle (light) | b[94] #f0f0f0 / b[88] | — |
| Stencil1 / Stencil2 (light) | b[90] / b[98] | — |
| colorBackgroundOverlay | rt[40] rgba(0,0,0,.4) | — |
| colorScrollbarOverlay | rt[50] | — |

Полный набор (~300 токенов) — функции `cp`/`dp` в бандле; здесь только потребляемые Mars
(алиасы --mars-* в `base.less`).

### --colorBrand* (дефолтная рампа Wi; при кастомной теме рампа перегенерируется из BrandColor)

Рампа: `10:#061724 20:#082338 30:#0a2e4a 40:#0c3b5e 50:#0e4775 60:#0f548c 70:#115ea3
80:#0f6cbd 90:#2886de 100:#479ef5 110:#62abf5 120:#77b7f7 130:#96c6fa 140:#b4d6fa
150:#cfe4fa 160:#ebf3fc`

| Токен | Light (t=Wi) | Dark |
|---|---|---|
| BrandForeground1 | t[80] #0f6cbd | t[100] #479ef5 |
| BrandForeground2 | t[70] #115ea3 | t[120] #77b7f7 |
| BrandForegroundLink | t[70] #115ea3 | t[100] #479ef5 |
| LinkHover / LinkPressed | t[60] / t[40] | t[110] / t[90] |
| BrandBackground (+Hover/Pressed/Selected) | t[80] (t[70]/t[40]/t[60]) | — |
| BrandStroke1 / Stroke2 | t[80] / t[140] | — |
| ForegroundOnBrand / NeutralForegroundOnBrand | #ffffff | #ffffff |

### --colorStatus* (генерятся из палитр; mapping `yt`: success→**green**, warning→**orange**, danger→**cranberry**)

ВНИМАНИЕ: статусные токены v5 — НЕ Red/Yellow/Green палитры: danger=cranberry #c50f1f,
warning=orange #f7630c, success=green #107c10 (рампы `dl`/`Pu`/`ul` в бандле).
Mars-мост в `base.less` перекрашивает и `--colorStatus*`, и палитры Red/Green/Yellow/... в
--mars-статусы, поэтому расхождение для админки не видно.

Формула light (на рампе `r`): Background1=r.tint60, Background2=r.tint40, Background3=r.primary,
Foreground1=r.shade10, Foreground2=r.shade30, Foreground3=r.primary, ForegroundInverted=r.tint30,
BorderActive=r.primary, Border1=r.tint40, Border2=r.primary.

| Токен (light) | Success (green) | Warning (orange) | Danger (cranberry) |
|---|---|---|---|
| Foreground1 | #0e700e | **#bc4b09** (override shade20) | #b10e1c |
| Foreground2 | #094509 | #8a3707 | #6e0811 |
| Foreground3 | #107c10 | **#bc4b09** (override) | #c50f1f |
| ForegroundInverted | #54b054 | #faa06b | #dc626d |
| Background1 | #f1faf1 | #fff9f5 | #fdf3f4 |
| Background2 | #9fd89f | #fdcfb4 | #eeacb2 |
| Background3 | #107c10 | #f7630c | #c50f1f |
| Border2 | #107c10 | **#bc4b09** (override) | #c50f1f |

Overrides light: WarningForeground1/3=Border2=orange.shade20; DangerBackground3Hover=shade10,
DangerBackground3Pressed=shade20. Overrides dark (объект `to`): DangerForeground3=tint40,
DangerBorder2=tint30, DangerBackground3Hover/Pressed=shade10/shade20, SuccessForeground3=Border2=
green.tint40, **WarningForegroundInverted=orange.shade20 #bc4b09**.

### --colorPalette* (light-семейства)

Семейства light-набора (`Vi`): red, green, darkOrange, yellow, berry, lightGreen, marigold;
dark-набор (`Ui`): darkRed, cranberry, pumpkin, peach, gold, brass, brown, forest, seafoam,
darkGreen, lightTeal, teal, steel, blue, royalBlue, cornflower, navy, lavender, purple, grape,
lilac, pink, magenta, plum, beige, mink, platinum, anchor. Формула та же, что у статусов
(Background1=tint60 ... Border2=primary). Primaries снятых рамп: red #d13438, green #107c10,
darkOrange #da3b01, marigold #eaa300, cranberry #c50f1f, orange #f7630c (yellow/berry/lightGreen —
не извлекались). Точечные overrides: YellowForeground1=yellow.shade30, RedForegroundInverted=
red.tint20 #dc5e62, GreenForegroundInverted=green.tint20, YellowForegroundInverted=yellow.tint40.

### Шрифты (бандл, объект `_r`/`Vr`/`Ur`/`_o` — статика, не зависят от темы)

- `--fontSizeBase100..600`: 10/12/14/16/20/24px; `--fontSizeHero700..1000`: 28/32/40/68px.
- `--lineHeightBase100..600`: 14/16/20/22/28/32px; `--lineHeightHero700..1000`: 36/40/52/92px.
- `--fontWeightRegular/Medium/Semibold/Bold`: 400/500/600/700.
- `--fontFamilyBase`: `'Segoe UI', 'Segoe UI Web (West European)', -apple-system,
  BlinkMacSystemFont, Roboto, 'Helvetica Neue', sans-serif`; `--fontFamilyMonospace`:
  `Consolas, 'Courier New', Courier, monospace`; `--fontFamilyNumeric`: Bahnschrift-стек.
  (Mars перебивает Base/Monospace мостом на --mars-font-*.)

### Геометрия (бандл — `Rr`, `_e`, `Wr`; хардкод, темы не зависят)

- `--borderRadius{None,Small,Medium,Large,XLarge}`: 0/2/4/6/8px; `2XLarge..6XLarge`: 12/16/24/32/40px;
  `Circular`: 10000px. (Mars-мост перебивает None..Circular + Circular на --mars-radius-*.)
- `--strokeWidth{Thin,Thick,Thicker,Thickest}`: 1/2/3/4px.
- Spacing-шкала `_e`: none 0, xxs 2px, xs 4px, sNudge 6px, s 8px, mNudge 10px, m 12px, l 16px,
  xl 20px, xxl 24px, xxxl 32px → токены `--spacingHorizontal*` и `--spacingVertical*`
  (None/XXS/XS/SNudge/S/MNudge/M/L/XL/XXL/XXXL).
- Грабли: compat-лист в бандле dodatkowo объявляет на `:root` фолбэки
  `--spacingVertical/HorizontalXXXL: 28px` и `XXXXL: 32px` — БЕЗ SNudge/MNudge и с другим XXXL;
  inline-токены на html (32px для XXXL) их перебивают. Не использовать XXXL из compat-блока как истину.

### Тени (функция `Vo(ambient,key)`; ambient=rgba(0,0,0,.12), key=rgba(0,0,0,.14); brand: .30/.25)

| Токен | Значение (light, neutral) |
|---|---|
| `--shadow2` | `0 0 2px ambient, 0 1px 2px key` |
| `--shadow4` | `0 0 2px ambient, 0 2px 4px key` |
| `--shadow8` | `0 0 2px ambient, 0 4px 8px key` |
| `--shadow16` | `0 0 2px ambient, 0 8px 16px key` |
| `--shadow28` | `0 0 8px ambient, 0 14px 28px key` |
| `--shadow64` | `0 0 8px ambient, 0 32px 64px key` |

Есть `--shadow{N}Brand`-варианты. Дополнительно: `--colorNeutralShadowAmbient/.12`,
`...ShadowKey/.14`, `...Lighter/.06+.07`, `...Darker/.20+.24`, `--colorBrandShadowAmbient/.30`,
`--colorBrandShadowKey/.25`. (Mars-мост перебивает shadow2..64 на --mars-shadow-*.)

### Motion (бандл, `fn`/`gn`)

- `--duration{UltraFast,Faster,Fast,Normal,Gentle,Slow,Slower,UltraSlow}`: 50/100/150/200/250/300/400/500ms.
- `--curve{AccelerateMax,AccelerateMid,AccelerateMin,DecelerateMax,DecelerateMid,DecelerateMin,EasyEaseMax,EasyEase,Linear}` — 9 cubic-bezier.

### Compat-мост на `:root` (статический лист в бандле — ЖИВ)

| Compat-токен | Определён как | Light-значение (дефолт-тема) |
|---|---|---|
| `--success` | var(--colorStatusSuccessForeground1) | #0e700e |
| `--warning` | var(--colorStatusWarningForeground1) | #bc4b09 |
| `--error` | var(--colorPaletteRedForeground1) | #bc2f32 (red.shade10) |
| `--info` | var(--colorNeutralForeground3) | #616161 |
| `--success-inverted` | colorStatusSuccessForegroundInverted | #54b054 |
| `--warning-inverted` | colorStatusWarningForegroundInverted | #faa06b |
| `--error-inverted` | colorPaletteRedForegroundInverted | #dc5e62 |
| `--info-inverted` | colorNeutralForegroundInverted2 | #ffffff |
| `--font-monospace` | var(--fontFamilyMonospace) | Consolas-стек |
| `--highlight-bg` | литерал | #fff3cd |
| `--presence-{available,away,busy,dnd,offline,oof,blocked,unknown}` | палитры (LightGreen/Marigold/Red/Berry/Neutral) | — |

Так как все они `var(--colorStatus*/Palette*)`, important-оверрайды Mars-моста подхватываются
автоматически. Там же `body:has(.prevent-scroll){overflow:hidden}`.

### МЁРТВЫЕ токены v4 (в 5.0.0 не определены нигде в пакете)

`--type-ramp-*`, `--accent-fill-*`, `--neutral-fill-*`, `--neutral-layer-*`,
`--neutral-foreground-*`, `--neutral-stroke-*` (включая `--neutral-fill-stealth-rest`),
`--design-unit`, `--neutral-base-color`, `--control-corner-radius`, `--base-height-multiplier`,
`--badge-fill-*`, `--dialog-width` (ширина диалога — через `::part(dialog)`).
Инвариант: мёртвый токен в less НЕ ломает сборку — только молчаливую визуальную деградацию.

---

## 2. Карта `::part` (по шаблонам бандла lib.module.js, 5.0.0)

| Компонент (host-тег) | parts | Примечание |
|---|---|---|
| FluentButton (`fluent-button`) | `content` | span.content > slot; **внутреннего `<button>` НЕТ**. Уточнение CssRefactoringGuide («частей нет»): part="content" в бандле ЕСТЬ (шаблон `Bi`, регистрация `{name:ag=prefix-button, template:lc}`) |
| FluentAnchorButton (`fluent-anchor-button`) | `content` | `::slotted(a)` растянут inset:0 |
| FluentCompoundButton (`fluent-compound-button`) | `content` | + slot `description` |
| FluentCard (`fluent-card`) | `content` | |
| FluentAccordionItem (`fluent-accordion-item`) | `heading`, `button`, `content` | v4 `content-region` мёртв |
| FluentDialog (`fluent-dialog`) | `dialog` | v4 `control` мёртв; у shadow-`dialog` жёсткий `max-width:600px` |
| FluentDrawer (`fluent-drawer`) | `dialog` | |
| FluentDialogBody (`fluent-dialog-body`) | `title`, `content`, `actions` | слоты: title, title-action, close, action |
| (drawer/dialog-body вариант) | `header`, `content`, `footer` | второй шаблон `vb` в бандле — вероятно drawer-body (не проверено) |
| FluentField (`fluent-field`) | `label`, `input`, `message` | слоты label/input/message |
| FluentLabel (`fluent-label`) | `asterisk` | красная `*` при Required |
| FluentCheckbox (`fluent-checkbox`) | `content`, `description` | slot `checked-indicator` |
| FluentRadio (`fluent-radio`) | `indicator` | |
| FluentSwitch (`fluent-switch`) | `checked-indicator` | v4 `switch` мёртв; host несёт `checked="true"` |
| FluentSlider (`fluent-slider`) | `track-container`, `thumb-container` | |
| FluentTextInput (`fluent-text-input`) | `label`, `root`, `control` | `control` = input#control (v4-хак жив) |
| FluentTextArea (`fluent-textarea`) | `label`, `root`, `control` | `control` = textarea#control |
| FluentMenuItem (`fluent-menu-item`) | `content` | слоты: indicator, submenu, submenu-glyph |
| FluentTreeItem (`fluent-tree-item`) | `positioning-region`, `content`, `chevron`, `aside`, `items` | v4 `content-region` мёртв |
| FluentOverflow (`fluent-overflow`) | `container`, `items`, `trigger` | |
| FluentNavItem (`fluent-navitem`) | `content` | (не проверено — вывод по шаблону с tabindex/clickHandler) |
| FluentColorPicker (`fluent-color-picker`) | `canvas`, `hue-bar`, `indicator`, `hue-indicator` | Mars не использует |

DataGrid/SortableList/Grid/Stack — НЕ shadow DOM (см. раздел 3), `::part` неприменим.
`::deep` из scoped razor.css shadow DOM веб-компонента не пробивает (как и в v4).

---

## 3. Rendering-факты (host-теги, DOM, селекторы)

Host-теги: подтверждённые в репо (E2E/CSS/обходы) помечены ✓, остальные — строки из сборки
DLL (5.0.0) / css-бандла, т.е. надёжно, но без DOM-проверки.

| Компонент | Рендерит |
|---|---|
| FluentButton ✓ | `<fluent-button>`; shadow: icon-слоты + `span.content > slot`; **без внутреннего `<button>`** — E2E-клик по самому `fluent-button[type='submit']`, getByRole('button') не работает |
| FluentTextInput ✓ | `<fluent-text-input>`; атрибут `name` на хосте, реальный `<input>` в shadow; Playwright-селектор `[name='x'] input` пробивает shadow; `input[type='text']` может отсутствовать (использовать `input:not([type='color'])`) |
| FluentNumberInput | отдельного тега нет в строках DLL — вероятно тот же `fluent-text-input` с numeric-поведением (не проверено) |
| FluentTextArea | `<fluent-textarea>` (без дефиса перед area!) |
| FluentText | `<fluent-text>` (селекторы default-fuib.css: `fluent-text[weight] > *`, `fluent-text[size] > *`) |
| FluentIcon | НЕ custom element — инлайн `<svg>`; `fluent-icon` тега в DLL нет |
| FluentSelect | `<fluent-dropdown>` (тега `fluent-select` в 5.0.0 нет) |
| FluentListbox | `<fluent-listbox>` |
| FluentOption ✓ | `<fluent-option>` |
| FluentCheckbox / Switch / Radio / RadioGroup | `<fluent-checkbox>` / `<fluent-switch>` / `<fluent-radio>` / `<fluent-radio-group>` |
| FluentBadge ✓ | `<fluent-badge>` + класс `.fluent-badge`; **всегда пишет атрибут `color`** (дефолт Brand → `color="brand"`); цвета в bundle.scp.css по селектору `.fluent-badge[color=...]` |
| FluentMenu / MenuList / MenuItem ✓ | `<fluent-menu>` / `<fluent-menu-list>` / `<fluent-menu-item>`; внутри каждого `fluent-menu` в DOM должен быть `fluent-menu-list` (см. грабли) |
| FluentMenuButton | `<fluent-menu-button>` (жив в 5.0.0, переработан; Mars заменил на Button+Menu) |
| FluentDialog / DialogBody / Drawer ✓ | `<fluent-dialog>` / `<fluent-dialog-body>` / `<fluent-drawer>` (+`fluent-drawer-body`, `fluent-dialog-provider`) |
| FluentDataGrid ✓ | **НЕ custom element** — `<div class="fluent-data-grid">`, внутри обычная HTML-таблица; классы `.fluent-data-grid*` (87 правил в bundle.scp.css) |
| FluentMessageBar ✓ | `<fluent-message-bar>` (+`fluent-message-bar-provider`, `fluent-message-box`) |
| FluentAutocomplete | отдельного тега в строках DLL нет (не проверено — надстройка над dropdown/field) |
| FluentDivider ✓ | `<fluent-divider>` |
| FluentSpinner | `<fluent-spinner>` |
| FluentProgressBar | `<fluent-progress-bar>` |
| FluentSortableList | `<fluent-sortable-list>` + класс `.fluent-sortable-list` (49 правил в bundle.scp.css); CSS-переменная `--fluent-sortable-list-item-height` |
| FluentSkeleton | `<fluent-skeleton>` + паттерны `fluent-skeleton-{1..8}`, `fluent-skeleton-circular-{1..8}` (div'ы) |
| FluentStack | `<fluent-stack-horizontal>` / `<fluent-stack-vertical>` |
| FluentSpacer / FluentHeader / FluentDropZone / FluentDragContainer | отдельных тегов нет — обычные div'ы |
| FluentCard ✓ | `<fluent-card>` (+`fluent-badge-container` для attached-бейджа) |
| FluentValidationMessage / Summary | `<fluent-validation-message>` / класс `fluent-validation-errors` |
| FluentOverflow ✓ | `<fluent-overflow>` (+`fluent-overflow-more`, `fluent-overflow-gap`) |
| FluentDatePicker / TimePicker | `<fluent-datepicker>` / `<fluent-timepicker>` (+`fluent-calendar`, `fluent-day-view`, `fluent-month-view`, `fluent-year-view`) |
| FluentTabs / Tab | `<fluent-tabs>`, `<fluent-tablist>`, `<fluent-tab>`, `<fluent-tab-panel>` |
| FluentMultiSplitter ✓ | `<fluent-multi-splitter>`, `-bar`, `-pane` (класс `.fluent-multi-splitter*`) |
| FluentLabel ✓ | `<fluent-label>`; **БЕЗ класса `.fluent-input-label`** (v4-селектор мёртв) — только тег |
| FluentSlider | `<fluent-slider>` |
| FluentProviders | `<fluent-providers>` |
| FluentRatingDisplay | `<fluent-rating-display>` |
| FluentNav ✓ | `<fluent-nav>`, `<fluent-navitem>`, `<fluent-navcategoryitem>`, `<fluent-navsectionheader>`, `<fluent-navsubitem>`, `<fluent-navsubitemgroup>` (navitem/navcategoryitem — СЛИТНО, не kebab-case!) |
| FluentInputFile | `<fluent-inputfile-container>` (слитно) |
| FluentTooltip ✓ | `<fluent-tooltip>` (+`fluent-tooltip-provider`) |
| FluentTreeView / TreeItem ✓ | `<fluent-tree>` / `<fluent-tree-item>` |
| FluentAvatar | `<fluent-avatar>` |
| FluentPaginator | `<fluent-paginator>` |
| FluentLink | `<fluent-link>` |
| FluentLayout | `<fluent-layout>`, `fluent-layout-item`, `fluent-layout-hamburger` |
| FluentAccordion / Item | `<fluent-accordion>` / `<fluent-accordion-item>` |
| FluentGrid / GridItem | `<fluent-grid>` / div (GridItem без собственного тега — не проверено) |
| FluentOverlay ✓ | `<fluent-overlay>` (+`fluent-overlay-global`) |
| FluentField | `<fluent-field>` |
| Новые в v5 (не используются Mars) | `fluent-appbar*`, `fluent-wizard*`, `fluent-toggle-button`, `fluent-split-button` (строки DLL), `fluent-compound-button`, `fluent-anchor-button`, `fluent-counter-badge`, `fluent-presence-badge`, `fluent-color-picker`, `fluent-image`, `fluent-pull-container` |

Общие факты DOM/стилей:
- Стиль-загрузка: v5 сам грузит JS-модуль `lib.module.js` (Blazor JS module auto-load) и CSS
  через adoptedStyleSheets (fetch `./_content/Microsoft.FluentUI.AspNetCore.Components/css/*.css`).
  Ручные ссылки в хостах не нужны.
- Adopted-листы НЕ important → авторский `!important` (мост `html:root` в base.less) перебивает
  и их, и inline-токены на `<html>`.
- `default-fuib.css` задаёт body (100dvh, overflow:hidden, шрифт, цвет) и `a{text-decoration:underline}`;
  грузится ПОСЛЕ авторских css в adopted-слое — в Mars перебит `body a` в base.less.
- Атрибуты-переключатели: `no-fuib-style` (отключить default-fuib), `use-reboot` (включить reboot.css)
  — на body или html; MutationObserver реагирует на лету.
- IMask и Sortablejs грузятся ЛЕНИВО с CDN unpkg и после onload проверяют глобал
  (`window.IMask`/`window.Sortable`); при живом AMD-лоадере (monaco `loader.js`) UMD-билд
  уходит в AMD-ветку и глобал не создаётся → ошибка «IMask library failed to load».
  В Mars вылечено вендорингом `imask.min.js`/`Sortable.min.js` в index.html ДО monaco.
- IThemeService (DI, scoped; JS `Blazor.theme.*`): SetThemeAsync, CreateCustomThemeAsync,
  GetColorRampFromSettingsAsync, SwitchThemeAsync, SetThemeToElementAsync (inline ПОЛНЫЙ набор
  токенов на элемент, включая хардкод-геометрию `borderRadiusMedium:"4px"` и т.п.), персист в
  localStorage. ThemeSettings: Color, HueTorsion (-0.5..0.5), Vibrancy (-0.5..0.5), Mode
  (Light/Dark/System), IsExact.

---

## 4. Параметры/события используемых Mars компонентов (по XML-докам 5.0.0)

Общая база входов `FluentInputBase<T>` (наследуется TextInput/NumberInput/TextArea/Select и др.):
`Value`(@bind), `ValueExpression`, `Disabled`, `ReadOnly`, `Required`, `Name`, `Id`, `AriaLabel`,
`Class`, `Style`, `Data`, `Label`, `LabelTemplate`, `LabelPosition`, `LabelWidth`, `LabelInfo`,
`Autofocus` (ЖИВ, именно с маленькой f), `Margin`, `Padding`, `FieldStartTemplate`,
`FieldEndTemplate`, `Message`, `MessageCondition`, `MessageState`, `MessageTemplate`,
`MessageIcon`, `FocusLost`, `ValidationFieldFor`, `UseNativeConstraintValidationUI`.

База списков `FluentListBase<TOption,TValue>` (Select/Combobox/Autocomplete/Listbox):
`Items`, `Appearance`, `Multiple`, `Width`, `Height`, `OptionText`, `OptionValue`,
`OptionDisabled`, `OptionTemplate`, `OptionClass`, `OptionSelectedComparer`,
`OptionValueToString`, `OptionNoWrapMaxWidth`, `SelectedItem(s)`, `SelectedItem(s)Changed`,
`SelectedItem(s)Expression`, `Tooltip`, `ChildContent`. (v4 `SelectedOption(s)`/
`SelectedOptionChanged` — МЕРТВЫ.)

Ключевые собственные параметры (сокращено; `✝` = чего из v4 НЕ стало):

- **FluentButton**: `Appearance` (ButtonAppearance: Default/Outline/Primary/Subtle/Transparent),
  `BackgroundColor`, `Color`, `Size`, `Shape`, `Type`, `IconStart`, `IconEnd`, `IconOnly`,
  `Loading`, `AutoFocus`, `FormId/FormAction/FormEncType/FormMethod/FormNoValidate/FormTarget`,
  `Disabled`, `DisabledFocusable`, `Value`, `Name`, `Required`, `Title`, `Label`,
  `StopPropagation`, `OnClick`, `Tooltip`, `EmptyContent`. ✝ `Autofocus`→`AutoFocus`,
  `Action`→`FormAction`, `Enctype`→`FormEncType`; Appearance/Color: `Neutral`→`Default`, `Accent`→`Primary`.
- **FluentTextInput**: `TextInputType` (Text/Email/Password/Telephone/Url/Color/Search/Number),
  `Appearance` (TextInputAppearance), `Placeholder`, `StartTemplate`/`EndTemplate` (v4 ChildContent-слоты
  start/end МЕРТВЫ), `MaxLength` (v4 `Maxlength`), `MinLength`, `Pattern`, `DataList`,
  `MaskPattern`/`MaskLazy`/`MaskPlaceholder` (IMask), `AutoComplete`, `Width`, `Size`,
  `Spellcheck`, `InputMode`, `ChangeAfterKeyPress`/`OnChangeAfterKeyPress`, `Tooltip`,
  `HidePasswordToggle`, `HideContactsToggle`. ✝ компонент `FluentTextField`/`FluentSearch` удалён → FluentTextInput; `TextFieldType`→`TextInputType`.
- **FluentNumberInput<TValue>**: `Min`, `Max`, `Step`, `StepButtons`, `Culture`, `Placeholder`,
  `StartTemplate`/`EndTemplate`, `Appearance`, `Size`, `Width`, `IsDecimal`(get). ✝ `FluentNumberField` удалён.
- **FluentTextArea**: `MaxLength`, `MinLength`, `AutoResize`, `Resize`, `Rows`(через Size/Height),
  `Width`, `Height`, `Spellcheck`, `AutoComplete`, `ChangeAfterKeyPress`(+событие).
- **FluentText**: `As` (TextTag: Span/Paragraph/Pre/H1..H6), `Size` (TextSize: Size100..Size1000),
  `Weight`, `Align`, `Font`, `Color`, `CustomColor` (#RRGGBB), `Nowrap`, `Truncate`, `Block`,
  `Italic`, `Underline`, `Strikethrough`, `Tooltip`. Замена типографики v4 `FluentLabel Typo=`:
  Body→Size300, Subject→Size400, Header→Size500, PaneHeader→Size600, EmailHeader→Size700,
  PageTitle→Size800, HeroTitle→Size900, H1–H6→As=TextTag.H* + Size800/700/600/500/400/300.
- **FluentIcon<TIcon>**: `Value` (Icon; **никогда не null!**), `Color`, `CustomColor`, `Width`,
  `Slot`, `Title`, `Focusable`, `OnClick`, `OnClickStopPropagation`, `OnClickPreventDefault`,
  `Tooltip`. Дефолтный цвет — `currentColor` (в v4 был Accent). Иконки:
  `Icons.{Regular,Filled,Color,Light}.Size{10,12,16,20,24,28,32,48}.<Name>`, `.WithColor(Color.*)`.
- **FluentSelect<TOption,TValue>**: база списков + `Placeholder`, `DropdownType`, `IsImmediate`,
  `DropdownStyle`, `ListStyle`, `Size`, `ControlStyle`. Два параметра типов (v4 — один TOption).
- **FluentOption<TValue>**: `Value` (для string — литералы `Value="@("...")"`), `Text`, `Selected`,
  `Disabled`, `Name`, `Description`, `ChildContent`.
- **FluentCheckbox**: `CheckState`(+`CheckStateChanged`), `ThreeState`, `ShowIndeterminate`,
  `ThreeStateOrderUncheckToIntermediate`, `Shape`, `Size`, `Width`, `ChildContent`(подпись), `Tooltip`.
- **FluentSwitch**: `Checked`(@bind), `CheckedMessage`, `UncheckedMessage`, `Width`, `ChildContent`, `Tooltip`.
- **FluentRadio<TValue>** / **FluentRadioGroup<TValue>**: Radio: `Value`, `Label`, `LabelTemplate`,
  `LabelWidth`, `Disabled`, `Owner`; Group: `Items`, `RadioValue`, `RadioLabel`, `RadioDisabled`,
  `Orientation`, `Wrap`, `Width`, `ChildContent`.
- **FluentBadge**: `Content` (**ТЕКСТ — сюда**; `ChildContent` в v5 = обёрнутый/attached-контент,
  текст в ChildContent даёт пустой бейдж), `Color` (BadgeColor: Brand/Danger/Important/Informative/
  Severe/Subtle/Success/Warning), `Appearance` (BadgeAppearance: Filled/Ghost/Outline/Tint),
  `BackgroundColor`, `Shape`, `Size`, `Positioning`, `OffsetX/OffsetY`, `AnchorContent`,
  `IconStart/IconLabel/IconEnd`. ✝ `Fill` МЕРТВ; `OnClick` МЕРТВ (оборачивать в span @onclick);
  v4 `Appearance.Neutral/Accent` → Color/BadgeAppearance.Filled.
- **FluentMenu**: `Trigger` (**string — id элемента-триггера**), `OpenOnHover`, `OpenOnContext`,
  `CloseOnScroll`, `PersistOnItemClick`, `Height`, `RenderWhen`, `OnClick` (MenuItemEventArgs),
  `OnCheckedChanged`, `OpenedChanged`; методы `OpenMenuAsync()`, `OpenMenuAsync(targetId,x,y)`.
  ✝ `UseMenuService`, `Anchor`, `@bind-Open` МЕРТВЫ; дети — ОБЯЗАТЕЛЬНО через `FluentMenuList`.
- **FluentMenuList**: `ChildContent`, `OnClick`, `OnCheckedChanged` — обёртка-контейнер пунктов.
- **FluentMenuItem**: `Role` (menuitem/menuitemcheckbox/menuitemradio), `Checked`(+Changed),
  `Disabled`, `IconStart/IconEnd/IconSubmenu/IconIndicator`, `Label`, `MenuItems` (подменю),
  `OnClick`, `ChildContent`.
- **FluentDialog**: `Instance` (IDialogInstance), `Alignment`, `Modal`, `PreventDismissOnEscape`,
  `OnStateChange` (DialogState: Closed/Opening/Open/Closing), `DialogService`; методы
  `ShowAsync()`/`HideAsync()`. ✝ `Hidden`, `TrapFocus`, `PreventScroll`, `@ondialogdismiss` МЕРТВЫ.
- **FluentDialogBody**: `TitleTemplate`, `TitleActionTemplate`, `ActionTemplate`,
  `FixedHeaderFooter`, `Instance`, `ChildContent`. (v4 FluentDialogHeader/Footer удалены.)
- **FluentDataGrid<T>**: `Items`/`ItemsProvider`/`RefreshItems`, `Virtualize`, `OverscanCount`,
  `MaxItemCount`, `ItemSize`, `ResizableColumns`, `ReorderableColumns`, `ResizeColumnOnAllRows`,
  `ResizeType`, `ColumnResizeMenuSettings`, `ColumnReorderMenuSettings`, `ColumnSortMenuSettings`,
  `ColumnOptionsMenuSettings`, `HeaderCellAsButtonWithMenu`, **`UseMenuService` (ЖИВ у DataGrid!)**,
  `ItemKey`, `Pagination`, `GenerateHeader` (DataGridGeneratedHeaderType: None/Default/Sticky),
  `GridTemplateColumns`, `ColumnOrder`(+Changed), `SortColumns`, `SortMode`, `ShowMultiSortActions`,
  `OnSortChanged`, `OnRowClick/DoubleClick/Focus`, `OnCellClick/Focus`, `OnToggle/OnExpandAll/
  OnCollapseAll`, `RowDetails/HasRowDetails/OnRowDetailsToggle`, `RowClass/RowStyle`, `ShowHover`,
  `EmptyContent`, `Loading`/`LoadingContent`/`OnItemsLoading`, `HandleLoadingError`, `ErrorContent`,
  `AutoFit`, `AutoItemsPerPage`, `StripedRows`, `DisplayMode`, `RowSize`, `MultiLine`,
  `SaveStateInUrl`/`SaveStatePrefix`, `AutoFocus`, `IsFixed`. ✝ `ColumnOptionsLabels`→
  `ColumnOptionsMenuSettings`, `ColumnResizeLabels`→`ColumnResizeMenuSettings`; у
  GridItemsProviderRequest ✝ `SortByColumn/SortByAscending` → `SortColumns`; `Align`→DataGridCellAlignment (Start/Center/End); `GenerateHeaderOption`→`DataGridGeneratedHeaderType`.
- **FluentMessageBar**: `Intent` (MessageBarIntent: Success/Warning/Error/Info/Custom; v4
  `MessageIntent` МЕРТВ), `Layout`, `Shape`, `Animation`, `AriaLive`, `Icon`, `Title`,
  **`Visible` (ЖИВ)**, `AllowDismiss`, `ActionsTemplate`, `TimeStamp`, `MessageBarInstance`.
- **FluentAutocomplete<TOption,TValue>**: `OnOptionsSearch`, `OnSetValue`, `SelectedItem(s)`(+Changed),
  `MaximumOptionsSearch`, `MaximumSelectedOptions`(+Message), `ShowProgressIndicator`,
  `MaxAutoHeight`, `MaxSelectedWidth`, `ShowDismiss`, `IconDismiss`, `IconSearch`,
  `SelectedOptionTemplate`, `HeaderContent`, `FooterContent`, `InputAppearance`, `Placeholder`,
  `Multiple`, `ImmediateDelay` (default 400ms), `Size`. ✝ `AutoComplete="off"` МЕРТВ (есть только у TextInput/TextArea).
- **FluentDivider**: `Appearance`, `Inset`, **`Vertical` (bool)**, `AlignContent`, `ChildContent`.
  ✝ `Orientation=` МЕРТВ.
- **FluentSpinner**: `Size` (SpinnerSize: Medium/Tiny/ExtraSmall/Small/Large/ExtraLarge/Huge),
  `Visible`, `AppearanceInverted`, `Stroke`, `Tooltip`. ✝ `Width=` МЕРТВ.
  `FluentProgressRing` в 5.0.0 ещё существует как alias FluentSpinner («will be removed in a future release»).
- **FluentSortableList<T>**: `Items`(+Changed), `ItemTemplate`, `OnUpdate/OnRemove/OnAdd`,
  `Group`, `Clone`, `Drop`, `Sort`, `Handle`, `ItemFilter`, `Fallback`, `AriaLabel`.
  ✝ `ListItemHeight`, `ListItemFilteredColor`, `ListBorderWidth` МЕРТВЫ →
  `Style="--fluent-sortable-list-item-height: …"`.
- **FluentSkeleton**: `Visible`, `Shimmer`, `Circular`, `Width` (default "100%"), `Height`
  (default "48px"), `Pattern` (ClassRectangle1..8/ClassCircular1..8).
- **FluentSpacer**: `Width`, `Height`, `Orientation`.
- **FluentStack**: `Orientation`, `HorizontalAlignment`, `VerticalAlignment`, `Width` (default 100%),
  `Height`, `Wrap`, `Reversed`, `HorizontalGap`/`VerticalGap` (**string** — числа без px
  компилируются, но ломают отступы; v4 int? МЕРТВ).
- **FluentDropZone<T>**: **`Data` (ЖИВ, Object)**, `Container`, `Item`, `Droppable`, `Draggable`,
  `StopPropagation`, `OnDragStart/End/Enter/Over/Leave`, `OnDropEnd`, `IsOver`.
- **FluentDragContainer<T>**: `OnDragStart/End/Enter/Over/Leave`, `OnDropEnd`, `StartedZone`.
- **FluentCard**: `Appearance`, `Shadow`, `Width`, `Height`, `Role`, `OnClick`, `ChildContent`.
- **FluentValidationMessage<TValue>** (ЖИВ в 5.0.0, generic!): `For`, `Field`, `Icon`.
- **FluentValidationSummary**: `UseErrorTextColor`, `EditContext`.
- **FluentOverflow<T>**: `Items`, `ItemTemplate`, `ItemText`, `OverflowTemplate`, `MoreTemplate`,
  `MaxRenderedItems`, `Orientation`, `Selector`, `VisibleOnLoad`, `UseTooltipService`,
  `OnMoreClick`, `OnOverflowRaised`, `ItemsOverflow`(get), `OverflowCount`(get), `IdMoreButton`(get).
- **FluentDatePicker<TValue>** (generic в v5): `Icon`, `Appearance`, `RenderStyle`, `Width`,
  `Placeholder`, `DaysTemplate`, `Opened`, `OnCalendarOpen`, `PickerMonthChanged`,
  `OnDoubleClick`, `DoubleClickToDate`.
- **FluentTab**: **`Header`/`HeaderTemplate`** (v4 `Label` МЕРТВ), `HeaderClass/HeaderStyle`,
  `IconStart`, `IconColor`, `Disabled`, `Visible`, `DeferredLoading`, `LoadingTemplate`, `Tooltip`.
- **FluentTabs**: `ActiveTabId`(+Changed), `ActiveTab`(+Changed), `Appearance`, `Size`,
  `Orientation`, `Disabled`, `Overflow`, `MoreTemplate`, `OverflowTemplate`, `MoreButtonLabel`,
  `Height`, `Width`.
- **FluentMultiSplitter**: `Orientation`, `BarSize` (default 8px), `Width`, `Height`,
  `OnCollapse/OnExpand/OnResize`. **FluentMultiSplitterPane**: `Size`, `Min`, `Max`, `Collapsed`,
  `Collapsible`, `Resizable`.
- **FluentLabel**: `Required` (звёздочка), `Size`, `Weight`, `Disabled`, `Tooltip`, `ChildContent`.
  ✝ `Typo`, `Color` МЕРТВЫ (молча уходят в AdditionalAttributes) → FluentText.
- **FluentSlider<TValue>**: `Min`, `Max`, `Step` (default 1), `Size`, `Orientation`, `Width`,
  `ImmediateDelay`, `Tooltip`.
- **FluentProviders**: только ClassValue/StyleValue — заменяет 4 провайдера v4
  (Toast/Dialog/Tooltip/MessageBar). Конфиг Toast: `AddFluentUIComponents(config => config.Toast.Position = ToastPosition.TopCenter)`.
- **FluentRatingDisplay**: `Value`, `Max`, `Count`, `Color`, `Size`, `Shape`, `Compact`, `Tooltip`.
  ✝ `FluentRating` с `Thresholds=` удалён.
- **FluentNav**: `UseIcons`, `UseSingleExpanded`, `Density`, `Width`, `BackgroundColor`
  (default `var(--colorNeutralBackground4)`), `BackgroundColorHover`, `LayoutContainer`,
  `OnItemClick`, `OpenedHamburgers`. **FluentNavItem**: `Href`, `Match`, `IconRest`, `IconActive`,
  `Active`(get), `ActiveClass`, `Disabled`, `Target`, `Tooltip`, `Category`, `OnClick`.
  **FluentNavCategory**: `Title`, `Expanded`(+Changed), `IconRest/IconActive`, `ActiveClass`,
  `Tooltip`. **FluentNavSectionHeader**: `Title`, `Owner`. Один уровень вложенности.
- **FluentInputFile**: `Mode`, `Multiple`, `MaximumFileCount`, `MaximumFileSize` (default 10MiB),
  `BufferSize` (default 10KiB), `Accept`, `Disabled`, `Width/Height`, `DragDropZoneVisible`,
  `AnchorId`, `ProgressTemplate`, `ProgressTitle/Percent/FileDetails/Style`, `OnInputFileChange`,
  `OnFileUploaded`, `OnProgressChange`, `OnFileError`, `OnCompleted`. Грабли Mars: при заданном
  `OnInputFileChange` встроенная загрузка отключается (память проекта fluent-input-file-gotcha).
- **FluentTooltip**: **`Anchor` (ЖИВ**, id компонента, Required), `Delay`, `Positioning`,
  `MaxWidth` (default 240px), `SpacingHorizontal/Vertical` (default 4px), `UseTooltipService`,
  `OnDismissed`, `OnToggle`. ✝ `Visible` МЕРТВ.
- **FluentTreeView**: `Items`, `ItemTemplate`, `LazyLoadItems`, `Size`, `Appearance`,
  `SelectionMode`, `HideSelection`, `SelectedId`(+Changed), `SelectedItem`(+Changed),
  `SelectedItems`(+Changed), `MultipleSelectionVisibility`, `OnExpandedChanged`, `OnSelectedChanged`.
  (У TreeViewItem в 5.0.0 появились `IconStart/IconEnd/IconAside`; XML-доков у класса нет вообще.)
- **FluentAvatar**: `Image`, `Initials`, `Name`, `Icon`, `Color` (AvatarColor: Neutral/Colorful/
  Anchor/Beige/.../Brand/...), `Shape`, `Size`, `Active`, `ActiveAppearance`, `Slot`, `OnClick`,
  `Tooltip`. Замена FluentPersona (имя/цвет теряются — в Mars добавлены вручную).
- **FluentPaginator**: `State` (PaginationState), `CurrentPageIndexChanged`, `Disabled`,
  `SummaryTemplate`, `PaginationTextTemplate`.
- **FluentLink**: `Href`, `Target`, `Appearance`, `Inline`, `IconStart/IconEnd`, `LinkType`,
  `HrefLang`, `Rel`, `ReferrerPolicy`, `ForceLoad`, `OnClick`. Замена FluentAnchor.
- **FluentLayout**: `Areas`, `GlobalScrollbar`, `Width`, `Height`, `MobileBreakdownWidth`
  (default 768px), `OnBreakpointEnter`, `NavigationDeferredLoading`, `HasHeader/HeaderHeight/
  HeaderSticky/HasFooter/...`(get). ✝ `FluentBodyContent` удалён.
- **FluentAccordion**: `ExpandMode`(+Changed), `HeadingLevel`, `Size`, `MarkerPosition`, `Block`,
  `ActiveId`(+Changed), `OnAccordionItemChange`. **FluentAccordionItem**: `Header`/`HeaderTemplate`
  (v4 `Heading*` МЕРТВЫ), `HeaderTooltip`, `Expanded`(+Changed), `HeadingLevel`, `Disabled`,
  `Size`, `MarkerPosition`, `Block`.
- **FluentGrid**: `Spacing` (int 0..10, «multiple of 4px»; **default 0 — в v4 был 3**, молчаливое
  визуальное изменение), `Justify`, `AdaptiveRendering`, `OnBreakpointEnter`, `CurrentSize`(get).
  **FluentGridItem**: `Xs/Sm/Md/Lg/Xl/Xxl`, `Justify`, `Gap`, `HiddenWhen`, `AdaptiveRendering`.
- **FluentOverlay**: `Visible` (**ЖИВ**)+Changed, `FullScreen`, `Interactive`, `BackgroundColor`
  (default `var(--colorBackgroundOverlay)`), `Opacity` (default 40%), `CloseMode`.
- **FluentProgressBar**: `Value`, `Min`, `Max`, `Visible` (ЖИВ), `Width`, `Shape`, `State`
  (Success/Warning/Error), `Color`, `BackgroundColor`, `Stroke`, `Thickness`.

---

## 5. v4 → v5: удалённое/переименованное (сверено с пакетом 5.0.0)

### Компоненты (проверено: 0 вхождений в XML-доках = удалены)

| v4 | v5 |
|---|---|
| FluentTextField, FluentSearch | FluentTextInput (+TextInputType.Search; иконку поиска рисовать самим через StartTemplate — v5 не рендерит её) |
| FluentNumberField | FluentNumberInput<TValue> |
| FluentAnchor | FluentLink |
| FluentPersona | FluentAvatar (имя/цвет — вручную) |
| FluentSplitter | FluentMultiSplitter + FluentMultiSplitterPane |
| FluentToolbar | удалён без замены (в Mars → div) |
| FluentNavMenu / FluentNavGroup / FluentNavLink | FluentNav / FluentNavCategory / FluentNavItem (+FluentNavSectionHeader) |
| FluentBodyContent | удалён (обычный div) |
| FluentRating | FluentRatingDisplay |
| FluentProgress | FluentProgressBar |
| FluentProgressRing | FluentSpinner (alias ещё жив, «will be removed») |
| FluentDesignSystemProvider | удалён → IThemeService |
| FluentToastProvider/DialogProvider/TooltipProvider/MessageBarProvider | один FluentProviders |
| IDialogService.ShowDialogAsync/DialogParameters (v4-набор) | v5-диалоги; в Mars — шим `Mars.Admin.Framework/Dialogs/` |
| FluentDialogHeader/Footer | FluentDialogBody (TitleTemplate/ActionTemplate) |
| IToastService | INotificationService (в Mars — FluentMessageServiceBridge) |
| FluentValidationMessage | ЖИВ (generic `<TValue>`) — пункт плана «удалён» был неточен |
| FluentMenuButton | ЖИВ, но переработан (в Mars заменён на FluentButton + FluentMenu Trigger) |

### Параметры (✝ = молча умрёт через AdditionalAttributes, если не переименовать)

| v4 | v5 |
|---|---|
| Button.Autofocus / Action / Enctype / Method | AutoFocus / FormAction / FormEncType / FormMethod |
| Label.Typo / Label.Color | ✝ → FluentText (Size/As/Color) |
| Badge.Fill / Badge.OnClick / текст в ChildContent | ✝ / ✝ / Content |
| Tooltip.Visible | ✝ (Anchor ЖИВ) |
| Menu.UseMenuService / Menu.Anchor / @bind-Open | ✝ / ✝ / ✝ → Trigger (id) + OpenMenuAsync |
| Divider.Orientation | ✝ → Vertical (bool) |
| Spinner.Width | ✝ → Size (SpinnerSize) |
| SortableList.ListItemHeight / ListItemFilteredColor / ListBorderWidth | ✝ → Style="--fluent-sortable-list-item-height:…" |
| DataGrid.ColumnOptionsLabels / ColumnResizeLabels | ColumnOptionsMenuSettings / ColumnResizeMenuSettings |
| DataGrid Align / GenerateHeaderOption | DataGridCellAlignment / DataGridGeneratedHeaderType |
| Tab.Label | Header |
| AccordionItem.Heading / HeadingContent | Header / HeaderTemplate (HeadingLevel ЖИВ) |
| TextInput Maxlength / TextFieldType / FluentInputAppearance.Filled | MaxLength / TextInputType / TextInputAppearance.FilledDarker |
| Select.SelectedOption(s) / SelectedOptionChanged | Value / SelectedItems / ValueChanged |
| MessageBar MessageIntent | MessageBarIntent |
| Stack.HorizontalGap/VerticalGap (int?) | string («10» без px компилируется, но визуалка ломается) |
| Dialog Hidden / TrapFocus / PreventScroll / @ondialogdismiss | ✝ → ShowAsync/HideAsync + OnStateChange(DialogState) |
| Color/Appearance enum: Neutral / Accent | Default / Primary |
| Autocomplete.AutoComplete | ✝ (нет такого параметра) |
| GridItemsProviderRequest.SortByColumn/SortByAscending | SortColumns |

Удалённые компоненты ловит сборка (неизвестный тег → warning RZ10012; чистая сборка = мёртвых
тегов нет). Удалённые ПАРАМЕТРЫ живых компонентов сборка НЕ ловит — только grep (раздел 7).

---

## 6. Известные тихие грабли v5 (сверено с пакетом/репо)

1. **FluentMenu требует обёртку FluentMenuList**: веб-компонент берёт ПЕРВЫЙ элемент дефолтного
   слота как `_menuList` и вешает на него popover; без обёртки пункты рендерятся статично, в
   попап улетает первый пункт. Инлайн-`<style>` внутри меню перехватывает `_menuList` — выносить.
2. **NEVER null в Icon-параметры**: любой null-Icon долетает до `Activator.CreateInstance<Icon>()`
   → `NotSupportedException` («Please use the constructor including parameters»). Убрать иконку
   параметром нельзя (v5 не умеет прятать, например, search-иконку Autocomplete).
3. **FluentBadge**: текст только в `Content`; `OnClick` нет (обёртка span); `Fill` мёртв;
   компонент всегда пишет атрибут `color` (дефолт brand — Mars глобально серит оверрайдом
   `fluent-badge[color="brand"]:not(.badge-accent)`); `Subtle` невидим на светлом фоне
   (= colorNeutralBackground1 = белый); в css библиотеки опечатка `[color=sucess]` (bundle.scp.css —
   проверено), компонент пишет `success`, оверрайды работают.
4. **FluentLabel без Typo/Color** — молчаливая потеря стиля через AdditionalAttributes;
   типографика через FluentText (маппинг в разделе 4).
5. **FluentGrid.Spacing default 0** (v4: 3) — молчаливое визуальное изменение.
6. **FluentIcon default currentColor** (v4: Accent) — молчаливая визуальная регрессия; акцент
   точечно `Color`/`WithColor`.
7. **FluentDataGrid + Virtualize**: первый запрос даёт `Count=0` (не null) — `req.Count ?? Default`
   не срабатывает; clamp `req.Count is > 0 ? … : Default`.
8. **ObjectDisposedException DataGrid после закрытия диалога**: глобальный FluentKeyCode
   (ресайз колонок) не отписывается до dispose; поздний keydown → `SetColumnWidthDiscreteAsync`
   бросает. Не влияет на данные; в Mars — фильтр `IsKnownFrameworkRace` в BrowserErrorTracker.
9. **FluentTextInput: атрибут `value` ≠ свойство**: Blazor рендерит Value как АТРИБУТ хоста, у
   внутреннего input атрибут = defaultValue — программная очистка `Value=""` видимый текст НЕ
   стирает; нужно JS (`el.value=''`). Быстрый «ввод+Enter» теряет символ (async JS-interop) —
   лечить `ChangeAfterKeyPress`+`OnChangeAfterKeyPress` (значение в `FluentKeyPressEventArgs.Value`).
10. **Диалоги**: v5 `Hidden`/`TrapFocus`/`PreventScroll`/`@ondialogdismiss` мертвы; инлайн-диалог —
    `@ref` + `ShowAsync()/HideAsync()` (синхронизация с флагом в OnAfterRenderAsync) +
    `OnStateChange(DialogState.Closed)`. Ширина: `--dialog-width` мёртв, у shadow-`dialog`
    жёсткий `max-width:600px` — ширина через `::part(dialog)`.
11. **IMask/Sortable с CDN + AMD-лоадер monaco** — глобалы не создаются; вендоринг до
    `loader.js` (в Mars сделано: `src/Mars.Admin/wwwroot/js/imask.min.js`, `Sortable.min.js`).
12. **FluentInputFile**: `OnInputFileChange` отключает встроенную загрузку (память проекта).
13. **default-fuib.css всегда грузится** и перебивает поздние `a{}`-правила каскадом adopted-листа
    (underline) — лечить специфичностью (`body a`), а не порядком файлов. Отключается только
    `no-fuib-style`.
14. **SetThemeToElementAsync пишет ПОЛНЫЙ набор токенов inline на элемент**, включая хардкод-
    геометрию (borderRadiusMedium:"4px" и пр.) — в скоуп-превью геометрию класть ВНУТРЕННИМ
    элементом (ближе к компонентам), иначе слайдеры не работают.
15. **Important-мост перебивает IThemeService**: глобальную геометрию через Theme-мутацию не
    задать — только CSS-переменные (StylerCssVars); Theme-канал релевантен лишь для не покрытых
    мостом токенов (Spacings/Typography).
16. **Razor-значения НЕстрок-параметров парсятся как C#-выражения без `@`** (`Visible="_flag"` —
    валидно); не путать с мёртвыми атрибутами при grep-аудите.
17. **FluentNav**: пункты красятся `--nav-bg-color` (default Background4) — для «прозрачного»
    сайдбара ставить `--nav-bg-color: transparent` на контейнер.
18. **Селекторы**: у `fluent-button` нет внутреннего button (клик по хосту), но ЕСТЬ
    `::part(content)` (span-обёртка слота, проверено живым DOM 2026-10-01); `name` на хосте
    `fluent-text-input`; DataGrid — `table.fluent-data-grid` (не custom element; проверено
    живым DOM 2026-10-01); `fluent-label` без класса;
    nav-теги слитные (`fluent-navitem`, `fluent-navcategoryitem`, `fluent-inputfile-container`,
    `fluent-datepicker`).
19. **FluentCombobox вызывает `GetOptionValue/GetOptionText` с null** (OnAfterRenderAsync →
    `FluentListBase.GetOptionValue(null)`) — лямбды `OptionValue`/`OptionText` ОБЯЗАНЫ быть
    null-safe (`p => p?.Id ?? 0`), иначе NRE при рендере (поймано crawl 2026-10-01 на
    GallerySelectsSection; FluentSelect таким не страдает). Кандидат на upstream-репорт.
20. **FluentDataGrid фиксирует ПОРЯДОК РЕГИСТРАЦИИ колонок** (v4 брал порядок из дерева
    рендера). Если статичная колонка (напр. Actions) отрендерилась на первом рендере ДО
    асинхронно строящегося `@foreach` — она навсегда останется первой. Лечение: не рендерить
    грид, пока все колонки готовы (`@if (_gridReady)`, паттерн ManagePostView 2026-10-01);
    пересоздание через `@key` порядок исправляет только если колонки уже на месте.
21. **fluent-button host несёт `min-width: 96px`** (стандарт текстовой кнопки Fluent v9,
    из host-стилей бандла). Icon-only через параметры `IconStart`/`IconEnd` компонент
    square'ит сам (32×32), а кнопка с иконкой в **ChildContent** растягивается 3:1.
    Оверрайд в `fluent-ui.less`: `fluent-button:has(> svg:only-child) { min-width: auto; }`.
    Связанный факт: **v5 FluentIcon рендерит голый `<svg>`** (без обёртки `fluent-icon`).
22. **FluentDataGrid DOM-контракт v5** (вытянуто из bundle.scp.css/DLL 2026-10-06):
    корень — `table.fluent-data-grid` + атрибут `display-mode` из параметра
    `DisplayMode` (`DataGridDisplayMode.Grid|Table`, ДЕФОЛТ Grid); в режиме Grid —
    `display:grid`, `thead/tbody/tr` = `display:contents`, th/td — ПРЯМЫЕ grid-элементы
    (fr-единицы в `GridTemplateColumns` работают только в Grid-режиме; в Table — ширины
    через `Width` у колонок). Следствие: стили «строки» (фон/hover/разделители) задавать
    ПО ЯЧЕЙКАМ, у tr нет бокса. Docs-предупреждение: с `Virtualize` авторы рекомендуют
    Table-режим («Grid может давать odd scrolling behavior»); в Mars Grid+Virtualize
    используется штатно (ManagePostView, UsersPage) — при странностях скролла первым
    делом пробовать Table. `ItemSize` обязателен при Virtualize.
    Hover бандла: `tr[hover]:not([row-type=header],[row-type=sticky-header],…):hover td`
    → `cursor:pointer; background: var(--datagrid-hover-color, --colorNeutralStroke2)`
    (фон переопределяется CSS-переменной без войны специфичностей; cursor — только
    дублированием селектора). Sticky-заголовок: `tr[row-type=sticky-header]>th` →
    `position:sticky;top:0;background-color:var(--colorNeutralBackground4);z-index:2`.
    Состояния строк: `tr[row-state=empty-content|loading-content|error-content|detail-content]`.
    Фрагменты: `ChildContent/RowDetails/EmptyContent/LoadingContent/ErrorContent` —
    при использовании именованных колонки ОБЯЗАТЕЛЬНО заворачивать в явный `<ChildContent>`
    (RZ9996). Базовые th/td: `border-bottom: var(--strokeWidthThin) solid var(--colorNeutralStroke2)`.

---

## 7. Машинно-читаемый блок для sweep-скрипта

Мёртвые v4-параметры/компоненты для grep по `.razor`/`.cs` (и `.less`/`.css` для токенов).
Ложные срабатывания описаны в `note`; `excludeFiles` — заведомо легитимные вхождения.

```json
"sweep-dead-params"
{
  "patterns": [
    {"re": "<FluentToolbar\\b", "note": "компонент удалён"},
    {"re": "<FluentAnchor\\b", "note": "удалён → FluentLink"},
    {"re": "<FluentPersona\\b", "note": "удалён → FluentAvatar"},
    {"re": "<Fluent(NavMenu|NavLink|NavGroup)\\b", "note": "удалены → FluentNav*"},
    {"re": "<FluentSplitter[\\s>]", "note": "удалён → FluentMultiSplitter"},
    {"re": "<Fluent(TextField|NumberField)\\b|<FluentSearch[\\s>]", "note": "удалены → TextInput/NumberInput"},
    {"re": "<FluentBodyContent\\b", "note": "удалён → div"},
    {"re": "<FluentRating[\\s>]", "note": "удалён → FluentRatingDisplay"},
    {"re": "<FluentProgressRing\\b", "note": "alias FluentSpinner, будет удалён — мигрировать"},
    {"re": "\\bTypo=", "note": "мёртв у FluentLabel → FluentText Size/As"},
    {"re": "\\bFill=\"", "note": "мёртв у FluentBadge; lowercase fill= в svg не матчится"},
    {"re": "\\bThresholds=", "note": "мёртв (FluentRating удалён)"},
    {"re": "\\bEnctype=", "note": "мёртв у FluentButton → FormEncType"},
    {"re": "\\bColumnOptionsLabels|\\bColumnResizeLabels", "note": "→ ColumnOptionsMenuSettings/ColumnResizeMenuSettings"},
    {"re": "\\bListItem(Height|FilteredColor)=|\\bListBorderWidth=", "note": "мертвы у FluentSortableList"},
    {"re": "UseMenuService", "note": "ЖИВ у FluentDataGrid; мёртв только у FluentMenu — проверять контекст тега"},
    {"re": "Intent=\"MessageIntent", "note": "razor-атрибут Intent с v4-enum → MessageBarIntent; Mars.Core.Models.MessageIntent (C#) легитимен"},
    {"re": "\\bSelectedOptions?=|\\bSelectedOptions?Changed\\s*=\\s*\"", "note": "v4-параметры FluentSelect → Value/SelectedItems/ValueChanged; SelectedOptionChanged как API GroupedSelectDropDown (Mars) легитимен — смотреть тег"},
    {"re": "\\bGenerateHeaderOption", "note": "→ DataGridGeneratedHeaderType"},
    {"re": "@bind-Open\\b", "note": "мёртв у FluentMenu → Trigger/OpenMenuAsync/OpenedChanged"},
    {"re": "\\bHeading=\"|\\bHeadingContent", "note": "→ Header/HeaderTemplate у AccordionItem; HeadingLevel ЖИВ"},
    {"re": "<FluentTab\\b[^>]*\\sLabel=", "note": "Label мёртв у FluentTab → Header; у Button/MenuItem/InputBase Label ЖИВ"},
    {"re": "<FluentTooltip\\b[^>]*\\sVisible=", "note": "Visible мёртв ТОЛЬКО у FluentTooltip; ЖИВ у MessageBar/Overlay/Spinner/ProgressBar/Skeleton/Tab"},
    {"re": "<FluentButton\\b[^>]*\\sAutofocus=", "note": "у Button → AutoFocus; у входов (FluentInputBase.Autofocus) ЖИВ"},
    {"re": "<FluentDivider\\b[^>]*\\sOrientation=", "note": "мёртв у Divider → Vertical; у Stack/Tabs/Slider/RadioGroup/MultiSplitter/Overflow/Spacer Orientation ЖИВ"},
    {"re": "<FluentSpinner\\b[^>]*\\sWidth=", "note": "мёртв у Spinner → Size; Width жив у других"},
    {"re": "<FluentAutocomplete\\b[^>]*\\sAutoComplete=", "note": "мёртв у Autocomplete; ЖИВ у TextInput/TextArea"},
    {"re": "<FluentMenu\\b[^>]*\\s(Anchor|UseMenuService)=", "note": "мертвы у FluentMenu"},
    {"re": "--type-ramp-|--accent-fill-|--neutral-(fill|layer|foreground|stroke)-|--design-unit|--neutral-base-color|--control-corner-radius|--base-height-multiplier|--badge-fill-|--dialog-width", "note": "мёртвые v4 CSS-токены/переменные (в .less/.css/inline-Style)"},
    {"re": "::part\\(control\\)", "note": "жив только у fluent-text-input/fluent-textarea; мёртв у dialog(→dialog)/switch(→checked-indicator)/tree-item(→content+positioning-region)/button(→content)"},
    {"re": "\\.fluent-input-label", "note": "v5 рендерит fluent-label без класса → селектор по тегу"},
    {"re": "\\b(TrapFocus|PreventScroll)\\s*=\\s*\"", "note": "razor-атрибуты FluentDialog мертвы; C#-инициализаторы DialogParameters (без кавычек) — свойства шима Mars, легитимны"}
  ],
  "excludeFiles": [
    "src/Admin/Mars.Admin.Framework/Dialogs/",
    "tests/Mars.E2E.Tests/",
    "ai/",
    "wwwroot/css/style.css",
    "docs/**/bin/",
    "**/obj/",
    "**/*.min.js"
  ]
}
```

Легитимные вхождения (не чинить): `Anchor=` у FluentTooltip (ЖИВ); `Visible=` у
FluentMessageBar/FluentOverlay/FluentSpinner/FluentProgressBar/FluentSkeleton/FluentTab;
`Autofocus` у входов (FluentInputBase); `TrapFocus`/`PreventScroll` в
`Mars.Admin.Framework/Dialogs/` (шим DialogParameters); `UseMenuService` у FluentDataGrid;
`Data=` у FluentDropZone (ЖИВ); закомментированные `@* <FluentTextField … *@` в
OpenIDClientOptionEditForm.razor и EditPostTypePage.razor (4 шт., вне сборки).
