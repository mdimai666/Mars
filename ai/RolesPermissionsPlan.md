# RolesPermissionsPlan — роли = наборы прав (гибрид)

Статус: **фаза 1 ВЫПОЛНЕНА полностью (T1–T5, 2026-11-17), закоммичено одним коммитом
в ветку `features/role-cap` (2026-11-20), не запушено.** Фаза 2 — бэклог.
Решения закрыты 2026-11-17.
Модель: роль = именованный набор гранулярных прав (caps), проверка по праву, а не по имени
роли (WordPress-подход). По закрытии — схлопнуть в гайд подсистемы (ai/PlanLifecycleGuide.md).

## Решения (пользователь, 2026-11-17)

- Гибрид, а не только роли: плагины должны объявлять свои права без правки ядра; облаку
  нужны кастомные роли.
- Обычные пользователи создают посты — **свои** (пресет Author).
- Тех-зона — грубые капы, пресет Developer (не жёсткий Admin).
- Роль Manager заменяется на Editor/Author (миграция существующих БД).
- Медиа — **общая библиотека**, пар владения нет.
- `Post.Publish` — отдельное право (автор пишет черновики, редактор публикует).
- `User.View/Add/Update/Delete` слиты в один `User.Manage`.
- `AiChat.Use` входит в пресет Developer.
- Per-user исключений (право в обход роли) нет.
- Проверка: `IAuthorizationHandler` + кэш прав ролей, **не клеймы в токене** — смена прав
  действует сразу, без релогина.
- Видимость WASM-страниц в фазе 1 — по ролям; cap-based UI — фаза 2.

## As-is (проверено 2026-11-17)

- Сидятся 4 роли: Viewer/Admin/Manager/Developer —
  `src/Mars.Modules/Mars.Cms.Host/Seeding/SeedRoles.cs` (только при пустой Roles; вызов из
  `CmsSeedDataHandler`). Manager не даёт ничего (упоминается лишь в resx).
- Вся авторизация — хардкод `[Authorize(Roles = "Admin")]` в ~60 местах (контроллеры +
  razor-страницы); `PluginController` — "Admin, Developer". Сайдбар фильтруется по ролям
  (`AdminLayout`/`CloudyLayout`/`ActionCenter` подмешивают Viewer).
- Мёртвый скелет гранулярных прав:
  - `Mars.Identity.Contracts/ViewModels/RoleCaps.cs` — константы; **баг**:
    `NavMenuCap.Manage = "PostType.Manage"`; `ContactPersonCap` мёртв (сущности нет).
  - `RoleClaimEntity` (IdentityRoleClaim<Guid>) — таблица есть во всех провайдерах.
  - `RoleService.GetClaimsBlankList()` — reflection-построитель списка прав для UI, работает;
    `RoleCapGroupCheckable` — готовые view-модели чекбокс-матрицы.
  - `RoleService.EditRolesViewModel/SaveRoleClaims/HasCap` — `NotImplementedException`
    (region OLD, логика диффа в комментариях).
  - `RoleController` — эндпоинты EditRolesViewModel/SaveRoleClaims закомментированы;
    `RoleServiceClient` — те же методы `NotImplementedException`.
  - `IRoleRepository.ListAllClaims` есть; методов записи claims нет.
  - `AppClaimsPrincipalFactory` клеймы ролей в principal не кладёт; `IAuthorizationHandler`
    в проекте нет ни одного.
- WASM-админка: `AddAuthorizationCore()` без политик —
  `src/Admin/Mars.Admin.Framework/MainAdminFramework.cs:79`.
- **Дыры:**
  - `PostController` Create/Update/Delete — просто `[Authorize]`; в `PostService` нет
    проверки владельца (`PostEntity.UserId` есть, не сравнивается). Любой аутентифицированный
    правит/удаляет чужие посты через API.
  - `GenSourceCodeController` — `[Authorize]`: генерация кода доступна любому.
  - Под аудит: `MediaController` (удаление), `NodesAppEntityController`, `AIToolController`,
    `PostJsonController`, `ViewModelController` (Admin.Host).
- Самообслуживание с проверкой владельца: `AccountController` (прецедент:
  `targetUserId != currentUserId && !User.IsInRole("Admin")`), `ApiKeyController`,
  `PasskeyController`.

## Каталог прав (26)

ClaimType — константа `mars_cap` (рядом с RoleCaps). Хранение: `RoleClaimEntity`
(RoleId, ClaimType = mars_cap, ClaimValue = имя права).

