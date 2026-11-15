# CloudyGuide — устройство админки Mars и Cloudy-паттерны

Гайд для агента: как админка устроена СЕЙЧАС (пространства, лэйауты, паттерн-компоненты),
как делать новые страницы и что переиспользовать. Редизайн «cloudy» завершён и влит в master
(2026-11-15, merge `3b13b36a`); история работ — `git show 3b13b36a:ai/CloudyRedesignGuide.md`.
Источник истины по палитре — Vue-прототип `C:\Users\D\Documents\VisualStudio\2026\mars-redesign-project`
(`src/assets/main.css` — ВСЯ палитра: `:root` = светлая зона + тёмный tech, `.tech.light` = светлый tech;
`src/components/*.vue` — разметка страниц; `admin_*.png` — референс-скриншоты).
Смежные гайды: `ai/FluentUiGuide.md` (как верстать на FluentUI v5), `ai/CssRefactoringGuide.md`
(слои токенов, `--mars-*`), `ai/FluentV5Reference.md` (факты библиотеки), `ai/TestingGuide.md`.
Пользователь: без пиксель-перфекта, «общая схема».

Выбор стиля: разведка игровых/киношных языков (сентябрь 2026, превью
`ai/concepts/design/{cyberpunk,eagle-eye,crysis}/ui-style-preview.html` — автономные
страницы-словари стиля) закрыта выбором **Cloudy** по Vue-прототипу; превью оставлены
как история выбора, контракт сравнения и рецепт разведки — в истории:
`git show 3b13b36a:ai/concepts/design/StyleExplorationPlan.md`.

## Раскладка кода

