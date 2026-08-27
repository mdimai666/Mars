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
5. **Рендеринг сайта** — модуль **`Mars.SiteEngine`**: Templators, WebSite-скрипты/ассет-провайдеры, вливание `Mars.WebSiteProcessor`; провайдеры шаблонизации подключаются отдельно (вопрос слияния `Mars.TemplateEngine.*` — открытый).
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
| `Mars.TemplateEngine.Host`, `.Providers.*` | открыто | кандидат на слияние с SiteEngine как провайдеры |
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
- Чистка имён: опечатки (`MetaValueRequestExternsions`, `AuthCreditionals`, `FileSizeExstension`), `MockClass2`, и т.п.

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
