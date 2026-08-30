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
- ~~`SiteSettings`: wire-имена под тип в следующее ломающее окно~~ — ✅ выполнено 2026-08-30 без алиаса (см. секцию «`SysOptions` → `SiteSettings` — финальный wire-переезд» в конце файла); свойства-носители (`PageRenderContext`, `Q.Site`, viewmodels) были переименованы ранее.

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

- ~~`ETotalResponeResult`~~ — ✅ удалено (2026-08-30): определение в репо не находилось (старый внешний тип); активные использования отсутствовали — вырезана мёртвая ветка `#if !true` в `DefaultEfQueries.cs` и закомментированный блок в `MediaController`; остаток только в галерейном стеке (вне компиляции).
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
11. **Кодировки — финальная проверка (обязательно в конце, перед релизом)**: скриптовые правки фазы 2 читают файлы как UTF-8; файл в другой кодировке при этом молча портится — кириллица заменяется на U+FFFD. Прецедент: `src/Mars.Contracts/Contracts/SSO/IdTokenModel.cs` (был в Windows-1251; фикс переносов его испортил, восстановлен из git, переведён в UTF-8 без BOM, неймспейс `Mars.Contracts.SSO` поправлен пользователем вручную). **В конце просканить все файлы на два признака:** (а) байты ≥0x80, не образующие валидный UTF-8 → файл в чужой кодировке (прочитать как Windows-1251/CP1251 и пересохранить в UTF-8 без BOM); (б) последовательность байт `EF BF BD` (U+FFFD) → испорченная кириллица (восстанавливать из git). На 2026-08-27 среди `.cs`/`.razor` других проблемных файлов не найдено. **Финальный скан 2026-08-30 — ✅ выполнен** (все отслеживаемые файлы: 3363 текстовых проверено, 68 бинарных пропущено): невалидный UTF-8 — 0; U+FFFD — 0; UTF-16 — 0. BOM: 2026-08-30 вырезан из всех отслеживаемых файлов (143 файла, в основном csproj из старых шаблонов) побайтово, без перекодировки; контрольный рескан — 0. `.editorconfig` (`charset = utf-8` в `[*]`) уже запрещает BOM для редакторов, уважающих EditorConfig.

## Разнос опций по владельцам (UseMarsOptions-хуки) — ✅ выполнен (2026-08-28)

- Модели разъехались по контрактным пакетам владельцев: `MediaOption` → `Mars.Media.Contracts.Options`; `SEOOption`, `FaviconOption`(+Gen) → `Mars.SiteEngine.Contracts.Options`; `PluginManagerSettingsOption` → `Mars.Plugin.Contracts.Options`; `ApiOption`, `MaintenanceModeOption` → `Mars.Server.Contracts.Options`; `DevAdminStyleOption` → **новый `Mars.Admin.Contracts`**; `OpenIDClientOption`, `OpenIDServerOption`, `AuthVariantConstOption` → `Mars.SSO.Contracts.Options`. `Mars.Options/Models` и `Interfaces/IImageConverOption.cs` удалены; изображенческие контракты (`IImageConverConfig`, `IProcessImageResult`, `ImageConverConfig`, `ProcessImageResult`) → `Mars.Media.Contracts`.
- Регистрации — по Use-хукам владельцев: ядро (`UseMarsServerOptions` в `MainMarsHost`, вызов из `UseMarsHostServices` — до SeedData), `UseMarsMedia`, `UseMarsNotifications`, `UseMarsWebSiteProcessor` (+ фавикон-хук переехал из `Options.Host` в SiteEngine), `UsePlugins`, `UseDevAdmin`. `UseMarsOptions` схлопнут до движка (`IOptionService` + `IFrontRequestHandler` + `FrontsOption`).
- Спецкейсы `OptionService` удалены: `SysOption`, `RobotsTxt()`, `MailSettings`, `SaveSmtpSettings` (+ члены интерфейса); потребители переведены на `GetOption<T>()`; инвалидация `_fileHostingInfo` по типу сохранена.
- **`SysOptions` → `SiteSettings`** — переименован тип и файл; имена свойств и свойства-носители (`Q.Site.SysOptions`, `PageRenderContext.SysOptions`, шаблоны `{{{SysOptions.*}}}`, API-путь `SysOptions`) сохранены — wire не меняется; ключ хранения в БД меняется на «SiteSettings» (старая строка сиротеет — приемлемо на 0.8.0-alpha). `Mars.Options.Host` отпустил ссылку на `Mars.SiteEngine`, получил прямые `Mars.Server.Contracts` + `Mars.SiteEngine.Abstractions`; движок отпустил `Mars.Server.Contracts` + `Mars.Notifications.Abstractions`.
- **Seed** — логика `AppDbContextSeedData.SeedFirstOption` вынесена в `ISeedFirstOptionHandler`/`SeedFirstOptionHandler` (Mars.Server, регистрация в `AddMarsHost`); Setup-конфиг читается через DTO `SetupSiteConfig`; неиспользуемый `MarsDbContext` из сигнатуры убран.
- URL-ссылки на страницы опций обновлены на новые FullName (`ManageMediaPage`, доки SSO ×4).

Верификация: `dotnet build Mars.slnx` — 0 ошибок. Точечно: Test.Mars.Server (OptionService/OptionReaderTool) 36/36; Mars.Integration.Tests (Controllers.Options/Medias/Plugins + Modules.SSO) 37/37; WebApiClient.Integration.Tests (Options + RegisterAccount) 13/13; Mars.Plugin.Integration.Tests 7/7; Test.Mars.SiteEngine (рендер/QueryLang) 8/8. Docker-набор не гонялся (не требовался). Коммит — по команде пользователя.

Открытые хвосты: `UserService.cs:191` — закомментированная строка `//var defaultRoles = _optionService.SiteSettings.Default_Role; //TODO: setup in options` осталась (старое имя `SysOption` в ней ушло при переименовании типа); удалить или реализовать вместе с настройкой дефолтной роли.

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

Следующие кандидаты из `Mars.WebApp` (обсуждено 2026-08-29): ~~CLI по владельцам~~ — ✅ сделано 2026-08-29 (см. ниже); ~~ядро в `Mars.Server`~~ — ✅ сделано 2026-08-29 (см. ниже); ~~`RenderPageNodeImpl` → `WebApp.Nodes.Host`~~ — ✅ сделано 2026-08-29 (см. ниже); ~~ревизия `AddMarsHostServices`~~ — ✅ сделано 2026-08-29 (см. ниже; `DevAdminConnectionService` остался до среза хостинга админки); ~~хостинг админки~~ — ✅ сделано 2026-08-29 (см. ниже; новый модуль `Mars.Admin.Host` забрал и `DevAdminConnectionService`); ~~XActions~~ — ✅ сделано 2026-08-29 (см. ниже; стек `Mars.XActions` + `Mars.XActions.Host`, каталог разобран по владельцам, контексты — оверлеем в `Mars.Admin.Host`). Осталось: ~~Setup-визард~~ — решение 2026-08-30: остаётся в хосте, выделять не нужно; ActionCenter админки — отдельный срез (решение пользователя). **Аудит абстракций/контрактов (2026-08-29):** пункты 1–9 ✅ сделаны; уточнения по `ExecuteActionRequest`/Datasource/медиа (инструкции 1–3) ✅ сделаны (см. ниже); ~~очередь: 10) расщепление `Mars.Options`~~ — ✅ сделано 2026-08-29 (см. ниже).

**CLI-команды по владельцам (2026-08-29):** `UserCommandCli`/`RoleCommandCli` → `Mars.Identity.Host/CommandLine` (ns `Mars.Identity.Host.CommandLine`), `OptionCommand` → `Mars.Options.Host/CommandLine` (ns `Mars.Options.Host.CommandLine`); конструкторы переведены с конкретного `CommandLineApi` на `ICommandLineApi` (интерфейс уже имеет `GetCommand<T>`/`Register<T>`). Созданы Use-хуки `UseMarsIdentity` и `UseMarsOptions` с регистрацией через `ICommandLineApi.Register<T>()` (паттерн `Nodes.Host`/`AiChat.Host`/`Datasource.Host`), вызовы — в `MarsWebAppStartup` после `UseMarsHost`. Обнаружение команд из сборки экзешника для них прекратилось — теперь только через регистрацию модулей; `InfoCommand` (паспорт хоста) и `MigrationCommandCli` (уедет с миграционным срезом) остались в `Mars.WebApp`. Верификация: сборка 0 ошибок; тесты поднимают полный хост и инстанцируют все зарегистрированные CLI при `InvokeCommands` — WebApiClient.Integration.Tests (RegisterAccount + Get/UpdateOption) 11/11. Коммит — по команде пользователя.

