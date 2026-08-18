# CSS Refactoring Guide — подготовка к редизайну админки Mars

## Что было сделано

Три фазы рефакторинга CSS админки (`src/AppAdmin/wwwroot/css/`):

1. **Чистка мёртвого кода** — удалено ~40-50% неиспользуемого CSS
2. **CSS Custom Properties** — система дизайн-токенов `--mars-*`
3. **BEM-переименование** — переход на kebab-case / BEM-нaming

---

## 1. Дизайн-токены `--mars-*`

Все токены определены в `:root` в `base.less`. Система семантическая — имена по назначению, не по цвету.

### Цвета

```css
/* Brand */
--mars-color-primary      /* главная кнопка, ссылки, активные состояния */
--mars-color-accent       /* второстепенный акцент, теги, бейджи */
--mars-color-success      /* подтверждение, позитивный статус */
--mars-color-warning      /* предупреждение */
--mars-color-danger       /* ошибка, удаление */
--mars-color-info         /* информационный бейдж */

/* Текст */
--mars-text-primary       /* основной текст */
--mars-text-secondary     /* вспомогательный, подписи */
--mars-text-disabled      /* неактивный элемент */
--mars-text-inverse       /* текст на тёмном/акцентном фоне */
--mars-text-link          /* ссылка */

/* Фон */
--mars-bg-page            /* фон страницы (body) */
--mars-bg-surface         /* карточка, панель, модалка */
--mars-bg-surface-hover   /* hover на карточках/строках */
--mars-bg-surface-active  /* active/selected */
--mars-bg-subtle          /* второстепенный фон (секции) */
--mars-bg-overlay         /* затемнение под модалкой */

/* Границы */
--mars-border-default     /* обычная граница */
--mars-border-subtle      /* еле видимая */
--mars-border-focus       /* фокус-ring */
--mars-border-error       /* граница при ошибке */
```

### Размеры и эффекты

```css
/* Radius */
--mars-radius-sm / md / lg / full

/* Shadow */
--mars-shadow-sm / md / lg

/* Z-index */
--mars-z-dropdown / sticky / overlay / modal / toast

/* Motion */
--mars-duration-fast (100ms) / normal (200ms) / slow (400ms)
--mars-easing-default (ease-out)
```

### Связь с Fluent UI

`--mars-*` — источник правды. Fluent DesignSystemProvider получает значения из них:

```css
:root {
  --mars-color-primary: var(--accent-fill-rest, #2f71fc);
  --mars-text-primary: var(--neutral-foreground-rest, #1a1a1a);
  --mars-bg-page: var(--neutral-fill-layer-rest, #ffffff);
  --mars-radius-sm: calc(var(--control-corner-radius, 4) * 1px);
}
```

Когда пользователь меняет настройки в StylerPage (`/builder/styler`) — Fluent tokens обновляются → `--mars-*` автоматически пересчитываются.

### Bootstrap bridge

Bootstrap переменные маппятся на `--mars-*`:

```css
:root {
  --bs-primary: var(--mars-color-primary);
  --bs-success: var(--mars-color-success);
  --bs-danger: var(--mars-color-danger);
  /* ... */
}
```

---

## 2. Dark Mode

Два механизма (оба переопределяют `--mars-*`):

```css
/* Системная тема */
@media (prefers-color-scheme: dark) {
  :root:not(.light-mode) { /* переопределения */ }
}

/* Ручное переключение */
body.dark { /* переопределения */ }
```

Класс `body.dark` — основной механизм переключения в админке. `prefers-color-scheme` — автоматический fallback.

---

## 3. BEM-именование

Принят стандарт: **kebab-case** для блоков, `__` для элементов, `--` для модификаторов.

### Переименования

| Было | Стало | Где |
|------|-------|-----|
| `.horizontal-menu` | `.top-navbar__menu` | HeaderAdmin1.razor |
| `.menu-button` | `.icon-button` | HeaderAdmin1.razor |
| `.adaptive-table` | `.data-table--adaptive` | 9 файлов (FluentDataGrid) |
| `.GalleryEditView` | `.gallery-edit-view` | file-uploader.less + GalleryEditView.razor |
| `.ExampleFileView` | `.example-file-view` | pages.less + ExampleFileView.razor (AppFront.Shared), ExampleFileView2.razor (AppFront.Main) |
| `.MediaTable` | `.media-table` | MediaTable.less + FSelectMedia.razor, FluentMediaFilesList.razor (AppFront.Main) |

> Важно: глобальный CSS админки используют и другие проекты — `AppFront.Main`, `AppFront.Shared`, `Mars.Nodes.*`, `Mars.Modules.*`, `Mars.WebApp`. Любое переименование/удаление класса нужно проверять по всему `src/`, а не только по `AppAdmin`.

