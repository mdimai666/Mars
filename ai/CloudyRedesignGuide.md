# CloudyRedesignGuide — редизайн админки «cloudy»

Гайд для агента по редизайну админ-зоны Mars (ветка `feat/redesign-cloudy-variant`, старт 2026-10-03).
Источник истины по UI — Vue-прототип `C:\Users\D\Documents\VisualStudio\2026\mars-redesign-project`
(`src/assets/main.css` — ВСЯ палитра: `:root` = светлая зона + тёмный tech, `.tech.light` = светлый tech;
`src/components/*.vue` — разметка страниц; `admin_*.png` — референс-скриншоты).
Смежные гайды: `ai/FluentUiGuide.md` (компоненты v5), `ai/CssRefactoringGuide.md` (слои токенов,
`--mars-*`), `ai/TestingGuide.md`. Пользователь: без пиксель-перфекта, «общая схема».

## Раскладка кода

- Лэйауты: `src/Mars.Admin/Shared/Cloudy/`
  - `CloudyLayout.razor` — DefaultLayout всей админки (`App.razor`); светлая зона, классы `.cloudy-*`.
    `AdminLayout` оставлен (rollback / явный `@layout`).
  - `CloudyTechLayout.razor` — тёмная «тех»-зона `/tech/*` (topbar + рельс); `CloudyTechTopBar`,
    `CloudyTechSideBar`.
  - Паттерн-компоненты списочных страниц (уровень 2): `CloudyPageHead`, `CloudyKpi`, `CloudyBrowser`
    (параметр `Scrollable` — flex-fill тело с внутренним скроллом для DataGrid-страниц),
    `CloudyChips`, `CloudySearchBox` (встроенный дебаунс 300мс), `CloudyPager`, `CloudyAvatar`,
    `CloudyTag` — API однострочное, композиции без «конструктора страниц».
  - `CloudyGridProvider.cs` — фабрика `GridItemsProvider<T>` для FluentDataGrid: маппит
    skip/take/sort запроса на серверный List-вызов (`ListDataResult<T>`), дефолт take=50.
  - Модалки: штатный `FluentDialog` через `IDialogService` (нативный v5 API); своего шелла
    НЕТ — cloudy даёт только компонент заголовка `CloudyDialogHead` (вставляется в
    `FluentDialogBody.TitleTemplate`) и css-скин.
- Стили: `src/Mars.Admin/wwwroot/css/`
  - `cloudy.less` — светлая зона: токены `--cld-*` на `.cloudy-layout` + тёмный блок
    `body[data-theme="dark"] .cloudy-layout`; топбар/сайдбар/hero/kpi/acrylic/chip/badge/user-menu.
  - `cloudy-tech.less` — tech-зона: токены `--ct-*` на `.cloudy-tech` (база тёмная) + светлый блок
    `body:not([data-theme="dark"]) .cloudy-tech` (значения `.tech.light` прототипа); ремап
    `--cld-menu-*` → `--ct-*` для UserBar-меню.
  - `cloudy-list.less` — общие паттерны списочных страниц: `.cloudy-browser` (+ `--scroll`
    модификатор и `__body`), `.cloudy-list` (колонки через `--cloudy-list-cols` страницы),
    `table.cloudy-grid.fluent-data-grid` (скин DataGrid) + `.cloudy-grid-member`,
    `.cloudy-tint-0..4`/`.cloudy-dot-0..4`, `.cloudy-avatar`, `.cloudy-tag(s)`, `.cloudy-stack`,
    `.cloudy-time`, `.cloudy-row-actions`, `.cloudy-empty`, `.cloudy-chips`, `.cloudy-searchbox`,
    `.cloudy-pager`, `u-hide-1100/900`.
  - `cloudy-dialog.less` — скин штатного диалога: `fluent-dialog::part(dialog)` (панель/рамка/
    тень/`max-width`, shadow-диалог жёстко ≤600px), паддинги `fluent-dialog-body::part(title/
    content/actions)`, `.cloudy-dialog-head(__title)`; токены `--cld-*`. Действует на ВСЕ
    FluentDialog-и админки (сервис рендерит их в общем провайдере).
  - Импорт всех четырёх — в конце `style.less`; компиляция только через `tools/ui/build-css.ps1 -Entry admin`
    (см. `ai/CssRefactoringGuide.md`; руками `style.css` не править).