- Лэйауты: `src/Mars.Admin/Shared/Cloudy/`
  - `CloudyLayout.razor` — DefaultLayout всей админки (`App.razor`); светлая зона, классы `.cloudy-*`.
    `AdminLayout` оставлен (rollback / явный `@layout`).
  - `CloudyTechLayout.razor` — тёмная «тех»-зона `/tech/*` (topbar + рельс), guard `Roles="Admin"`;
    `CloudyTechTopBar`, `CloudyTechSideBar`, `CloudyTechDock` (фикс-иконки hub/память в левом
    нижнем углу без фона, popover при наведении — замена статус-бара удалённого BuilderLayout).
  - Паттерн-компоненты списочных страниц (уровень 2): `CloudyPageHead`, `CloudyKpi`, `CloudyBrowser`
    (параметр `Scrollable` — flex-fill тело с внутренним скроллом для DataGrid-страниц;
    `SubHead` + `SubHeadVisible` — полноширинный ряд под тулбаром головы для панелей
    фильтров и т.п.; при `SubHeadVisible=false` ряд НЕ рендерится — пустой div добавлял бы
    лишний gap головы),
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
- Пространства страниц (структура 2026-10-14): `src/Mars.Admin/`
  - `SiteSpace/Pages/` — светлая зона (CloudyLayout): `Index.razor` (Home), моки
    `DashboardPage`/`LegionPage` (+ scoped `.razor.css`), `PostsViews/ManagePostPage`,
    `PostCategoryViews/ManagePostCategoryPage`, `UserViews/UsersPage` (перестроена по
    прототипу), `Settings/SettingsPageWrapper` (cloudy-подложка подразделов настроек),
    PostType/PostCategoryType/UserType/NavMenu/Media/Feedback/Plugin-домены.
    `SiteSpace/Components/` — компоненты только этой зоны (ChildPostEditorDrawer,
    FormBlockEditor, PostStatusesEditor, SystemFieldSettingsPanel,
    RegenerateGeneratedMetaValuesForm).
  - `TechSpace/Pages/` — tech-зона: layout задаёт `TechSpace/_Imports.razor`
    (`@layout CloudyTechLayout`). Все страницы живые (моков не осталось):
    `TechLogsPage` (`/dev/tech/logs`, данные `client.AppDebug.GetLogs`),
    `AutomationPage` (`/dev/tech/automation` — нод-редактор `NodeEditor1`;
    2026-11-05: заменила мок TechAutomationPage, `NodeRedPage` `/tech/nodered` удалена),
    `TechPluginsPage`/`TechMarketplacePage` (PluginViews, живые данные 2026-11-12;
    диалоги NugetInstallDialog/ZipUploadDialog/MarketplacePluginDialog там же,
    на CloudyDialogHead, интерфейс английский), `FrontsPage`/`FrontEditorPage`
    (FrontEditorViews, 2026-11-12), `DatasourcePage` (универсальный workbench
    `/tech/datasource/{Slug?}`, 2026-11-14) + `DatasourceEditConfigPage`
    (`/tech/datasource/config`, BEM ds-config-page), тех-хаб `BuilderAppsPage`
    (`/tech`, BEM tech-hub), `BuilderModelsPage`/`ClassModelListPage`/
    `ViewComponentsPage`/`Playbooks`/`SchedulerPage`/`StyleDesignerPage`
    (рестайл CloudyTech 2026-11-14), StyleGalleryViews/DevTestPage (дев-утилиты).
    Docker-страница вынесена в плагин `Mars.DockerPlugin` (отдельный репо;
    пункт рельса условный по наличию `Mars.DockerPlugin.Front`).
    `NodesPage` (`/tech/nodes`, заглушка) удалена 2026-11-14 — ноды в Automation.
    `/tech/forms` — осиротевшее демо FluentSortableList (не билдер форм), ссылок нет.
    Маршруты унифицированы под `/tech/*` (2026-10-14):
    `/builder/`→`/tech` (Overview), `/builder/models(/classmodels)`→`/tech/models(/classmodels)`,
    `/builder/com/list`→`/tech/components`, `/datasource*`→`/tech/datasource*`,
    `/builder/docker`→`/tech/docker`, `/nodered`→`/tech/nodered`, `/Nodes`→`/tech/nodes`,
    `/forms`→`/tech/forms`, `/front`→`/tech/frontend` (мок TechFrontendPage удалён,
    реальная FrontsPage заняла маршрут), `/front/editor/{Slug}`→`/tech/frontend/editor/{Slug}`,
    `/builder/styledesigner|style-gallery|playbooks|scheduler`→`/tech/*`,
    `/builder/DevTestPage`→`/tech/devtest`. `BuilderLayout`/`BuilderASide`/
    `BuilderSettingsPage` (дубль /Settings)/бейджи статус-бара/`builderlayout.less`
    удалены; slug фронта для ИИ парсится из `/tech/frontend/editor/{slug}`
    (`MarsFrontFilesTools`).
  - `Public/` — Login/Register/Logout/Privacy, лэйаут `CloudyPublicLayout`
    (`Shared/Cloudy/`, вне зон админки; `BlankLayout` оставлен, но Public на нём больше не сидит).
    Редизайн 2026-11-15 (светлый Cloudy + glass, в Vue-прототипе auth-страниц нет):
    бренд-шапка (`SiteName`) + центрированная glass-карточка `.cloudy-auth-card` (440px,
    acrylic-токены, `margin: auto` — не клипует верх при overflow) + футер «Главная · Политика».
    Стили — `cloudy-public.less` (токены миксинами `.cloudy-tokens()`/`.cloudy-tokens-dark()`
    на `.cloudy-public`, тёмная тема через `body[data-theme="dark"]`; фон — штатный
    `.cloudy-backdrop`). Паттерны: `.cloudy-auth-divider` («или» перед SSO), `.cloudy-auth-alt`
    (ссылка вход/регистрация), `.cloudy-auth-success`, `.cloudy-auth-logout` (спиннер «Выходим…»).
    Ошибки — `FluentMessageBar`, не bootstrap-алерты. Инварианты совместимости: E2E-селекторы
    `[name='login-email'] input`, `[name='password'] input`, `fluent-button[type='submit']`,
    роут `/dev/Login`, `nameof(LoginForm.ExecuteLogin)` (Keycloak-тесты) — не менять;
    code-behind Login/Register/Logout не тронут (SSO-callback, пасскей, cookie forceLoad).
    Ссылки внутри Public — от `Navigation.BaseUri` (роут `/Login/sso/{slug}` ломает относительные).
    Privacy переделана 2026-11-15 по аналогии с WP Settings→Privacy: контент — пост
    из опции `PrivacyPolicyOption.PageId` (`Mars.Cms.Contracts/Options`, регистрация
    в `MainCms.UseMarsCms` с `appendToInitialSiteData` → анонимное чтение
    `Q.Site.GetOption<PrivacyPolicyOption>()`). Рендер: `client.Post.Get(id)`
    (анонимный, Content уже HTML через PostTransformer) + `BlazoredHtml`;
    RemotePageViewer НЕ подходит (заточен под Admin-only AdminFront/Render).
    Если пост не выбран — заглушка-гайд + админ-кнопки (Настроить / Создать черновик /
    Написать с ИИ). Настройки: `PrivacyPolicyOptionEditForm` (раздел «Политика»,
    пикер `MetaValueRelationSelectDialog` ModelName="Post", AfterSave обновляет
    `Q.Site.Options` — сеттер сбрасывает optCache). Черновик:
    `PrivacyPolicyDraftButton` (Framework) — скелет разделов 152-ФЗ под редактор
    типа (Editor.js/WYSIWYG/plain), в опцию НЕ назначается автоматически.
    ИИ-помощь: скилл `Mars.AiChat.Host/ai-skills/mars-privacy-policy/SKILL.md` +
    preload в `PageSkillRouter` на разделе настроек (тесты `Mars.AiChat.Tests/
    PrivacyPolicySkillTests`).
  - `Shared/` — только кросс-зонное: лэйауты + `Cloudy/`, UserBar, ActionCenter,
    XActionFormDialog, FluentDialogXActionFormPresenter, FrontDemoActionRunner,
    CodeEditorExtraToolbar, GeneralSectionActions, DataGridSharedViews, DevErrors,
    MobileMenu, AsideBottomItems, AccessDenied/RedirectToLogin, AdminLayout+AdminHeader
    (rollback).
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
   Панель продвинутых фильтров — в слоте `SubHead` (`SubHeadVisible` = флаг панели), НЕ в
   `ChildContent`: тело `--scroll` скроллится, и панель уезжала бы вместе с гридом, ломая
   виртуализацию. Стили панели — `.cloudy-filters(__col/__label/__row)` в cloudy-list.less;
   debounce текстового ввода — только `Debouncer` (см. граблю про CTS).
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
- Параметризованные метрики (2026-11-04): `IParameterizedKpiHandler` (`Mars.Contracts/Common/
  IKpiHandler.cs`) — ключ запроса вида `{Key}:{parameter}` (напр. `posts.total:blog` — число
  записей типа). `KpiController` после точного совпадения режет ключ по первому ':' и зовёт
  `GetAsync(parameter)`; чистый ключ — глобальное значение через базовый `GetAsync`. Результат
  ОБЯЗАН вернуть полный запрошенный ключ (`KpiResult.Key`), `Label` — стабильный ключ без
  параметра (resx-перевод). Кэш per-параметр (`kpi:{key}:{parameter}`); инвалидация: у постов
  тип парсится из топика события (`entity.post/{type}/…`, wildcard-подписка `PostAnyOperation()`
  в конструкторе), у категорий события глобальные (`entity.PostCategory/add|delete`) — хендлер
  помнит запрошенные параметры (`ConcurrentDictionary`) и сбрасывает их все. Пилоты:
  `PostsTotalKpiHandler`/`PostCategoriesTotalKpiHandler` (`Mars.Cms.Host/Kpi/`), ключи —
  `PostKpiKeys.TotalFor(typeName)`, `PostCategoryKpiKeys.TotalFor(typeName)`
  (`Mars.Cms.Contracts`); подписи `posts.total`/`postcategories.total` в `AppRes*.resx`.
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

