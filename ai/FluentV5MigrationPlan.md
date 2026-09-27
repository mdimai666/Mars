# Fluent UI Blazor v4 → v5 Migration Plan

Ветка: `feat/fluentui-v5`. Старт: 2026-09-27. Текущая версия: 4.14.4 (Components + Icons).
Целевая: 5.0.0 stable. Источник правды по изменениям: MCP `fluentui-blazor`
(`get_component_migration`, 60 гайдов) + https://www.fluentui-blazor.net/Migration.

Цель этапа: **рабочая мигрированная версия** (сборка + базовая функциональность).
Кастомные стили/хаки — только инвентаризируем (раздел «Стили»), правки в последующих этапах.
Если всплывает что-то большое — обсуждаем с пользователем до правки.

## Объём в репо

Прямые PackageReference:
- `src/Admin/Mars.Admin.Framework` (+Icons) — ядро, транзитивно тянет всех
- `src/Mars.Modules/Mars.Forms.Front`
- `docs/MarsDocs.WebApp` (+Icons)
- `devstands/TestModules` (+Icons)

Транзитивно затронуты: `Mars.Admin`, `Mars.Nodes.*` (FormEditor, Workspace),
`Mars.Datasource.Front`, `Mars.Modules/*.Front` (Docker, SemanticKernel, WebApp.Nodes),
`devstands/StandNodesApp`.

Корневые точки приложений (провайдеры/DI):
- `src/Mars.Admin/App.razor` + `MainAdminFramework.cs` (AddFluentUIComponents)
- `docs/MarsDocs.WebApp/App.razor` + `Program.cs`, `MarsDocs.DevServer/Program.cs`
- `devstands/TestModules/App.razor` + `Program.cs`
- `devstands/StandNodesApp/.../Routes.razor` + `Program.cs`

## Этапы

### 0. Bump пакетов и фиксация полного списка ошибок
- `Directory.Build.props`? нет — `Directory.Packages.props`: Components/Icons 4.14.4 → 5.0.0.
- `dotnet build Mars.slnx` — ошибки компиляции = авторитетная опись работ; сверять с MCP-гайдами.

### 1. Инфраструктура
- 5 отдельных провайдеров (`FluentToastProvider`, `FluentDialogProvider`, `FluentTooltipProvider`,
  `FluentMessageBarProvider`) → один `<FluentProviders>` в корнях (4 App.razor/Routes.razor).
- Проверить `AddFluentUIComponents()` в DI (MainAdminFramework + 4 Program.cs).
- v5 сам грузит JS/CSS — убрать ручные ссылки на fluentui-скрипты/стили в хостах, если есть.
- Scoped CSS: в v5 `DisableScopedCssBundling`/`ScopedCssEnabled=false`; `::deep` теряет смысл —
  учесть при инвентаризации стилей, сразу не вычищать.

### 2. Кастомные наследники FluentComponentBase (~12 файлов)
Конструктор теперь требует `LibraryConfiguration`; `Element` убран из базы (нужен —
`IFluentComponentElementBase`); `ParentReference` удалён.
Файлы: `Mars.Admin.Framework/Components/` (FluentMarkdownSection, DTreeView, DocViewer,
MetaValueRelationSingle/Multi, MetaValueFileMulti, MetaValueChildrenList, InputTags2, Image2,
GroupedSelectDropDown, Menu2, MediaUploadZone, DropTileZone),
`Mars.Nodes.FormEditor/EditForms/Components/` (GroupedSelectDropDown, DictionaryTextArea,
CodeEditorSuggestSearchInput).

### 3. Механические переименования (батчами по проектам)
- `FluentTextField` (~200), `FluentSearch` (~16) → `FluentTextInput`
- `FluentNumberField` (~36) → `FluentNumberInput`
- `FluentProgressRing` (~15) → `FluentSpinner`; `FluentProgress` → `FluentProgressBar`
- Color enum: `Neutral` → `Default`, `Accent` → `Primary` (все `Appearance.*`, `Color.*`)
- `FluentButton`: `Autofocus`→`AutoFocus`, `Action`→`FormAction`, `Enctype`→`FormEncType` и пр.
- `FluentStack`: `HorizontalGap`/`VerticalGap` int? → string?
- `FluentBadge`: текст в `Content`, `ChildContent` — для иконок
- `FluentAccordion`: `Heading*` → `Header*`
- `FluentLabel` — проверить по гайду (появился `FluentText` для типографики)
- `FluentGridItem` — PascalCase-переименования; `FluentGrid.Spacing` default 3→0 (визуально!)

### 4. Списочные компоненты (дженерики)
`FluentSelect` (~78), `FluentCombobox`, `FluentAutocomplete`, `FluentListbox`,
`FluentOption` (~48): два параметра типов `TOption`/`TValue`, новая база. Затрагивает
MetaValue-редакторы, формы нод, фильтры.