- Страницы:
  - Шаг 1 (in-place на cloudy): `Pages/Index.razor` (Home), `Pages/PostsViews/ManagePostPage`,
    `Pages/PostCategoryViews/ManagePostCategoryPage`, `Pages/UserViews/UsersPage` (полностью
    перестроена по прототипу), `Pages/Settings/SettingsPageWrapper` (даёт cloudy-подложку всем
    подразделам настроек). Моки: `Pages/DashboardPage`, `Pages/LegionPage` (+ scoped `.razor.css`).
  - Шаг 2 (tech-зона): `Pages/TechViews/` — моки Automation/Database/Frontend/Marketplace/Plugins +
    реальная `TechLogsPage` (`/dev/tech/logs`, данные `client.AppDebug.GetLogs`); `_Imports.razor`
    задаёт `@layout CloudyTechLayout`. Реальный Builder и nodered НЕ тронуты.
  - `Shared/UserBar.razor` — аватар + bootstrap-dropdown `.cloudy-user-menu`; пункт переключения темы
    (`DevAdminStyleOption.StylerStyle.Mode` → `Q.Root.Emit("App.SetupTheme")` → `App.razor.cs`
    `SetupThemeAsync` → `IThemeService` → `body[data-theme]`; хелпер `marsIsDarkTheme` в
    `wwwroot/js/scripts.js`).

## Темизация (решение 2026-10-06)

- ОДИН глобальный переключатель (UserBar); обе зоны либо светлые, либо тёмные.
- Хук темы — `body[data-theme="dark"]` (ставит IThemeService v5); Fluent-компоненты флипаются сами,
  cloudy-зоны — через переопределения токенов в less (см. выше).
- Тёмная палитра cloudy-зоны ВЫДУМАНА на языке tech-палитры прототипа (в прототипе админ-зона
  light-only): surface/card `#141824`, line `#1d2637`, ink `#e8eaf6`, primary `#8375fa`, небо
  `--tech-sky-*`. Светлая tech-зона — один-в-один `.tech.light`.
- Лэйауты публикуют токены с прототипными именами для страниц: tech — `--tint-*`, `--tk-*`,
  `--tech-canvas/grid-line/accent-deep` + константы `--white/--field/--mist-*/--mac-*/--gold/--warn/
  --err/--violet/--blue-deep/--ink-slate/--line-pale/--line-strong` (светлые мокапы-превью остаются
  светлыми в обеих темах); cloudy — доп. токены `--muted-out/--muted-dim/--line-*/--blue(-soft)/
  --purple/--teal/--green-deep/--red(-live)/--amber-live/--violet-dot` (Dashboard/Legion/Users).

## Рецепты

**Списочная страница на `.cloudy-list` (статичный список без грида; UsersPage до 2026-10-06):**
1. `CloudyPageHead` (Title/Subtitle/KpiCols) + `CloudyKpi` (Icon или Tint-точка). Карточка KPI —
   фиксированной ширины `--cloudy-kpi-width: 168px` (= (720−3×16)/4 из прототипа), колонки
   `--cloudy-kpi-cols` (дефолт 4); НЕ растягивать карточки на контейнер (1fr) — при cols<4 разъезжаются.
2. `CloudyBrowser`: `HeadLeft` = `CloudyChips`, `HeadRight` = `CloudySearchBox` + `.cloudy-btn-primary`;
   `<ChildContent>` ОБЯЗАТЕЛЬНО явный (RZ9996); `Foot` = `TotalResultsFound` + `CloudyPager`.