### Посты — `PostController` / `PostService`

| Право | Что разрешает |
|---|---|
| `Post.Add` | создавать посты (свои) |
| `Post.Update` | редактировать **свои** посты |
| `Post.UpdateAny` | редактировать **чужие** посты |
| `Post.Delete` | удалять **свои** посты |
| `Post.DeleteAny` | удалять **чужие** посты |
| `Post.Publish` | менять статус / публиковать (посты, которые можно редактировать) |

Владение — `PostEntity.UserId == IRequestContext.User.Id`, проверка в `PostService`.

### Медиа — `MediaController` (общая библиотека)

| Право | Что разрешает |
|---|---|
| `Media.Add` | загружать файлы |
| `Media.Update` | переименовывать/перемещать/метаданные |
| `Media.Delete` | удалять файлы |

### Структура сайта

| Право | Что гейтит |
|---|---|
| `PostCategory.Manage` | `PostCategoryController` |
| `PostType.Manage` | `PostTypeController` + `PostCategoryTypeController` (типы и мета-поля) |
| `NavMenu.Manage` | `NavMenuController` |

### Пользователи и роли

| Право | Что гейтит |
|---|---|
| `User.Manage` | `UserController` (список + CRUD) |
| `User.UpdateRole` | назначение ролей в `UserController` (эскалация — только Admin) |
| `UserType.Manage` | `UserTypeController` |
| `Role.Manage` | `RoleController` (роли и их права — только Admin) |

### Прочее

| Право | Что гейтит |
|---|---|
| `Feedback.Manage` | `FeedbackController` (отправка остаётся AllowAnonymous) |
| `Settings.Manage` | `OptionController` (все опции) |
| `Notifications.Manage` | `NotificationsController` |

### Тех-зона — пресет Developer

| Право | Что гейтит |
|---|---|
| `Plugin.Manage` | `PluginController` |
| `Flow.Manage` | `NodeController`, `NodeEditorToolController`, `CodeCompletionController` |
| `Datasource.Manage` | `DatasourceController` |
| `Scheduler.Manage` | `SchedulerController` |
| `Front.Manage` | `FrontController` + `AdminFrontController` |
| `AiChat.Use` | `AiChatController` + `AIToolController` |

### Система — только Admin

| Право | Что гейтит |
|---|---|
| `System.Manage` | `SystemController` (админ-методы), `KpiController`, `AppDebugController`, `GenSourceCodeController` |

### Без прав (не гейтим капами)

- `AccountController`, `ApiKeyController`, `PasskeyController` — самообслуживание + владелец.
- `SearchController` — `[Authorize]` как есть.
- SSO/OAuth-контроллеры, `PageRenderController` — протокол/публичный рендер.

## Пресеты

| Роль | Права |
|---|---|
| Admin | все 26 |
| Editor | Post.* (6), Media.* (3), PostCategory.Manage, Feedback.Manage = 11 |
| Author | Post.Add, Post.Update, Post.Delete, Media.Add = 4 |
| Viewer | пусто (публичное чтение и так анонимно) |
| Developer | Plugin.Manage, Flow.Manage, Datasource.Manage, Scheduler.Manage, Front.Manage, AiChat.Use = 6 |

## Фаза 0 — заткнуть дыры (ВЫПОЛНЕНА 2026-11-17)

- [x] `PostService.Update/Delete/DeleteMany/GetEditModel`: `EnsureOwnerOrAdmin` — владелец
  или Admin (роль из БД через `_requestContext.Roles`). DeleteMany — всё-или-ничего.
  **Фоновый контекст (`_requestContext.User is null`) — разрешено** (правки генерала):
  флоу/scheduler без HTTP-пользователя работали до фазы 0, fail-closed их ломал;
  пользовательские HTTP-эндпоинты закрыты `[Authorize]`. В фазе 1 — системный контекст через капы.
- [x] `PostController.GetEditModel` — оказался **анонимным** (дыра больше заявленной),
  добавлен `[Authorize]` + проверка в сервисе. Create/GetEditModelBlank/GetOrCreateSingle не тронуты.
- [x] `GenSourceCodeController` — `[Authorize(Roles="Admin")]` (временно, до `System.Manage`).
- [x] `ForbiddenExceptionFilterAttribute` — новый, `Mars.Server.Abstractions/ExceptionFilters`
  (по образцу NotFoundExceptionFilter, ProblemDetails 403); повешен на `PostController`
  и `PostJsonController` (правки генерала: PostJson.Update делегирует в PostService и давал 500).