**Ядро в `Mars.Server` + сидинг в Cms (2026-08-29):**
- `MarsStartupInfo`(+`MarsStartupInfoObject`) → `Mars.Server/Startup` (ns `Mars.Server.Startup`); `MarsSystemService` → `Mars.Server/Services`, регистрация в `AddMarsHost`. `AboutSystem` теперь читает версию/`SourceRevisionId`/`RepositoryUrl` из сборки `Mars.Server` — для этого в `Mars.Server.csproj` добавлены `AssemblyMetadata`-атрибуты (аналог веб-апповских).
- `MarsStartupPartMigrations` → `Mars.Server/Startup/MarsDbStartup.cs` (`MarsRequireMigrate`/`MarsAutoMigrateCheck`/`MigrateAsync`/`SeedData`), `AppDatabaseMigrationOptions` → `Mars.Server/Models`. `MigrationCommandCli` → `Mars.Server/CommandLine` (ns `Mars.Server.CommandLine`); так как `migrate` — базовая команда (исполняется до сборки DI), он добавлен в `initialCommands` рядом с `InfoCommand`; категория логгера — `ILogger<MigrationCommandCli>` (вместо `ILogger<Program>`).
- **Сидинг — решение пользователя: весь в Cms** (первоначальный контент сайта). Новый контракт `ISeedDataHandler` (`Mars.Data/Seeding`, нейтральное место: его видят и ядро, и модули без нарушения направленности). `SeedRoles`/`SeedUsers`/`SeedPostData`/`SeedPostCategories` + `CmsSeedDataHandler` (порядок: роли → админ → типы/меню/посты → категории) — в `Mars.Cms.Host/Seeding`, регистрация в `AddMarsCms`. Ядро в `SeedDataAsync` исполняет `ISeedFirstOptionHandler` + все `ISeedDataHandler` по `Order`. `AppDbContextSeedData` (обёртка) удалён.
- `LocalizerXmlResLoaderFactory` — мёртвый код (ни одной регистрации/инстанциации; потребители резолвят `IAppFrontLocalizer?` nullable) — **удалён**, вместе с ним из `Mars.WebApp.csproj` убран пакет `ResXResourceReader.NetStandard`.
- `Mars.Server.csproj`: + `Npgsql`, + ссылка `Mars.CommandLine.Abstractions`.
- Потребители переключены: `Program`/`MarsWebAppStartup`/`SetupWizardHost`/`InfoCommand`/`FixDebugModeBaseDirectory`/`ConfigureAppConfiguration` (WebApp), фикстуры `Mars.Integration.Tests` и `Mars.E2E.Tests` (`MarsDbStartup.SeedData`), `GetSystemInfoTests`.
- `MarsStartupInfo` — решение пользователя (2026-08-29): статик **возвращён в `Mars.WebApp`** (`Mars.UseStartup.MarsStartupInfo`) — информация о запуске принадлежит конкретному приложению, не ядру. Ядро потребляет через `IMarsStartupInfo`: `MarsSystemService` переведён на инъекцию `IMarsStartupInfo`, регистрация `AddSingleton<IMarsStartupInfo>(MarsStartupInfo.Instance)` — в `MarsWebAppStartup.ConfigureBuilder`.
- **Фикс пользователя (2026-08-29):** после среза модульные контроллеры отдавали 404 (сборка приложения теряла application parts модулей), а `Mars.Cli.EndToEnd.Tests` падали на `-h/--help` — починено пользователем: регистрации контроллеров в мейнах модулей (`MainIdentity`/`MainMedia`/`MainCms`/`Scheduler`/`SiteEngine`/`Nodes.Host`/`Options.Host`, `ApplicationPluginExtensions`) и в `Mars.Server`, плюс `CommandLineApiClassifyTests`.
- Верификация: сборка 0 ошибок; `GetSystemInfoTests` 18/18 (полный хост, сидинг через новый хендлер, `MarsSystemService`). Коммит: `7e9e5833` (вместе с фиксами пользователя).

**`RenderPageNodeImpl` → WebApp.Nodes.Host (2026-08-29):** импл переехал `Mars.WebApp/Nodes` → `Mars.Modules/Mars.WebApp.Nodes.Host/Nodes` (ns `Mars.WebApp.Nodes.Host.Nodes`) — регистрация автоматическая: `UseMarsWebAppNodes` уже сканирует свою сборку (`typeof(ExcelNodeImplement).Assembly`); из `UseMarsHostServices` регистрация `RegisterAssembly(typeof(RenderPageNodeImpl).Assembly)` убрана (в хосте имплов нод больше нет). Чтобы модуль не ссылался на реализацию SiteEngine (правило 3), контракты рендера `IWebRenderEngine`/`IWebRenderEngineFactory`/`IWebRenderEngineLocator` перенесены `Mars.SiteEngine/Interfaces` → `Mars.SiteEngine.Abstractions/WebSite` (ns `Mars.SiteEngine.Abstractions.WebSite`); using'и переключены в 16 файлах (SiteEngine, SiteEngine.Handlebars, фикстуры тестов). Попутно починен дубль регистрации `MigrationCommandCli` (регистрация в `UseMarsHost` + `initialCommands` → ArgumentException при старте хоста): оставлен только `initialCommands` — «migrate» базовая команда и исполняется до `ConfigureApp`, где модульные `Register<T>()` ещё не работали. `Mars.WebApp.Nodes.Host.csproj`: + ссылка `Mars.SiteEngine.Abstractions`. Верификация: сборка 0 ошибок; лёгкий фронтовый набор + `GetPageRenderTests` 55/55; Docker-регрессия рендера `HandlebarsAppFrontTests` 17/17. Коммит — по команде пользователя.

**Ревизия `AddMarsHostServices` (2026-08-29):** разнесено всё, кроме `DevAdminConnectionService` (оставлен до среза хостинга админки — жёстко связан с `Mars.Admin.App` + ChatHub + `StartupDevAdmin`).
- `AIToolService` → `Mars.SemanticKernel.Host/Service` (ns не менялся — `Mars.Services`), регистрация в `AddMarsSemanticKernel`, т.е. **под флагом `AITool`** (решение пользователя). Обоснование безопасности: единственный путь резолва `IAIToolService` — конструкторы `IAIToolScenarioProvider`-имплов (`AiCreatePostTool` регистрируется под тем же флагом; `MarsSQLQueryPromptHelper` из Datasource.Host — безусловным сканом атрибутов, но резолвится только лениво через `IAIToolScenarioProvidersLocator.GetProvider`, единственный потребитель которого — `MarsAIService`, тоже под флагом; `AIToolController` под `[FeatureGate]`). При выключенном флаге сервис никто не резолвит — безусловная регистрация была артефактом.
- Регистрация `IWebSiteProcessor → MapWebSiteProcessor` → `AddMarsWebSiteProcessor` (SiteEngine; импл уже лежал там, конструктор без зависимостей). Потребители (`MaintenanceFrontRequestHandler` Options.Host, `RenderPageNodeImpl` WebApp.Nodes.Host, сам SiteEngine) зависят только от контракта из `Mars.SiteEngine.Abstractions`.
- `ModelInfoService`/`IBlazorPagesService` — имплы уже в Admin.Framework; серверные регистрации вынесены в новый хук `AddAdminFrameworkServerServices` (`MainAppFrontShared.cs`): WASM-двойники в `AddAppFront` на сервере не выполняются (ранний выход по `!IsBrowser()`), а серверные нужны (Blazor Server админки + `DevAdminConnectionService` через favicon-путь SiteEngine). Хук вызывается из `AddMarsHostServices` **безусловно** — паритет с прежним поведением при `NOADMIN` (`AddAppFrontMain` вызывается под `#if !NOADMIN`, туда регистрации класть было нельзя).
- `AddSingleton<IServiceCollection>(services)` → `AddMarsHost` (Mars.Server) — хост-инфраструктура; потребители `FunctionNodeImpl` (Nodes) и `KernelFactory_v2` (SemanticKernel.Host).
- Вызовы `AddMarsQueryLang().AddMetaModelGenerator()` вынесены из `AddMarsHostServices` в цепочку `ConfigureBuilder` (перед `AddMarsHostServices`, порядок сохранён).
- В `AddMarsHostServices` осталось: `AddDatabaseDeveloperPageExceptionFilter`, `IDevAdminConnectionService`, `AddMarsHost(wenv)`, `AddAdminFrameworkServerServices`. Using'и файла вычищены (были дубли).
- Верификация: сборка 0 ошибок; лёгкий интеграционный набор включая `GetSystemInfoTests` (полный старт хоста) 67/67; Docker-регрессия рендера `HandlebarsAppFrontTests` 17/17. Коммит — по команде пользователя.