3. В теле — `.cloudy-list` + свой класс страницы (`<div class="cloudy-list users-list">`);
   `--cloudy-list-cols` задаётся В SCOPED-CSS НА ЭТОМ ЭЛЕМЕНТЕ (`.users-list { --cloudy-list-cols: … }`
   + media-варианты) — не на `CloudyBrowser`/предке: см. граблю про custom properties. Ячейки:
   `CloudyAvatar`, `.cloudy-stack` (title/sub), `.cloudy-tags`+`CloudyTag`, `.cloudy-time`,
   `.cloudy-row-actions`; empty — `.cloudy-empty`; скрываемые колонки — `u-hide-1100/900` (и в head,
   и в row).
4. Данные: `client.X.ListDetail(new(){ Skip, Take, Sort, Search, Roles/CreatedFrom })`; счётчики KPI —
   через KPI-эндпоинт (см. рецепт ниже; старый способ `Take=1` → `TotalCount` не использовать).

**Страница с FluentDataGrid (бесконечная подгрузка, как UsersPage 2026-10-06):**
1. `CloudyBrowser Scrollable` — тело становится flex-fill скролл-контейнером
   (`height: calc(100vh - var(--cloudy-browser-inset, 520px))`); при необходимости страница
   уточняет inset в scoped-css через `Class` (корневой элемент панели inherits scope-атрибут).
2. `FluentDataGrid`: `ItemsProvider` из `CloudyGridProvider.Create(loader)` (loader —
   `(skip, take, sort) → client.X.ListDetail/List`), `Virtualize` + `ItemSize` (≈ высота строки:
   2×13px padding + контент), `GridTemplateColumns` — схема колонок СТРОКОЙ в razor (не CSS;
   дефолтный `DisplayMode=Grid` даёт fr/minmax — см. FluentV5Reference №22 про Virtualize+Grid),
   `GenerateHeader=Sticky`,
   `Class="cloudy-grid"` (скин в cloudy-list.less). Фут — только `TotalResultsFound`
   (total обновляет loader провайдера + `StateHasChanged`); `CloudyPager` не используется.
3. Колонки — `TemplateColumn` с паттерн-ячейками: Member = `.cloudy-grid-member`
   (CloudyAvatar + `.cloudy-stack`), теги = `.cloudy-tags`, дата = `.cloudy-time`,
   действия = `.cloudy-row-actions` (`Align="DataGridCellAlignment.End"`).
   Сортировка — только маппед-поля (`GridSort<T>.ByAscending(p => p.LastName)`);
   дефолтная сортировка таблиц (конвенция 2026-10-06) — **CreatedAt DESC**:
   `IsDefaultSortColumn` + `ByDescending(p => p.CreatedAt)` на колонке даты, фолбэк
   loader'а — `nameof(T.CreatedAt)`; стикер аватара — детерминированный тинт из id
   (`(id.GetHashCode() & 0x7FFFFFFF) % 5`).
4. Фильтры/поиск/удаление/create → `_grid.RefreshDataAsync()` (не пересоздание провайдера).
5. Колонки и `EmptyContent`/`LoadingContent` — ОБЯЗАТЕЛЬНО явный `<ChildContent>` для колонок
   (RZ9996, та же грабля что у CloudyBrowser). Responsive-скрытие колонок (`u-hide-*`) на
   DataGrid НЕ перенесено — при необходимости менять `GridTemplateColumns` + nth-child по брейкпоинтам.