- [x] Медиа: `Delete/DeleteMany/MoveFiles` — временно Admin. **Спорное, не тронуто:**
  `RenameFolder/DeleteFolder` — любой аутентифицированный; решить в T3 (кап `Media.Update`).
- [x] Тесты: `PostOwnershipTests` — 11/11; фильтр `~Posts` — 45: 43 passed, 2 skipped (старые).
  **Пробел:** нет теста на фоновый контекст (null user) — покрыть в T3.

Аудит (T1): `NodesAppEntityController`/`NodeEditorToolController` — read-only справочники,
write нет → `Flow.Manage` в T3. `AIToolController` — AI-генерация любому аутентифицированному
(ресурсы, без записи сущностей) → `AiChat.Use` в T3. `PostJsonController` — чтение анонимно
(публичный JSON-рендер), Create — свой, Update — проверка владельца действует автоматически.
`ViewModelController` — анонимный InitialSiteData сознательно (те же данные в хост-странице) —
оставить; эффективные права туда — фаза 2.

## Фаза 1 — инфраструктура прав

Каталог и хранение (ВЫПОЛНЕНО T2, 2026-11-17):
- [x] `RoleCaps`: 26 прав / 18 групп по каталогу; `ContactPersonCap` и `PostCap.CommentCap`
  удалены; `NavMenuCap.Manage` починен; `ClaimType = "mars_cap"` живёт в **Contracts**
  (Abstractions ссылается на Contracts, алиас дал бы цикл). View-модели матрицы не тронуты.
- [x] `IRoleRepository.SetRoleClaims` + реализация в `RoleRepository` — дифф-замена,
  только `ClaimType == mars_cap`, чужие типы не трогает.
- [x] `RoleService`: `EditRolesViewModel` (матрица все роли × 18 групп, Checked по claims),
  `SaveRoleClaims` (неизвестные роли/ids игнорируются), `HasCap` (роли из `IRequestContext`
  + кэш); region OLD удалён; объявления добавлены в `IRoleService`.
- [x] `RoleCapsCache` (singleton, volatile FrozenDictionary + SemaphoreSlim, паттерн
  `UserMetaLocator`, scoped-захват через `IServiceScopeFactory.ExecuteScopedAsync`);
  инвалидация при SaveRoleClaims и Create/Update/Delete роли (rename меняет ключ).
  Ключи — имена ролей, OrdinalIgnoreCase.
- [x] resx: 44 ключа (18 групп + 26 прав) ru/en; ключ права `"{группа}.{право}"` —
  различает одноимённые Manage/Add разных групп (fallback на короткое имя).

API (ВЫПОЛНЕНО T2):
- [x] `RoleController`: GET `EditRolesViewModel` / PUT `SaveRoleClaims` (nameof-маршруты);
  `RoleServiceClient` — оба метода реализованы.
- [x] Тесты T2: `RoleClaimsTests` (7) + `RoleCapsCacheTests` (4); фильтр
  `~Roles|~RoleCapsCache` — 28/28 passed.

Проверка (T3, ВЫПОЛНЕНО 2026-11-17):
- [x] `CapAttribute` (Policy = "Cap:" + cap, константа PolicyPrefix) + `CapRequirement` —
  `Mars.Server.Abstractions/Authorization/`. `CapPolicyProvider` (fallback на default) +
  `CapAuthorizationHandler` (роли из ClaimTypes.Role → кэш, Admin bypass, без auth — fail) —
  `Mars.Identity.Host/Authorization/`; регистрация в MainIdentity (provider singleton, handler scoped).
- [x] Проводка 24 контроллеров по карте каталога. Анонимное сохранено: чтения Post/PostJson/
  PostCategory/PostType/PostCategoryType/UserType, Feedback.Create, Option.GetSiteSettings/
  SetLanguage (публичные опции идут через InitialSiteData), Plugin.RuntimePluginManifests,
  CodeCompletion.FunctionCodeSuggest, System.HealthCheck. Временные Admin фазы 0 сняты.
- [x] `PostService`: `EnsurePostCap` (свой → ownCap||anyCap, чужой → anyCap), `EnsurePublishCap`
  (Create со статусом publish; Update — только переход не-publish→publish; слаг "publish").
  Admin bypass и фоновый null-user сохранены. GetEditModel — сервисная проверка.