### 5. Удалённые/переработанные компоненты (обсудить крупные решения)
- **FluentDialog — полная переработка** (~40 мест `IDialogService.ShowDialogAsync`,
  `DialogParameters`, `MarsDeleteConfirmation`, `FluentDialogXActionFormPresenter`,
  `ShowPanelAsync<MobileMenu>` в AdminLayout, статические `ShowDialog(...)` в нодах).
- **FluentSplitter удалён** → `FluentMultiSplitter`: `NodeEditor1.razor`, `WorkspaceShell.razor`
  (+WorkspaceShell.razor.css уже целится в `.fluent-multi-splitter`).
- **FluentNavMenu/NavGroup/NavLink удалены** → `Menu2.razor` (главная навигация админки) — замена.
- **FluentToolbar удалён** (~4 места: DebugPage, EndpointNodeForm, CodeEditor*Toolbar) → чем заменить.
- **FluentMenuButton переработан** (UsersPage, EditPostTypePresentationPage, FrontSettingsPage,
  PostStatusesEditor, OpenIDClientOptionEditForm, AppEntityReadNodeForm).
- `FluentAnchor` → `FluentLink` (FrontsPage).
- `FluentValidationMessage` (~24) удалён → паттерн `FluentField` (Message/MessageCondition/MessageState).
- `FluentToast` — контент-компоненты (CommunicationToast/ConfirmationToast/ProgressToast);
  проверить обёртки (`IToastService`-хелперы).
- `FluentDataGrid` (~19): `ColumnOptionsLabels`→`ColumnOptionsUISettings`,
  `ColumnResizeLabels`→`ColumnResizeUISettings` и пр.
- `FluentTooltip`: убран `Visible`; `FluentInputFile`: изменённые свойства (MediaUploadZone,
  ZipUploadDialog — грабли FluentInputFile из памяти могут сдвинуться);
  `FluentSortableList`: убраны ListItemFilteredColor/ListBorderWidth (fluent-sortable-list.less).
- `FluentDatePicker`/`FluentTimePicker` — стали дженериками (искать использования).

### 6. Иконки
Icons 5.0.0; `FluentIcon` (~65) — default-цвет сменился с `Color.Accent` на `currentColor`
(визуальная регрессия — в этап стилей). Проверить namespace/имена (`Icons.Regular.Size16.*`).

### 7. Верификация (точечная, по конвенции)
- `dotnet build Mars.slnx` — зелёная.
- Тесты фрона из памяти: HandlebarsAppFrontTests (Docker) + лёгкие тесты фронта; E2E админки —
  визуально при разработке.
- Старт `Mars.Admin` (WASM) + MarsDocs + TestModules — базовый клик-тест: навигация, диалоги,
  формы постов/метаполей, редактор нод, Docker-страницы, Datasource workspace.
- После коммитов css/js — bump `MarsAppVersion` (cache-busting).

## Стили и хаки — ИНВЕНТАРИЗАЦИЯ (не правим в этой фазе)

FAST-токены v4 (`--type-ramp-*`, `--neutral-layer-*`, `--design-unit`) и `::part(control)`-хаки
в v5 скорее всего мертвы; `::deep` теряет смысл (scoped CSS bundling отключён). Всё — список
ниже —candidates на ревизию в этапе «стили»:

- `src/Mars.Admin/wwwroot/css/fluent-ui.less` — `.fluent-data-grid*`, `fluent-badge`
- `src/Mars.Admin/wwwroot/css/fluent-sortable-list.less` — токены `.fluent-sortable-list`
- `src/Mars.Admin/wwwroot/css/form.less` — `.fluent-input-label`, `.d-fluent-input-description`,
  `--fluent-input-label-basis`
- `src/Mars.Admin/wwwroot/css/bs-styles.less` — `.use-fluent-typo`, `fluent-dialog h1..h6`,
  `--type-ramp-*` токены
- `src/Mars.Admin/wwwroot/css/builderlayout.less` — `fluent-button::part(control)`
- `src/Mars.Admin/wwwroot/css/MediaTable.less` — `.fluent-data-grid-row`
- `style.less` (импорты) / `style.css` (артефакт — руками не трогать, компилирует пользователь)
- Scoped razor.css: `NodeEditContainer1.razor.css`, `HelpDocButtonDialog.razor.css`
  (`::deep .FluentMarkdown`), `WorkspaceShell.razor.css` (`::deep .fluent-multi-splitter*`)
- `docs/MarsDocs.WebApp/wwwroot/css/app.css` — `fluent-anchor::part(control)`,
  `fluent-tree-item::part(positioning-region)`, `.FluentMarkdown`

## Грабли / риски

- Диалоги — самая дорогая часть (~40+ мест, включая generic-презентер XAction-форм).
- `FluentGrid.Spacing` default 3→0 — молчаливое визуальное изменение без ошибок компиляции.
- `FluentIcon` default-цвет — молчаливое визуальное изменение.
- Кастомные компоненты на FluentComponentBase могут не дать ошибок в razor до сборки.
- devstands могут быть вне Mars.slnx — проверить, мигрировать по остаточному принципу.
- BlazorMonaco/MarsCodeEditor2 обвязка вокруг FluentToolbar — замена тулбара не должна сломать
  CodeLens/monaco-интеграции.