**Новая страница в tech-зоне:** положить в `TechSpace/Pages/` (layout из
`TechSpace/_Imports.razor`), маршрут
`@page "/tech/*"` (в URL — `/dev/tech/*`); зона admin-only (guard в лэйауте); цвета — только
`var(--ct-*)`/прототипные токены зоны.

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
- **`NavigateTo` с ведущим '/' уходит в корень origin, минуя маунт админки** (`/dev`):
  `NavigateTo("/EditPost/…")` дал 404 на `localhost:5005/EditPost/…` (баг 2026-11-15).
  Относительный URI без слэша (`"EditPost/…"`) резолвится от `BaseUri`; для `<a href>`
  в разметке — `Navigation.BaseUri + "путь"` (в `EditPostPage` исторически хардкод `/dev`).
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

## Механика доменных страниц (справочник)

Устойчивые факты по страницам (выжимка из истории редизайна; полная история —
`git show 3b13b36a:ai/CloudyRedesignGuide.md`, секция «Статус / следующие шаги»).

### KPI-ключи по страницам

Реестр и механика — рецепт «KPI-карточки» выше. Ключи-константы живут в Contracts
модуля-владельца, хендлеры — в Host модуля.

| Страница | Ключи | Хендлеры / инвалидация |
|---|---|---|
| UsersPage | `users.total`, `users.newThisMonth` | `Mars.Identity.Host/Kpi/Users*KpiHandler`, ключи `UserKpiKeys` (`Mars.Identity.Contracts/Users`); TTL 10 мин + `entity/user/add\|delete` |
| FeedbackListPage | `feedbacks.total`, `feedbacks.newThisWeek`, `feedbacks.unread` | `Mars.Cms.Host/Kpi/Feedbacks*KpiHandler` + `FeedbacksUnreadKpiHandler`, ключи `FeedbackKpiKeys`; `FeedbackAdd/FeedbackDelete` + `entity/feedback/read`. **newThisWeek — за НЕДЕЛЮ от понедельника** (решение пользователя, не «за месяц» как у users) |
| ListPostTypePage | `posttypes.total/componentTypes/metaFields/metaValues` | ключи `PostTypeKpiKeys` (`Mars.Cms.Contracts/PostTypes`), `Mars.Cms.Host/Kpi/`. Все метрики — ТОЛЬКО в рамках PostType: metaFields считает поля с `PostTypeId != null`, metaValues — только `PostMetaValues` (без UserType/PostCategory-владельцев). Инвалидация: `entity.PostType/**`; metaValues — `entity.post/**` (Defaults-хелпер `PostAnyOperation()`) |
| ListUserTypePage | `usertypes.total` | `UserTypeKpiKeys` (`Mars.Identity.Contracts/UserTypes`), `UserTypesTotalKpiHandler`; `entity.UserType/**` |
| ListPostCategoryTypePage | `postcategorytypes.total` | `PostCategoryTypeKpiKeys` (`Mars.Cms.Contracts/PostCategoryTypes`), `PostCategoryTypesTotalKpiHandler`; `entity.PostCategoryType/**` |
| ManagePostPage | `posts.total:{typeName}`, `postcategories.total:{typeName}` | параметризованные (`IParameterizedKpiHandler`): `PostsTotalKpiHandler`/`PostCategoriesTotalKpiHandler` (`Mars.Cms.Host/Kpi/`), ключи `PostKpiKeys.TotalFor(typeName)`/`PostCategoryKpiKeys.TotalFor(typeName)`. Карточка Categories рендерится только при включённой фиче Category |
| ManagePostCategoryPage | `postcategories.total:{type}` | тот же параметризованный хендлер |
| ManageMediaPage | `media.files`, `media.storage` | `MediaKpiKeys` (`Mars.Media.Contracts/Files`), `Mars.Media.Host/Kpi/`; storage = SUM FileSize в байтах, фронт хьюманизирует `ToHumanizedSize()`. События `MediaFileAdd/MediaFileDelete` (`entity/mediafile/*`) добавлены в Defaults; триггеры — FileService (WriteUpload/Delete/DeleteMany) и MediaService (ScanFilesAndSaveInDB) |