- [x] `User.UpdateRole` двухуровнево: эндпоинт UpdateUserRoles + сервисная проверка в
  `UserService` (Create с ролями, Update при изменении набора ролей, UpdateUserRoles всегда;
  UpdateProfile копирует текущие роли — не триггерит). `[ForbiddenExceptionFilter]` на UserController.
- [x] Проектные ссылки: Server.Abstractions → WebApp.Nodes.Host, Scheduler.Host,
  SemanticKernel.Host; Identity.Contracts → CodeCompletion.Host, Scheduler.Host.
- [x] Тесты T3: 25 новых (CapAuthorizationTests 5, PostCapsTests 13, UserRoleCapsTests 4,
  PostServiceBackgroundContextTests 3). Полный прогон: 521 = 513 passed / 4 failed / 4 skipped.
  **4 падения — ожидаемые:** PostOwnershipTests (T1) кодируют фазу-0 «Viewer правит свои»;
  в кап-модели актору нужны Post.Update/Delete → миграция тестов на Author в T4.

**Находка приёмки T3 (бэклог):** публичные чтения постов НЕ фильтруют по статусу —
черновики видны анонимно (существующее поведение). `Post.Publish` гейтит смену статуса,
а не видимость. Фильтрация публичной выдачи по статусу — отдельная задача (фаза 2).

Решения генерала (2026-11-17, при приёме T2):
- **Admin — суперпользователь**: `CapAuthorizationHandler` для роли Admin всегда успешен
  (защита от локаута при редактировании матрицы). Пресет Admin = все 26 всё равно сеется (T4)
  для консистентности матрицы. Сервисные проверки (PostService) тоже обходят Admin.
- **Новые локальные пользователи получают роль Author** (T4) — продуктовое решение
  «обычные юзеры создают свои посты». SSO-пользователи остаются Viewer (внешние — чтение,
  повышает админ). `Post.Create/GetOrCreateSingle` → `[Cap(Post.Add)]`.
- Спорные папки медиа (`RenameFolder/DeleteFolder`) → `Media.Update` (T3).

Сид и миграция (T4, ВЫПОЛНЕНО 2026-11-17):
- [x] `SeedRoles` переработан: 5 пресет-ролей идемпотентно (каждая при отсутствии);
  пресеты капов — только ролям без mars_cap-claims (кастомизации не перетираются);
  Viewer без прав. Пресеты точно по таблице плана.
- [x] Миграция Manager → Editor: `SeedRoles.MigrateManagerToEditor` (вызов до SeedUsers):
  нет Editor — rename (тот же Id); есть — пользователи переносятся (дедуп), claims и
  роль Manager удаляются. Повторный старт — no-op.
- [x] Регистрация: `AccountsService.RegisterUser` → `AddToRoleAsync(user, "Author")`.
  SSO остаётся Viewer, первый админ (SeedUsers) не задет.
- [x] Тесты T4: RoleSeedTests (7) + RegistrationDefaultRoleTests (1); мигрированы
  4 теста инициативы (PostOwnershipTests → Author + Status=draft + Cache.Invalidate;
  RoleClaimsTests/UserRoleCapsTests/CapAuthorizationTests — Manager→Editor, пустой Admin).
  Полный прогон: **529 = 525 passed / 0 failed / 4 skipped** (проверено генералом).
- [x] **Остаток закрыт (санкция пользователя 2026-11-17):** committed-тест
  `Mars.WebApiClient.Integration.Tests/Tests/Accounts/RoleRefreshTests.cs` — механическая
  замена "manager" → "editor" (3 места), логика теста не менялась. Проверка прогоном —
  на финальной приёмке (после T5, чтобы не пересекаться сборками).

UI (T5, ВЫПОЛНЕНО 2026-11-17):
- [x] `src/Mars.Admin/SiteSpace/Pages/RoleViews/`: `RolesPage` (/Roles, Cloudy-список:
  Name/Title, Permissions «n/26» + тег superuser, edit/delete; Create → CreateRoleDialog),
  `EditRolePage` (/EditRole/{Id}: селектор роли, имя + матрица 18 групп × 26 прав
  FluentCheckbox, SaveRoleClaims полной матрицей + rename; Admin — все checked+disabled,
  **имя тоже disabled** (rename сломал бы bypass по имени), Delete скрыт, плашка).
  Стили: `cloudy-roles.less` (+ импорт в style.less).