**Модальное окно (FluentDialog через сервис, 2026-10-07):** своих модалок НЕ делаем — штатный
сервис-диалог. Компонент диалога = только тело: корень `FluentDialogBody` (`TitleTemplate` →
`<CloudyDialogHead Title="…"/>`, контент в ЯВНОМ `<ChildContent>` — RZ9996), данные —
`[Parameter] Content` (класть в `DialogOptions.Parameters["Content"]`), `[CascadingParameter]
IDialogInstance Dialog`. Показ из страницы (нативный v5 API, шим удалён 2026-10-07):
`var result = await _dialogService.ShowDialogAsync<TDialog>(new DialogOptions { Modal = true,
Header = { CloseAction = { Visible = true } }, Parameters = { ["Content"] = content } })` —
сразу `DialogResult` (`Cancelled`/`Value`). Сохранение закрывает: `Dialog.CloseAsync(model)`;
крестик/ESC — встроенные (`Header.CloseAction.Visible`; у v5 НЕТ `PreventDismissOnOverlayClick`/
`PreventScroll`/`TrapFocus` — modal и так не закрывается кликом по оверлею).
Кнопку «Cancel» в тело не добавлять — `StandardEditForm1` сам рендерит футер (Save/Delete).
Инлайн-`FluentDialog` с императивной синхронизацией (`@ref`+`ShowAsync/HideAsync`, `_dialogShown`,
`OnStateChange`) — прошлый хак, НЕ повторять. Скины панелей/бэкдропа — НЕ css-классами диалога, а
`cloudy-dialog.less` (грабля: диалоги рендерятся вне `.cloudy-layout` — токены `--cld-*` дублированы
на хосте `fluent-dialog/fluent-drawer`, синхронизировать с cloudy.less). Мигрированы:
`CreateUserDialog`, `ChangePasswordDialog` (вызов из UsersPage), `ViewFeedbackDialog`
(FeedbackListPage, read-only тело на FluentStack). Tech-зона: `log-modal` в TechLogsPage
остаётся своей разметкой (`--ct-*` токены).

**KPI-карточки (паттерн 2026-10-06/07):** серверные метрики — НЕ `ListDetail(Take=1)`-хаками, а через
реестр `IKpiHandler` (`Mars.Contracts/Common/IKpiHandler.cs`, `KpiResult(Key, Value, Label?)`) +
агрегатор `GET api/Kpi?keys=a,b` (`Mars.Server/Controllers/KpiController.cs`: Admin-only,
неизвестные ключи игнорирует, `keys=a,b` и `keys=a&keys=b` равноценны). Контроллер — ЧИСТЫЙ
агрегатор БЕЗ кэша: кэширование и инвалидация — ответственность хендлера (иначе TTL контроллера
перебивает событийную инвалидацию — баг 2026-10-06: значения обновлялись «через минуту», не по событию).
- Хендлер живёт в модуле-владельце метрики (пилот: `Mars.Identity.Host/Kpi/Users*KpiHandler` —
  `IMemoryCache` TTL 10мин + инвалидация по `entity/user/add|delete` через `IEventManager`;
  ключи-константы `Mars.Identity.Contracts/Users/UserKpiKeys.cs`). Второй пилот:
  `Mars.Cms.Host/Kpi/Feedbacks*KpiHandler` (инвалидация по `FeedbackAdd/FeedbackDelete`;
  ключи `Mars.Cms.Contracts/Feedbacks/FeedbackKpiKeys.cs`).
- Хендлер — SINGLETON, слушатели событий подписываются В КОНСТРУКТОРЕ (конструируется один раз
  при первом резолве `IEnumerable<IKpiHandler>` контроллером; ленивая подписка до первого
  `/api/Kpi` безвредна). Регистрация — обычная `AddSingleton<IKpiHandler, MyHandler>()`.
  `IMarsAppLifetimeService` для этого НЕ использовать: его `GetOrderedList` подбирает только
  Singleton-регистрации с явным `ImplementationType` и резолвит их по `ServiceType` — несколько
  регистраций под одним интерфейсом ломают вызов `OnStartupAsync` (баг 2026-10-06: слушатели
  не подписывались, значения обновлялись только по TTL). `TriggerEvent` — синхронный, инлайн.
- Фронт: один вызов `_client.Kpi.Get([keys])` на страницу → словарь; `Label` с сервера — стабильный
  ключ, переводится `IStringLocalizer<AppRes>` (записи в `Mars.Contracts/Resources/AppRes*.resx`,
  имена = ключи, напр. `users.total`).
- Фильтр `CreatedFrom` (DateTimeOffset?) для «новых за месяц» проброшен
  `ListUserQueryRequest` → `ListUserQuery` → `ToQuery` (`Mars.Identity.Abstractions/Dto/Users/
  UserRequestExtensions.cs`) → `UserRepository.ListAllInternal`.
- Пересмотреть позже: scope-группы ключей вместо явного списка, роли на отдельные ключи.

