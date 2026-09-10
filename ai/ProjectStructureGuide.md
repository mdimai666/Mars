# Mars — гайд по структуре решения

Карта решения, конвенции именования проектов и правила добавления нового кода.
Написан по итогам большой реструктуризации, завершённой 2026-08-30 (см. «История реструктуризации»).
Пошаговое подключение нового модуля — в `ai/FeatureIntegrationGuide.md`.

## Карта решения

### src/

| Директория | Что внутри |
|---|---|
| `src/Mars.WebApp` | Корень композиции — единственное место, где модули собираются в приложение |
| `src/Mars.Core` | Базовые примитивы, общие для всех |
| `src/Mars.Contracts` | Глобальные wire-DTO (клиент↔сервер), WASM-безопасные |
| `src/Server` | Ядро и данные (см. ниже) |
| `src/Mars.Modules` | Все модули платформы, плоско (см. ниже) |
| `src/Mars.Nodes` | Движок нод визуального программирования |
| `src/Mars.Datasource` | Подключения к внешним базам данных |
| `src/Admin` | Библиотеки админки: `Mars.Admin.Framework` (компоненты), `Mars.Admin.Contracts`, `Mars.Admin.Host` (хостинг админки на сервере) |
| `src/Mars.Admin` | Админка — Blazor WASM-приложение |
| `src/Mars.WebApiClient` | Типизированный клиент API Mars |
| `src/Plugin` | Система плагинов (`Mars.Plugin.*` + `PluginExample`) |
| `src/Modules` | Переиспользуемые библиотеки и UI-компоненты (`MarsEditors`, `EditorJsBlazored`, `MarsCodeEditor2`, `BlazoredHtmlRender`, `Mars.HttpSmartAuthFlow`) |

`src/Server` (плоско):

- `Mars.Server` — тонкое ядро: пайплайн, middleware, сидинг, XAction-инфраструктура;
- `Mars.Server.Abstractions` — сквозные серверные интерфейсы (`IRequestContext`, `IFileStorage`, хендлеры);
- `Mars.Server.Contracts` — wire-DTO ядра;
- `Mars.Data` — общий `MarsDbContext` (не разрезается на модульные контексты);
- `Mars.Data.PostgreSQL` / `Mars.Data.InMemory` — провайдеры БД;
- `Mars.Data.Repositories` / `Mars.Data.Infrastructure` — репозитории и инфраструктура данных.

`src/Mars.Modules` — физически плоско: каждый проект лежит прямо в `src/Mars.Modules/<ИмяПроекта>/`,
без подпапок. Группировка в Solution Explorer — только виртуальные папки `Mars.slnx`. Модули:

Cms, Media, Identity, Notifications, SiteEngine (+ Handlebars, Templators), Options, XActions,
SSO, Scheduler, Docker, Excel, QueryLang, AiChat, SemanticKernel, TemplateEngines
(Host + Providers.Handlebars/Scriban), WebApp.Nodes, MetaModelGenerator, CommandLine.

`src/Mars.Nodes` — `Mars.Nodes.Core` (+ `.Core.Implements`), `Abstractions`, `Host`,
`Front.Abstractions`, `Workspace` (Blazor-редактор), `FormEditor`.

### tests/

Конвенция имён: `Mars.X.Tests` (юниты), `Mars.X.Integration.Tests` (интеграционные).
Кроме того: `Mars.E2E.Tests`, `Mars.Cli.EndToEnd.Tests`, `Mars.Test.Common` (общие фикстуры),
`ExternalServices.*` (инфраструктура Testcontainers).

Верификация — точечная: сборка + тесты только затронутых областей, без прогонов всего сьюта.

### Корень репо

`devstands/` — дев-стенды, `benchmarks/` — нагрузочные тесты,
`docs/` — сайт документации (источники в `docs/dev_docs`, рендерит `docs/MarsDocs.WebApp`),
`ai/` — гайды и контекст для ИИ-агентов.

## Конвенция суффиксов проектов

| Суффикс | Роль | Требования |
|---|---|---|
| `Mars.X` (без суффикса) | общие типы модуля | |
| `Mars.X.Contracts` | wire-DTO через границу клиент↔сервер | WASM-безопасен: без AspNetCore/EF |
| `Mars.X.Abstractions` | серверные контракты: интерфейсы, точки расширения | без тяжёлой имплементации (DI-граф, EF) |
| `Mars.X.Host` | серверная реализация модуля | на него не ссылаются другие модули |
| `Mars.X.Front` | WASM-фронт модуля | компилируется под браузер |

Легаси-суффиксы `.Shared` / `.Host.Shared` не используются.
Роли-образцы: `.Abstractions` = контракт, `.Kit` (`Mars.Plugin.Kit.Host/Front`) = тяжёлые готовые помощники для плагинов.

### Что живёт в `Contracts` vs `Abstractions` (де-факто конвенция)

Граница между `Mars.X.Contracts` и `Mars.X.Abstractions` — **WASM**: типы, которые видит
браузер (фронт `Mars.Admin`/`.Front`, WebApiClient), живут в `Contracts`; всё, что нужно
только серверу, — в `Abstractions`. Критерий размещения — «кто это видит».