### Feedback (read/unread)

- `FeedbackEntity.IsRead`; миграция `20261115010627_AddFeedbackIsRead`: backfill старых строк
  `DEFAULT true`, затем `DROP DEFAULT`; `HasDefaultValue` в модели НЕТ — иначе EF опустил бы
  CLR-default false и новые строки стали бы true.
- `POST api/Feedback/{id}/read` (`FeedbackController.MarkRead`, `Mars.Cms.Host`) — идемпотентен,
  событие `entity/feedback/read` только при реальном изменении; фильтр `IsRead` в List-запросе.
- UI: чипы All/Unread, точка + утолщённый заголовок непрочитанных, MarkRead при открытии диалога,
  refresh грида и KPI после закрытия. Excel-колонка IsRead не добавлялась (бинарный шаблон).
- Type — тег БЕЗ сортировки (поле сущности `FeedbackType`), тинт ФИКСИРОВАННЫЙ:
  InfoMessage=0, BugReport=3, Question=1. Чипы-фильтры по Type отклонены (нет фильтра
  в `ListFeedbackQueryRequest`).
- CreatedAt — «сегодня/вчера» через `FormatTime()`
  (`Mars.Admin.Framework/Extensions/TimeFormatExtensions.cs`, общий с TechLogsPage).
- Тесты: `tests/Mars.Server.Tests/Feedbacks`.

