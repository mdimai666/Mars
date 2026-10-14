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
  - `cloudy-form.less` — BEM-блок `edit-container` шелла `StandardEditContainer`: базовая
    раскладка без bootstrap-утилит (действует в любом лэйауте), cloudy-скин под `.cloudy-layout`
    (hero-хедер, glass-карточка `__main`, side-карточки `__card`) + модификатор
    `edit-container--flat` (вложенные редакторы без glass/hero).
  - Импорт всех cloudy-файлов — в конце `style.less`; компиляция только через `tools/ui/build-css.ps1 -Entry admin`
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
   фиксированной ширины `--cloudy-kpi-width: 180px` (отклонение от прототипных 168px — решение
   пользователя 2026-10-07; label — одна строка с ellipsis и `title` при наведении), колонки
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
`cloudy-dialog.less` (грабля: диалоги рендерятся вне `.cloudy-layout` — на хосте
`fluent-dialog/fluent-drawer` ВЕСЬ набор `--cld-*` публикуется less-миксинами
`.cloudy-tokens()`/`.cloudy-tokens-dark()` из cloudy.less — единый источник, руками не
синхронизировать; до 2026-10-12 был ручной дубль 5 токенов). Мигрированы:
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

**Edit-страница (cloudy-скин StandardEditContainer, 2026-10-14):** шелл
(`Mars.Admin.Framework/Components/StandardEditContainer.razor`) — BEM-блок `edit-container`
БЕЗ bootstrap-утилит (`__header/__title/__subtitle/__tools/__body/__main/__fields/__aside/
__aside-inner/__card(-body/-title/-meta/-section/-actions)/__extra/__save-row/__skeleton*`);
стили — `cloudy-form.less`: базовая раскладка вне скоупа (работает и в старом AdminLayout)
+ cloudy-скин под `.cloudy-layout`. Все страницы на шелле (пост, PostType, UserType,
PostCategoryType, NavMenu…) получают cloudy-вид автоматически: hero-хедер (30px + подзаголовок),
glass-карточка основной колонки `__main` (acrylic-токены), aside `__aside` 320px (sticky) с
side-карточками.
- Сайдбар — паттерн `.cloudy-sidebar`: `position: sticky; top: 24px` + `align-self: flex-start`
  + каппинг `max-height: calc(100vh - 48px)` + собственный `overflow-y: auto` (без каппа у
  sticky нет хода — грабля в «Граблях»); на `__body` при ≥992px — `align-items: flex-start`.
  JS-`Affix` из шелла убран; инлайн-стили (width/sticky/max-width) заменены BEM-элементами
  с дефолтами в less.
- `SectionSubtitle` (RenderFragment&lt;TModel&gt;) — подзаголовок hero; рендерится с model
  шелла, поэтому обновляется после загрузки (строковый параметр от страницы-родителя не
  обновился бы — родитель не ре-рендерится при загрузке модели).
- Вложенные редакторы — модификатор `edit-container--flat` (без glass/hero; иначе «стекло в
  стекле»): `[Parameter] Flat` у `EditPostView` добавляет класс в `Class` шелла. Используют:
  single-режим ManagePostPage (внутри `.cloudy-acrylic`) и `ChildPostEditorDrawer`.
- Карточки страниц внутри `SectionExtraSidebar` — пока bootstrap `.card`; скин накрывает их
  селектором `__extra .card` (cloudy-вид без правки страниц).
- Поля формы (`Mars.Forms.Front`), Title-редактор и кнопки НЕ стилизованы — только обёртка
  (решение пользователя 2026-10-14); FluentUI-поля флипаются темой сами.
