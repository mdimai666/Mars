# CSS Guide — стили админки Mars на FluentUI v5

Переписан под FluentUI Blazor v5 (2026-09-29). Доре-волюционная история (чистка мёртвого
кода, BEM-переименования, список удалённых файлов/классов) — в git-истории этого файла;
здесь только актуальное состояние. Основной объект: `src/Mars.Admin/wwwroot/css/`
(entry point `style.less`, артефакт `style.css`). Отдельно компилируется FormEditor:
`src/Mars.Nodes/Mars.Nodes.FormEditor/wwwroot/css/style.less`.

## Архитектура: три слоя

1. **FluentUI v5-токены** (`--colorNeutral*`, `--colorBrand*`, `--colorStatus*`,
   `--colorPalette*`, `--borderRadius*`, `--shadow*`, …). Генерируются в рантайме JS'ом
   библиотеки: пишутся **inline на `<html>`** (`style.setProperty`) и/или в
   `document.adoptedStyleSheets` (`:root`-декларации). Brand/neutral-палитры — из
   `StylerStyle` через `IThemeService` (см. «Стайлер»).
2. **`--mars-*`** — семантический слой и **источник правды** (определены в `:root` в
   `base.less`). Часть — алиасы на v5-токены (флипаются при смене темы автоматически),
   часть — Mars-литералы (статусы, accent, геометрия).
3. **Мост `html:root { … !important }`** (`base.less`) — натягивает выбранные Fluent-токены
   на Mars-значения. Плюс Bootstrap-мост `--bs-* → --mars-*` (там же).

Решение пользователя (2026-09-29, план A): **Mars-токены главнее Fluent**, а не наоборот.
Альтернатива (переход на Fluent-нейминг в нашем css) отклонена.

## Механика моста

`!important` в авторском листе перебивает ОБА механизма v5: inline на `<html>` без
important и adoptedStyleSheets (там декларации тоже не important). Исключение —
скоуп-превью стайлера (`SetThemeToElementAsync`, inline на элементе): inline глубже
каскада, в своём поддереве побеждает мост — так задумано.

Что мост переназначает (`base.less`, блок `html:root`):

| Fluent-токен | Откуда |
|---|---|
| `--colorStatus{Success,Warning,Danger}Foreground1/Inverted` | `--mars-color-{status}-fg/-on` |
| `--colorPalette{Red,DarkRed}*` (10 токенов) | `--mars-color-danger-*` (миксин `.fluent-palette`) |
| `--colorPalette{Green,DarkGreen,LightGreen}*` | `--mars-color-success-*` |
| `--colorPalette{Yellow,Marigold,DarkOrange}*` | `--mars-color-warning-*` |
| `--borderRadius{None,Small,Medium,Large,XLarge,Circular}` | `--mars-radius-*` (None=0, Small=sm÷2, Medium=sm, Large=md, XLarge=lg, Circular=full) |
| `--shadow{2,4,8,16,28,64}` | `--mars-shadow-{sm,md,lg}` |
| `--strokeWidthThin` | `--mars-stroke-hairline` |
| `--fontFamilyBase` / `--fontFamilyMonospace` | `--mars-font-family` / `--mars-font-mono` |

Что НЕ трогаем: `--colorBrand*` и `--colorNeutral*` (их генерит IThemeService из
StylerStyle), preset-палитры (Berry/Teal/…) — ими никто не пользуется.

Следствие: семантика MessageBar/Badge/Toast красится в Mars-статусы; шрифт Fluent-компонентов
— Roboto-стек (`--mars-font-family`); тени Fluent — простые Mars-тени.

## Mars-токены (`base.less`, `:root`)

Семантические имена — по назначению, не по цвету. Актуальный набор:

