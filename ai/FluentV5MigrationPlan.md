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

- **Диалоги**: делаем **шим поверх v5** в Mars.Admin.Framework — compat-слой с v4-подобным API
  (ShowDialogAsync<T>(content, DialogParameters-подобные опции), MarsDeleteConfirmation,
  IXActionFormPresenter) поверх v5-примитивов (FluentDialogInstance, DialogOptions, ShowAsync/HideAsync).
  Call-сайты (~40) почти не трогаем; компоненты-диалоги переписываем на FluentDialogInstance.
  Позже — постепенный переход на нативный API.
- **Навигация (Menu2/FluentNavMenu)**: решаем позже, отдельным обсуждением (варианты: своя
  разметка на FluentLink vs FluentTreeView). До решения — предупреждения RZ10012 (тихий render
  в HTML) допустимы, но в релизную ветку так нельзя.

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

## Статус

- [x] Ветка `feat/fluentui-v5`
- [x] План (этот файл)
- [ ] Этап 0: bump + опись ошибок
- [ ] Этап 1: инфраструктура/провайдеры
- [ ] Этап 2: FluentComponentBase-наследники
- [ ] Этап 3: механические переименования
- [ ] Этап 4: списочные компоненты
- [ ] Этап 5: удалённые/переработанные (диалоги — обсудить подход)
- [ ] Этап 6: иконки
- [ ] Этап 7: верификация