- Страница-обёртка (`EditPostPage`) — БЕЗ `ContentWrapper`: `flex-fill` div напрямую
  в `cloudy-content`, как ManagePostPage.

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
- **Дебаунс `CloudySearchBox` — через `Debouncer`** (`Mars.Core/Features/Debouncer.cs`, глобальный
  namespace): компонент создаёт его в `OnInitialized(DebounceMs)` и диспожит (`@implements IDisposable`).
  НЕ писать инлайн-дебаунс на `CancellationTokenSource`: прежний `using var cts = _cts = …` диспожил
  CTS по завершении, поле указывало на мёртвый объект — следующий ввод падал
  `ObjectDisposedException: CancellationTokenSource_Disposed`, а обработчик удаления умирал до
  очистки фильтра (поиск залипал на последнем значении; баг 2026-10-07). `Debouncer` проверяет
  `token.IsCancellationRequested` ПОСЛЕ задержки под локом — устаревший текст не выстреливает.
- **Сортировка списков**: `Sort="FullName"` НЕ работает (FullName `[NotMapped]` у `UserEntity`) —
  использовать маппед-поля (`LastName`, `-CreatedAt`).
- **Fire-and-forget `_ = LoadFiles()` в синхронном обработчике** — рендер происходит один раз
  на возврат void-хендлера (в него успевает попасть только `_filesLoading = true`); континуация
  задачи меняет состояние БЕЗ re-render — «Loading…» залипает навсегда (баг сортировки
  ManageMediaPage 2026-10-12). Обработчики событий, грузящие данные, — `async Task` + `await`,
  loading-флаг снимать в `try/finally`.
- **`ListDataResult.Empty()` при пустой выборке** → `TotalCount == null`, не 0 (`ToListDataResult` в
  `src/Server/Mars.Data/Extensions/ListDataExtensions.cs`) — счётчики писать через `?? 0`.
- **Фильтр `Roles`** в `UserRepository.ListAllInternal` — семантика `Any` («имеет хотя бы одну роль»);
  до 2026-10-06 был баг `All` (пользователь должен иметь ВСЕ указанные роли и никакие другие).
- **Мёртвый `EUserStatus`**: `UserEntity.Status` никогда не пишется и не маппится в DTO — колонки
  «статус пользователя» в UI не существует; не показывать фейк.
- **JS-`Affix` внутри `position: sticky`-контейнера** — при скролле сайдбар «прыгает» по ширине:
  Affix переключает контент в `position: fixed`, содержимое выходит из потока и aside теряет
  intrinsic-ширину (баг StandardEditContainer; фикс 2026-10-14 — Affix из шелла убран, оставлен
  только sticky). Не совмещать два механизма прилипания; в `EditNavMenuPage` остался свой
  отдельный Affix (не в sticky-контейнере).
- **Sticky-сайдбар «не липнет» = нет хода**: sticky скользит только внутри родителя и только
  пока родитель выше элемента; высокая стопка карточек (Публикация + Extra) выходит вровень
  с основной колонкой → ход ≈ 0. Одного `height: fit-content` недостаточно — элемент может
  быть выше вьюпорта (низ всегда за экраном). Лечение — паттерн `.cloudy-sidebar`: каппинг
  `max-height: calc(100vh - 48px)` + собственный `overflow-y: auto` + `align-self/align-items:
  flex-start` (фикс 2026-10-14, `edit-container__aside`).