### Media (`/Media`)

- Страница = CloudyPageHead + KPI + `<MediaBrowser Mode=Manage>` (~40 строк). Ядро браузинга —
  `MediaBrowser` (`SiteSpace/Pages/MediaViews`): `MediaBrowserMode = Manage | SelectSingle |
  SelectMulti`, колбэки OnFilePicked/SelectionChanged/OnContentChanged, `GetSelectedFiles()`.
- Плитки `.cloudy-tiles`/`.cloudy-tile`: папки → файлы → плитка загрузки в ОДНОЙ области
  (`.cloudy-browser-group`). Крошки `.cloudy-crumbs` НЕ локализовать (корень — литерал "Media",
  имена папок как есть; страница англоязычная). Сортировка — чипы Date/Name/Size (поля сущности
  `CreatedAt`/`FileName`/`FileSize`, НЕ response-DTO), клик по активному меняет направление.
  Flat-вид — одна иконочная кнопка в HeadLeft (`FolderId=null` = все файлы, сброс сортировки
  в CreatedAt DESC).
- Страница ОТКРЫВАЕТСЯ В ПАПКЕ ГОДА: `OpenYearFolder` (find-or-create `Name == DateTime.Now.Year`
  в корне; ошибка — молчаливый фолбэк в корень). Загрузка всегда в ТЕКУЩУЮ папку.
- `MediaUploadTile` — нативный `InputFile`, растянутый на плитку с opacity:0 (НЕ FluentInputFile);
  лимиты 1000 МБ / 50 шт, ошибки пофайлово через IMessageService; drag-состояние — события
  на label (`.cloudy-tile--drag`).
- Изображения — spotlight.js (якорь `.spotlight` + контейнер `.spotlight-group`). НЕ-изображения —
  ОДИН общий `FluentPopover` с динамическим `AnchorId` (при переключении: закрыть → `Task.Delay(50)`
  → открыть, иначе anchored-region не перепривязывается); свойства — `MediaFilePropsPopover`.
  Текст — `MediaTextFileDialog` (whitelist расширений + лимит 512 КБ в `MediaTextPreview.cs`;
  `window.blazor_fetch_text(url, maxChars, encoding?)` — utf-8 fatal → фолбэк windows-1251;
  бейдж кодировки = ре-fetch с форсированной кодировкой; oversized — только popover с Download).
- Создание текстового файла — `MediaCreateTextFileDialog` (имя без точки → `.txt`;
  `CodeEditor2 Lang=plaintext` в обёртке `.cloudy-create-text__editor` 340px — дефолтные 80vh
  ломают раскладку). ОШИБКИ СОХРАНЕНИЯ — ВНУТРИ диалога (`.cloudy-dialog-error`, catch
  `MarsValidationException`): действие делает сам диалог, `CloseAsync(true)` ТОЛЬКО при успехе,
  OK Disabled на время busy; страница проверяет `result.Value is not true`.