### Замена на Bootstrap-эквиваленты

| Было | Стало | Где |
|------|-------|-----|
| `text-strong` | `fw-bold` | NodeEditor1.razor (Mars.Nodes.Workspace) |
| `text-strike` | `text-decoration-line-through` | FormMetaField.razor (AppFront.Main) |
| `text-italic` | `fst-italic` | FormMetaField.razor (AppFront.Main) |

### Утилиты (остались без изменений)

`.xcenter`, `.scroll-y`, `.position-relative`, `.cursor-pointer`, `.spacer-1/2/3/5`, `.fz10/12/14/16/18/20/22/24px`, `.fw-400/600`, `.text-accent/fade/black2`, `.lines-1/2`, `.Montserrat`, `.color-accent`

---

## 4. Структура файлов

```
wwwroot/css/
├── style.less              ← entry point (@import всех файлов)
├── _variables.less         ← LESS-переменные (цвета, breakpoints, media queries)
├── base.less               ← reset, :root --mars-* tokens, dark mode, body
├── mixins.less             ← LESS-миксины (.xcenter(), .bg(), .bg-contain(), etc.)
├── typography.less         ← текстовые утилиты (fz10-24px, fw-400/600, lines-1/2, text-*)
├── class.less              ← layout-утилиты (xcenter, scroll, bg, position, cursor), .custom-scroll1
├── spacers.less            ← .spacer, .spacer-1, .spacer-2, .spacer-3, .spacer-5
├── animations.less         ← @keyframes
│
├── form.less               ← .top-navbar, .EditOptionForm, fluent input layout
├── layout.less             ← .top-navbar nav-profile, .icon-button, .rounded-15/8, .admin-layout, .btn-backbutton, .layout-standart-title
├── header.less             ← pre.wrap, svg, .monaco-editor-container
├── bs-styles.less          ← Bootstrap overrides (form-control, btn-primary, pagination), .use-fluent-typo / fluent-dialog h1-h6
├── blazor.less             ← Blazor boilerplate, .d-document-uploader / .d-file-* (медиа-списки), validation
│
├── fluent-ui.less          ← Fluent UI overrides (.data-grid, .data-table--adaptive)
├── action-center.less      ← Command Palette
├── spotlight.less          ← Spotlight overlay
├── dialogs.less            ← Dialog styles
│
├── a-icons.less            ← Icon definitions (604 lines)
├── extra-icons.less        ← Additional icons
├── builder_classmodels.less← Builder model badges
├── builderlayout.less      ← Builder layout
│
├── pages.less              ← .example-file-view, .DEV_btn_page__refresh, .d-card-glow
├── extra2.less             ← Animations, decorative effects
├── file-uploader.less      ← File uploader components
├── kanban.less             ← Kanban board
├── slick-slider.less       ← Slider styles
├── success-check.less      ← Success animation
├── MediaTable.less         ← Media table
│
├── loader.less             ← Loader animations
├── loader-hex.less         ← Hex loader
├── loader-techwork.less    ← Techwork loader
├── print.less              ← Print styles
```

### Удалённые файлы (мёртвый код)

- `gutters.less` — Quasar-наследие; мёртв (`.q-gutter-md` заменён на Bootstrap `gap-3` в разметке)
- `ant-widgets.less` — Ant Design overrides, Ant выпилен
- `metafields.less` — только `.ant-*` селекторы
- `buttons.less` — весь файл закомментирован
- `fix.less` — мёртвые хаки (IE, jQuery UI, glitch-эффект); живое (pre.wrap, svg, .monaco-editor-container) перенесено в header.less
- `dashboard.less` — Ant Design dashboard виджеты, не используются

### Удалённые мёртвые классы (из живых файлов)

Из `layout.less`: `.btn-push`, `.menu-button-search`, `.user-avatar`, `.d-paginator.float-up`, `.mainmenu-grid-items`, `.mainmenu-left1`, `.text-overflow-ellipsis`, `.content-wrapper` (правило было пустым), `.PlusButton`, `.bg-lime/aqua/null`, `.navbar-brand-wrapper`, `.menu-admin1___fff`

Из `pages.less`: `.neu-search-bar`, `.s-card`, `.x-card-1`, `.x-card-2`, `.d-project-mode-select-modal`

Из `blazor.less`: `.box-list-icon`, `.driver-info`, `.stretch-card`, `.no-transition`, `.text-line`, `.form-compact` (правила только под `.ant-*`), `.d-teeth-uploader`, `.d-input-file-area`, `.spl-my-control-*`