**Новая страница в tech-зоне:** положить в `Pages/TechViews/` (layout из `_Imports`), маршрут
`/tech/*` или `/dev/tech/*`; цвета — только `var(--ct-*)`/прототипные токены зоны.

## Грабли

- **Scoped-css страниц НЕ должен определять токены темы локально** (`--tint-*: …` на корне страницы) —
  локальные определения перекрывают наследование и блокируют флип темы. Все локальные блоки удалены
  2026-10-06; новые значения — только в less лэйаутов.
- **RZ9996**: у компонента с именованными RenderFragment-параметрами (HeadLeft/Foot/…) неявный контент
  не смешивается — оборачивать в явный `<ChildContent>`.
- **Custom property, объявленная на самом паттерн-элементе, перебивает унаследованную от предка**
  (свойство на элементе всегда ближе наследования). Поэтому `.cloudy-list` НЕ объявляет
  `--cloudy-list-cols` — дефолт через `var(--cloudy-list-cols, minmax(0,1fr))` в месте использования,
  а значение страница задаёт на своём элементе (класс страницы рядом с `.cloudy-list`). Баг 2026-10-06:
  переменная на `CloudyBrowser`-предке не доходила, строки списка схлопывались в одну колонку.
- **z-index tech-лэйаута** (фикс 2026-10-06): `.cloudy-tech-topbar` — `position:relative; z-index:30`
  (иначе дропдаун UserBar перекрывается панелями страниц); `.cloudy-tech-body` — БЕЗ z-index (иначе
  фиксированные модалки страниц z-60+ заперты под топбаром). Светлый `.cloudy-topbar` stacking context
  не создаёт.
- **`.cloudy-search` ≠ `.cloudy-searchbox`**: первое — кликабельная «поисковая строка» топбара (300px,
  kbd), второе — реальный input списка из `cloudy-list.less`. Не путать/не объединять.
- **Сортировка списков**: `Sort="FullName"` НЕ работает (FullName `[NotMapped]` у `UserEntity`) —
  использовать маппед-поля (`LastName`, `-CreatedAt`).
- **`ListDataResult.Empty()` при пустой выборке** → `TotalCount == null`, не 0 (`ToListDataResult` в
  `src/Server/Mars.Data/Extensions/ListDataExtensions.cs`) — счётчики писать через `?? 0`.
- **Фильтр `Roles`** в `UserRepository.ListAllInternal` — семантика `Any` («имеет хотя бы одну роль»);
  до 2026-10-06 был баг `All` (пользователь должен иметь ВСЕ указанные роли и никакие другие).
- **Мёртвый `EUserStatus`**: `UserEntity.Status` никогда не пишется и не маппится в DTO — колонки
  «статус пользователя» в UI не существует; не показывать фейк.
- **Сборка при живом `tools/ui/serve.ps1`** ломает fingerprint-ассеты (404 на .wasm) — сначала
  остановить сервер (детали: память проекта `wasm-fingerprint-stale-server`).
- После коммита, затрагивающего css/js — bump `MarsAppVersion` в `Directory.Build.props` (cache-busting).

## Инварианты

- Новые/переделанные страницы — только классы `cloudy.*` / `cloudy-tech.*` / `cloudy-list.*` и токены
  `--cld-*` / `--ct-*`; хардкод цветов — только rgba-«проливки» статусов (работают в обеих темах).
- Палитра — из прототипа (`main.css`); новые оттенки сначала искать там (`:root` / `.tech.light`).
- Метод замещения: старые страницы продолжают работать в `CloudyLayout` (их стили на `--mars-*`
  флипаются темой автоматически); «старое пока игнорируем» (пользователь, 2026-10-06).
- Blazor-компоненты ≤ ~400–500 строк (razor + code-behind суммарно).
- Проверка точечная: `build-css.ps1 -Entry admin -Check` + `dotnet build` затронутых проектов;
  визуальная проверка — пользователем (или `tools/ui` crawl/probe, БЕЗ скриншотов по умолчанию).

## Отклонено