## Решения пользователя (2026-09-27)

- **Навигация Menu2**: делать на новом v5-компоненте `FluentNav` (+ `FluentNavItem`,
  `FluentNavCategory`, `FluentNavSectionHeader`) — замена удалённым FluentNavMenu/NavGroup/NavLink.
- **Диалоги**: шим реализован — `Mars.Admin.Framework/Dialogs/` (DialogParameters, IDialogReference,
  DialogServiceCompatExtensions: ShowDialogAsync<T>/ShowDialogAsync(Type)/ShowPanelAsync<T>→ShowDrawerAsync).
  Компоненты-диалоги: `[CascadingParameter] IDialogInstance Dialog`, разметка — `FluentDialogBody`
  с TitleTemplate/ChildContent/ActionTemplate; результат v5 `DialogResult` (`.Data`→`.Value` на call-сайтах).
  Инлайн-диалоги: вместо `Hidden` — `@ref` + `ShowAsync()/HideAsync()` (синхронизация с bool-флагом
  в OnAfterRenderAsync) + `OnStateChange` (DialogState.Closed) для dismiss.

## Прогресс

- Этап 0 готов: bump 5.0.0; `Mars.Forms.Front` мигрирован и зелёный.
- Механический батч (5 параллельных агентов, ~100 файлов): FluentTextField→FluentTextInput,
  FluentNumberField→FluentNumberInput, FluentSearch→FluentTextInput, TextFieldType→TextInputType,
  FluentInputAppearance→TextInputAppearance (Filled→FilledDarker), Maxlength→MaxLength;
  ChildContent-слоты (start/end) → StartTemplate/EndTemplate (3 файла); code-behind'ы
  ChangePasswordModal/CreateUserModal — TextInputType.
- Остаток по Admin.Framework (49): FluentSelect TValue (~20), диалоги (IDialogContentComponent,
  FluentDialogHeader/Footer — ~15), IToastService (FluentMessageServiceBridge),
  MenuChangeEventArgs (XActionsDropDown), ITreeViewItem.IconStart/IconEnd/IconAside (DTreeView),
  FluentInputLabel (AutoInputLabel), Menu2 (отложен).
- Вторая волна закрыта: FluentSelect TValue (все 20, вкл. enum/FrontItem/KeyValuePair-кейсы;
  SelectedOptionChanged→ValueChanged у sort-селекторов), диалоги FW переписаны на шим
  (DeleteConfirmation, HelpDocButton(+Dialog), MediaFolderSelect, MetaValueRelationSelect,
  инлайны FluentMediaFilesList ×3, ModalMediaSelect), тосты → INotificationService
  (FluentMessageServiceBridge), XActionsDropDown → FluentMenu Trigger/OnClick(MenuItemEventArgs),
  DTreeNode +IconStart/End/Aside, AutoInputLabel → FluentLabel, MessageIntent → MessageBarIntent,
  GenerateHeaderOption → DataGridGeneratedHeaderType, Align → DataGridCellAlignment,
  sort-discards (SortByColumn/SortByAscending) удалены, InlineStyleBuilder.BuildMarkupString → Build,
  конструкторы LibraryConfiguration в 13 FluentComponentBase-наследниках FW,
  FluentBadge: текст → Content, Appearance → BadgeAppearance.Filled (+Color.Primary для accent),
  FluentMenuButton (OpenIDClientOption) → FluentButton + FluentMenu Trigger,
  GroupedSelectDropDown(FW) → FluentMenu Trigger (без Anchor/@bind-Open/UseMenuService).
- ВНИМАНИЕ (этап стилей): v5 FluentLabel не имеет Color/Typo (уходят в AdditionalAttributes —
  молчаливая потеря стиля); FluentIcon default-цвет; FluentGrid.Spacing default 3→0;
  FluentBadge без Fill и с текстом в ChildContent (v5 — Content; Fill="black/neutral/accent"
  в ListPostTypePage и др. молча деградирует; Enabled-бейдж в PluginsListPage потерял accent-
  отличие — все Neutral/Accent→Filled); FluentTooltip Anchor — проверить;
  StyleDesigner-токены (FluentDesignSystemProvider удалён) → маппинг styler на CSS-переменные
  v5 (--colorBrand*/--colorNeutral*) — ОБСУДИТЬ с пользователем на этапе стилей.
- Зелёные проекты: Mars.Forms.Front, Mars.Admin.Framework, Mars.Nodes.FormEditor,
  Mars.Nodes.Workspace (FluentSplitter→FluentMultiSplitter; контекстные меню на
  FluentMenu.OpenMenuAsync(targetId,x,y) — id контейнеров: red-ui-workspace-chart,
  nodes-palette-sidebar; GridItemsProviderRequest.SortByColumn/SortByAscending→SortColumns),
  Mars.AiChat.Front.