Из `typography.less`: `.fx-*`, `.text-center/left/right-sm/md/lg`, `.fz26-38px`, `.fw-500/700`, `.lines-3/4`, `.text-bold/normal/italic/underline/semibold/strike` (заменены на Bootstrap там, где использовались), `.Exo2`, `.Roboto`, `.title-2`, `.text-orange/light/light-accent/dark-accent`, `.ondark_text-accent`

Из `class.less`: `.xcenter-disable-*`, `.display-inline-block`, `.bg-accent/light-accent/dark-accent/red`, `.json`, `.tagslist`, `.btn-tag`, `.clip-thumbnail`, `.nodecoration`, `.neu`, `.neu-shadow`, `.only-hover`, `.hidden-xs`, `.hideincrementor`

Из `spacers.less`: `.spacer25/50/75/100/150/200`, `.spacer-4/6/7/8/9/10`

Из `bs-styles.less`: `.card.white`, `.carousel-*` (карусели не используются)

### Восстановленные классы (оказались живыми за пределами AppAdmin)

Первая чистка проверяла использования только в `src/AppAdmin`; эти классы используются в `AppFront.Main`, `AppFront.Shared`, `Mars.Nodes`, `Mars.Datasource`, `Mars.WebApp` и были восстановлены:

- `.d-document-uploader` + `.d-file-list/.d-file-item/.d-file-preview-icon/.d-file-item-actions-*` → `blazor.less` (FSelectMedia, FluentMediaFilesList; без `.ant-*` частей)
- `.media-table` (переименование `.MediaTable` доделано в FSelectMedia.razor, FluentMediaFilesList.razor)
- `.example-file-view` (переименование `.ExampleFileView` доделано в ExampleFileView.razor, ExampleFileView2.razor)
- `.DEV_btn_page__refresh` → `pages.less` (SinglePost, RemotePageViewer, OnePage + querySelector в scripts.js)
- `.btn-backbutton`, `.layout-standart-title` → `layout.less` (DBackButton, ContentWrapper)
- `.q-gutter-md` → заменён на Bootstrap `gap-3` в разметке (SmtpSettingsEditForm, ClientEditPost1, EditOptionForm), CSS удалён
- `.custom-scroll1` → `class.less` (DatabaseQueryWorkspace, NodeEditor1, DebugMessagesConsole, AppFrontTemplateViewPage)
- `.spacer-3`, `.spacer-5` → `spacers.less` (LoginPage, RegisterPage, GalleryEditView, BuilderAppsPage, AdminEditUserPage, EditNavMenuPage)
- `.fz10px`, `.fz16px`, `.fz20px`, `.fz22px` → `typography.less` (MetaFieldViews, UserBar, NodeEditor1, LoginPage, RegisterPage, PluginsListPage, EditFF1/DisplayFF1)
- `.use-fluent-typo`, `fluent-dialog` h1-h6 → `bs-styles.less` (NodeEditor1)

---

## 5. LESS-переменные (`_variables.less`)

```less
/* Цвета */
@color-accent: #009d9d;      /* → var(--mars-color-accent) */
@color-primary: #2f71fc;     /* → var(--mars-color-primary) */
@color-bg: #ededf3;          /* → var(--mars-bg-subtle) */
@color-bg-dark: #3e3e42;     /* → var(--mars-bg-subtle) dark variant */
@color-light: #f6f2fa;
@color-light-accent: #d3ebff; /* → var(--mars-bg-surface-active) */
@color-dark-accent: #333084;
@color-orange: #d95331;      /* → var(--mars-color-danger) */
@color-blue: #284f9a;

/* Breakpoints */
@media_xs: 576px;  @media_sm: 768px;  @media_md: 992px;  @media_lg: 1200px;
```

Эти LESS-переменные ещё используются для компиляции. При переходе на чистый CSS — заменятся на `--mars-*`.

---

## 6. Что осталось для следующего шага

### Вынос общих стилей в AppFront.Main (отложено)

**Как сейчас:**
- `AppFront.Main` (NuGet `mdimai666.Mars.AppFront.Main`) — общая UI-библиотека: на неё ссылаются AppAdmin, Mars.WebApp, Mars.Docker/Datasource/Options/AiChat.Front, Mars.Nodes.FormEditor, Mars.Plugin.Kit.Front
- Её компоненты (`FluentMediaFilesList`, `FSelectMedia`, `DBackButton`, `ContentWrapper`, `ExampleFileView`, `EditOptionForm`, `SinglePost`/`OnePage`/`RemotePageViewer`, MetaFieldViews...) зависят от глобального CSS, который живёт в AppAdmin (`style.less`)
- Загружает его только админка (`AppAdmin/wwwroot/index.html`); layout'ы Mars.WebApp `style.css` не подключают — на сайт-фронте общие компоненты фактически без стилей
- В самом AppFront.Main css нет вообще (`wwwroot/js`)