**Хостинг админки → новый модуль `Mars.Admin.Host` (2026-08-29):** создан `src/Mars.Modules/Mars.Admin.Host` (Sdk.Razor + `AddRazorSupportForMvc`, FrameworkReference AspNetCore, пакет `Microsoft.AspNetCore.Components.WebAssembly.Server`; в `Mars.slnx` — папка `/Mars.Modules/Admin/`). Забрал: `StartupDevAdmin` → `UseMarsAdmin()` (ветка `MapWhen("/dev")` + `RegisterOption<DevAdminStyleOption>`), `StartupHostFiles` разобран (корневой `UseStaticFiles` остался в композиции одной строкой; no-cache для `/dev/_framework` ушёл в `UseMarsAdminHost()`), `Pages/_AdminHost.cshtml` (ns `Mars.Pages` не менялся), `Services/DevAdminConnectionService` (+ регистрация из `AddMarsHostServices`), `MapHub<ChatHub>("/_ws/admin")`, `AddAppFrontMain(configuration, typeof(Mars.Admin.App))` (был под `#if !NOADMIN`) и `UseAppFrontMain()`. Хуки: `AddMarsAdminHost(configuration)` / `UseMarsAdminHost()` (на месте прежнего `UseHostFiles` — до корневого `UseStaticFiles`, порядок no-cache сохранён) / `UseMarsAdmin()` (на месте прежнего `UseDevAdmin` — после `UsePlugins`).
- `IInitialSiteDataViewModelHandler` — новый интерфейс в `Mars.Server.Abstractions/Handlers` (решение пользователя): модули не ссылаются на имплементацию `Mars.Server` (паттерн подтверждён зависимостями SiteEngine), а `_AdminHost.cshtml` инжектит хендлер. `InitialSiteDataViewModelHandler` реализует интерфейс; `ViewModelController` и cshtml переведены на интерфейс; регистрация в `AddMarsHost` — `AddScoped<IInitialSiteDataViewModelHandler, InitialSiteDataViewModelHandler>()`.
- **Подводные камни Razor-модулей (задокументировать):** (1) страницы модуля обнаруживаются только как `CompiledRazorAssemblyPart` — `services.AddRazorPages().PartManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(assembly))`; обычный `AddApplicationPart` даёт `AssemblyPart`, у которого нет маршрутов страниц (`Cannot find the fallback endpoint: { page: /_AdminHost }`); (2) csproj обязан иметь `AddRazorSupportForMvc=true` (иначе RAZORSDK1004).
- `NOADMIN`: условная ссылка на `Mars.Admin` в csproj модуля зеркалит веб-апповскую (механизм по-прежнему спящий), `#if`-гарды в `AddMarsAdminHost`/`UseMarsAdmin` дублируют прежние; вызовы хуков в `MarsWebAppStartup` безусловные. Предупреждение CS8974 в `_AdminHost.cshtml` (`GetOption<SiteSettings>()`) — существовало у оригинала, не трогали.
- Верификация: сборка 0 ошибок; лёгкий интеграционный набор 67/67; Docker-регрессия рендера `HandlebarsAppFrontTests` 17/17 — включая оба теста на `/dev` (`Basic_DevAdmin_ShouldNotBeInterceptedByFrontFallback`, `Maintenance_Enabled_AdminPanelWorks`), которые проверяют рендер `_AdminHost` из модуля. Коммит — по команде пользователя.

**XActions → отдельный стек пакетов + разбор каталога по владельцам (2026-08-29):** решения пользователя: (1) XActions — отдельный пакет с абстракциями и хостом; (2) регистрация действий — у владельцев, CMS-связанное в Cms; (3) механизм привязки к страницам админки — **вариант D: оверлей в `Mars.Admin.Host`** (владельцы регистрируют команды без контекстов; привязку ид→страницы навешивает единственное место, знающее типы страниц); (4) ActionCenter админки — отдельный срез позже.
- Новый стек `src/Mars.XActions/` (папка `/XActions/` в `Mars.slnx`, по образцу `/Options/`): **`Mars.XActions`** (WASM-safe: контракты `XActionCommand`/`XActionBuilder`/`XActionEffect` из `Mars.Contracts/Contracts/XActions`, `IActionManager` + `IXActionCommandsProvider`/`IXActionOptionsSource` и `AddXActionHandlers` из `Mars.Server.Abstractions/Managers`; namespaces не менялись — фаза 1) и **`Mars.XActions.Host`** (`XActionManager`, `ActController` — эндпоинты `/api/Act/Inject|list|options/{ключ}`, хук `AddMarsXActionsHost` с регистрацией application part). `ActionHistoryService`/`IActionHistoryService`/`ActionHistoryLevel` — **не** XActions (аудит, живые потребители в Notifications/Options) — остались в ядре. `ExecuteActionRequest` остался в `Mars.Contracts/Common/UserActionResult.cs`.
- Новый метод `IActionManager.AddFrontContexts(id, contexts)` — реализация в `XActionManager` через `with`-замену рекорда + инвалидация кеша; для оверлея.
- Разбор каталога `Mars.WebApp/XActions` (папка удалена): контентные акты + их команды (`CreateMockPostsAct`, `RegenerateGeneratedMetaValuesAct`, `CreatePostTypePresentationTemplateAct`, ссылки TemplateViewer/Feedback-Excel/GenSourceCode, `AddOptionsSource(postTypes)`) → **`Mars.Cms.Host/XActions`** (`AddCmsXActions` вызывается из `AddMarsCms`, `UseCmsXActions` — в `ConfigureApp` на месте прежнего `UseConfigureActions`); `Bogus` переехал в `Mars.Cms.Host.csproj`. Хостовые/отладочные (`ClearCacheAct`, `DummyAct`, `FormTestAct`, `FrontDemoXAction`, команды «Очистить кеш»/«App.Logs») → **`Mars.Server/XActions`** (`UseMarsHostXActions`; скан хендлеров `AddXActionHandlers(typeof(ClearCacheAct).Assembly)` в `AddMarsHost`; регистрация `IActionManager` из `AddMarsHost` убрана — теперь в `AddMarsXActionsHost`).
- **Оверлей** `Mars.Admin.Host/XActions/AdminXActionsOverlay.cs` (`UseMarsAdminXActions`, вызывается в конце `UseMarsAdmin`): `AddFrontContexts` по литеральным идам команд (иды — стабильный контракт, сборке видны только они, без ссылок на Cms.Host/Mars.Server) + админские записи, которым нужны типы страниц («Логи» с идом `typeof(DebugPage).FullName`, debug-ссылки `#if DEBUG`).
- `FrontManager.AdminFrontSlug` (константа имплементации SiteEngine) → **`AppAdminConstants.AdminFrontSlug`** в `Mars.SiteEngine.Abstractions`; переключены `FrontManager`, `PageRenderController`, `FrontController`, `AdminFrontRenderHandler`, `FrontManagerTests` и акт создания шаблона.
- `Mars.WebApp`: каталога XActions больше нет; убраны `AddConfigureActions`/`UseConfigureActions`, пакет `Bogus` и **условная ссылка на `Mars.Admin`** (хост больше не ссылается на WASM-сборку админки — только `Mars.Admin.Framework` и `Mars.Admin.Host`). `Mars.Server.Contracts` получил ссылку на `Mars.XActions.Contracts` (`InitialSiteDataViewModel.XActions`; до расщепления 2026-08-29 — на единый `Mars.XActions`), `Mars.WebApp` — на `Mars.XActions.Host`; `ActionManagerMock` в девстенде дополнен `AddFrontContexts`.
- Верификация: сборка 0 ошибок; лёгкий интеграционный набор 69/69 — включая `InjectActTests` (исполнение акта через переехавший `ActController`/`XActionManager`); Docker-регрессия рендера `HandlebarsAppFrontTests` 17/17 (каталог в `InitialSiteDataViewModel` + оверлей контекстов на `/dev`). Коммит — по команде пользователя.

**Проход по абстракциям и контрактам — пункты 1–3 (2026-08-29):** полный аудит карты ссылок всех проектов `src/` (карта с классификацией по конвенции `.Contracts`/`.Abstractions`/`.Host`/`.Front` собрана в сессии 2026-08-29; очередь исправлений — в «Следующих кандидатах» выше).
- **1. `Mars.XActions` расщеплён** (решение пользователя — «почему нет пакета абстракций»): базовый пакет переименован в **`Mars.XActions.Abstractions`** (`IActionManager`, `AddXActionHandlers`; ссылки: `Mars.XActions.Contracts` + `Microsoft.Extensions.DependencyInjection.Abstractions`), создан **`Mars.XActions.Contracts`** (`XActionCommand`, `XActionBuilder`, `XActionEffect`, `XActResult`, `IAct` + доперенесённый из `Mars.Contracts/Common/UserActionResult.cs` **`ExecuteActionRequest`**; namespaces не менялись). Ссылки потребителей переключены: `Mars.Server.Contracts` → Contracts; `Mars.Cms.Host` → оба; `Mars.Server`, `Mars.Nodes.Core.Implements`, `Mars.Admin.Host` → Abstractions; `Mars.XActions.Host` → Abstractions.
- **2. Создан `Mars.Notifications.Contracts`** (`EmailSendMessageDto`, `SendSmsModelRequest`, `SmtpSettingsModel`, `TestMailMessage` — wire-DTO, которые раньше лежали в `Mars.Notifications.Abstractions` и тянули серверные абстракции в WASM-клиент). `Mars.WebApiClient` переключён с Abstractions на Contracts; `Mars.Notifications.Abstractions` ссылается на Contracts (интерфейсы `INotifyService`/`ISmsSender`/`IMarsEmailSender` остались в нём).
- **3. Убрана лишняя ссылка `Mars.Plugin` → `Mars.Plugin.Front`**: серверный лоадер плагинов использует только `Mars.Plugin.Front.Abstractions` (прямая ссылка) — фронтовая сборка больше не в графе серверного пакета. Попутно всплыла скрытая зависимость `Mars.Plugin` → `Mars.Options` (раньше приходила транзитивно через `Plugin.Front`) — оформлена явно (долг: вынести интерфейс опций в абстракции — часть пункта 10).
- Верификация: сборка 0 ошибок; лёгкий интеграционный набор 69/69; Docker-регрессия рендера 17/17. Коммит — по команде пользователя.