| Проект / папка | Что кладём |
|---|---|
| `Mars.X.Contracts` | wire-DTO через клиент↔сервер, модели запросов/ответов API, типы для WASM-фронта и WebApiClient. Никаких EF / AspNetCore / DI-зависимостей |
| `Mars.X.Abstractions/Dto/` | серверные query/command-объекты и их валидаторы (`CreatePostQuery`, `UpdateUserQuery`), серверные DTO, не уходящие в браузер |
| `Mars.X.Abstractions/Mappings/` | маппинги entity/DTO → Contracts-ответы (`PostMapping`, `OptionMapping`) |
| `Mars.X.Abstractions/Services/` | интерфейсы сервисов модуля (`IPostService`, `IOptionService`) |
| `Mars.X.Abstractions/Interfaces/` | прочие сквозные интерфейсы (например `IRequestContext`) |
| `Mars.X.Abstractions/` (остальное) | точки расширения, модели, валидаторы — без тяжёлой имплементации (DI-граф, EF) |

Query-объекты и маппинги лежат в `Abstractions` (не в `Contracts`), даже если их имена
похожи на wire-DTO: фронт их не использует, и тащить их в WASM-пакет незачем.
Внутри модуля допустимо отклоняться от набора папок (например `SiteEngine.Abstractions`
добавляет `WebSite/`, `Templators/`) — главное, чтобы `Contracts` оставались WASM-чистыми.

## Правила направленности и композиции

1. Модуль ссылается только на чужие `Contracts`/`Abstractions`. Ссылки на чужие `.Host` запрещены.
2. Сборка модулей — только в корне композиции (`Mars.WebApp`).
3. Каждый модуль регистрируется своими хуками: `AddXxx()` (DI) и при необходимости `UseXxx()` (пайплайн); контроллеры — через application parts в мейне модуля.
4. `MarsDbContext` общий; доступ через свои сервисы/репозитории, напрямую в контекст — только где оправдано. Без MediatR.
5. Физическая раскладка плоская; подпапки на диске не создаются, группировка — виртуальные папки `Mars.slnx`.
6. Опции: движок — семейство `Mars.Options` (Abstractions/Contracts/Host); конкретные модели опций живут в `Contracts` модуля-владельца и регистрируются его Use-хуком.

## Как добавлять новый модуль

1. Состав: как правило `Mars.X.Contracts` + `Mars.X.Abstractions` + `Mars.X.Host` (+ `.Front` при наличии WASM-фронта).
2. Проекты — плоско в `src/Mars.Modules/<ИмяПроекта>/`, добавить в `Mars.slnx` (виртуальная папка по домену).
3. Подключение в `Mars.WebApp`: вызовы `AddXxx()`/`UseXxx()` в `MarsWebAppStartup`.
4. Тесты: `tests/Mars.X.Tests` / `tests/Mars.X.Integration.Tests`.

Подробности и грабли (feature-флаги, контроллеры, WebApiClient, SignalR) — в `ai/FeatureIntegrationGuide.md`.

## История реструктуризации (кратко)

2026-08-30 завершена большая реструктуризация решения: монолитный `Mars.Host` разрезан на модули,
унифицированы имена проектов, namespace'ы, раскладка папок и имена тестов. Влито в `ai/begin` одним
сквош-коммитом `47c60b56` — *refactor [solution] major restructure: modules, renames, folder layout*;
полная история (71 коммит) сохранена в ветке `ai/restructure-phase1`.

Старые имена в репо больше не существуют (могут встречаться только в старых доках и планах):

| Старое имя | Новое имя |
|---|---|
| `Mars.Host` | `Mars.Server` (содержимое разнесено по модулям) |
| `Mars.Host.Shared` | распущен в `*.Abstractions` модулей и `Mars.Server.Abstractions` |
| `Mars.Shared` | `Mars.Contracts` |
| `Mars.Host.Data*` | `Mars.Data*` |
| `AppAdmin` | `Mars.Admin` |
| `AppFront.Main`, `AppFront.Shared` | `Mars.Admin.Framework` |
| `Mars.WebSiteProcessor` | `Mars.SiteEngine` |
| `Test.Mars.*` (тесты) | `Mars.X.Tests` / `Mars.X.Integration.Tests` |

## Агентам

- Сначала определи по карте, какой модуль владеет задачей; поиск и правки скоупь на этот модуль и его тесты. Не сканируй весь проект без необходимости.
- Для структурного поиска используй граф кода (codebase-memory) со скоупом по `path`, либо grep по конкретным директориям.
- Фронт (`Mars.Admin`, `Mars.Admin.Framework`) в графе кода не индексируется — фронтовые зависимости проверяй grep'ом.
- Не клади wire-DTO в `.Host` и серверные зависимости в `Contracts` (WASM-граница).
- Новые проекты — плоско; группировка только виртуальными папками `Mars.slnx`.
- После структурных изменений: `dotnet build Mars.slnx` + точечные тесты затронутых областей.