- Пикер: `MediaSelectDialog` (CloudyDialogHead + MediaBrowser в Select-режиме); ширина
  min(1200px, 100vw−48px) через `fluent-dialog[fuib]:has(.cloudy-media-select)::part(dialog)`
  (`[fuib]` обязателен — FluentV5Reference факт №10); свежая загрузка АВТОВЫБИРАЕТСЯ.
  Фасад `IAppMediaService.OpenSelectMedia/Many` сохранён (~10 call-сайтов); static-хендлеры
  регистрирует `MediaSelectRegistrar` (`App.razor`, AuthorizeView). ПОСЛЕДСТВИЕ: dev-стенд
  StandNodesApp БЕЗ медиа-пикера (хендлеры только в Mars.Admin; перенос медиа-семейства
  в Framework — отклонённый вариант).
- УДАЛЕНЫ (2026-10-12): ModalMediaSelect, FluentMediaFilesList, AdminSharedContainer;
  из IAppMediaService убраны ListTable/Delete.

### Posts (ManagePostPage / ManagePostView)

- DataGrid-паттерн; тулбар: HeadLeft — фильтр-категория + иконочные кнопки (фильтры/колонки/
  refresh), HeadRight — только поиск + Create (решение пользователя 2026-11-04).
- Настройка колонок — сервис-диалог `PostGridSettingsDialog` (CloudyDialogHead +
  PostTypeGridSettingsEditor); результат — wrapper `PostGridSettingsDialogResult`, т.к.
  null-настройки = «сброс» и неотличимы от отмены.
- Панель фильтров — слот `SubHead`/`SubHeadVisible` + `.cloudy-filters`; дебаунс — `Debouncer`.
- Single-режим — `EditPostView Flat` в `.cloudy-acrylic` (не тронут редизайном списков).
- XActionsDropDown в hero закомментирован (место не определено — см. бэклог).

### Categories

- ManagePostCategoryPage: DataGrid-паттерн, иерархия отступом `(LevelsCount-1)*20` в Title-ячейке,
  дефолт-сортировка SlugPath asc (порядок дерева). ManagePostCategoryView поглощён в страницу.
- EditPostCategoryPage — на StandardEditContainer; `SelectParentCategoryDropDown` получает
  реальный тип из маршрута (хардкод PostType="post" был багом).

### NavMenu

- ManageNavMenuPage: DataGrid-паттерн БЕЗ KPI; Title-stack со ссылкой на `/dev/EditNavMenu/<id>`
  + slug; Tags — `CloudyTag` c `tag.StableTint()` (`StringTintExtensions.cs`).
  Детерминированный тинт строки — `StableTint()`, НЕ `string.GetHashCode()`
  (рандомизирован на процесс — цвета «прыгали» при перезагрузке).

### Tech-хаб и tech-страницы

- `/tech` (BuilderAppsPage) — BEM tech-hub: hero + 17 карточек в 4 группах
  (Main/Manage/System/Dev utilities), английский, tint на группу (purple/blue/teal/green).
- `/tech/models` — 5 доменов: PostType, PostCategoryType, Users, UserType, ClassModels.
- SchedulerPage/StyleDesignerPage — BEM scheduler-page/styledesigner-page на `--ct-*`;
  панели БЕЗ overflow:hidden/backdrop-filter (иначе клиппинг fixed-оверлеев
  FluentSelect/Autocomplete/DatePicker из StyleSampleForm). Скид грида scheduler локальный:
  `cloudy-grid` из cloudy-list.less сидит на `--cld-*` и в tech-зоне не работает.
- XActionsDropDown явно возвращён в hero Scheduler/StyleDesigner; на Fronts/Datasource/Logs
  пропал молча (открытый вопрос — см. бэклог).

## Бэклог

- Responsive-скрытие колонок DataGrid не перенесено: на 390px грид шире шелла
  (FeedbackList/Users — системный лимит DataGrid; mobile-дефекты отложены).
- XActions в cloudy — место не определено: дропдаун в hero Posts закомментирован,
  на Scheduler/StyleDesigner возвращён в hero, на Fronts/Datasource/Logs отсутствует.
- Моки Dashboard/Legion — оживить или убрать (решение пользователя: пока оставлены).
- Tech-вариант cloudy-grid скина — если появятся ещё tech-гриды.
- KPI: scope-группы ключей вместо явного списка, роли на отдельные ключи.