**Пункт 4 — разрыв `Mars.WebApp.Nodes.Host → Mars.WebApp.Nodes.Front` (2026-08-29):** модели `AppEntityCreateFormSchema`/`EntityPropertyFormField`/`AppEntityCreateFormsBuilderDictionary`/`AppEntityCreateFormSchemaEditModel` (чистые C#, только `Mars.Contracts.Models`) перенесены из `Mars.WebApp.Nodes.Front/Models/AppEntityForms` в базовый `Mars.WebApp.Nodes/Models/AppEntityForms` (namespaces не менялись — фаза 1; в Front остался только `NodeEntityQuery/NodeEntityQueryRequestModelEditModel`). В `MainWebAppNodes` убраны вызовы фронтовых хуков: `AddMarsWebAppNodesFront()` (регистрация `INodeEditorToolServiceClient` — нужна только в WASM) и `UseMarsWebAppNodesFront()` — вместо него сервер регистрирует модели нод сам: `INodesLocator.RegisterAssembly(typeof(ExcelNode).Assembly)` (паттерн `MainDatasource`/`MainSemanticKernel`); регистрацию форм (`INodeFormsLocator`) и клиента делает `Mars.Admin/Program.cs` через те же фронтовые хуки. В `Mars.WebApp.Nodes.Host.csproj` ссылка на Front заменена прямой ссылкой на базовый `Mars.WebApp.Nodes` (раньше база приходила транзитивно). Серверный пакет больше не тянет фронтовую сборку. Верификация: сборка 0 ошибок; лёгкий интеграционный набор 69/69; Docker-регрессия рендера 17/17. Коммит — по команде пользователя.

**Пункт 5 — разрыв `Mars.Nodes.Host → Mars.Nodes.Workspace` (2026-08-29):** имплементация `NodesLocator` (чистая рефлексия по типам `Node`, зависит только от `Mars.Core`/`Mars.Nodes.Core`) перенесена в `Mars.Nodes.Core/Locators` (ns `Mars.Nodes.Workspace.Locators` не менялся; класс остался `internal` — наружу торчит только новое DI-расширение `AddNodesLocator()` там же, регистрирующее локатор + keyed `JsonSerializerOptions`; в `Mars.Nodes.Core.csproj` добавлен `Microsoft.Extensions.DependencyInjection.Abstractions`). `MainMarsNodes` больше не зовёт фронтовые хуки: `AddMarsNodes` зовёт `AddNodesLocator()`, `UseMarsNodes` сам делает `RegisterAssembly(typeof(InjectNode).Assembly)`. **Схема по решению пользователя:** фронтовые хуки в не-браузере выходят сразу, серверные регистрации — безусловные в хостах. `Add/UseNodeWorkspace` и `Add/UseDatasourceWorkspace` (Mars.Datasource.Front) начинаются с `if (!OperatingSystem.IsBrowser()) return` — на сервере они больше ничего не регистрируют (раньше серверная композиция получала через них `INodeFormsLocator`/клиенты, которые на сервере никто не резолвит; регистрация нод-ассемблей на сервере уже есть в `UseMarsNodes`/`MainDatasource`, в WASM её делают сами хуки). В WASM хуки регистрируют локатор и фронтовые сервисы как раньше. Побочный фикс прежней двойной регистрации `INodesLocator` на сервере — исчез вместе с серверным исполнением хуков. В `Mars.Nodes.Host.csproj` ссылка на Workspace заменена прямой ссылкой на `Mars.Nodes.Front.Abstractions` (wire-DTO `NodeTaskJob*`, которые контроллеры/маппинги хоста использовали транзитивно через Workspace — контракты фронта, корректное направление). Серверный движок нод больше не тянет Blazor-редактор. Верификация: сборка 0 ошибок; лёгкий интеграционный набор 69/69; Docker-регрессия рендера 17/17. Коммит — по команде пользователя.

**Пункт 6 — инверсия `Mars.Datasource.Abstractions` (2026-08-29):** абстракции зависели от базового `Mars.Datasource`, потому что `IDatasourceDriver` использует его wire-типы. Типы перенесены в `Mars.Datasource.Abstractions/Models`: `QDatabaseStructure`, `QTable`, `QTableColumn`, `QTableSchema`, `SqlQueryResultActionDto` (+ `SqlQueryJsonResultActionDto`, `SqlNonQueryResultActionDto` в том же файле; namespaces не менялись). Направление ссылок инвертировано: `Mars.Datasource.Abstractions` ссылается только на `Mars.Core` (`IUserActionResult`), базовый `Mars.Datasource` получил ссылку на Abstractions. Потребители (провайдеры MsSQL/MySQL/PostgreSQL, Datasource.Host, Front) ссылались на оба/на базу — компилируются без изменений. Верификация: сборка 0 ошибок; лёгкий интеграционный набор 69/69; Docker-регрессия рендера 17/17. Коммит — по команде пользователя.

**Пункт 8 — переименование `Mars.SiteEngine` → `Mars.SiteEngine.Host` (2026-08-29):** имплементация движка лежала в пакете без суффикса при существующих `SiteEngine.Abstractions/Contracts/Handlebars/Templators`. Папка и csproj переименованы (сборка теперь `Mars.SiteEngine.Host`; namespaces `Mars.SiteEngine.*` не менялись — фаза 1; в csproj не было PackageId/AssemblyName-оверрайдов). Обновлены три ссылки: `Mars.WebApp.csproj`, `Mars.SiteEngine.Handlebars.csproj`, `Mars.slnx` (папка `/Mars.Modules/WebSiteProcessor/`). Строковых ссылок на имя сборки (`"Mars.SiteEngine"`) в коде нет; тестовые проекты ссылались только на Handlebars/Abstractions. **Изъян коммита `bc8ab225` (на исправление):** `git add` был только по новой папке — удаления старого каталога `src/Mars.Modules/Mars.SiteEngine/` не вошли в коммит и докоммичены отдельным коммитом; историю (объединение в один rename-коммит) правим в конце по решению пользователя. Верификация: сборка 0 ошибок; лёгкий интеграционный набор 69/69; Docker-регрессия рендера 17/17. Коммит — по команде пользователя.

**Пункт 7 — чистка `*.Abstractions → Mars.Data` (2026-08-29):** единственной связкой трёх пакетов абстракций (Media, Scheduler, Plugin) с `Mars.Data` были пейджинг-расширения из `Mars.Data.Extensions` — маппинги отображают DTO→Response, EF-сущностей в сигнатурах нет. Финальная раскладка (обсуждена с пользователем; новый пакет решили не плодить): чистые LINQ-хелперы `AsListDataResult`/`AsPagingResult` (IEnumerable) и `ToMap`×4 вынесены в **`Mars.Contracts/Extensions`** (`ListDataExtensions.cs`, `PaginationExtensions.cs`; namespace `Mars.Data.Extensions` не менялся — фаза 1, теперь живёт в двух сборках); EF-версии `ToListDataResult`/`ToPagingResult` и `ApplyPaging` (IQueryable, единственный потребитель — EF-методы) остались в **`Mars.Data/Extensions`**. Важное семантическое различие семейств (зафиксировано при обсуждении): `As*` считает всю коллекцию и делает повторную енумерацию источника (`hasMoreData = total > items`), `To*` берёт `Take+1` строк и считает общий счёт только по `IncludeTotalCount` — это разные алгоритмы, смешивать их нельзя. Ссылки на `Mars.Data` убраны из `Mars.Media.Abstractions`, `Mars.Scheduler.Abstractions`, `Mars.Plugin.Abstractions`. Транзитивная волна, оформленная явно: `Mars.SiteEngine.Host` получил прямую ссылку на `Mars.Data` (реальная — `RenderRazorHost` принимает `MarsDbContext`), `Mars.Nodes.Core.Implements` — `PackageReference Microsoft.EntityFrameworkCore` (реальная — `EntityFrameworkQueryableExtensions` как маркер ассембли для Roslyn-скриптов в `FunctionNodeImpl`), мёртвый `using Microsoft.EntityFrameworkCore` убран из `OptionService` (Options.Host). Верификация: сборка 0 ошибок; лёгкий интеграционный набор 69/69; Docker-регрессия рендера 17/17. Коммит — по команде пользователя.

**Пункт 9 — `Mars.AiChat.Host → Mars.Datasource.Host` на абстракции (2026-08-29):** `MarsSqlTools` использовал только контракт `IDatasourceService`, но тот лежал в имплементации `Mars.Datasource.Host`, из-за чего AiChat.Host ссылался на чужой `.Host` (нарушение правила 3). `IDatasourceService` — чистый интерфейс без имплементационной логики, перенесён в `Mars.Datasource.Abstractions/Services` (namespace `Mars.Datasource.Host.Services` не менялся — фаза 1). Вместе с ним в `Mars.Datasource.Abstractions/Models` ушли 4 DTO из его сигнатур: `DatasourceConfig`, `DatasourceOption` (ns `Mars.Datasource`), `SelectDatasourceDto`, `ConnectionStringTestDto` (ns `Mars.Datasource.Dto`) — все чистый C#. Иначе был бы цикл (база `Mars.Datasource` уже ссылается на Abstractions после пункта 6). **Коррекция пункта 1:** `ExecuteActionRequest` (общий wire-DTO, используется Datasource/Media/Admin.Framework/WebApiClient — не только XActions) возвращён из `Mars.XActions.Contracts` в `Mars.Contracts/Common` (ns `Mars.Contracts.Common` не менялся) — иначе `Datasource.Abstractions` пришлось бы ссылаться на `XActions.Contracts`. В `Mars.Datasource.Abstractions.csproj` добавлена ссылка `Mars.Contracts` (для `UserActionResult`/`ExecuteActionRequest`). `Mars.AiChat.Host.csproj`: `Mars.Datasource.Host` → `Mars.Datasource.Abstractions`; попутно оформлены явно две транзитивные зависимости, которые раньше приходили через Datasource.Host: `Mars.Nodes.Abstractions` (`ChatHub`) и `Mars.CommandLine.Abstractions` (`ICommandLineApi`). В базе `Mars.Datasource/Dto` остались Response-типы для клиента (`QDatabaseStructureResponse` и др.) — не тронуты. Верификация: сборка 0 ошибок; лёгкий интеграционный набор 69/69; Docker-регрессия рендера 17/17. Коммит — по команде пользователя.

**Уточнения по `ExecuteActionRequest` + Datasource + медиа (инструкции пользователя, 2026-08-29):**
- `ExecuteActionRequest` — XActions-тип, после миграции медиа и Datasource на свои механизмы **остался сиротой** (потребителей нет, сами XActions используют `XActionCommandCall`) и **удалён** из `Mars.XActions.Contracts` по решению пользователя.
- **Собственный тип Datasource:** создан `DatasourceActionRequest` (`Mars.Datasource.Abstractions/Models`, ns `Mars.Datasource`; `ActionId` + `Arguments`, под будущую задумку пользователя). `IDatasourceService.ExecuteAction`, `DatasourceService`, `DatasourceController`, `IDatasourceServiceClient`, `DatasourceServiceClient`, `DataSourceInfoComponent.razor` и тест переключены на него. (Вошло в коммит `68cbb504` вместе с пунктом 9.)
- **Медийные действия → XActions (инструкция 3):** ранний хардкод-диспетчер `MediaService.ExecuteAction` (до появления XActions) разобран. Созданы акты `ScanMediaFilesAct` (`mars.media.scanFiles`) и `GenerateThumbnailsAct` (`mars.media.generateThumbnails`) в `Mars.Media.Host/XActions` — инжектят `IMediaService` + `IRequestContext` (пользователь = `requestContext.User.Id`). `IMediaService` лишился `ExecuteAction`, взамен наружу выведены `ScanFilesAndSaveInDB(userId)` и `GenerateThumbnails(onlyWithEmptyMeta)`. Регистрация: `AddMediaXActions` (скан хендлеров) вызывается из `AddMarsMedia`, `UseMediaXActions` (метаданные команд, категория «Медиа») — из `ConfigureApp` рядом с `UseCmsXActions`. Удалены: диспетчер в `MediaService`, эндпоинт `MediaController.ExecuteAction`, `IMediaServiceClient.ExecuteAction`/`MediaServiceClient.ExecuteAction`, `IAppMediaService.ExecuteAction`/`AppMediaService.ExecuteAction` (убран полностью, без обёртки). UI: кнопки тулбара `FluentMediaFilesList` зовут `client.Act.Inject(id)` напрямую (уже существующий `IActServiceClient`), id в разметке переключены на новые; модалка результата осталась на `UserActionResult` (конверсия `XActResult → UserActionResult` в компоненте). `FrontContexts` у медийных команд пока не заявлены — доделать вместе с ActionCenter. Попутно убрана ставшая ненужной ссылка `XActions.Contracts` из `Mars.Media.Abstractions` (в `Mars.Admin.Framework` ссылка осталась — там `ActAppService`/`XActionForms` и др. используют `Mars.Contracts.XActions`-типы). Верификация: сборка 0 ошибок; лёгкий интеграционный набор 69/69; Docker-регрессия рендера 17/17. Коммит — по команде пользователя.

**Пункт 10 — расщепление `Mars.Options` (2026-08-29):** база по содержимому уже была абстракциями движка (модели опций разъехались по владельцам ещё 2026-08-28). Решения пользователя: базу переименовать в `.Abstractions` (имя пакета тоже), скрытых потребителей оформить явно, тест-эндпоинты почты/SMS перенести в Notifications, `SystemImportSettingsFile_v1` удалить, статик `Configuration` убрать, формы опций оставить до следующего шага.
- **`Mars.Options` → `Mars.Options.Abstractions`** (папка + csproj + PackageId `mdimai666.Mars.Options.Abstractions` + Product + Description; namespaces `Mars.Options.*` не менялись — фаза 1; имя `Mars.Options` как проекта больше не существует; семейство = Abstractions + Contracts + Host — `Front` расформирован шагом ниже). Обновлены все 14 ссылающихся csproj + `Mars.slnx`. Транзитивность сохранена: несущие цепочки (`Media.Abstractions → Options.Abstractions`, `Data.Repositories → …`, `Admin.Framework → …`, `Options.Front → …`) остались на месте.
- **Явные ссылки** добавлены скрытым потребителям: `Cms.Host`, `Notifications.Host`, `SSO.Host`, `SemanticKernel.Host`, `AiChat.Host`, `AiChat.Front`, `Media.Host`, `Datasource.Host`, `Nodes.Core.Implements` → Abstractions; `Mars.Admin` → явная `Options.Contracts`. `SemanticKernel.Front`: лишняя ссылка `Options.Front` (нужны только атрибуты) заменена на явные `Admin.Framework` + `Options.Abstractions` (раньше Admin.Framework приходил транзитом через Options.Front).
- **`SendTestEmail`/`SendTestSms`** уехали из `OptionController` в новый `NotificationsController` (`Mars.Notifications.Host/Controllers`, маршрут `api/Notifications`; регистрация application part в `AddMarsNotifications` — паттерн модульных мейнов). WebApiClient: новые `INotificationsServiceClient`/`NotificationsServiceClient` (`client.Notifications`); из `IOptionServiceClient`/`OptionServiceClient` методы удалены; фронтовые `SmtpSettingsEditForm`/`SMSSettingsEditForm` переключены. `Options.Host` отпустил ссылку `Notifications.Abstractions`; из `OptionController` убран неиспользовавшийся `IActionHistoryService`.
- **Мёртвая цепочка импорта/экспорта настроек удалена** (серверных эндпоинтов уже не существовало): `SystemImportSettingsFile_v1`(+`_select`) из Abstractions; `SettingsImportPage.razor` (была исключена из компиляции `Content Remove`) вместе с этой строкой в `Mars.Admin.csproj`; пустые `SystemImportSettingsFile_v1Request/Response` из `Server.Contracts/Systems`; `ViewModelService.SystemImportSettings`/`SystemExportSettingsUrl`; закомментированные упоминания в `EditPostTypePage`/`EditNavMenuPage`.
- **Статик `IOptionService.Configuration` убран:** `OptionService` получает `IConfiguration` конструктором (`GetDefaultDatabaseConnectionString`); присвоение из `MarsStartupPartCore` удалено; пакет `Microsoft.Extensions.Configuration` снят с абстракций. `OptionServiceTests` — инстанциация с `new ConfigurationManager()`.
- **Не трогали:** `src/Plugin/Mars.Plugin.PluginPublishScript/Mars.deps.json` — генерируемый артефакт, обновляется следующим publish'ем.
- Верификация: сборка 0 ошибок; Test.Mars.Server 413/413; WebApiClient.Integration.Tests (Options + RegisterAccount, полный хост) 15/15; Mars.Integration.Tests (Controllers.Options) 7/7. Docker-регрессия рендера не гонялась (рендер не затронут). Коммит — по команде пользователя.

**Пункт 10, хвост — консолидация форм опций (2026-08-29):** `Mars.Options.Front` расформирован. Решение пользователя: все формы пока в `Admin.Framework` («формы показывает только админка»), `SMSSettingsEditForm` оставлена под будущий SMS-провайдер. 9 форм (Api, MaintenanceMode — Server; Favicon, SEO — SiteEngine; Media + подкомпонент `ImagePreviewSizeConfigEditForm` — Media; OpenIDClient/OpenIDServer — SSO; PluginManagerSettings — Plugin) перенесены в `Mars.Admin.Framework/OptionEditForms`; неймспейс форм стал `Mars.Admin.Framework.OptionEditForms` (дефолтный по папке), нужные `@using` добавлены в сами формы (стиль соседних файлов). Единственная новая ссылка: `Mars.Plugin.Contracts` в Admin.Framework (остальные контракты владельцев уже были). Точки регистрации не правились: маркер `typeof(ApiOptionEditForm).Assembly` в `Program.cs`/`MarsWebAppStartup` теперь указывает на сборку Admin.Framework, повторную регистрацию схлопывает `HashSet` локатора; лишние `using`/`@using` старого неймспейса убраны (`Program.cs`, `MarsWebAppStartup`, `SettingsOptionEditPage`, `ASideOptions`). Ссылка из `Mars.Admin.csproj` и строка в `Mars.slnx` удалены, файлы проекта (`csproj`, `_Imports.razor`) стёрты. Итоговая карта форм: у модулей со своими фронтами формы там (AiChat, SemanticKernel, плагины), у модулей без фронтов (Server, SiteEngine, Media, SSO, Notifications, Plugin-manager) — в `Admin.Framework/OptionEditForms`. Попутный факт (не чинили — форма и так не показывается): у `SMSSettingsEditForm` атрибут ссылается на саму форму (`typeof(SMSSettingsEditForm)`), модели SMS-опции в коде нет. Финальный скан кодировок (хвост фазы 2): 0 файлов с U+FFFD; невалидный UTF-8 только в двух загруженных медиа-файлах `wwwroot/upload` (не код). Верификация: сборка 0 ошибок; WebApiClient.Integration.Tests (Options + RegisterAccount, полный хост) 15/15. Коммит — по команде пользователя.

**Иерархия папок: физические переезды + папки `Mars.slnx` (2026-08-30):** решения пользователя — физика плоско, без подпапок; группировка — виртуальными папками решения.
- **`src/Server/`** (новая): `Mars.Server`, `Mars.Server.Abstractions`, `Mars.Server.Contracts`, `Mars.Data`, `Mars.Data.PostgreSQL`, `Mars.Data.InMemory`, `Mars.Data.Infrastructure`, `Mars.Data.Repositories` — плоско. В `Mars.slnx`: `/Host/` → `/Server/` с подпапками `/Server/Server/` и `/Server/Data/`.
- **`src/Admin/`** (новая): `Mars.Admin.Framework`, `Mars.Admin.Contracts`, `Mars.Admin.Host` (уехал из `Mars.Modules`). WASM-приложение `Mars.Admin` осталось в корне (физически и в решении). Папки решения `/AppFront/` и `/Mars.Modules/Admin/` заменены одной `/Admin/`.
- **Options и XActions — в `src/Mars.Modules/` плоско** (`Mars.Options.Abstractions/Contracts/Host`, `Mars.XActions.Abstractions/Contracts/Host`); пустые `src/Mars.Options/` и `src/Mars.XActions/` удалены; корневые папки решения `/Options/` и `/XActions/` стали `/Mars.Modules/Options/` и `/Mars.Modules/XActions/`.
- **Папки решения в `/Mars.Modules/`:** `/WebSiteProcessor/` → `/SiteEngine/` (туда же собраны плоские `SiteEngine.Abstractions/Contracts/Templators`); плоские проекты собраны в `/Excel/`, `/QueryLang/`, `/Scheduler/`, `/MetaModelGenerator/`.
- **Не тронуто:** `Mars.Datasource` (физика и папка `/Mars.Modules/Mars.Datasource/`), `/Tests/`, `/Modules/`, `/Benchmarks/`, `/Mars.Nodes/`, `/Mars.Plugin/`, `/Core/`.
- Ссылки: относительные пути `ProjectReference` пересчитаны скриптом по карте «имя проекта → фактическая папка» — 59 csproj (включая глубину иконки `icon-nuget.png` у переехавших). Имена сборок/неймспейсы/PackageId не менялись. `git mv` — история переименований сохранена.
- Вне csproj: Dockerfile работает с `src/` целиком, в CI/скриптах/benchmarks упоминаний переехавших путей нет. **Находки (не чинили):** `src/Mars.WebApp/CheckNugets.ps1` — список пакетов устарел ещё до переезда (там `Mars.Host.*`, `AppFront.*`, `Mars.Shared`), нужен отдельный проход целиком; в `docs/dev_docs/AppFront/Handlebars/HandlebarsAppFront.md` и нескольких `ai/*.md` остались ссылки на старые пути — косметика.
- Верификация: сборка 0 ошибок; Test.Mars.Server 413/413; лёгкий фронтовый набор без Docker 49/49. Коммит — по команде пользователя.

**Тесты — структурный раунд (2026-08-30):** аудит всех 24 проектов `tests/` (юниты/интеграции/E2E/инфраструктура). Решения пользователя: лишнее удалить, крупные слияния не нужны, унификация имён — отдельный раунд.
- **Удалён `TestApp.Mars.Data`** — не тесты: консольная песочница миграций/сидинга (`MarsDbContextFactory : IDesignTimeDbContextFactory` + сид юзеров/постов), потребителей нет. Папка и строка в `Mars.slnx` удалены.
- **Распущен `Test.Mars.Datasource.Host`** — один кейс на `SelectDatasourceDto.HelpLinkConnectionString` при 5 ссылках (включая неиспользуемую `Mars.Excel.Host`). Кейс перенесён в `Mars.Datasource.Integration.Tests/SourceBuilderTests.cs` (обычный `[Fact]`, без фикстур), проект удалён.
- **`Test.Mars.MetaModelGenerator`** — оставлен отдельно (интеграционный на `ApplicationFixture`, но самодостаточен со своими Tools/Fixtures).
- **`Mars.Integration.Tests` и `Mars.WebApiClient.Integration.Tests` не сливать** — разные слои (прямые запросы/сервисы против типизированного клиента); дублирование покрытия одних эндпоинтов (`InjectActTests`, `UpdateOptionTests` и др.) — тема раунда методов.
- Находки на раунд имён/методов: разнобой префиксов `Test.Mars.*` против `Mars.*.Tests`; одиночный суффикс `.Test` у `Mars.Nodes.Implements.Test`; устаревшее имя `AppFront.Tests` (тестирует `Mars.Admin.Framework`); `Test.Mars.Server` — свалка модулей без своих тестов (Cms.Host, Media.Host, Options.Host, CommandLine, TemplateEngine.Host) + папка `SomeTests`; `Test.Mars.SiteEngine` подмешивает QueryLang; `Mars.Integration.Tests` ссылает `Test.Mars.Plugin` ради одного хелпера (`NuspecExtension`); `ExternalServices.Integration.Tests/WordPressTests/WordPressPerformanceTest .cs` — пробел в имени, похоже на scratch; `Mars.AiServices.Integration.Tests` зависит от локального Ollama (модель захардкожена) без opt-in гейта; `Test.Mars.Datasource.Host` больше не существует.
- Верификация: сборка 0 ошибок; перенесённый кейс 1/1. Коммит — по команде пользователя.

**Тесты — раунд переименования проектов (2026-08-30):** унификация на общепринятый стиль `Mars.X.Tests` / `Mars.X.Integration.Tests`. Решения пользователя: вся карта из 7 + `DockerContainer` переименовать так, чтобы не пересекалось с модулем `Mars.Docker` (→ `DockerImage`); `AiServices.Integration.Tests`/`MetaModelGenerator`/`Mars.Test.Common`/`Test.EditorJsBlazored` не трогать; `/Tests/` в slnx оставить плоским. **Неймспейсы внутри переименованных проектов не трогались — правит пользователь сам.**
- Переименовано 8 проектов (папка + csproj через `git mv`, история сохранена): `Test.Mars.Core` → `Mars.Core.Tests`; `Test.Mars.Server` → `Mars.Server.Tests`; `Test.Mars.SiteEngine` → `Mars.SiteEngine.Tests`; `Mars.Nodes.Implements.Test` → `Mars.Nodes.Tests`; `Test.Mars.Plugin` → `Mars.Plugin.Tests`; `AppFront.Tests` → `Mars.Admin.Framework.Tests`; `Mars.AppFrontEngines.Integration.Tests` → `Mars.SiteEngine.Integration.Tests` (пара к `Mars.SiteEngine.Tests`); `Mars.DockerContainer.Tests` → `Mars.DockerImage.Tests`.
- `Mars.slnx`: 8 путей в `/Tests/` (порядок алфавитный, слои не вводились).
- Ссылки: `Mars.Integration.Tests.csproj` → `..\Mars.Plugin.Tests\Mars.Plugin.Tests.csproj`.
- IVT в `src/*.csproj` (16 замен, байт-сохраняющий скрипт): `Test.Mars.Server` → `Mars.Server.Tests` (×10), `Mars.Nodes.Implements.Test` → `Mars.Nodes.Tests` (×5), `Test.Mars.Plugin` → `Mars.Plugin.Tests` (×1). Попутно вычищены 5 мёртвых IVT: `$(MSBuildProjectName).Test` в `Mars.Server`, `Mars.WebApp`, `Mars.Data.Repositories`, `Mars.Datasource.Host` и `Test.Mars.Datasource.Host` в `Mars.Datasource.Host` (таких сборок не существует).
- Строковые пути в коде (иначе рантайм-феллы): `SolutionPathHelper.Resolve(...)` в `DirReadNodeTests` (→ `Mars.Nodes.Tests`), `PluginManifestProviderTests` (→ `Mars.Plugin.Tests`), `HandlebarsAppFrontApplicationFixture` (→ `Mars.SiteEngine.Integration.Tests`); комментарий в `CliThinClientEndToEndTests`.
- Доки: `QWEN.md` (Build & Test + Conventions; попутно починен совсем устаревший `Test.Mars.Host`), `ai/FeatureIntegrationGuide.md`, `ai/AiSkillsMemoryNodesPlan.md`, `ai/Prompts/CreateAiSkillsAndMemoryPrompt.md`. Исторические планы (`FrontReworkPlan`, `MediaFilesPlan`, `ActionCenterPlan`, старые секции этого файла) не трогались — они фиксируют прошлое.
- Известный переходный фейл (не чинить — уйдёт с неймспейсами): `Mars.Admin.Framework.Tests` → `BlazorPagesServiceTests.BuildRelativeSourcePath_NamespaceToFolders` — `BuildRelativeSourcePath` срезает с неймспейса префикс имени сборки; сборка уже `Mars.Admin.Framework.Tests`, неймспейсы фейков ещё `AppFront.Tests.*`.
- Верификация: сборка 0 ошибок; юнит-наборы переименованных проектов: `Mars.Core.Tests` 46/46, `Mars.Server.Tests` 413/413, `Mars.SiteEngine.Tests` 61/61, `Mars.Nodes.Tests` 415/415, `Mars.Plugin.Tests` 3/3 (+1 skip), `Mars.Admin.Framework.Tests` 25/26 (1 переходный фейл выше). Docker-сьюиты не гонялись. Коммит — по команде пользователя.

**Тесты — раунд имён, Объём 1: выбивающиеся имена (2026-08-30).** Пользователь сам прошёл неймспейсы переименованных проектов (коммит `193ab859`); раунд имён открыт его задумкой AAA-конвенции. Решения: AAA = имена + тела; стиль ожидания — голый глагол (`_ReturnsX`/`_FailsX`); полная унификация тремя объёмами: (1) выбивающиеся имена, (2) `Should` → голый глагол скриптом, (3) тела под Arrange/Act/Assert. Объёмы 2 и 3 — впереди.

Объём 1 — выполнено:
- **~40 методов** приведены к виду `Метод_Состояние_Ожидание`: одиночные слова (`CheckConnection`, `ClearTest`, `NewSizeTest`, `ParseBracketPairs`, `IsSlug`, `AsJson`, `Convert`, `ExecuteCommand`, …), `Test`-префиксы (`TestTranslateToPostSlug`, `TestMarkdownJsonBlock`, `TestExtractJson`, `TestResponseTimeFor10000RandomPostPages` → `RenderRandomPostPages_1000Requests_MeasuresResponseTime` — цикл в тесте на 1000, а не 10000), методы-дубли имён классов (`PostgresEngineTests` → `Driver_CreatedTodoTable_ReturnsRowsColumnsAndStructure` ×3 движка), `TestMany_Success/Fail`, `TestSpecials`, опечатка `FilterSharedFildes` → `FilterFiles_SharedFilesPresent_OnlyPluginFilesRemain`, Theory-методы (`PasswordGenerator`, `BackupPlainFile`). Не тронуты: мёртвые под `#if false` (`IsDayOffTests.cs` — кандидат на удаление, сломанный конструктор) и `#if Experiments` (`GoLinq1`).
- **Классы/файлы:** удалены неиспользуемые модели `testPost`/`testPost2`/`testPostUsered` (2 файла); `WordPressPerformanceTest .cs` → `WordPressPerformanceTests.cs` (пробел + суффикс); `WysiwygEditorHelperTest` → `WysiwygEditorHelperTests`; `PostgreSqlContainerTest` → `PostgreSqlContainerTests`; `EventManagerUnitTest` → `EventManagerTests` (+ файл); папка `AppFront.Main/` → `Components/` (неймспейс синхронизирован); `SomeTests/` распущена — `OptionReaderToolTests.cs` в корне `Mars.Server.Tests`.
- **Доломанный неймспейс-синк пользователя** (сборка на его коммите была красная, 38 ошибок) — починено: (а) синк-скрипт выкинул `using Mars.Nodes.Implements.Test.NodesForTesting;` вместо переименования — возвращён как `Mars.Nodes.Tests.NodesForTesting` в 4 файла; (б) затенение `Options.Create` неймспейсом `Mars.Server.Options` — квалифицировано в 4 местах; (в) затенение `Handlebars.Compile` неймспейсом `Mars.SiteEngine.Handlebars` — квалифицировано в `BasicExpressionTests` (6 мест); (г) пропущенный `namespace AppFront.Tests.Fakes` → `Mars.Admin.Framework.Tests.Fakes`; (д) остаток `Test.Mars.Server.CommandLine` в `MarsCliSocketPathTests`.
- Верификация: сборка 0 ошибок; `Mars.Core.Tests` 46/46, `Mars.Server.Tests` 413/413, `Mars.SiteEngine.Tests` 61/61, `Mars.Nodes.Tests` 415/415, `Mars.Plugin.Tests` 3/3 (+1 skip), `Mars.Admin.Framework.Tests` 26/26 (переходный фейл ушёл). Интеграционные проекты — только компиляция. Коммит — по команде пользователя.

**Тесты — раунд имён, Объём 2: `Should` → голый глагол (2026-08-30).** 486 переименований, `Should` в именах тест-методов больше нет (0 по свипу).
- Скрипт (433): замена последнего сегмента по карте: точные (`ShouldSuccess`→`Succeeds` ×230, `ShouldOK/ShouldOk`→`Succeeds`, `ShouldFail`→`Fails`, `ShouldUnauthorized`→`ReturnsUnauthorized`, `ShouldException`/`ShouldThrow`→`Throws`, `ShouldNotThrowError`→`DoesNotThrowError`, `ShouldOptionNotRegisteredException`→`ThrowsOptionNotRegisteredException`, `ShouldResponseOK`→`RespondsOk`, опечатка `ShouldSuccesss`→`Succeeds`), префиксные (`ShouldBe→Is`, `ShouldNotBe→IsNot`, `ShouldStatus→ReturnsStatus`, `ShouldStatusCode→ReturnsStatusCode`), общий (спряжение первого глагола в 3-е лицо: `ShouldReturn*`→`Returns*`, `ShouldSaveFile`→`SavesFile`, `ShouldCarryRecommendedPriority`→`CarriesRecommendedPriority`, `Have`→`Has` и т.п.). Правки шли только в строки деклараций тест-методов (поиск от атрибута Fact/Theory), байты/кодировки файлов сохранены.
- Вручную (53): составные глаголы (`CreatesTaskAndReturnCount`→`CreatesTaskAndReturnsCount`, `RejectsInvalidAndAcceptValid`→`...AcceptsValid`, `StoresAndReturnThem`→`...ReturnsThem`), ошибочные спряжения (`ValidationsErrorOf...`→`ReturnsValidationError`, `FailsResult`→`ReturnsFailResult`), перестановки `When/Should`-сегментов (`AreDirectlyConnected_ShouldReturnTrue_WhenConnected`→`AreDirectlyConnected_Connected_ReturnsTrue` ×4, `Generate_ShouldThrowArgumentNullException_WhenTemplateIsEmpty`→`Generate_EmptyTemplate_ThrowsArgumentNullException` ×3), нестандартные (`ShouldOnePack`→`EmitsSinglePack`, `ShouldDidntChangePayload`→`DoesNotChangePayload`, `FieldShouldWriteInNodeMsg`→`WritesFieldToNodeMsg` ×2, `Fail404ShouldReturnNullInsteadException`→`Fails404ReturnsNull` ×10), попутно исправлены опечатки в самих именах: `Corrent`→`Correct`, `Insensevity/Insensetive`→`Insensitivity/Insensitive`, `Terminalte`→`Terminate`.
- Интерфейсы-контракты: `IDefaultRenderEngineTests` — члены синхронизированы с реализацией (`Basic_IndexPage_Succeeds` и др.); это единственный случай ссылки имени тест-метода из кода. `ITemplateEngineSyntaxTests`/`ITemplateEngineInterfaceTests` уже были в каноне.
- Досдача: первый свип пропустил Theory с комментариями между атрибутом и InlineData — повторным проходом добиты последние 5 имён (`MatchUrl_*_ShouldExpect` ×3, `ExtractPortFromUrls`/`NormalizeUrl` в `OptionReaderToolTests`).
- Верификация: сборка 0 ошибок; юнит-наборы: `Mars.Core.Tests` 46, `Mars.Server.Tests` 413, `Mars.SiteEngine.Tests` 61, `Mars.Nodes.Tests` 415, `Mars.Plugin.Tests` 3 (+1 skip), `Mars.Admin.Framework.Tests` 26 — всё зелёное. Интеграционные — компиляция (лексику фильтров в гайдах проверить при следующем прогоне с Docker).

**Тесты — раунд имён, Объём 3: тела под Arrange/Act/Assert (2026-08-30).** Правило: фазы разделяются пустыми строками, ожидания (массивы `expect`) — до акта, новые комментарии-маркеры не добавляются, существующие `//Arrange//Act//Assert` в уже структурированных телах не трогались. Обследование показало: почти все «файлы без разметки» в `Mars.Server.Tests`/`Mars.Nodes.Tests` — свежие тесты, уже структурные; реальная работа сосредоточилась в старых файлах.
- `Mars.Core.Tests`: `TextHelperTests` — ожидания подняты над актом, `SplitArguments` разбит на два теста (`_QuotedAndTupleGroups_` / `_MenuItemsWithNestedBrackets_`), локальная опечатка `notValis`→`notValid`; `CoreAttributesTests` — тест с двумя актами разбит на `IsValid_...` и `TryValidateObject_...` с табличными данными; `TextChainParseTests` — ожидание наверх, убраны отладочный `ITestOutputHelper` и мёртвое поле. 48/48.
- `Mars.Admin.Framework.Tests`: `WysiwygEditorHelperTests` — `WysiwygEditorHelper_GetImages` → `NodeToImageInfo_HtmlWithVariousImages_ReturnsCountAndMinMaxSizes`, `WysiwygEditorHelper_ModifyImages` → `ModifyImages_ByFirstImage_AllImagesGetFirstWidth` (+ убрана неиспользуемая переменная, тела пересобраны).
- `Mars.SiteEngine.Tests`: `BasicExpressionTests` — акты/ассерты разведены (`IfBlock`, `EachBlock` — результаты собираются в переменные, ассерты вместе), удалена закомментированная строка; `DataQueryScenariosTests` — фазы разделены; `WebPageTests` — имена `MatchUrl_*` в канон; **найден и починен латентный баг**: `content.TrimStart().StartsWith(...)` без ассерта (проверка-пустышка) в `WebSitePart_ParseContentTests` ×2 → `.Should().StartWith(...)`; имя `ParseContent_NonHeaderAttributeIgnore_MustIgnored` → `..._IsIgnored`. 61/61.
- `Mars.Server.Tests`: `InterprerTests` → `InterpreterTests` (файл+класс, опечатка); `OptionReaderToolTests` — два имени в канон; `CliUrlsParserTests` — `Parse_SingleUrl/_MultipleUrls` получили ожидания. Остальные файлы партии (валидаторы, сервисы, XActions) — уже AAA, не трогались. 413/413.
- `Mars.Nodes.Tests`: `NodesDocTests` — `Check_*` → `NodeTypes_AllRegistered_HaveFunctionApiDocumentAttribute`, `DocFiles_AllAttributedNodesAndLangs_ExistOnDisk`. 415/415.
- `Mars.Plugin.Tests`: `ReadFromFileContent_ReturnsCorrectValues` → `..._ValidNuspec_ReturnsAllFields`. 3/3 (+1 skip).
- Не тронуто: компактные таблицы проверок, где каждая строка — акт+ассерт (`MyFunctionsTests`, `MyHandlebarsContextFunctionsTests`); суффиксы `_Success`/`_Fail`/`_Tests` — отдельный заход, если потребуется (вне согласованных трёх объёмов).
- Верификация: сборка 0 ошибок; все шесть юнит-проектов зелёные (48+413+61+415+3+26, один скип). Коммит Объёма 3 — по команде пользователя.

**`SysOptions` → `SiteSettings` — финальный wire-переезд (2026-08-30).** Решение пользователя: ломающее окно 0.8.0 открыто, режем всё на `SiteSettings` **без алиаса**. Тип и свойства-носители (`PageRenderContext.SiteSettings`, `Q.Site.SiteSettings`, viewmodels) были переименованы раньше — этот срез добил точки потребления:
- API: `OptionController` — маршруты `[HttpGet/Put("SysOptions")]` → `"SiteSettings"`, методы `GetSysOptions`/`SaveSysOptions` → `GetSiteSettings`/`SaveSiteSettings`.
- `WebApiClient`: `IOptionServiceClient`/`OptionServiceClient` — те же методы + строка маршрута `"SiteSettings"`.
- Рендер-контекст: `HandlebarsTmpCtxBasicDataContext.SysOptionsParamKey` → `SiteSettingsParamKey = "SiteSettings"`; встроенные шаблоны `Res/front_templates/**` (5 файлов, 9 использований) → `{{{SiteSettings.*}}}`. **Пользовательские шаблоны с `{{{SysOptions.*}}}` ломаются осознанно** (алиас не оставляли).
- Админка: компонент `SysOptionsEditForm` → `SiteSettingsEditForm` (`git mv` + 2 использования: `SettingsPage`, `BuilderSettingsPage`).
- Косметика: локальная `sysOptions` → `siteSettings` (`TokenService`, `QueryLangProcessingTests`), комментарии (`MarsSiteTools`, `HeaderAdmin1`).
- Тесты: имена/вызовы/строки маршрутов — `Mars.Server.Tests` (OptionServiceTests, включая `GetSysOptionFromRepo`), `Mars.SiteEngine.Tests` (`Render_ContextHaveBasicData_HasData` — шаблон и словарь), `Mars.Integration.Tests` и `Mars.WebApiClient.Integration.Tests` (Controllers/Options: имена, `nameof(OptionController.*)`, путь `"SiteSettings"`).
- Доки: `docs/dev_docs/AppFront/Handlebars/HandlebarsAppFront.md` — таблица контекста (`SiteSettings` + актуальный путь файла); `ai/FeatureIntegrationGuide.md` — убрано упоминание удалённого спецкейса `SysOption`.
- Верификация: сборка 0 ошибок; `Mars.Server.Tests` OptionServiceTests 9/9; `Mars.SiteEngine.Tests` (RenderEngineRenderTests + QueryLangProcessingTests) 8/8; `Mars.Integration.Tests` (Controllers.Options + лёгкий фронт-набор + GetPageRenderTests) 62/62; `Mars.WebApiClient.Integration.Tests` (Options) 11/11; Docker-регрессия рендера `HandlebarsAppFrontTests` 17/17. ✅ Коммит `7fd1d24`.

**QueryLang — чистка мёртвого кода (2026-08-30).** Решение пользователя: контрактный проект остаётся **`Mars.QueryLang` без суффикса `.Abstractions`** (в отличие от Cms/Identity/Media и др.).
- Удалены мёртвые файлы `Mars.QueryLang.Host` (0 внешних ссылок по всему репо): `DefaultEfQueries.cs` (stub из `NotImplementedException`, предок `EfStringQuery`; вместе с ним ушли остатки `ETotalResponeResult`/`TotalResponse` из мёртвой ветки `#if !true`), `IQueryChainFilter.cs` (эксперимент `QueryBase`/`QueryChainFilter<T>`/`QueryGetter`/`QWhere`/`RegisterFilter` — ссылки только внутри файла), `SqlQueryObjectMappingExtensions.cs` (сниппет `SqlQuery<T>` с захардкоженным Npgsql).
- Закомментированный блок `ListTable` с `ETotalResponeResult` убран из `MediaController`.
- Опечатка `localVaribles` → `localVariables`: параметр `IQueryLangProcessing.Process` + реализация `QueryLangProcessing`, комментарий в `QueryLangLinqDatabaseQueryHandler`, параметр `PageRenderContext.CreateInterpreter`. Вызовы позиционные — правка только именований.
- Живое ядро модуля не тронуто: `IQueryLangProcessing`/`QueryLangProcessing` (`$context` рендера), `IQueryLangLinqDatabaseQueryHandler`/`EfStringQuery<T>`, `IQueryLangHelperAvailableMethodsProvider`, `IDefaultEfQueries<T>`/`IDynamicEfQuery`, `MyThrowHelper`.
- Открытый кандидат на будущую чистку: семейство опечатки `Varibles` (33 вхождения) — свойство `PageRenderContext.TemplateContextVaribles`, интерфейс `ITemplateContextVariblesFiller` + 4 реализации, надпись «global Varibles» в `NodeEditor1.razor`, `VarNode.md`.
- Верификация: сборка 0 ошибок; `Mars.SiteEngine.Tests` (QueryLangProcessingTests + RenderEngineRenderTests) 8/8; Docker-регрессия рендера `HandlebarsAppFrontTests` 17/17.