- Раздельные переключатели темы для cloudy/tech-зон (как sun/moon в TechTopBar прототипа) — пока
  глобальный; идея отложена (2026-10-06).
- Подсчёт пользователей по ролям (чипы-счётчики, ролевые KPI) — убрано; KPI = Total + New this month.
- Колонки Status/Last active из прототипа — нет реальных данных (см. грабли); заменены на CreatedAt.
- `DeskDemoPage` из прототипа не переносить.
- Мега-компонент «конструктор страниц» — вместо него мелкие компонуемые компоненты + CSS-паттерны.
- Пиксель-перфект по скриншотам прототипа.
- `CloudyModal`/`CloudyModalHead` — своя модалка (backdrop + панель + крестик, 2026-10-07,
  отменено в тот же день): велосипед поверх FluentDialog — возня со стеками модалок, нет
  ESC/focus-trap, ручная синхронизация `Visible`. Замена: штатный сервис-диалог +
  `CloudyDialogHead` (заголовок) + скин `cloudy-dialog.less` (см. рецепт выше).

## Статус / следующие шаги

- Готовы: лэйауты + топбары/сайдбары, темизация обеих зон, шаг 1 (Index/Posts/Categories/Users/
  Settings + моки Dashboard/Legion), шаг 2 (TechViews-моки + рабочая TechLogsPage), паттерны списков
  (cloudy-list.less + 8 компонентов), UsersPage мигрирована на них.
- 2026-10-06: FluentDataGrid-паттерн для cloudy (решение пользователя: DataGrid + скин, паттерн
  без обёртки-компонента, flex-fill скролл) — `CloudyGridProvider`, `.cloudy-browser--scroll`,
  `table.cloudy-grid` скин; UsersPage переведена на бесконечную подгрузку (CloudyPager убран
  с неё, компонент жив для пейджинговых страниц). ManagePostView (tech-зона) не тронут.
- 2026-10-07: KPI-эндпоинты (`IKpiHandler` + `api/Kpi`, пилот UsersPage); модалки — штатный
  `FluentDialog` через `IDialogService` (шим-вызов `ShowDialogAsync<T>`, `CloudyDialogHead`
  в TitleTemplate, скин `cloudy-dialog.less`) — CreateUserDialog/ChangePasswordDialog переведены
  на сервис-диалоги. FeedbackListPage переведена на DataGrid-паттерн (KPI `feedbacks.*`
  хендлерами `Mars.Cms.Host/Kpi/`, ViewFeedbackDialog — на `CloudyDialogHead`, колонка CreatedAt
  с «сегодня/вчера» — форматтер вынесен в расширение `FormatTime()`
  (`Mars.Admin.Framework/Extensions/TimeFormatExtensions.cs`, общий с TechLogsPage), Type — тегом
  без сортировки: поле сущности `FeedbackType`, Sort по `Type`
  не сработает; тинт Type — ФИКСИРОВАННЫЙ маппинг (InfoMessage=0, BugReport=3, Question=1) +
  детерминированный фолбэк: `string.GetHashCode()` рандомизирован на процесс, цвета «прыгали»
  при каждой перезагрузке). Чипы-фильтры по Type отклонены (нет фильтра в `ListFeedbackQueryRequest`).
  KPI-метрика фидбеков — «новых за НЕДЕЛЮ» (`feedbacks.newThisWeek`, отсчёт от понедельника;
  решение пользователя 2026-10-07, не «за месяц» как у users).
- Дальше: Feedback — статус «прочитан/непрочитан» (поле на сущности + отметка при просмотре)
  и замена KPI `feedbacks.total` («всего обращений») на НЕПРОЧИТАННЫЕ (задача пользователя
  2026-10-07); перенос Posts/Categories на list-паттерны (кандидаты на DataGrid-паттерн);
  responsive-скрытие колонок DataGrid (не перенесено); остальные диалоги
  (PasskeysPage, PluginViews) — на `CloudyDialogHead`/скин по мере правки страниц;
  редактор поста и Nodes — ПОСЛЕДНИЕ шаги.