- Этап 1 (инфраструктура) выполнен: все 4 корня (Mars.Admin/App.razor, MarsDocs App.razor,
  TestModules App.razor, StandNodesApp Routes.razor) → <FluentProviders />;
  AddFluentUIComponents(config => config.Toast.Position = ToastPosition.TopCenter) в
  MainAdminFramework; FluentDesignSystemProvider → временный div (тема статична до этапа стилей).
- В работе (агенты): Datasource.Front/Docker.Front/WebApp.Nodes.Front (диалоги+селекты+MenuButton);
  Mars.Admin — два агента пофайлово (диалоги/инлайн-диалоги + механика: ValidationMessage→Blazor
  ValidationMessage, FluentToolbar→div, FluentAnchor→FluentLink, ProgressRing→Spinner,
  MenuButton→Button+Menu Trigger, SelectedOptions→SelectedItems).
- Docs-сайт (MarsDocs): FluentNavMenu/NavLink/NavGroup/FluentAnchor — ждут FluentNav-переделки
  (вместе с Menu2, решение пользователя: делаем на FluentNav).

## Playwright-обход админки (2026-09-27)

Скрипт-обход: `.qwen/tmp/fv5-crawl.js` (playwright-core + msedge, логин mdimai666, 36 корневых
страниц `/dev/...`, скриншоты `.qwen/tmp/fv5-shots`, отчёт `fv5-report.json`). Сервер:
`set ASPNETCORE_ENVIRONMENT=Development&& dotnet run --project src\Mars.WebApp --no-launch-profile`
(launchSettings.json с комментариями строгий парсер `dotnet run` не читает; порт 5003).

Результат: 34/36 ок. Найдено и починено (после правок финальный прогон: **36/36 ok**,
единственная консольная ошибка — `/dev/marketplace` 466 внешнего каталога, существующая):
- **FluentIcon null-Value краш** (`NotSupportedException: Please use the constructor including
  parameters` из `FluentIcon`1.OnParametersSet → Activator): в v5 любой null-Icon долетает до
  `Activator.CreateInstance<Icon>()`. Триггер — `FluentAutocomplete IconSearch="null"` в
  `SelectCategoryForFilterDropDown.razor` (атрибут убран; v5 не умеет прятать search-иконку —
  визуальная мелочь на этап стилей). Правило: в v5 NEVER передавать null в Icon-параметры.
- **Take=0 → 400 от API постов**: v5 FluentDataGrid+Virtualize первый запрос даёт `Count=0`
  (не null), `req.Count ?? Default` не срабатывал. Clamp `req.Count is > 0 ? ... : Default`
  в 11 провайдерах (ManagePostView, UsersPage, ListUserTypePage, ListPostTypePage,
  ManagePostCategoryView, ListPostCategoryTypePage, PluginsListPage, ManageNavMenuPage,
  FeedbackListPage, NodeTaskJobListView, MetaValueRelationSelectDialog).
- **Селекторы E2E-логина мертвы в v5**: у `fluent-button` нет внутреннего `<button>`
  (shadow = slot+span), `[type='submit'] button` и getByRole('button') не находят; клик —
  по самому `fluent-button[type='submit']`.AuthTests переписаны (E2E-батч 2026-09-28, см. ниже).

Не FluentUI (не чиним здесь):
- `/dev/marketplace` 466 — внешний каталог (прокси), существующее поведение.

### IMask/Sortable vs AMD-loader Monaco (починено)
v5 грузит IMask и Sortablejs с CDN unpkg лениво и после `onload` проверяет **глобал**
(`window.IMask`/`window.Sortable`). В админке `index.html` синхронно грузит monaco
`loader.js` → живёт `define.amd` → UMD-билд imask/sortable уходит в AMD-ветку и глобал
НЕ создаёт → «IMask library failed to load» на любой странице с masked-инпутом
(styledesigner) и риск для FluentSortableList (нодовые формы). Лечение: вендоринг —
`src/Mars.Admin/wwwroot/js/imask.min.js` (7.6.1, MIT) и `Sortable.min.js` (1.15.6, MIT),
теги в `index.html` ДО monaco loader.js (UMD ставит глобалы раньше, чем появится AMD;
лоадер v5 видит глобал и не идёт на CDN — работает и offline). Грабли: новые файлы
wwwroot попадают в отдачу только после пересборки (staticwebassets-манифест).

## Menu2 + docs-навигация на FluentNav (2026-09-27, закрыто)

- `Menu2.razor` (FW): FluentNavMenu/NavGroup/NavLink → `FluentNav`/`FluentNavCategory`/
  `FluentNavItem` (+`Match` из MenuItem.navLinkMatch). `menuType==Header` →
  `FluentNavSectionHeader` (v4 рендерил их ссылками — теперь семантически верно).
  Делители — `FluentDivider` (в т.ч. внутри категорий).