- **`default-fuib.css` (adopted) задаёт `body{overflow:hidden}` — убивает ВСЕ sticky админки**:
  body становится нескроллящимся scroll-контейнером (скроллится окно) — sticky привязан к body
  и не активируется; левый рельс `.cloudy-sidebar` был мёртв с октября, JS-`Affix` в шелле
  edit-страниц — обход именно этого. Фикс 2026-10-14: `html body{overflow-x:clip;overflow-y:visible}`
  в base.less (специфичность 0,0,2 > adopted 0,0,1; диалоговый lock `body:has(.prevent-scroll)`
  0,1,1 уцелел), проверено playwright-замером (aside/рельс липнут на top:24). Static-раскопки
  less такого не видят — правила в `document.adoptedStyleSheets` (детали: FluentV5Reference).
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
  решение пользователя 2026-10-07, не «за месяц» как у users). ManageNavMenuPage — тоже
  DataGrid-паттерн, БЕЗ KPI (Title-stack со ссылкой на `/dev/EditNavMenu/<id>` + slug, Tags —
  `CloudyTag` c `tag.StableTint()`); мёртвый OnRowClick (unreachable-код с ViewFeedbackDialog)
  удалён. Детерминированный тинт строки — расширение `StableTint()`
  (`Mars.Admin.Framework/Extensions/StringTintExtensions.cs`), НЕ `string.GetHashCode()`.
  ListPostTypePage — DataGrid-паттерн + 4 KPI (`posttypes.total/componentTypes/metaFields/
  metaValues`, ключи `Mars.Cms.Contracts/PostTypes/PostTypeKpiKeys.cs`, хендлеры
  `Mars.Cms.Host/Kpi/`; все метрики — ТОЛЬКО в рамках PostType, решение пользователя
  2026-10-11: metaFields считает поля с `PostTypeId != null`, metaValues — только
  `PostMetaValues`, без UserType/PostCategory-владельцев). Инвалидация: total/component/
  metaFields — wildcard `entity.PostType/**`; metaValues — `entity.post/**` (новый
  Defaults-хелпер `PostAnyOperation()` без typeName). Чекбокс «Компонентные типы»
  (IncludeComponent) — в HeadLeft.
  ListUserTypePage — тот же паттерн (2026-10-11): KPI только `usertypes.total` (ключи
  `Mars.Identity.Contracts/UserTypes/UserTypeKpiKeys.cs`, хендлер
  `Mars.Identity.Host/Kpi/UserTypesTotalKpiHandler`, инвалидация wildcard
  `entity.UserType/**`), колонки Title-stack/Tags(`StableTint`)/CreatedAt DESC/удаление.
  ListPostCategoryTypePage — тот же паттерн (2026-10-11): KPI только `postcategorytypes.total`
  (ключи `Mars.Cms.Contracts/PostCategoryTypes/PostCategoryTypeKpiKeys.cs`, хендлер
  `Mars.Cms.Host/Kpi/PostCategoryTypesTotalKpiHandler`, инвалидация wildcard
  `entity.PostCategoryType/**`).
  ManageMediaPage (`/Media`) — ПЛИТОЧНЫЙ паттерн по прототипу CategoriesPage.vue (2026-10-11):
  новые паттерны cloudy-list.less — `.cloudy-tiles`/`.cloudy-tile` (плитки папок/файлов,
  hover-действия rename/delete/move, превью-миниатюры), `.cloudy-crumbs` (хлебные крошки
  папок), `.cloudy-browser-group` (группы Folders/Files), `.cloudy-count`. KPI `media.files` +
  `media.storage` (`MediaKpiKeys` в Mars.Media.Contracts/Files, хендлеры `Mars.Media.Host/Kpi/`;
  storage = SUM FileSize в байтах, фронт хьюманизирует `ToHumanizedSize()`). Медиа-событий
  НЕ было — добавлены Defaults `MediaFileAdd/MediaFileDelete` (`entity/mediafile/*`) и
  триггеры в FileService (WriteUpload/Delete/DeleteMany) и MediaService (ScanFilesAndSaveInDB);
  конструктор FileService теперь с IEventManager (тест FileServiceTests обновлён). Диалоги —
  MediaFolderNameDialog/MediaMoveFileDialog (сервис-паттерн, страницы Pages/MediaViews).
  `FluentMediaFilesList` не тронут (его использует ModalMediaSelect) — ОБА УДАЛЕНЫ
  2026-10-12 (вариант 1, см. ниже). XActions-дропдаун
  (скан файлов/миниатюры) и ссылка «Настройки медиа» со страницы сняты — команды доступны
  через ActionCenter (открытый вопрос XActions-меню в cloudy). Файлы — НЕ DataGrid:
  пейджинг `CloudyPager` (Skip/Take через `client.Media.List`, FolderId: null=все,
  Guid.Empty=корень).
  ManageMediaPage — клики по файлам, сортировка, flat-вид (2026-10-12): изображения —
  spotlight.js (якорь `class="spotlight"` + контейнер плиток `.spotlight-group`, `data-media`/
  `data-title`; API бандла: клик-делегация на document, группа = closest `.spotlight-group`).
  НЕ-изображения — popover свойств: ОДИН общий `FluentPopover` с динамическим `AnchorId`
  (id плитки `media-file-{Guid}`), контент `.cloudy-file-props` (cloudy-list.less); при
  переключении на другой файл — сначала закрыть, `Task.Delay(50)`, потом открыть (иначе
  anchored-region не перепривязывается). Текстовые файлы (whitelist расширений в
  ManageMediaPage.razor.cs + лимит `MaxTextPreviewSize` 512 КБ) — `MediaTextFileDialog`
  (сервис-паттерн): monospace `pre.cloudy-mono-preview`, широкая панель 920px через
  `fluent-dialog:has(.cloudy-mono-preview)::part(dialog)` (cloudy-dialog.less); текст
  тянется `window.blazor_fetch_text(url, maxChars)` (scripts.js, fetch + truncation);
  oversized-текст — только popover с Download («Too large to preview»). Сортировка как в
  старом FluentMediaFilesList — чипы Date/Name/Size (поля сущности
  `CreatedAt`/`FileName`/`FileSize`, НЕ поля response-DTO), клик по активному чипу
  переключает направление (иконка bi-arrow-down/up-short); расстановка (swap по запросу
  2026-10-12): чипы сортировки — в HeadLeft рядом с кнопкой вида, хлебные крошки —
  в `.cloudy-group-head` над плитками (во flat-виде скрыты). Вид «All files» (как галерея
  телефона) — ОДНА иконочная кнопка `.cloudy-icon-btn` в HeadLeft (bi-collection ↔
  bi-folder2-open, без текста, `ToggleView`): flat-вид прячет крошки/папки/New folder,
  `FolderId=null` (=все файлы), при включении сбрасывает сортировку в CreatedAt DESC.
  Папки и файлы — ОДНА область (заголовки секций Folders/Files убраны по запросу
  пользователя 2026-10-12): один `.cloudy-browser-group` с одним гридом `.cloudy-tiles` —
  папки → файлы → плитка загрузки последней. Загрузка — новый `MediaUploadTile`
  (Pages/MediaViews) вместо `MediaUploadZone` (на странице больше не используется):
  нативный `InputFile`, растянутый на плитку с opacity:0 (клик = picker, drag&drop =
  нативный drop на input; НЕ FluentInputFile — см. память про его грабли), по файлу
  `client.Media.Upload` (лимиты 1000 МБ / 50 шт как у MediaUploadZone, ошибки —
  IMessageService пофайлово), стиль `.cloudy-tile--upload` (пунктир). Хлебные крошки
  НЕ локализовать: корень — литерал "Media", имена папок как есть (страница вся
  англоязычная, `AppRes.Media` в крошке давал разнобой «Медиа» среди English).
  ManageMediaPage — второй раунд (2026-10-12): страница ОТКРЫВАЕТСЯ В ПАПКЕ ГОДА —
  `OpenYearFolder` ищет в корне папку `Name == DateTime.Now.Year`, нет — создаёт
  `CreateFolder(ParentId=null)` (серверный `Create` даёт тот же `Path="Media/{год}"`, что
  `GetOrCreateByPath` при загрузке); ошибка — молчаливый фолбэк в корень. Загрузка всегда
  в ТЕКУЩУЮ папку (tile `FolderId=_currentFolderId`; в корне/null — серверный фолбэк
  Media/{год}). Кнопка «New folder» → bootstrap-dropdown «New» (`data-bs-toggle`, скин
  `.cloudy-dropdown-menu` в cloudy-list.less): Folder / Text file / Upload files (JS
  `blazor_click_element('media-upload-input')` кликает скрытый input плитки). Создание
  текстового файла — `MediaCreateTextFileDialog`: имя (без точки → дописать `.txt`) +
  `CodeEditor2 Lang=plaintext` (ContainerCssStyle height:100% + обёртка
  `.cloudy-create-text__editor` 340px — грабля дефолтных 80vh), результат
  `MediaNewTextFile(Name, Content)` → страница грузит `client.Media.Upload`
  (MemoryStream, UTF-8 БЕЗ BOM). Drag-состояние плитки загрузки: Blazor
  `@ondragenter/@ondragleave/@ondrop` на label (input накрывает плитку — события
  всплывают до label), класс `.cloudy-tile--drag` = сплошная рамка + primary. Кодировки
  превью: `blazor_fetch_text(url, maxChars, encoding?)` — arrayBuffer,
  `TextDecoder('utf-8',{fatal:true})` → при ошибке `windows-1251` (детект «простой»,
  по запросу пользователя), возвращает `{text, encoding}` → `record TextPreviewResult`;
  в заголовке `MediaTextFileDialog` бейдж `.cloudy-enc-badge` — клик = ре-fetch
  с форсированной кодировкой (utf-8 ↔ windows-1251; токены бейджа — только 5,
  продублированных на хосте fluent-dialog). Страница у лимита 500 строк — плитка файла
  вынесена в `MediaFileTile.razor` (File + EventCallback OnClick/OnMove/OnDelete),
  текстовые хелперы — в `MediaTextPreview.cs` (whitelist расширений, MaxSize 512 КБ,
  IsPreviewable/IsOversized).
  ManageMediaPage — третий раунд (2026-10-12): ОШИБКИ СОХРАНЕНИЯ — ВНУТРИ ДИАЛОГА, при
  ошибке диалог НЕ закрывается (кейс пользователя: создание json → серверный валидатор
  `UploadMediaFileValidator`, whitelist расширений `MediaOption.AllowedFileExtensions` →
  400 → `MarsValidationException` на клиенте; раньше возникал на странице ПОСЛЕ закрытия
  диалога и выглядел как сырое исключение). Действие переехало в диалог:
  `MediaCreateTextFileDialog` сам делает `Upload` (`[Parameter] FolderId`),
  `MediaFolderNameDialog` принимает `[Parameter] Func<string, Task> SubmitAction`
  (лямбды create/rename со страницы); catch `MarsValidationException` (join Errors) и
  Exception → инлайн `.cloudy-dialog-error` (cloudy-dialog.less; в токены хоста диалога
  добавлен `--cld-red-badge` #e5484d/#ff6b70 — синхронно с cloudy.less),
  `CloseAsync(true)` ТОЛЬКО при успехе, OK-кнопка `Disabled` на время busy; страница
  проверяет `result.Value is not true` (было — string/MediaNewTextFile; record
  MediaNewTextFile удалён). Раскладка плиток: имя+размер ПРИЖАТЫ ВНИЗ, иконка/превью —
  по центру оставшейся части: `margin: auto 0` на прямом иконочном ребёнке `.cloudy-tile`
  (папки/загрузка) и на icon/thumb внутри `__open` (файлы), `__open` — `flex: 1`.
  ModalMediaSelect → cloudy-диалог выбора (вариант 1 «фасад + реализация в приложении»,
  2026-10-12): ядро браузинга выделено из ManageMediaPage в `MediaBrowser`
  (Pages/MediaViews; `Mode = Manage | SelectSingle | SelectMulti`, колбэки
  OnFilePicked/SelectionChanged/OnContentChanged, публичный `GetSelectedFiles()`);
  страница /Media = CloudyPageHead + KPI + `<MediaBrowser Mode=Manage>` (~40 строк).
  `MediaSelectDialog` — CloudyDialogHead + браузер в Select-режиме + футер
  Cancel/«Add selected (N)»; single — клик по плитке закрывает сразу (Cancel —
  `Dialog.CancelAsync()`, не `CloseAsync(null)` — CS8625); свежая загрузка
  АВТОВЫБИРАЕТСЯ (single → первый файл, multi → все; ищем в перезагруженном списке
  по Id — дефолт CreatedAt DESC кладёт новые на первую страницу). Показ — через
  ФАСАД: сигнатуры `IAppMediaService.OpenSelectMedia/Many` сохранены (~10 call-сайтов в
  Framework/Nodes.Front/FormEditor/приложении не тронуты), `AppMediaService` вместо
  static-ссылки на компонент держит static-хендлеры `SelectFileHandler`/
  `SelectFilesHandler`, которые регистрирует `MediaSelectRegistrar` (App.razor,
  AuthorizeView — вместо AdminSharedContainer). Framework-чистка: УДАЛЕНЫ
  ModalMediaSelect, FluentMediaFilesList (razor+cs), AdminSharedContainer; из
  IAppMediaService убраны ListTable/Delete (единственный потребитель — список).
  MediaFileTile расширен под выбор: `Selectable` (нет spotlight-якоря и hover-действий,
  клик = выбор; id плитки не рендерится — popover только в Manage), `MultiSelect`/
  `Selected` (бейдж-галочка `.cloudy-tile__check`, `.cloudy-tile--selected`); popover
  свойств вынесен в `MediaFilePropsPopover` (публичный `Open(file, anchorId)` —
  страница/браузер в лимите ~500 строк). Диалог выбора: ширина min(1200px, 100vw−48px)
  через `fluent-dialog[fuib]:has(.cloudy-media-select)::part(dialog)` — `[fuib]` в
  селекторе ОБЯЗАТЕЛЕН (adopted-правило default-fuib.css бьёт документальное при равной
  специфичности — FluentV5Reference факт №10; фикс пользователя 2026-10-13), тело —
  max-height 68vh со скроллом; компактные оверрайды плиток в `.cloudy-media-select`
  (padding/gap меньше, thumb 48px, имя/meta 13/12px, чек 20px; сетка auto-fill
  minmax(148px) и иконки 34px — как на странице, финальная правка пользователя). Токены
  cloudy.less — миксины `.cloudy-tokens()`/`.cloudy-tokens-dark()` (вызываются на
  `.cloudy-layout` И на хостах диалогов — полный набор, грабля «5 токенов» ушла).
  ПОСЛЕДСТВИЕ: dev-стенд StandNodesApp остался БЕЗ медиа-пикера (мёртвый маунт в
  Routes.razor убран; хендлеры регистрирует только Mars.Admin → OpenSelectMedia
  возвращает null); если стенду нужен пикер — перенос медиа-семейства в Framework
  (отклонённый вариант 2).
- 2026-10-14: редактор поста — общий cloudy-скин шелла `StandardEditContainer`; раунд 2
  в тот же день — шелл переписан на BEM-блок `edit-container` без bootstrap-утилит и из
  сайдбара убран JS-`Affix` (фикс «прыгающей» ширины при скролле); раунд 3 — sticky не
  лип (нет хода: стопка карточек вровень с колонкой), фикс = паттерн `.cloudy-sidebar`
  (каппинг `max-height: calc(100vh - 48px)` + свой `overflow-y:auto` + flex-start, top:24px);
  раунд 4 — корень «не липнет»: `default-fuib.css` (adopted) задаёт `body{overflow:hidden}`
  и убивает ВСЕ sticky админки (левый рельс тоже), фикс `html body{overflow-x:clip;
  overflow-y:visible}` в base.less, проверено playwright-замером (aside/рельс липнут top:24;
  грабли выше). `cloudy-form.less`: базовая раскладка + скин (hero-хедер/`SectionSubtitle`,
  glass `__main`, aside 320px, `edit-container--flat` для вложенных — single ManagePostPage
  и ChildPostEditorDrawer через `[Parameter] Flat` у EditPostView); `EditPostPage` без
  ContentWrapper. Все остальные edit-страницы шелла получили тот же вид автоматически.
  Поля формы/Title-редактор не тронуты (решение пользователя). Рецепт — §«Edit-страница».
- Дальше: Feedback — статус «прочитан/непрочитан» (поле на сущности + отметка при просмотре)
  и замена KPI `feedbacks.total` («всего обращений») на НЕПРОЧИТАННЫЕ (задача пользователя
  2026-10-07); перенос Posts/Categories на list-паттерны (кандидаты на DataGrid-паттерн);
  responsive-скрытие колонок DataGrid (не перенесено); остальные диалоги
  (PasskeysPage, PluginViews) — на `CloudyDialogHead`/скин по мере правки страниц;
  Nodes — ПОСЛЕДНИЙ шаг.