```
Цвета:      --mars-color-primary (→ --colorBrandForeground1), --mars-color-accent (литерал),
            --mars-color-{success,warning,danger,info} (LESS-литералы @mars-*)
Статус-производные: --mars-color-{status}-{fg,hover,pressed,on,bg,bg-hover,bg-strong,border,border-strong}
            (генерят миксины .mars-status / .mars-status-dark; ест Fluent-мост)
Текст:      --mars-text-primary/secondary/disabled (→ colorNeutralForeground1/3/Disabled),
            --mars-text-inverse (#fff), --mars-text-link (→ colorBrandForegroundLink)
Фон:        --mars-bg-page/surface/surface-hover/surface-active/subtle (→ colorNeutralBackground1/2/3/1Selected/3),
            --mars-bg-overlay (rgba-литерал)
Границы:    --mars-border-default/subtle (→ colorNeutralStroke1/2),
            --mars-border-focus (→ primary), --mars-border-error (→ danger)
Radius:     --mars-radius-base (скаляр стайлера, default 4px; 0 = острые углы HUD),
            sm=base, md=base×1.5, lg=base×3.75 (calc), full=9999px
Stroke:     --mars-stroke-hairline (скаляр стайлера, default 1px)
Shadow:     --mars-shadow-alpha (скаляр стайлера 0..2), --mars-shadow-{sm,md,lg-o} —
            базовая непрозрачность темо-зависима (light .05/.07/.1, dark .2/.3/.4);
            calc в альфа-канале считает браузер (LESS отдаёт буквально через ~"...")
Шрифты:     --mars-font-family (Roboto-стек), --mars-font-mono
Z-index:    --mars-z-{dropdown,sticky,overlay,modal,toast} (1000..1070)
Motion:     --mars-duration-{fast,normal,slow} (100/200/400ms), --mars-easing-default
```

Алиасы на `--colorNeutral*`/`--colorBrand*` в тёмной теме флипаются сами (v5
перегенерирует токены) — ручных dark-оверрайдов для них не нужно.

## v5-токены: живые и мёртвые

**Живые** (пакет 5.0.0, бандл lib.module.js / reboot.css / default-fuib.css):
`--colorNeutral*`, `--colorBrand*`, `--colorStatus*`, `--colorPalette*`,
`--fontSizeBase100..600`, `--fontSizeHero700..1000`, `--borderRadius*`, `--strokeWidth*`,
`--shadow*`, `--spacing*`, `--fontFamily*`. Compat-мост на `:root`:
`--success/--warning/--error/--info` (+ `-inverted`) и `--font-monospace` — живы
(определены как `var(--colorStatus*)`, оверрайды моста подхватывают).

**Мёртвые** (v4 FAST, в v5 не определены): `--type-ramp-*`, `--accent-fill-*`,
`--neutral-fill-*`, `--neutral-layer-*`, `--neutral-foreground-*`, `--neutral-stroke-*`,
`--design-unit`, `--neutral-base-color`, `--control-corner-radius`, `--base-height-multiplier`,
`--badge-fill-*`.