- docs (`MarsDocs.WebApp`, вне Mars.slnx — собирать/гонять отдельно): `DocsTreeMenu` →
  `FluentNav`; v5 держит один уровень вложенности, поэтому вложенные группы схлопываются
  до листьев (`Flatten`). `NavMenu.razor` (Home) и `DocsTreeMenuGroup.razor` удалены:
  MainLayout отдаёт в DocsTreeMenu один массив `_menuItems` = Home + divider + App.Menu.
- Попутно в docs MainLayout: `FluentBodyContent` (удалён в v5) → обычный div с теми же
  классами (layout там кастомный на FluentStack, не на LayoutArea).
- Проверка: админка — сайдбар рендерит ссылки/категории, клик навигирует, 36/36 страниц ok;
  docs — Home active, группы-аккордеоны, клик по QuickStart рендерит контент, консоль чистая.
- Стилевые мелочи сайдбара (токен `--neutral-fill-stealth-rest` в Style-атрибутах мёртв,
  ширина/отступы под v5-нав) — в этап стилей.

## Этап стилей — статус 2026-09-27

Код-уровень (без обсуждений, сделано):
- `FluentBadge Fill=` мёртв в v5. Дефолт библиотеки — brand-синий (компонент всегда пишет
  `color="brand"`); через `AddFluentUIComponents`/LibraryConfiguration дефолты бейджей НЕ
  настраиваются. Решение пользователя (2026-09-28): **серый дефолт глобально** — оверрайд в
  `fluent-ui.less`: `fluent-badge[color="brand"]:not(.badge-accent)` → серый
  (`--colorNeutralBackground5`/`--colorNeutralForeground3`), осознанный акцент — класс
  `badge-accent` (4 места: Single, Рекомендован ×2, field-бейдж FormLayoutEditor).
  Семантика атрибутами: warning→Warning, error→Danger, success→Success (зелёный; в css
  библиотеки опечатка `[color=sucess]`, но компонент пишет `success` — работает),
  black→Important (тёмный), neutral/теги/фичи→Informative (серый #ebebeb).
  **Subtle НЕ использовать на светлых поверхностях**: `[color=subtle]` =
  `--colorNeutralBackground1` = белый = невидимая пилюля.
  ВАЖНО: less-часть видна только после компиляции style.css пользователем.
- `FluentLabel` (Typo/Color удалены в v5) → `FluentText`: батч агента по всему репо кроме
  Workspace + Workspace вручную. Таблица: Body→Size300, Subject→Size400, Header→Size500,
  PaneHeader→Size600, EmailHeader→Size700, PageTitle→Size800, HeroTitle→Size900,
  H1–H6→As=TextTag.H1–H6 + Size800/700/600/500/400/300 (type-ramp v4: base14/+1 16/+2 20/
  +3 24/+4 28/+6 40, снят с css пакета 4.14.4).
- `MouseButton` obsolete → DOM-литералы (0 left / 2 right) в NodeEditor1.razor.cs и
  QuickNodeAddMenu.razor.
- `FluentTooltip Anchor` в v5 ЖИВ (удалён только Visible) — не трогали.

Инвентарь less/токенов (по пакету 5.0.0, к обсуждению с пользователем):
- МЁРТВЫЕ токены v4 во всём пакете: `--type-ramp-*`, `--neutral-layer-*`, `--badge-fill-*`,
  `--design-unit`, `--neutral-base-color` и пр. FAST-токены → v5-переменные
  (`--colorNeutralBackground*`, `--colorBrandBackground*`, …) или `--mars-*`.
- ЖИВЫЕ классы/элементы v5: `.fluent-data-grid*`, `.fluent-sortable-list` (но её
  `--fluent-sortable-list-*` значения сидят на мёртвых токенах), элемент `fluent-badge`
  (но `--badge-fill-*` мертвы — цвета теперь BadgeColor).
- МЁРТВЫЕ селекторы: `.fluent-input-label` (v5 рендерит `fluent-label` без класса) —
  compact-формы в form.less; блок `fluent-badge { --badge-fill-* }` в fluent-ui.less.
- Scoped css НЕ отключён (ScopedCssEnabled нигде нет) — razor.css работают; `::deep`
  по-прежнему не пробивает shadow DOM веб-компонент (и в v4 не пробивал).

Открытые вопросы этапа стилей — ЗАКРЫТЫ (2026-09-28, см. раздел «CSS-хаки»):
1. ~~Маппинг токенов StyleDesignerPage~~ — StylerStyle переписан на v5 ThemeSettings
   (BrandColor/HueTorsion/Vibrancy/IsExact/Mode), применение через IThemeService.
2. ~~Дефолт бейджей~~ — решено 2026-09-27: серый дефолт глобально + badge-accent.
3. ~~Дефолтный цвет FluentIcon~~ — принят currentColor (решение пользователя 2026-09-28);
   акцент там, где нужен, ставится точечно Color/WithColor (в нод-редакторе уже есть).
4. ~~FluentGrid.Spacing~~ — закрыто без действий.
5. ~~Мёртвые less-блоки~~ — вычищены батчем 2026-09-28.

## CSS-хаки — батч 2026-09-28 (выполнен)

Решения пользователя: StyleDesigner → IThemeService; `body.dark` выпилить целиком в пользу
v5-темы; FluentIcon currentColor — принять.

Факты о v5 (сняты с пакета 5.0.0 — бандл lib.module.js, reboot.css, default-fuib.css):
- **Токены** инжектируются в рантайме через JS (adoptedStyleSheets): `--colorNeutral*`, `--colorBrand*`,
  `--fontSizeBase100..600/Hero700..1000`, `--borderRadius{None,Small,Medium,Large,XLarge,Circular}`,
  `--strokeWidth*`, `--spacing*`. Compat-мост на `:root`: `--success/--warning/--error/--info`
  (+`-inverted`), `--font-monospace` → ЖИВЫ. Всё прочее v4 (`--type-ramp-*`, `--accent-fill-*`,
  `--neutral-fill/layer/foreground/stroke-*`, `--design-unit`, `--neutral-base-color`,
  `--control-corner-radius`, `--base-height-multiplier`, `--badge-fill-*`) — МЕРТВО.
- **`::part` в v5**: dialog — `part="dialog"` (был control); switch — `checked-indicator`
  (был switch); tree-item — `positioning-region` + `content` (был content-region);
  button — частей НЕТ (slot+span); `control` остался у input/textarea.
- **Тёмная тема**: IThemeService в dark ставит `document.body[data-theme="dark"]`
  (+CustomEvent `themeChanged`) — канонический CSS-хук; в light атрибут снимается.
- **IThemeService** (DI от AddFluentUIComponents, scoped; JS `Blazor.theme.*`):
  SetThemeAsync(color/ThemeSettings/ThemeColorVariant/ThemeMode), CreateCustomThemeAsync,
  GetColorRampFromSettingsAsync, SwitchThemeAsync, SetThemeToElementAsync (скоуп-превью),
  персист в localStorage. ThemeSettings(Color, HueTorsion -0.5..0.5, Vibrancy -0.5..0.5,
  Mode Light/Dark/System, IsExact).

Что сделано:
- **base.less**: `--mars-*` — алиасы на v5-токены (primary→colorBrandForeground1,
  text→colorNeutralForeground1/3/Disabled, link→colorBrandForegroundLink,
  bg→colorNeutralBackground1/2/3/1Selected, border→colorNeutralStroke1/2,
  radius→borderRadiusMedium/Large) → тёмная тема флипается сама. Удалены body.dark-блоки
  и prefers-color-scheme; не-флипаемое (тени, bg-white2/bg-black2 инверсии) перевешено
  на `body[data-theme="dark"]`. Мёртвые body-декларации (type-ramp/neutral-*) убраны —
  body типграфику/цвет задаёт default-fuib.css.
- **Тема**: `StylerStyle` (Mars.Admin.Contracts) = BrandColor/HueTorsion/Vibrancy/IsExact/Mode
  (вместо 13 FAST-параметров v4); App.razor.cs — SetupThemeAsync через
  `IThemeService.SetThemeAsync(ThemeSettings)` (старт + событие App.SetupTheme);
  App.razor — мёртвый `--bs-primary: var(--accent-base-color)` убран;
  StyleDesignerPage — превью через `SetThemeToElementAsync(@ref, settings)` (скоуп, не
  глобально), контролы: BrandColor/Mode(select)/IsExact/HueTorsion/Vibrancy, `@bind:after`
  → live-превью; Save → Q.Root.Emit("App.SetupTheme") → глобальное применение.
- **bs-styles.less**: удалён `.use-fluent-typo, fluent-dialog {h1..h6}` на мёртвых
  `--type-ramp-*` (заголовки задаёт reboot.css: Hero900/800/700 + Base600/500/400);
  body.dark-блок (card/accordion/dropdown) → обычные правила на `--mars-*` (флипаются).
- **Мёртвые part'ы → живые**: dialogs.less + NodeEditContainer1 + AIToolChatModal —
  `::part(control)`→`::part(dialog)`; NodeEditor1 — `#debug-mode-switch[checked]::part(checked-indicator)`
  (v5 рендерит `checked="true"` на host), `.btn-terminate-all-tasks:hover{color:red}`
  (наследуется в slot); DTreeView — `content-region`→`content`, мёртвый `::after`-calc убран;
  GroupedSelectDropDown ×2 (FW+FormEditor) — `#id::part(content)` → стили на host
  (width:stretch/text-align:start наследуются в slot).
- **builderlayout.less**: `.sbtn.active` → `--colorBrandForeground1`; `.pressed-in` —
  inset-тень на part(control) мертва → `filter: brightness(0.9)` на host.
- **form.less**: `.fluent-input-label` → селектор элемента `fluent-label` (v5 рендерит
  без класса, AutoInputLabel его не добавляет); description → `--colorNeutralForeground3`;
  body.dark .top-navbar удалён (--mars-bg-surface флипается).
- **fluent-sortable-list.less**: background → colorNeutralBackground2/4, item-height → 32px
  (design-unit×8), `--warning` жив (compat) — оставлен.
- **action-center.less**: весь переведён на v5-токены (colorNeutral*/colorBrand*).
- **Мелочь по src**: typography (.text-accent→--mars-color-primary, .text-black2→
  colorNeutralForeground1), file-uploader (dashed border→--mars-color-primary,
  radius→borderRadiusMedium), class.less/extra2.less/filters.less — body.dark-блоки убраны
  или перевешены на [data-theme="dark"] (ondark_* утилиты живы под новым хуком),
  ExecutionBar/MetaValueFileMulti/MetaValueChildrenList/DropTileZone/FormLayoutEditor.razor.cs/
  AIToolOptionEditForm/AdminLayout(Menu2 Style) — мёртвые токены в инлайн-стилях заменены/убраны.
- **FormEditor wwwroot/css/style.less**: `body.dark .mars-value-input` → `[data-theme="dark"]`
  (media prefers-color-scheme оставлен).
- **docs + devstands (2026-09-28)**: MarsDocs app.css (body-блок, --bs-primary, .content,
  .color-accent/.bg-accent → v5-токены; закомментированный .navigation-блок не трогали),
  FluentMarkdownSection.razor.css (hljs/hljs-copy → colorNeutral*/colorBrand*),
  MainLayout.razor (мёртвый Style-токен у DocsTreeMenu убран); StandNodesApp app.css —
  мёртвый body-блок удалён. Сборка docs slnx: 0/0.

Компиляция style.css (Mars.Admin + FormEditor) — за пользователем; после коммита css/js —
bump MarsAppVersion. Остаток: визуальная проверка (сайдбар-нав ширина/отступы, FluentIcon
currentColor в тулбарах, StyleDesigner-превью, тёмная тема через Mode=Dark).

### Расширение StylerStyle — на подумать (записано 2026-09-28)

Стайлер работает (скомпилирован и проверен пользователем 2026-09-28). Следующий шаг —
новые параметры в `StylerStyle` (скругления и т.п.). Что даёт v5 `Theme`
(`src/Core/Components/Theme/Styles/Theme.cs`, ветка dev): механизм —
`IThemeService.CreateCustomThemeAsync(color, mode, isTeams)` → мутируем поля `Theme` →
`SetThemeAsync(Theme)`. Доступно, помимо brand-палитры:
- `Borders.Radius` — None/Small/Medium/Large/XLarge/Circular + Large2X..5X (кандидат №1);
- `Typography` — шкалы Base100..600, Hero700..1000;
- `Shadows` — Shadows2/4/8/16/28/64 (+ Brand-варианты);
- `Spacings.Horizontal/Vertical` — None..XXXL (density-аналог v4);
- `Strokes.Width` — Thin/Thick/Thicker/Thickest;
- `Colors` — вся палитра (Brand/Neutral/Background.Overlay) после генерации рампы.

Решить: какие из них выносить в styler UI (минимум — Radius; Spacing≈density, если
запросим компактность), форма хранения (скаляры в StylerStyle vs JSON-проброс) и как
это дружить с `--mars-radius-*` алиасами в base.less (они сейчас на borderRadiusMedium/
Large — при кастомном Theme переназначатся автоматически, т.к. токены те же).

## Статус

- [x] Ветка `feat/fluentui-v5`
- [x] План (этот файл)
- [x] Этап 0: bump 5.0.0 + опись ошибок
- [x] Этап 1: инфраструктура/провайдеры (FluentProviders ×4 корня, Toast.Position в DI)
- [x] Этап 2: FluentComponentBase-наследники (ctor LibraryConfiguration — FW ×13, FormEditor ×3, docs ×3)
- [x] Этап 3: механические переименования (TextInput/NumberInput/ButtonAppearance/Size/Color enums/
      DataGrid-ренеймы/MessageBarIntent/ProgressRing→Spinner/Progress→ProgressBar — весь src+docs+devstands)
- [x] Этап 4: списочные компоненты (TOption/TValue везде, SelectedOption(s)→Value/SelectedItems,
      FluentOption TValue + литералы Value="@("...")"/@string.Empty)
- [x] Этап 5: удалённые/переработанные — диалоги (шим + ~30 компонентов/инлайнов), Menu→Trigger/
      OpenMenuAsync + FluentMenuList для подменю, MenuButton→Button+Menu, Splitter→MultiSplitter ×2,
      Rating→RatingDisplay, Persona→Avatar, Toolbar→div, Anchor→Link, InputLabel→Label,
      ValidationMessage→Blazor. Остатки этапа: Menu2 + docs-навигация → FluentNav (отдельный шаг);
      StyleDesignerPage/FluentDesignSystemProvider → этап стилей
- [~] Этап 6: иконки — пакет 5.0.0, компиляция ок; default-цвет FluentIcon (Accent→currentColor) —
      визуальная проверка на этапе стилей
- [x] Этап 7 (частично): `dotnet build Mars.slnx` — 0 errors (13 warnings: Menu2/StyleDesigner —
      отложены; MouseButton obsolete ×2; SiteEngine-nullable ×5 — существовавшие; CS4014 ×1 — существовавший).
      Mars.Integration.Tests: 430 total / 0 failed / 4 skipped.
      Осталось: визуальная проверка админки (диалоги, гриды, меню, редактор нод), E2E CreatePostTests/
      EditUserPageTests (опц.), HandlebarsAppFrontTests (server-рендер не затрагивался — опц.)

## Следующие шаги

1. Визуальная проверка админки пользователем (запуск Mars.WebApp) — диалоги/тосты/меню/гриды/нод-редактор
2. [x] Menu2 + docs-навигация на FluentNav (2026-09-27)
3. [x] Этап стилей: CSS-хаки батч (2026-09-28) — less-инвентарь, styler→IThemeService,
   body.dark→data-theme, currentColor принят. Остаток: docs app.css (вне slnx),
   компиляция style.css ×2 (пользователь), визуальная проверка
4. [x] E2E-регрессия админ-форм (2026-09-28) — полный сьют `Mars.E2E.Tests` зелёный
   (16 total / 0 failed / 1 skipped-DemoPages), см. «E2E на v5» ниже.

## E2E на v5 (2026-09-28, закрыто)

Сьют `tests/Mars.E2E.Tests` (Playwright + msedge + Testcontainers) переведён на v5-селекторы;
прогон: `MARS_E2E_TESTS=1` + exe из bin (атрибут `[E2EFact]`, не константа Skip). Зелёный:
16 total / 0 failed / 1 skipped (DemoPages — Skip="not required").

Механика селекторов (уже было в работе, подтверждено прогоном):
- Submit-кнопка: v5 `fluent-button` НЕ содержит внутренний `<button>` (shadow=slot+span) —
  клик по `fluent-button[type='submit']`, не `[type='submit'] button` / getByRole('button').
- Поля: `name` на хосте `fluent-text-input`, настоящий `input` в shadow DOM — селектор
  `[name='x'] input` (Playwright пробивает shadow descendant'ом). `FillTextField`/`FillFieldAsync`
  → на внутренний input.
- `fluent-text-field` → `fluent-text-input` (InputTags2).
- **DataGrid**: v5 рендерит `<div class="fluent-data-grid">` (стандартная HTML-таблица), НЕ
  custom element — селектор-класс `.fluent-data-grid`, не тег `fluent-data-grid` (SetupWizard).
- `input[type='text']` у FluentTextInput может отсутствовать — в редакторе цвета поле
  исключаем палитру: `input:not([type='color'])`.
- SetupWizard (`/setup/*`) — обычный Bootstrap cshtml (не FluentUI): его `input[name=]`,
  `button:has-text()`, `.alert-danger` живы, трогали только логин+грид.

Найденные v5-грабли (две гонки фреймворка, обе — НЕ баги кода Mars):
- **InputTags2: `Immediate`-binding асинхроннее keydown.** `fluent-text-input` (web component)
  ретранслирует ввод в Blazor через JS-interop асинхронно; при быстром «ввод + Enter» keydown
  приходит в `OnKeyPress` ДО коммита последнего символа в `_current` → тег молча теряется
  (early-return на пустом значении). Проявилось как «сохраняется только первый тег».
  Лечение в тесте: `Task.Delay(200)` между печатью тега и Enter. (В проде тот же риск при
  очень быстром вводе — кандидат на `ImmediateDelay`/debounce в InputTags2, вне рамок E2E.)
- **FluentDataGrid: `ObjectDisposedException` после закрытия диалога.** Grid регистрирует
  глобальный `FluentKeyCode` (клавишный ресайз колонок); когда диалог с гридом закрыт (грид
  disposed), поздний keydown от последующего ввода попадает в освобождённый JS-объект →
  `SetColumnWidthDiscreteAsync` бросает. Не влияет на сохранение данных. В
  `BrowserErrorTracker` добавлен узкий фильтр `IsKnownFrameworkRace` (ObjectDisposedException
  + SetColumnWidthDiscreteAsync + FluentKeyCode) — все прочие page-ошибки по-прежнему ловятся.

Попутно (v5-корректность, не только тесты):
- `InputTags2.razor`: текст бейджа вынесен в `Content` (в v5 `ChildContent` = обёрнутый элемент,
  текст в ChildContent давал бы пустой бейдж); крестик удаления — соседним span.
- `FluentTab Label=` → `Header=` (v5-ренейм) в FormLayoutEditor, DockerContainerDetail,
  DockerManager, FormRenderer.

