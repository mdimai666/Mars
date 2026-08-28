# Mars — план реструктуризации решения

Статус: черновик по итогам дизайн-раунда 2026-08-27. План «издалека»; перед каждой фазой — углубление в её детали.

## Принятые решения

1. **Суффиксы контрактных проектов:**
   - `Mars.X.Abstractions` — серверный контракт: интерфейсы, базовые классы точек расширения, query-DTO. Ничего тяжёлого (DI-граф, EF) внутрь не класть.
   - `Mars.X.Contracts` — DTO через границу клиент↔сервер (Request/Response), ест фронт и `WebApiClient`. Не должен ссылаться на AspNetCore/EF (компилируется в WASM).
   - `.Shared` и `.Core` как суффиксы контрактов больше не используются.
   - Фронтовые контракты — инфикс `.Front.` (`Mars.X.Front.Abstractions`); уже есть `Mars.Plugin.Front.Abstractions`.
   - Роли-образцы: `.Abstractions` = контракт, `.Kit` (`Mars.Plugin.Kit.Host/Front`) = тяжёлые готовые помощники.
2. **`.Host`** — остаётся конвенцией серверной реализации модуля, `.Front` — фронтовой.
3. **Правило направленности:** модуль ссылается только на чужие `Abstractions`/`Contracts`, ссылки на чужие реализации (`*.Host`) запрещены. Сборка модулей — только в корне композиции (`Mars.WebApp`).
4. **Mars.Host распускается** по модулям: `Mars.Cms`, `Mars.Media`, `Mars.Identity`, `Mars.Notifications`, `Mars.SiteEngine`; остаток — тонкое ядро **`Mars.Server`** (+ `Mars.Server.Abstractions` для сквозных интерфейсов вроде `IRequestContext`, `IActionManager`, `IEventManager`). Каждый модуль регистрируется своим `AddXxx()`.
5. **Рендеринг сайта** — модуль **`Mars.SiteEngine`**: Templators, WebSite-скрипты/ассет-провайдеры, вливание `Mars.WebSiteProcessor`; провайдеры шаблонизации подключаются отдельно. **Слияния `Mars.TemplateEngine.*` с SiteEngine не будет** — SiteEngine будет потреблять TemplateEngine как отдельную подсистему (решение 2026-08-28).
6. **Фронт (упрощённо):** `AppAdmin` → `Mars.Admin`; `AppFront.Main` и `AppFront.Shared` сливаются в **`Mars.Admin.Framework`**. Выделение стабильного ядра и переиспользуемых компонентов (под мобилку и другие проекты) — отложено до появления необходимости.
7. **MarsDbContext не режется.** Контекст остаётся общим; семейство `Mars.Host.Data*` переименовывается в `Mars.Data*`. Модули ходят в контекст напрямую только там, где оправдано (рендер, метагенератор).
8. **Без MediatR.** Остаёмся на репозиториях/сервисах, точечные handler'ы как сейчас; позже — аудит однородности и согласованности.
9. **Ломаем одним мажорным релизом** + миграционный гайд для плагинов (старые сборки/namespace'ы/PackageId → новые).
10. **`src/Modules` не трогаем** (отложено; позже — сортировка по ролям: UI-компоненты / универсальные библиотеки / доменные вещи).
11. **Приоритет:** сначала разрезание `Mars.Host` на модули, затем переименования одним релизом. Изоляция Nodes и `Mars.Nodes.Runtime` — отложены в бэклог.
12. **Опции:** движок опций (`IOptionService`/хранилище/автоформы) — самостоятельный модуль `Mars.Options`; конкретные модели опций разносятся по модулям-владельцам и регистрируются их `AddXxx()` (централизованный `UseMarsOptions` со списком всех опций исчезает).

## Целевая картина

```
ЯДРО И КОНТРАКТЫ
  Mars.Core
  Mars.Contracts                    (быв. Mars.Shared)
  Mars.Server.Abstractions          (сквозные серверные интерфейсы)
  Mars.Server                       (тонкое ядро: пайплайн, middleware, композиция)

ДАННЫЕ (общие)
  Mars.Data / Mars.Data.PostgreSQL / Mars.Data.InMemory
  Mars.Data.Repositories / Mars.Data.Infrastructure

МОДУЛИ ПЛАТФОРМЫ (каждый: [Contracts] [Abstractions] Host [Front])
  Mars.Cms.*          посты, типы, метаполя, категории, меню, feedback
  Mars.Media.*        файлы, медиапапки, галереи
  Mars.Identity.*     пользователи, роли, токены, claims
  Mars.Notifications.* email/sms/notify
  Mars.SiteEngine*    рендер сайта + провайдеры (быв. Templators + WebSiteProcessor)
  Mars.Options.*      (уже существует — снять зависимость от реализации ядра)
  существующие: AiChat, Docker, SemanticKernel, SSO, Scheduler, Excel,
                QueryLang, CommandLine, WebApp.Nodes, MetaModelGenerator

ДОМЕННЫЕ ДВИЖКИ
  Mars.Nodes.*        Nodes.Core / Core.Implements / Abstractions / Host / Front…
  Mars.Datasource.*

ФРОНТ
  Mars.Admin.Framework     (быв. AppFront.Main + AppFront.Shared, слиты)
  Mars.Admin               (быв. AppAdmin, WASM-приложение)

КОМПОЗИЦИЯ
  Mars.WebApp
```

## Карта переименований

| Сейчас | Станет | Комментарий |
|---|---|---|
| `Mars.Core` | без изменений | |
| `Mars.Shared` | **`Mars.Contracts`** | глобальные клиент-серверные DTO |
| `Mars.Host.Shared` | распускается | содержимое — по `*.Abstractions` модулей; сквозное — в `Mars.Server.Abstractions` |
| `Mars.Host` | распускается | сервисы — по модулям; остаток — `Mars.Server` |
| `Mars.Host.Data` | `Mars.Data` | общий контекст, не резать |
| `Mars.Host.Data.PostgreSQL` | `Mars.Data.PostgreSQL` | |
| `Mars.Host.Data.InMemory` | `Mars.Data.InMemory` | |
| `Mars.Host.Repositories` | `Mars.Data.Repositories` | |
| `Mars.Host.Infrastructure` | `Mars.Data.Infrastructure` | аудит содержимого при углублении |
| `Mars.WebApiClient` | без изменений | ссылается на Contracts |
| — | **`Mars.Notifications.Abstractions`**, **`Mars.Notifications.Host`** (новые) | вынесено из `Mars.Host`/`Host.Shared`, фаза 1 |
| — | **`Mars.Cms.Abstractions`** (новый) | мета-контракты, фаза 1 |
| — | **`Mars.Identity.Abstractions`**, **`Mars.Identity.Host`** (новые) | вынесено из `Mars.Host`/`Host.Shared`, фаза 1 |
| `AppAdmin` | **`Mars.Admin`** | |
| `AppFront.Main` | **`Mars.Admin.Framework`** | сливается с `AppFront.Shared` |
| `AppFront.Shared` | → `Mars.Admin.Framework` | слияние; вынос переиспользуемого — позже |
| `Mars.AiChat.Shared` | `Mars.AiChat.Contracts` | |
| `Mars.AiChat.Host.Shared` | `Mars.AiChat.Abstractions` | |
| `Mars.Docker` | `Mars.Docker.Contracts` | |
| `Mars.Docker.Host.Shared` | `Mars.Docker.Abstractions` | |
| `Mars.SemanticKernel.Shared` | `Mars.SemanticKernel.Contracts` | |
| `Mars.SemanticKernel.Host.Shared` | `Mars.SemanticKernel.Abstractions` | |
| `Mars.SSO` | `Mars.SSO.Contracts` | |
| `Mars.CommandLine.Shared` | `Mars.CommandLine.Abstractions` | аудит: серверные контракты |
| `Mars.WebSiteProcessor` | вливается в **`Mars.SiteEngine`** | |
| `Mars.WebSiteProcessor.Handlebars` | `Mars.SiteEngine.Handlebars` | |
| `Mars.TemplateEngine.Host`, `.Providers.*` | без изменений | отдельная подсистема; SiteEngine будет её потреблять, слияния не будет (решение 2026-08-28) |
| `Mars.Nodes.Host.Shared` | `Mars.Nodes.Abstractions` | |
| `Mars.Nodes.Front.Shared` | `Mars.Nodes.Front.Abstractions` | аудит содержимого |
| `Mars.Datasource.Core` | открыто | вероятно `Mars.Datasource` |
| `Mars.Datasource.Host.Core` | открыто | вероятно `Mars.Datasource.Abstractions` |
| `Mars.Scheduler.Host` | имя остаётся | снять ссылку на `Mars.Host` (нарушение правила 3) |
| `Mars.Options` | имя остаётся, роль меняется | движок опций (базовые типы, атрибуты, автоформы) вместо свалки моделей |
| `Mars.Options.Host` | имя остаётся | сюда переезжает `OptionService` + Dto/Options из `Mars.Host`/`Host.Shared`; снять ссылку на `Mars.Host` |
| конкретные модели опций | → к владельцам | `MediaOption`, `FaviconOption`, image → `Mars.Media`; `OpenID*`, `AuthVariantConst` → `Mars.SSO`; `SEOOption` → `Mars.SiteEngine`; `SmtpSettingsModel` → `Mars.Notifications`; `DevAdminStyleOption` → админка; `PluginManagerSettingsOption` → `Mars.Plugin`; `ApiOption`, `MaintenanceModeOption`, `SysOptions` → `Mars.Server`; `FrontsOption` — открыто |
| `Mars.Plugin.*` | без изменений | имена уже соответствуют схеме |
| `src/Modules` | отложено | сортировка по ролям позже |

Решение открытых строк — на этапе углубления в соответствующую фазу.

## Фазы

### Фаза 1 — Разрезание Mars.Host (+ разнос опций)

Тактика больших срезов: файлы переносятся в новые проекты **без смены namespace** — namespace'ы меняются разом в фазе 2 (одним мажорным релизом). Новые файлы (точки регистрации `AddXxx`) сразу пишутся в финальных namespace'ах.

Порядок (фактический):
1. ✅ `Mars.Notifications` (`Abstractions` + `Host`): email/sms/notify.
2. ✅ `Mars.Cms.Abstractions` — мета-контракты (`Dto/MetaFields`, `IMetaValueUniquenessProvider`, `MetaValueOwnerCatalog`, мета-утилиты): фундамент, нужен всем владельцам мета-значений (пользователи, файлы, посты).
3. ✅ `Mars.Identity` (`Abstractions` + `Host`): users/roles/usertypes/accounts/tokens/claims + `IRequestContext`; SSO-контракты из `Host.Shared/SSO` переехали в `Mars.SSO`. Валидаторы/маппинги/`RequestExtensions` временно остались в `Host.Shared` (зависят от его остатков) — разносятся по ходу следующих срезов и в фазе 2.
4. ✅ `Mars.Media` (`Abstractions` + `Host`): files/mediafolders/galleries. `IFileStorage` остаётся общесистемным (уйдёт в ядро), `FileHostingInfo` уехал с медиа-контрактами; медиа-опции — на шаге движка опций; фавикон-цепочка — в SiteEngine.
5. ✅ `Mars.Cms` (`Abstractions` пополнен + `Host`): posts/posttypes/categories/navmenus/feedback + мета-движок + поиск. `UserMetaLocator` уехал в `Identity.Host`; keyed-провайдеры уникальности и `UserRelationModelProviderHandler` — в `Cms.Host`; инлайн-валидаторы из DTO вынесены отдельными файлами в `Host.Shared` (сканирование сборки). `InitialSiteDataViewModelHandler` временно остался в `Mars.Host` (владелец — ядро, решится на шаге 8).
6. ✅ `Mars.SiteEngine` — рендер-подсистема влита в `Mars.WebSiteProcessor` (переименование в `Mars.SiteEngine` — фаза 2): скрипты/ассеты сайта (`WebSite/Scripts` + `SiteScriptsBuilder`), темплейтор-функции и локатор, фавикон-цепочка; регистрации в `AddMarsWebSiteProcessor`/`UseMarsWebSiteProcessor`; embedded-ресурс сохранён с замороженным логическим именем. `Options.Host` больше не ссылается на `Mars.Host`. Рендер-контракты (`PageRenderContext`, `XInterpreter` и др.) остались в `Host.Shared` до роспуска — ими пользуются QueryLang/Nodes. TemplateEngine-подсистема самодостаточна, не тронута.
7. ✅ Движок опций — самостоятельный модуль: `IOptionService`/`OptionService`/`Dto/Options`/`IOptionRepository`/`OptionNotRegisteredException` в семействе `Mars.Options`; `FileHostingInfo` уехал в `Mars.Shared` (разрыв цикла). Каталог `RegisterOption` пока централизован в `UseMarsOptions` (это стартап модуля опций); разнос регистраций по `AddXxx()` владельцев — отдельный шаг, когда появятся модульные Use-хуки. Модели опций физически в `Mars.Options`, разнос по владельцам — вместе с шагом выше.
8. ✅ Ядро «похудело» само по себе: в `Mars.Host` остались только сквозные вещи (EventManager, XActionManager, ActionHistoryService, AI-локатор, валидатор-фактории, `UseFileStorages`, `InitialSiteDataViewModelHandler`, feature-gates, EF-расширения) + мёртвый код. Физический переезд остатка в `Mars.Server` делаем в фазе 2 вместе с общим переименованием (не плодим лишние смены сборок до мажорного релиза).
9. Починить нарушения: `Scheduler.Host` и `Options.Host` больше не ссылаются на реализацию ядра. (`Options.Host` — исправлено в шаге 6.)

Временные связки (снять по ходу срезов):
- `Mars.Identity.Host → Mars.Cms.Host` (+ `InternalsVisibleTo`): статический вызов `PostService.EnrichWithBlankMetaValuesFromMetaValues` в `UserService` — helper уйдёт в `Cms.Abstractions`.
- `Host.Shared → Identity.Abstractions/Cms.Abstractions`: остаточные валидаторы/маппинги и рендер-контракты (`RenderContextUser`) — рассасываются по ходу срезов.
- `Mars.Media.Host → Mars.Host`: `EfEntityBuilderExtensions` для полумёртвого `GalleryService` — уйдёт при разборе галерей.

Развязка `Mars.Host.Shared → Mars.Nodes.Core` происходит по ходу роспуска `Host.Shared` (типы уезжают с владельцами).

### Фаза 2 — Переименование (одним мажорным релизом)
1. Контрактные проекты по карте (`.Shared`→`.Contracts`, `.Host.Shared`→`.Abstractions`).
2. Ядро/данные/фронт по карте (`Mars.Data*`, `Mars.Contracts`, `Mars.Admin*`).
3. Namespace'ы, `Mars.slnx`, папки решения, PackageId, `PluginExample`, devstands, тесты, упоминания в доках.
4. Миграционный гайд для плагинов: таблица «старая сборка/тип → новые», версия-мажор.

### Фаза 3 — Отложенное (бэклог)
- Изоляция Nodes: развязка остаточных связей (`Nodes.Core.Implements → Host.Shared`, `Nodes.Host.Shared → Host.Shared`) → `Mars.Nodes.Runtime` (движок нод без UI и без CMS) и альтернативные конфигурации сборок (например, инстанс без админки).
- `src/Modules`: сортировка по ролям (UI-компоненты / универсальные библиотеки типа `HttpSmartAuthFlow` / доменные вещи).
- Фронт: выделение стабильного ядра и переиспользуемых компонентов из `Mars.Admin.Framework` (под мобилку и другие проекты со своей WASM-мордой), варианты — `Mars.Ui.*` / тонкое ядро; там же распутать зависимость `Nodes.Workspace` от каркаса админки.
- Аудит однородности сервисов/репозиториев/handler'ов (без MediatR).
- Владение сущностями общего контекста (регистрации конфигураций из модулей) — если потребуется.
- Чистка имён: опечатки (`MetaValueRequestExternsions`, `AuthCreditionals`, `FileSizeExstension`), `MockClass2`, и т.п. — **выполняется как отдельный проход**, см. секцию «Чистка имён (опечатки)».
- `SiteSettings`: тип переименован (`SysOptions`→`SiteSettings`), но свойства-носители и точки потребления остались `SysOptions` ради сохранения wire — переименовать их под тип в следующее ломающее окно: `InitialSiteDataViewModel.SysOptions`, `AppInitialViewModel.SysOptions`, `PageRenderContext.SysOptions`, `Q.Site.SysOptions`, `SysOptionsParamKey`, шаблоны `{{{SysOptions.*}}}`, API-путь/методы `SysOptions`, компонент `SysOptionsEditForm`.

## Чистка имён (опечатки) — план прохода (2026-08-28)

Статус: ✅ выполнен (2026-08-28). Окно: мажорный релиз 0.8.0 ещё не закрыт.

Принципы: меняются только имена типов/методов/файлов/тестов и пользовательские надписи; свойства сериализуемых DTO не трогаются (wire-формат не ломается); неймспейсы не меняются (опечаток в них нет).

### Ярус 1 — типы и файлы

| Сейчас | Станет | Где |
|---|---|---|
| `MetaValueRequestExternsions` | `MetaValueRequestExtensions` | `Mars.Cms.Abstractions/Dto/MetaFields/` (+ файл) |
| `AuthCreditionalsRequest` | `AuthCredentialsRequest` | `Mars.Contracts/Contracts/Auth/` (+ файл) |
| `AuthCreditionalsDto` | `AuthCredentialsDto` | `Mars.Identity.Abstractions/Dto/Auth/` (+ файл) |
| `AuthCreditionalsModel` | `AuthCredentialsModel` | `Mars.Admin/Pages/Public/LoginForm.razor.cs` |
| `FileSizeExstension` | `FileSizeExtension` | `Mars.Core/Extensions/` (+ файл) |
| константа `AllowExternsionsDefault` | `AllowExtensionsDefault` | `FluentMediaFilesList.razor.cs` (без ссылок — можно удалить) |
| `CreateSeperatorLine` (private) | `CreateSeparatorLine` | `Mars.Core/Utils/ConsoleTable.cs` |
| `ModelProperySel` | `ModelPropertySel` | `Mars.Admin.Framework/Services/ModelInfoService.cs` |
| `ExcelRespone` (интерфейс + реализации) | `ExcelResponse` | `Mars.Excel.Abstractions`, `Mars.Excel.Host`, `FeedbackController` |
| `initalCommands` / `_initalCommands` | `initialCommands` / `_initialCommands` | `Mars.CommandLine/CommandLineApi.cs` |
| `MockClass2.cs` | удалить | `Mars.Server/Options/` — пустой файл |
| надпись `Creditionals` | `Credentials` | `Mars.Nodes.FormEditor/EditForms/AuthFlowConfigNodeForm.razor` |

### Ярус 2 — имена тестов

| Сейчас | Станет |
|---|---|
| `Login_InvalidCreditional_Fail` | `Login_InvalidCredentials_Fail` |
| `ToJson_ProperyCaseMustLower_ExpecetValueNameLower` | `ToJson_PropertyCaseMustLower_ExpectedValueNameLower` |
| 3 имени тестов с `Retrive…` | `Retrieve…` (Test.Mars.Server, Mars.Nodes.Implements.Test) |
| локальная переменная `messsage` | `message` (`tests/Mars.Integration.Tests/Extensions/FlurlExtensions.cs`) |

### Ярус 3 — строки и комментарии (косметика)

- Сообщения исключений «Retrived»/«Retrive PostType» → «Retrieved» (`PostTransformerTests`, `BlockEditor1PostContentProcessor`).
- Док-комментарий `/// Retrive file list by path` → «Retrieve» (`FileListUtility.cs`), комментарий «//Act - retrive jump Link» (`KeycloakSSOClientTests`).
- Комментарий «BOOL attrubute» → «attribute» (`BlazoredHtml.razor.cs`).
- Тестовая строка `"invalid_passwrod"` → `"invalid_password"` (`BearerTokenStrategyTests`).

### Семейство Standart → Standard

- Файлы+классы: `StandartEditForm1`, `StandartEditContainer` (`Mars.Admin.Framework/Components/`, вместе с `.razor`/`.razor.cs`) → `StandardEditForm1`, `StandardEditContainer`; ~40 использований в страницах `Mars.Admin`; CSS-классы в разметке компонентов (`StandartEditForm1-main` и т.п.) тоже.
- CSS: `.layout-standart-title` → `.layout-standard-title` в `src/Mars.Admin/wwwroot/css/layout.less` (правится только .less, компилирует пользователь) + использование в `ContentWrapper.razor`.
- Комментарии «Standart…» — вместе с Ярусом 3.

### Удаление мёртвого «Zayavka» (решение пользователя: удалить всё связанное)

- `ViewUserPage.razor`: убрать стат-карточку «Заявок» с `@user.ZayavkaCount` (определения свойства в репо нет).
- `EditPostView.razor`: удалить закомментированный блок с `<ZayavkaFileUploadViewTmp>`.
- `FileEntity.cs:88`: удалить закомментированную строку `//    ZayavkaReport,`.
- `NotifyService.cs:161`: удалить закомментированную строку с `zayavka.Id`.
- `AppRes.resx` + `AppRes.ru.resx` + `AppRes.Designer.cs`: удалить ресурсы `Zayavka`, `Zayavka.many` (использований нет).

### Не трогаем

- `ETotalResponeResult` — используется в `QueryLang.Host`/`Media.Host`, но определение в репо не найдено (вероятно, внешний пакет); при углублении проверить.
- `_appsettings.Local.json` — локальная регистрация плагина `ZayavkaHostPlugin`; файл вне git, чистит пользователь сам.

### Верификация

Точечно: сборка `dotnet build Mars.slnx` + тесты затронутого контракта логина (`Mars.WebApiClient.Integration.Tests` — LoginAccountTests, `Mars.Integration.Tests` — AccountControllerTests). Без контрольных прогонов всего набора.

### Итог выполнения (2026-08-28)

Всё по карте выполнено. Сверх карты найдено и исправлено: `Account_LoginValidRequest_ShuldSuccess` и `WebPage_CorrectParse_ShuldSuccess` → `ShouldSuccess`. `AllowExternsionsDefault` переименован (не удалён). `dotnet build Mars.slnx` — 0 ошибок; точечные тесты логина — 4/4 зелёные (оба прогона с Testcontainers PostgreSQL). Осталось за пользователем: скомпилировать `layout.less` → `style.css`; убрать регистрацию `ZayavkaHostPlugin` из локального `_appsettings.Local.json` (вне git).

## Верификация (на все фазы)

Точечно: сборка + тесты затронутых клиентов/эндпоинтов. Без контрольных прогонов всего набора тестов.

## Риски

- **Плагины:** смена имён сборок и namespace'ов ломает скомпилированные плагины (грузятся в рантайм). Митигируется мажорным релизом + гайдом + проверкой версий при загрузке.
- **Объём переименований:** делать скриптом/пакетно по одной карте, не растягивать на месяцы.
- **Единый контекст `Mars.Data`:** модули видят чужие сущности — держать в рамках правила «доступ через свои сервисы».
- **WASM-граница:** в `Contracts` не должны попадать серверные зависимости.
- **Сцепление фронта:** до выноса (бэклог) все фронты, включая `Nodes.Workspace`, зависят от цельного `Mars.Admin.Framework`.

## Вне рамок

MediatR/CQRS-фреймворки; разрезание `MarsDbContext`; локализация; функциональные изменения; `Mars.Nodes.Runtime` и альтернативные конфигурации сборок (до бэклога); `src/Modules` (до бэклога).

## Статус фазы 2 — ✅ выполнена (2026-08-27)

Фаза 2 (переименование одним мажорным релизом) завершена. Ветка: `ai/restructure-phase1`.

**`dotnet build Mars.slnx` — зелёный (0 ошибок).** Точечные тесты зелёные: **569 пройдено** (Test.Mars.Core 46, Test.Mars.Server 413, Test.Mars.SiteEngine 61, лёгкий фронт-набор Mars.Integration.Tests 49 — без Docker). `MarsAppVersion` → `0.8.0-alpha.1`. Ссылки в `docs/` обновлены.

Что сделано:
1. **Фаза A — структурные переезды** (сборка была зелёной до переименований):
   - `Mars.Host.Shared` распущен: контракты разнесены по `*.Abstractions` модулей, сквозные — в новый `Mars.Server.Abstractions`; `IFileStorage`/`FileStorage`/`InMemoryFileStorage`, `MarsLogger`, `RouteUtil` → `Mars.Server.Abstractions`; `ITemplateEngine`/`ITemplateManager` → `Mars.Core` (ns `Mars.Core.TemplateEngine`); `Dto/Common` (`BasicListQuery`) → `Mars.Contracts`, EF-расширения пагинации → `Mars.Data.Extensions`.
   - `Mars.Host` → новый `Mars.Server` (тонкое ядро) + `Mars.Server.Abstractions`; `MainMarsHost` остался в `Mars.Server` (переименование метода `AddMarsHost` → `AddMarsServer` не делалось — только неймспейсы).
   - Регистрация валидаторов разнесена: `ValidatorFactory.AddValidatorsFromAssembly` вызывается в `AddMarsCms`/`AddMarsIdentity`/`AddMarsMedia`; `MainServer` только регистрирует `IValidatorFactory`.
   - `PostService.EnrichWithBlankMetaValuesFromMetaValues` + `GetBlankMetaValue` вынесены в `Mars.Cms.Abstractions/MetaValuesEnricher.cs`; временная связка `Identity.Host → Cms.Host` (+IVT) снята.
   - Фронт: `AppFront.Shared` слит в `AppFront.Main` (позже `Mars.Admin.Framework`); два `MainAppFrontShared` объединены.
   - SSO разрезан: `Mars.SSO` → `Mars.SSO.Contracts` (контракты), реализация → новый `Mars.SSO.Host`.
   - Новые проекты: `Mars.Server`, `Mars.Server.Abstractions`, `Mars.SiteEngine.Abstractions`, `Mars.Scheduler.Abstractions`, `Mars.Excel.Abstractions`, `Mars.SSO.Host`.
2. **Фаза B1 — переименование проектов** по карте + тесты: `Test.Mars.Host` → `Test.Mars.Server`, `TestApp.Mars.Host.Data` → `TestApp.Mars.Data`, `Test.Mars.WebSiteProcessor` → `Test.Mars.SiteEngine`. PackageId/Product/IVT/`Mars.slnx` обновлены.
3. **Фаза B2 — namespace'ы**: скриптовая перезапись деклараций (правило: новый неймспейс = корень проекта-назначения + хвост старого), глобальные подстановки для однозначных неймспейсов, type-driven починка using'ов для расщеплённых (`Mars.Host.Services`, `Mars.Host.Shared.Services` и др.), свип остатков старых токенов, ручные фиксы ~25 раундов. `Mars.WebApp` и `Mars.Integration.Tests` получили новые `GlobalUsings.cs`.

Решения по открытым строкам карты (приняты в ходе выполнения):
- `TemplateEngine` НЕ сливался с SiteEngine: подсистема осталась как была, интерфейсы переехали в `Mars.Core`. **Решение (2026-08-28): слияния не будет** — SiteEngine будет использовать TemplateEngine как внешнюю подсистему.
- `ViewModels/*` и identity-DTO — 2026-08-28 созданы `Mars.Identity.Contracts` и `Mars.Server.Contracts`: viewmodels (`EditUserViewModel`/`EditRolesViewModelDto`/`RoleCaps` → `Mars.Identity.Contracts.ViewModels`; `InitialSiteDataViewModel`/`StatisticPageViewModel` → `Mars.Server.Contracts.ViewModels`; `IViewModelService` → `Mars.Admin.Framework.Services`), затем весь identity-слой: `Users`/`Roles`/`Auth`/`UserTypes` → `Mars.Identity.Contracts`, `SsoUserInfoResponse`/`SsoProviderItemResponse` → `Mars.SSO.Contracts.Dto`, `UserPrimaryInfo` → `Mars.Identity.Contracts.ViewModels`.
- `Mars.SSO.Contracts` приведён к WASM-правилу: wire-DTO только там; серверные интерфейсы (`ISsoService`/`ISsoProvider`) вынесены в новый `Mars.SSO.Abstractions` (FrameworkReference + Identity.Abstractions). Причина: фронт начал есть SSO-контракты → FrameworkReference/Identity.Abstractions ломали WASM-сборку (`NETSDK1082`).
- `Mars.Cms.Contracts` — 2026-08-28: CMS wire-DTO (`Feedbacks`, `MetaFields`, `NavMenus`, `PostCategories`, `PostCategoryTypes`, `PostJsons`, `Posts`, `PostTypes`, `Search`; 52 файла, ns `Mars.Cms.Contracts.*`) → новый `Mars.Cms.Contracts` (ссылается на `Mars.Contracts`). Прямые потребители: `Cms.Host`/`Cms.Abstractions`, `WebApiClient`, `Admin.Framework`, `Identity.Contracts`, `Server.Contracts`; остальные (Data.Repositories, Mars.Server, AiChat.Host, SemanticKernel.CMS, WebApp.Nodes.Host, тесты) — транзитивно. `Renders`/опции/плагины/шедулеры — в `Mars.Contracts` до своих срезов.
- `Mars.Media.Contracts` — 2026-08-28: `Files`/`Galleries` (4 файла, ns `Mars.Media.Contracts.Files`/`.Galleries`) → новый `Mars.Media.Contracts`. Прямые потребители: `Media.Host`/`Media.Abstractions`, `WebApiClient`, `Admin.Framework` (медиа-компоненты). `GalleryService` в Admin.Framework исключён из компиляции (полумёртвый, разбор галерей — отдельный шаг).
- Мелкие срезы 2026-08-28: `ActionHistoryLevel` → `Mars.Server.Abstractions` (root ns); `Systems` → `Mars.Server.Contracts`; новый **`Mars.Scheduler.Contracts`** (шедулер-DTO; Scheduler.Abstractions тянет EF → не WASM-safe); `SendSmsModelRequest` → `Mars.Notifications.Abstractions`; `AIService` → `Mars.SemanticKernel.Contracts`; новые **`Mars.SiteEngine.Contracts`** (WebSite/Renders-DTO; SiteEngine.Abstractions не WASM-safe) и **`Mars.Plugin.Contracts`** (плагин-DTO; Plugin.Abstractions не WASM-safe).
- Опции 2026-08-28: `SysOptions` → `Mars.Server.Contracts.Options`; `SmtpSettingsModel`/`TestMailMessage` → `Mars.Notifications.Abstractions`; **`OptionRequest`/`OptionResponse` → новый `Mars.Options.Contracts`** (wire-DTO опций; выделен из-за цикла `Mars.Options ↔ Server.Contracts` — движок опций остаётся в `Mars.Options`); `FrontsOption`/`FrontItem` остаются в `Mars.Contracts` (их ест WASM-админка + рендер). Namespace `Mars.Contracts.Options` разрезан скриптом по типам + ручные фиксы (ловился `FrontItem` — тип-сосед `FrontsOption`).
- `Mars.Datasource.Core` → `Mars.Datasource`; `Mars.Datasource.Host.Core` → `Mars.Datasource.Abstractions`.
- `FrontsOption` остался в `Mars.Contracts` (его ест WASM-админка).
- `IFileStorage` → `Mars.Server.Abstractions` (не в Core — зависимость от `FileHostingInfo`, который в `Mars.Contracts`, иначе цикл/WASM-граница).
- Embedded-ресурс `Mars.Host.Options.BlazorScriptsAppend.html` заморожен (логическое имя не меняли).
- Известная шероховатость: `Mars.SiteEngine.Abstractions/Mappings/WebSiteParts/WebSitePartsMapping.cs` объявлял чужой неймспейс `…Mappings.NavMenus` — исправлен на `…Mappings.WebSiteParts`.

**Инцидент (важно для продолжения):** первый прогон скрипта B2 был сломан (баг PowerShell: массив пар защиты развернулся в строку → глобальная подстановка `'M'→'a'` + порча кодировок кириллицы в ~2900 файлах). Восстановление: `git checkout -- src tests devstands benchmarks` + `Mars.slnx` (одобрено пользователем), повтор ручных правок фазы A, перезапуск исправленного B2. ВСЕ отслеживаемые файлы восстановлены. **Не отслеживаемые/игнорируемые локальные данные остались повреждёнными** (см. ниже).

Итог по пунктам «Осталось»:
1. **Локальные данные** (`data/nodes/flows*.json`, `wwwroot/upload/*.json`, `_appsettings.Local.json`) — **НЕ чинятся по решению пользователя** (починит сам). Повреждение детерминировано: заглавная `M` → `a` (например `Mars.` → `aars.`, `MyMars…` → `ayaars…`). Резервная копия: `C:\Users\D\.qwen\tmp\mars-data-backup`.
2. **TODO-RESTRUCTURE-маркеры** — ✅ убраны (5 файлов: 4 в `src` + `devstands/StandNodesApp/Program.cs`). Реальные using'и уже были на месте и корректны (сборка зелёная); маркеры — лишь остаточные комментарии скрипта.
3. **Дедупликация using'ов** — не делалась (опционально, часть предупреждений).
4. **Точечные тесты** — ✅ зелёные (569, см. выше). Docker-набор `HandlebarsAppFrontTests` не запускался (не требовался).
5. **Миграционный гайд для плагинов** — **НЕ делается по решению пользователя**.
6. **Версия** — ✅ `0.8.0-alpha.1`.
7. **Доки** — ✅ упоминания в `docs/` обновлены (XActions, CreateFirstNode, HandlebarsAppFront, Expressions); README старых имён не содержал.
8. **Коммит** — ждёт явной команды пользователя (не коммитить без указания).
9. **Остаточные ссылки (дочищено)**: в `Mars.Identity.Host.csproj` убрана битая ссылка на удалённый `Mars.Host.Shared` (warning MSB9008) и временная `Mars.Cms.Host` (добавлена прямая `Mars.Cms.Abstractions`); в `Mars.Cms.Host.csproj` снят временный IVT для `Mars.Identity.Host`. Временная связка `Identity.Host → Cms.Host` теперь реально полностью снята.
10. **Переносы вокруг `namespace` (починено)**: скрипт B2 «съел» пустые строки вокруг file-scoped деклараций `namespace …;`. Нормализовано скриптом по всем `.cs` (src/tests/devstands/benchmarks) к ровно одной пустой строке до и после — изменён 1123 файла. Детекция переносов после — 0 проблем; сборка и точечные тесты зелёные. **Побочный эффект: фикс читал файлы как UTF-8 и повредил один файл в Windows-1251 — см. пункт 11.**
11. **Кодировки — финальная проверка (обязательно в конце, перед релизом)**: скриптовые правки фазы 2 читают файлы как UTF-8; файл в другой кодировке при этом молча портится — кириллица заменяется на U+FFFD. Прецедент: `src/Mars.Contracts/Contracts/SSO/IdTokenModel.cs` (был в Windows-1251; фикс переносов его испортил, восстановлен из git, переведён в UTF-8 без BOM, неймспейс `Mars.Contracts.SSO` поправлен пользователем вручную). **В конце просканить все файлы на два признака:** (а) байты ≥0x80, не образующие валидный UTF-8 → файл в чужой кодировке (прочитать как Windows-1251/CP1251 и пересохранить в UTF-8 без BOM); (б) последовательность байт `EF BF BD` (U+FFFD) → испорченная кириллица (восстанавливать из git). На 2026-08-27 среди `.cs`/`.razor` других проблемных файлов не найдено.

## Разнос опций по владельцам (UseMarsOptions-хуки) — ✅ выполнен (2026-08-28)

- Модели разъехались по контрактным пакетам владельцев: `MediaOption` → `Mars.Media.Contracts.Options`; `SEOOption`, `FaviconOption`(+Gen) → `Mars.SiteEngine.Contracts.Options`; `PluginManagerSettingsOption` → `Mars.Plugin.Contracts.Options`; `ApiOption`, `MaintenanceModeOption` → `Mars.Server.Contracts.Options`; `DevAdminStyleOption` → **новый `Mars.Admin.Contracts`**; `OpenIDClientOption`, `OpenIDServerOption`, `AuthVariantConstOption` → `Mars.SSO.Contracts.Options`. `Mars.Options/Models` и `Interfaces/IImageConverOption.cs` удалены; изображенческие контракты (`IImageConverConfig`, `IProcessImageResult`, `ImageConverConfig`, `ProcessImageResult`) → `Mars.Media.Contracts`.
- Регистрации — по Use-хукам владельцев: ядро (`UseMarsServerOptions` в `MainMarsHost`, вызов из `UseMarsHostServices` — до SeedData), `UseMarsMedia`, `UseMarsNotifications`, `UseMarsWebSiteProcessor` (+ фавикон-хук переехал из `Options.Host` в SiteEngine), `UsePlugins`, `UseDevAdmin`. `UseMarsOptions` схлопнут до движка (`IOptionService` + `IFrontRequestHandler` + `FrontsOption`).
- Спецкейсы `OptionService` удалены: `SysOption`, `RobotsTxt()`, `MailSettings`, `SaveSmtpSettings` (+ члены интерфейса); потребители переведены на `GetOption<T>()`; инвалидация `_fileHostingInfo` по типу сохранена.
- **`SysOptions` → `SiteSettings`** — переименован тип и файл; имена свойств и свойства-носители (`Q.Site.SysOptions`, `PageRenderContext.SysOptions`, шаблоны `{{{SysOptions.*}}}`, API-путь `SysOptions`) сохранены — wire не меняется; ключ хранения в БД меняется на «SiteSettings» (старая строка сиротеет — приемлемо на 0.8.0-alpha). `Mars.Options.Host` отпустил ссылку на `Mars.SiteEngine`, получил прямые `Mars.Server.Contracts` + `Mars.SiteEngine.Abstractions`; движок отпустил `Mars.Server.Contracts` + `Mars.Notifications.Abstractions`.
- **Seed** — логика `AppDbContextSeedData.SeedFirstOption` вынесена в `ISeedFirstOptionHandler`/`SeedFirstOptionHandler` (Mars.Server, регистрация в `AddMarsHost`); Setup-конфиг читается через DTO `SetupSiteConfig`; неиспользуемый `MarsDbContext` из сигнатуры убран.
- URL-ссылки на страницы опций обновлены на новые FullName (`ManageMediaPage`, доки SSO ×4).

Верификация: `dotnet build Mars.slnx` — 0 ошибок. Точечно: Test.Mars.Server (OptionService/OptionReaderTool) 36/36; Mars.Integration.Tests (Controllers.Options/Medias/Plugins + Modules.SSO) 37/37; WebApiClient.Integration.Tests (Options + RegisterAccount) 13/13; Mars.Plugin.Integration.Tests 7/7; Test.Mars.SiteEngine (рендер/QueryLang) 8/8. Docker-набор не гонялся (не требовался). Коммит — по команде пользователя.

Открытые хвосты: `UserService.cs` — закомментированная строка со старым `SysOption`.

Правило верификации (напоминание): только точечные тесты затронутых областей, без контрольных прогонов всего набора.

## Фронтовая подсистема → SiteEngine (FrontsOption, MarsAppFront) — ✅ выполнен (2026-08-28)

Принцип (пользователь): «всё фронтовое — в SiteEngine».

- **`FrontsOption` + `FrontItem`** → `Mars.SiteEngine.Contracts.Options` (WASM-safe; едят админка, рендер, `IFrontManager`). `Mars.Contracts/Options/` удалён (был только `FrontsOption`).
- **`MarsAppFront`** → `Mars.SiteEngine.Abstractions.Models` (это рендер-понятие: создаёт `WebRenderEngineLocator`, едят конвейер и `IFrontRequestHandler`). Поле `Front : FrontItem` осталось как есть — `SiteEngine.Abstractions` уже ссылается на `SiteEngine.Contracts`, DTO не понадобился. `Server.Abstractions` потерял фронт-модель (единственная связь «ядро → фронт-модель» снята).
- **Регистрация**: `FrontsOption` ушёл из `UseMarsOptions` в новый **ранний** хук `UseMarsSiteEngineOptions` (регистрация до `MigrateAppFrontToOption`/`EnsureDefaultFront`/`IFrontManager`, т.к. поздний `UseMarsWebSiteProcessor` не успевает). Туда же консолидированы `SEOOption`/`FaviconOption`(+хук)/`FaviconOptionGenaratedValues`; `UseMarsWebSiteProcessor` оставлен только под `UseSiteScriptsBuilders`. `UseMarsOptions` **удалён** (движок больше не владеет опциями) — вызов в `MarsWebAppStartup` убран, `MigrationCommandCli` переведён на `UseMarsServerOptions` (ему для `SeedData` нужен `SiteSettings`).
- Ссылки: `Mars.Admin` + прямая `SiteEngine.Contracts`; потребителям `MarsAppFront` добавлен `using Mars.SiteEngine.Abstractions.Models` (рядом с `Server.Abstractions.Models`, где остались `WebClientRequest` и др.).

Верификация: `dotnet build Mars.slnx` — 0 ошибок. Точечно: Test.Mars.SiteEngine 61/61; Mars.Integration.Tests (FrontManager/FrontRender/AiFrontFiles/HandlebarsEngineCache/WebTemplateService + Controllers.Options/PageRenders) 56/56. Docker-набор не гонялся. Коммит — по команде пользователя.

**`FrontManager` → SiteEngine (2026-08-28, в том же направлении):** реализация `FrontManager` переехала `Mars.WebApp/Services` → `Mars.Modules/Mars.SiteEngine/Services` (namespace `Mars.SiteEngine.Services`), регистрация `AddSingleton<IFrontManager, FrontManager>` перенесена из `StartupFront.AddFront` в `AddMarsWebSiteProcessor`. Потребителям статиков (`FrontManager.IsValidSlug`/`AdminFrontSlug`/`FrontsDirName`) добавлен `using Mars.SiteEngine.Services`. Верификация: сборка 0 ошибок; Test.Mars.SiteEngine 61/61; Mars.Integration.Tests (фронтовые + PageRenders) 49/49.

Открытые хвосты: ~~фронтовый сетап (`MigrateAppFrontToOption`/`EnsureDefaultFront`) и сервисы `FrontFilesService`/`FrontTemplateService`/`FrontRenderWarmupService` пока в `Mars.WebApp`~~ — ✅ уехали в SiteEngine (`Services/AppFrontMigration.cs`, `FrontFilesService`, `FrontTemplateService`, `FrontRenderWarmupService`).

**Фронтовый пайплайн запроса → SiteEngine (2026-08-29):** `Mars.WebApp/UseStartup/StartupFront.cs` удалён; весь пайплайн (robots.txt, резолв `MarsAppFront` в `HttpContext.Items`, конвейер `IFrontRequestHandler`, статика фронтов, `/api`-fallback, fallback-рендер) переехал в `Mars.Modules/Mars.SiteEngine/MarsSiteEngineFrontStartup.cs` как `UseMarsSiteEngineFront()` — вызывается последним, как раньше `UseFront()` (порядок не менялся). `builder.AddFront()` (обёртка над `AddWREHandlebars`) схлопнут в прямой вызов `builder.AddWREHandlebars()`. Маркер в `HandlebarsAppFrontApplicationFixture` переключён на `MarsSiteEngineFrontStartup.UseMarsSiteEngineFront`. В `Mars.WebApp` фронтового кода больше нет. Верификация: сборка 0 ошибок; лёгкий фронтовый набор + `GetPageRenderTests` 55/55; Docker-регрессия рендера `HandlebarsAppFrontTests` 17/17. Коммит — по команде пользователя.

Следующие кандидаты из `Mars.WebApp` (обсуждено 2026-08-29): CLI по владельцам (`UserCommandCli`/`RoleCommandCli` → Identity.Host, `OptionCommand` → Options.Host, паттерн `ICommandLineApi.Register<T>()`); ядро в `Mars.Server` (`MarsStartupInfo`, `MarsSystemService`, `LocalizerXmlResLoaderFactory`, миграционно-сидовая оркестрация + `MigrationCommandCli`); `RenderPageNodeImpl` → `WebApp.Nodes.Host`; далее — сиды по модулям, ревизия `AddMarsHostServices`, XActions (каталог упирается в типы страниц админки под `#if !NOADMIN`), хостинг админки (`StartupDevAdmin` + `StartupHostFiles` + `_AdminHost.cshtml`). Setup-визард — открыт (оставить в хосте или выделить в `Mars.Setup`).