- [x] Сайдбар: пункт Roles через `DevMenuFactory.Build` (slug dev, Admin-only).
  **Users в дефолтном меню НЕТ** — закомментирован пользователем (4cdea124 «clean menus»);
  Roles стоит один в бывшей зоне «Управление», пользовательское решение не отменено.
- [x] Role-gating 25 страниц: посты Admin,Editor,Author; медиа/категории/фидбек Admin,Editor;
  тех-страницы Admin,Developer; guard CloudyTechLayout Admin,Developer + явные Admin-атрибуты
  на 9 закрытых тех-страницах (Logs/Models/ClassModel/ViewComponents/Playbooks/StyleDesigner/
  DevTest/FormsBuilder/StyleGallery); настройки/юзеры/типы/меню/роли — Admin.
  Кнопка Builder в сайдбаре — AuthorizeView Admin,Developer.
- [x] Live-проверка (харнесс tools/ui, инстанс serve.ps1): UI ролей 24/24 (Admin-lock,
  пресеты, полный цикл Create→toggle→Save→персистентность→Delete), gating 26/26
  (временные Editor/Developer/Author), crawl-регресс 36 страниц ok; тестовые данные убраны.
  crawl.js/assertions.json расширены новыми страницами.

Финальная приёмка (генерал, 2026-11-17): `dotnet build Mars.slnx` чисто;
`Mars.Integration.Tests` — **529: 525 passed / 0 failed / 4 skipped** (старые);
`Mars.WebApiClient.Integration.Tests` — **214/214** (RoleRefresh после manager→editor зелёный).
Diff T1–T5 ревьюено выборочно по слоям. Dev-инстанс после проверки остановлен.

Находки T5 (бэклог/заметки):
- Docker-плагин: его страницы в отдельном репо без своих атрибутов — при загруженном
  плагине Developer получит URL-доступ (guard лэйаута ослаблен); пункт рельса скрыт.
  Капы плагинов — фаза 2.
- Mobile 390px: overflow грида Roles (452px) — системный лимит DataGrid (Users 400,
  FeedbackList 454); mobile-дефекты отложены (CloudyGuide).
- Существующий quirk: `DeleteUserQueryValidator` при единственном Admin блокирует
  удаление любого пользователя (400) — не наш, не чинился.
- Локализация матрицы — по культуре запроса (харнесс invariant → en titles).

Тесты:
- [x] `tests/Mars.Integration.Tests/Controllers/Roles` — SaveRoleClaims/EditRolesViewModel (T2).
- [x] `tests/Mars.Integration.Tests/Services` — кэш/инвалидация (T2, RoleCapsCacheTests).
- [ ] T3: handler (403 без права / 200 с правом / Admin bypass / свежесть после Save),
  Post-капы (Author свой/чужой, Editor чужой, Publish), фоновый контекст (null user).
- [ ] T4: пресеты, миграция Manager, дефолтная роль Author при регистрации.

## Фаза 2 — бэклог

- Регистрация прав плагинами (точка расширения при старте плагина).
- Per-PostType права (`Post.Update.<type>` и т.п.).
- Cap-based UI: эффективные права в InitialSiteData + клиентский policy provider;
  видимость страниц для кастомных ролей.
- `Post.Comment.*` — когда комментарии будут реализованы (в `PostEntity` закомментированы).
- Инвалидация кэша прав между инстансами облака.
- Владение медиа — если появится продуктовая нужда.

## Отклонено

- **Только роли** (фиксированные связки): плагины не объявляют права без правки ядра;
  облаку нужны кастомные роли.
- **Полная матрица сущность × действие × статус**: каталог разрастается; админ-зоны —
  грубые Manage.
- **Per-user исключения**: болото; в WordPress тоже нет.
- **Права клеймами в токене**: смена прав только после релогина; вместо этого handler + кэш.
- **Раздельные User.View/Add/Update/Delete**: слиты в User.Manage (пользователь, 2026-11-17).
- **Пары владения для медиа**: общая библиотека (пользователь, 2026-11-17).

## Проверки

```
dotnet build Mars.slnx
dotnet test tests/Mars.Integration.Tests --filter "FullyQualifiedName~Roles|FullyQualifiedName~Posts"
```

Полный прогон — `test-all.ps1`; харнесс и рецепты — `ai/TestingGuide.md`.