**Кандидаты на перенос:**
- Компоненты: `.media-table`, `.d-document-uploader` + `.d-file-*`, `.example-file-view`, `.DEV_btn_page__refresh`, `.btn-backbutton` + `.layout-standart-title`, `.d-card-glow`, `.EditOptionForm`
- Утилиты, которые они используют: `.xcenter`, `.spacer-*`, `.fz*`/`.fw*`/`.lines-*`/`.text-*`, `.cursor-pointer`, `.custom-scroll1`
- Токены `--mars-*` + dark mode (`base.less`)

**Варианты (обсуждались, выбор не сделан):**
- **A. Минимальный** — только стили компонентов; утилиты и токены остаются в AppAdmin (зависимость сохраняется частично)
- **B. Полный** (рекомендовался) — токены + база + утилиты + компоненты; AppFront.Main становится носителем дизайн-системы, сайт-фронт тоже сможет подключить общий css
- Формат нового файла: предпочтительно **чистый CSS с нативным nesting** — форпост миграции LESS→CSS, чтобы не растить LESS-долг

**Как делать, когда вернёмся:**
1. Инвентаризация классов, реально используемых компонентами AppFront.Main/AppFront.Shared (grep по разметке всего `src/`)
2. `AppFront.Main/wwwroot/css/mars-front.css`; LESS-миксины (`.xcenter()`, `.bg()`) инлайнить
3. `<link href="_content/mdimai666.Mars.AppFront.Main/css/mars-front.css">` в `index.html` админки **до** `css/style.css` (порядок каскада); перенесённые блоки удалить из less-файлов AppAdmin, перекомпилировать `style.css`
4. `dotnet build Mars.slnx`, визуальная проверка, bump `MarsAppVersion`, запись в этот гайд
5. Нюанс: хосты плагинов (Mars.Plugin.Kit.Front) должны подключать общий css — задокументировать

### LESS → чистый CSS

- LESS nesting → нативный CSS nesting (`& { }`)
- LESS mixins → inline CSS или `@layer`
- `:extend()` → дублирование правил
- `@import` → CSS `@import` или bundle
- Убрать build-шаг LESS компиляции
- Новая структура файлов (components/, pages/)

### Typography tokens

Сейчас font-size/weight заданы точечно (`.fz12px`, `.fw-600`). Для редизайна:
- Добавить `--mars-font-size-*` и `--mars-font-weight-*` в `:root`
- Заменить `.fz*px` на `var(--mars-font-size-*)`

### Spacing tokens

Сейчас отступы — классами (`.spacer-1`, `.spacer-2`). Для редизайна:
- Добавить `--mars-space-1..7` в `:root`
- Значения определишь при редизайне

### Не-BEM имена (остались)

- `.d-document-uploader`, `.d-file-*`, `.d-fluent-input-description`, `.d-card-glow` — префикс `d-`
- `.my-file-uploader1` — числовой суффикс
- `.ani-hover-onpush` — ad-hoc
- `.bg-trangle-start/end` — typo (trangle → triangle)
- `.DEV_btn_page__refresh` — dev-класс (можно убрать вместе с кнопкой при редизайне)

---

## 7. Как работать при редизайне

### Добавление нового цвета

1. Добавить в `:root` в `base.less`: `--mars-color-new: #value;`
2. Добавить dark mode override в `body.dark { }`
3. Использовать: `color: var(--mars-color-new);`

### Изменение существующего цвета

1. Изменить значение в `:root` (светлая тема)
2. Изменить в `body.dark { }` (тёмная тема)
3. Все места автоматически обновятся

### Стилер (StylerPage)

Пользователь может переопределять цвета через `/builder/styler`. Значения сохраняются в БД и передаются в `FluentDesignSystemProvider`. Если `--mars-*` маппится на Fluent token — он обновится автоматически.

### Проверка

**Важно:** `dotnet build` НЕ компилирует LESS. `style.css` — артефакт в git, его нужно перегенерировать отдельно:

1. **Visual Studio** — расширение *Web Compiler* компилирует автоматически при сохранении `.less` (конфиг: `src/AppAdmin/compilerconfig.json`, autoprefixer выключен, минификация выключена).
2. **Из командной строки** (эталонный компилятор lessc, нужен Node.js):

```bash
cd src/AppAdmin/wwwroot/css
npx --yes --package less lessc style.less style.css
```

Deprecation-предупреждения про `@media @mobiles` (bare @variable) — нормально, это устаревший LESS-синтаксис; убирается при миграции на чистый CSS (`@media (max-width: ...)`).

Затем сборка для проверки razor/проектов:

```bash
dotnet build Mars.slnx
```