**Инвариант:** мёртвый токен в less НЕ даёт ошибки сборки — только молчаливую визуальную
деградацию. Перед использованием любого не-Mars токена в стилях — проверить, что он живой
(по этому списку или grep'ом css-бандла пакета).

Живые классы/элементы для оверрайдов: `.fluent-data-grid*` (v5 рендерит
`<div class="fluent-data-grid">`, обычная HTML-таблица — НЕ custom element),
`.fluent-sortable-list`, элемент `fluent-badge`, элемент `fluent-label` (v5 рендерит
без класса `.fluent-input-label` — селектор только по тегу).

## `::part` — карта v4 → v5

| Компонент | v4 | v5 |
|---|---|---|
| dialog | `::part(control)` | `::part(dialog)` |
| switch | `::part(switch)` | `::part(checked-indicator)`; host несёт `checked="true"` |
| tree-item | `::part(content-region)` | `::part(content)` + `::part(positioning-region)` |
| button | `::part(control)` | частей НЕТ (shadow = slot+span); стили на host, наследуются в slot |
| input/textarea | `::part(control)` | без изменений |

`::deep` не пробивает shadow DOM веб-компонента (как и в v4). Scoped razor.css НЕ отключены
(`ScopedCssEnabled` нигде нет) — работают.

## Тёмная тема

- Единственный хук: **`body[data-theme="dark"]`** — его ставит v5 IThemeService при
  Mode=Dark/System (+ CustomEvent `themeChanged`); в light атрибут снимается.
- `body.dark`, `prefers-color-scheme`-блоки в админке и `FluentDesignSystemProvider`
  **выпилены — не возвращать**. (В FormEditor `style.less` media `prefers-color-scheme`
  оставлен — он вне IThemeService.)
- Mars-литералы и статус-производные оверрайдятся в **`html:has(body[data-theme="dark"])`** —
  именно на html, не на body: эти значения потребляет Fluent-мост, который резолвится
  на `<html>`, body-блок туда не дотягивается. Там же — плотность теней
  (`--mars-shadow-*-o`) и осветление accent.
- Не-флипаемые утилиты (`bg-white2`/`bg-black2` инверсии, `ondark_*`) — под обычным
  `body[data-theme="dark"]`.

## Стайлер (StyleDesignerPage, `/builder/styler`)

Модель `StylerStyle` (`Mars.Admin.Contracts.Options`): `BrandColor`, `HueTorsion` (-0.5..0.5),
`Vibrancy` (-0.5..0.5), `IsExact`, `Mode` (Light/Dark/System) + геометрические скаляры
`Radius` (int 0..12, default 4), `StrokeWidth` (1..3, default 1), `ShadowIntensity`
(double 0..2, default 1). Старый сохранённый JSON (13 FAST-параметров v4) десериализуется
в дефолты.

Применение — два независимых канала:

- **Цвет/тема** — `App.razor.cs` `SetupThemeAsync` → `IThemeService.SetThemeAsync(ThemeSettings)`
  (на старте + по событию `App.SetupTheme` от Save). Персист в localStorage средствами v5.
- **Геометрия** — через CSS-переменные, **НЕ через Theme-мутацию**: important-мост на html
  перебил бы Theme-инъекцию. `StylerCssVars.GlobalVars` (`src/Mars.Admin/Builder/StyleDesignerViews/`)
  пишет минимум 3 скаляра (`--mars-radius-base`, `--mars-stroke-hairline`, `--mars-shadow-alpha`)
  в `<style>html:root{…}</style>` в `App.razor` (реактивно, без JS-интеропа); производные
  (radius sm/md/lg, тени, fluent `--borderRadius*`/`--shadow*`/`--strokeWidthThin`) считает
  base.less/мост.

Превью на странице (важен порядок):

- **Внешний** div `@ref=previewElement` — JS-тема (`SetThemeToElementAsync`, скоуп, не глобально).
- **Внутренний** div со `style=@PreviewStyle` (`StylerCssVars.PreviewVars` — полный набор:
  mars-токены + готовые fluent `--borderRadius*`/`--strokeWidthThin`/`--shadow*`), т.к.
  important-мост на html в поддереве не перерезолвится.
- Порядок критичен: `Er(tokens, el)` в бандле пишет на элемент ПОЛНЫЙ токен-набор inline,
  включая хардкод-геометрию (`borderRadiusMedium:"4px"` и т.п.). Геометрия должна быть
  ближе к компонентам (внутри), иначе слайдеры не работают (баг, пойман 2026-09-29).
- `PreviewThemeAsync` вызывать в `OnLoadData` (реальная модель грузится асинхронно;
  `EditOptionForm._model` на первом рендере — дефолт, не null).

## Fluent-оверрайды в less (ключевые)

- `fluent-ui.less`: **серый дефолт бейджей глобально** —
  `fluent-badge[color="brand"]:not(.badge-accent)` → `--colorNeutralBackground5`/`Foreground3`
  (v5-компонент всегда пишет `color="brand"`, дефолт библиотеки — brand-синий); осознанный
  акцент — класс `badge-accent`. `.fluent-data-grid*`, `.data-table--adaptive`.
- Семантика бейджей атрибутами: warning→`Warning`, error→`Danger`, success→`Success`,
  black→`Important`, neutral/теги→`Informative`. **`Subtle` не использовать на светлых
  поверхностях** (`--colorNeutralBackground1` = белый = невидимая пилюля). В css библиотеки
  опечатка `[color=sucess]`, но компонент пишет `success` — оверрайды работают.
- `form.less`: лейблы — селектор элемента `fluent-label` (не класс).
- `fluent-sortable-list.less`: item-height 32px, фоны на `--colorNeutralBackground2/4`.
- `FluentIcon` в v5 по умолчанию `currentColor` (в v4 был Accent) — принято; акцент
  ставится точечно `Color`/`WithColor`.
- `FluentLabel` в v5 без `Typo`/`Color` (уходят в AdditionalAttributes — молчаливая потеря
  стиля) — типографика через `FluentText` (таблица маппинга v4 Typo→Size — в плане миграции).

## Именование классов

Конвенция: kebab-case/BEM (`блок__элемент--модификатор`). Утилиты без изменений:
`.xcenter`, `.scroll-y`, `.position-relative`, `.cursor-pointer`, `.spacer-1/2/3/5`,
`.fz10..24px`, `.fw-400/600`, `.text-accent/fade/black2`, `.lines-1/2`, `.Montserrat`,
`.color-accent`.

**Правило:** глобальный CSS админки потребляют многие проекты — `Mars.Admin.Framework`,
`Mars.Nodes.*`, `Mars.Modules/*.Front`, `Mars.WebApp`. Любое переименование/удаление класса
проверять grep'ом по всему `src/`, не только по `Mars.Admin`. Старых проектов
(`AppAdmin`, `AppFront.Main/Shared`) больше нет — общие UI-компоненты живут в
`src/Admin/Mars.Admin.Framework/`.

## Структура файлов (`src/Mars.Admin/wwwroot/css/`)

```
style.less              ← entry point (@import всех)
_variables.less         ← LESS-переменные: цвета (@color-accent #009d9d, @color-primary #2f71fc, …) и media
base.less               ← --mars-* токены, статус-миксины, Fluent-мост (html:root !important), dark-хук, body
mixins.less             ← LESS-миксины (.xcenter(), .bg(), .bg-contain())
typography.less         ← fz*/fw*/lines-*/text-*
class.less              ← layout-утилиты, .custom-scroll1, ondark_*
spacers.less            ← .spacer-*
animations.less         ← @keyframes
form.less               ← .top-navbar, .EditOptionForm, fluent-label/description
layout.less             ← .admin-layout, .icon-button, .btn-backbutton, .layout-standart-title
header.less             ← pre.wrap, svg, .monaco-editor-container
bs-styles.less          ← Bootstrap-оверрайды (form-control, btn-primary, pagination)
blazor.less             ← Blazor boilerplate, .d-document-uploader/.d-file-*, validation
fluent-ui.less          ← Fluent-оверрайды (бейджи, data-grid, adaptive-таблицы)
fluent-sortable-list.less ← токены .fluent-sortable-list
filters.less            ← фильтры списков
action-center.less      ← Command Palette (весь на v5-токенах)
spotlight.less          ← Spotlight overlay
dialogs.less            ← диалоги (::part(dialog))
a-icons.less / extra-icons.less ← иконки
builder_classmodels.less / builderlayout.less ← Builder
pages.less / extra2.less / file-uploader.less / kanban.less / MediaTable.less
slick-slider.less / success-check.less
loader.less / loader-hex.less / loader-techwork.less
print.less
```

## Рецепты

**Новый цвет:**
1. Литерал/алиас в `:root` в `base.less`; если нужен dark-вариант — оверрайд в
   `html:has(body[data-theme="dark"])` (для значений, потребляемых мостом) или
   `body[data-theme="dark"]` (для утилит).
2. Если цвет должны есть Fluent-компоненты — расширить мост: статус через миксины
   `.mars-status`/`.mars-status-dark` + `.fluent-palette(@fam; @status)`.
3. Использовать `var(--mars-color-*)` в less/razor.

**Перекрасить Fluent-компонент:** сначала искать v5-свойство компонента (Badge Color/Appearance
и т.п.), затем селектор элемента/атрибута (`fluent-badge[color=…]`), и только потом токен.
Мёртвые v4-токены и `Fill=` не использовать.

**Новый параметр стайлера:** скаляр в `StylerStyle` → `StylerCssVars.GlobalVars` (глобально)
и `PreviewVars` (превью) → слайдер в `StyleDesignerPage` с `:after=PreviewThemeAsync` →
производные в `base.less` (calc) → при необходимости мост на fluent-токен.
НЕ через `Theme`-мутацию/`CreateCustomThemeAsync` — important-мост её перебивает
(механизм релевантен только для Spacings/Typography, если они когда-то понадобятся).

## Правила для ИИ-агентов (инварианты)

- Правки стилей — **только в `.less`**. `style.css` — артефакт компиляции в git; руками не
  редактировать, компиляцию не запускать (делает пользователь: VS Web Compiler по
  `compilerconfig.json` или lessc). После коммита css/js — bump `MarsAppVersion` в
  `Directory.Build.props`.
- `dotnet build` LESS не компилирует. Проверка правок — тестовая компиляция во временный файл
  **двумя версиями** (Web Compiler ≈ less 4.1):

```bash
cd src/Mars.Admin/wwwroot/css
npx --yes --package less@4.1.3 lessc style.less %TEMP%\style-check.css
npx --yes --package less lessc style.less %TEMP%\style-check-latest.css
```

- FormEditor компилируется так же: `src/Mars.Nodes/Mars.Nodes.FormEditor/wwwroot/css/style.less`.
- Новые файлы `wwwroot` попадают в отдачу только после пересборки (staticwebassets-манифест).
- Deprecation-предупреждения less про `@media @mobiles` (bare @variable) — нормально.

## Грабли LESS (проверено на 2.7.3 / 3.9.0 / 4.1.3 / latest)

- Голая интерполяция `@{param}` в **значениях** custom properties
  (`--x: var(--y-@{param});`) падает `NameError` на less 3.9–4.1 (т.е. в Web Compiler).
  Рабочая форма для всех версий — escaped-строка: `--x: ~"var(--y-@{param})";`.
  Интерполяция в **именах** свойств (`--colorPalette@{fam}Foreground1:`) работает везде;
  `mix()` и голые `@var` в значениях custom properties вычисляются везде.
- `*/` внутри css-комментария (`--colorNeutral*/--colorBrand*`) закрывает комментарий раньше
  времени → ParseError. В комментариях не писать «звёздочка+слэш» подряд.
- `calc(var(--x) / 2)` в значениях проходит как есть (less 4 не лезет в calc).
- Вывод 4.1.3 и latest совпадает (кроме косметических пробелов).

## Отклонено / отложено

- **Нейминг Fluent в нашем css** (отказ от `--mars-*` как источника) — отклонено 2026-09-29.
- **Ручная тёмная тема** (`body.dark`, prefers-color-scheme в админке,
  `FluentDesignSystemProvider`) — выпилена, замена: v5 IThemeService + `data-theme` хук.
- **Вынос дизайн-системы в общий css сайт-фронта** (бывшая идея «AppFront.Main») — отложено.
  Компоненты теперь в `Mars.Admin.Framework`, сайт-фронт `style.css` по-прежнему не грузит;
  при возврате — инвентаризация классов заново (старые списки в git-истории этого файла).
- **Typography/spacing токены** (`--mars-font-size-*`, `--mars-space-*`) — к редизайну;
  сейчас font-size/отступы классами (`.fz*px`, `.spacer-*`).
- **Не-BEM имена** (к переименованию при редизайне): `.d-document-uploader`, `.d-file-*`,
  `.d-fluent-input-description`, `.d-card-glow`, `.my-file-uploader1`, `.ani-hover-onpush`,
  `.bg-trangle-start/end` (typo), `.DEV_btn_page__refresh`.
