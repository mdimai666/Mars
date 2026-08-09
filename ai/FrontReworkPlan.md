# План рефакторинга рендера фронта (Front Rework Plan)

Задача: [ReworkTemplateEnginePrompt.md](./ReworkTemplateEnginePrompt.md).
Документ — пошаговый план: рефакторить/переделать рендер фронта (Handlebars), уйти от настроек в
appsettings и «магических» постов в БД к файловым фронтам в `data/fronts/`, управляемым из админки
в рантайме, и добавить редактор фронта с ИИ-чатом.

## Принятые решения

1. **Мультифронт — остаётся.** Модель: список фронтов, у каждого свой slug, Url-маунт (`/`, `/app2`, …),
   путь к папке, движок рендера, вкл/выкл.
2. **DB-легаси — полный демонтаж.** DB-рендер (посты типов `page/template/block/layout`, HostHtml как
   `_root`) отключается; сами пост-типы удаляются из системы.
3. **Пайплайн — полностью динамический.** Смена движка, вкл/выкл фронта и добавление нового URL
   работают без рестарта приложения.
4. **ИИ-чат — переиспользуем глобальный AiChat.** Страница редактора получает дополнительный контекст
   (PageContext со slug фронта) и файловые инструменты агента.
5. **Blazor-рендер и ноды RenderPage — не трогаем** (следующие задачи). `{{#context}}`-eval — не делаем.

---

## 1. Как устроено сейчас (as-is)

Точка входа — `src/Mars.WebApp/UseStartup/StartupFront.cs`:

- `AddFront()` (рано, **до** `AddPlugins()`): создаёт `MarsAppProvider`
  (`src/Mars.WebApp/Services/MarsAppProvider.cs`) — **один раз** читает секцию `AppFront` из
  appsettings (`AppFrontSettingsCfg`: `Mode/Url/Path`, enum `AppFrontMode` в `src/Mars.Core/Models/`),
  регистрирует singleton `IMarsAppProvider`; вызывает `AddWREHandlebars/AddWREDatabaseHandlebars/AddWREBlazor`.
- `UseFront()` (**последний** вызов в `ConfigureApp`, после `UsePlugins()`): для каждого app из
  `AppProvider.Apps` берёт `IWebRenderEngine` из `MarsAppFront.Features` (FeatureCollection,
  `src/Mars.Host.Shared/Models/MarsAppFront.cs`) — движок туда кладут `UseWRE*`-методы модулей по
  фильтру `Mode` (`ActivatorUtilities.CreateInstance`), т.е. **движки не живут в DI** — затем
  `renderEngine.Setup()` и `app.Map(url, …)` с пайплайном (Routing/Authorization/MapControllers +
  fallback).
- Рендер запроса: fallback → `IWebSiteProcessor.Response`
  (`src/Mars.Modules/Mars.WebSiteProcessor/Endpoints/MapWebSiteProcessor.cs`) →
  `WebSiteRequestProcessor` (`.../Services/WebSiteRequestProcessor.cs`): матчинг URL по
  `CompiledHttpRouteMatcher` (страницы с атрибутом `@page`), кэш по `@cache`/`@cache-force`,
  подготовка `PageRenderContext` → `renderEngine.RenderPage(...)`.
- Источник шаблонов: `WebTemplateService` (`.../Services/WebTemplateService.cs`) —
  **смешивает файлы и БД**: `WebTemplateFilesystemSource` (папка `Path`, FileSystemWatcher на
  `.hbs/.css/.js/.resx`, SignalR-события `reload`/`refreshcss` через `ChatHub`) +
  `WebTemplateDatabaseSource` (`.../WebSite/SourceProviders/WebTemplateDatabaseSource.cs`) — читает
  посты типов `page/template/block/layout` и `FrontOptions.HostItems[].HostHtml` как содержимое
  `_root.hbs` («магия», от которой уходим).
- Модель частей сайта: `WebSitePart`/`WebPage`/`WebRoot`/`WebSiteTemplate`
  (`src/Mars.Host.Shared/WebSite/Models/`) — парсинг атрибутов `@page`/`@layout`/`@cache`/`@title`
  из начала файла уже реализован и остаётся.
- Настройки в админке: `SettingsHostHtml.razor` (`src/AppAdmin/Pages/Settings/`) редактирует
  `FrontOptions` (`src/Mars.Shared/Options/FrontOptions.cs` — `HostItems[{Url, HostHtml}]`),
  read-only `FrontSettingsPage.razor` (`/Settings/Front`) показывает `AppFrontSettingsCfg` через
  `OptionController.AppFrontSettings()`.
- Опции: таблица `Options` (`OptionRepository`), `IOptionService.RegisterOption/SaveOption/GetOption`,
  события: `IEventManager` топик `Option.{ClassName}` и `OnOptionUpdate` — всё готово для рантайм-реакции.
- `Mars.TemplateEngine.Host` (`ITemplateManager` + `ITemplateEngine`) — **не** про фронт, это движки
  шаблонов для нод/писем. Для фронта делаем свой реестр (см. фазу 1), но паттерн (DI + метаданные
  `[Display]`) берём за образец.
- Пример целевой структуры папки фронта:
  `C:\Users\D\Documents\VisualStudio\2025\MyMarsHandlebarsSiteTemplate` —
  `_root.hbs`, `pages/` (`@page "/posts"`), `blocks/`, `layout/`, `wwwroot/`.

Ключевые грабли:

- `hot-reload.js` (`src/Mars.WebApp/wwwroot/mars/js/hot-reload.js`) подключается к `/_ws/ws`, но хаб
  замаплен на `/_ws/admin` (`ChatHub`) — авто-перезагрузка страниц фронта сейчас не работает.
- `AddFront()` выполняется до `AddPlugins()`, поэтому плагины не могут влиять на создание движков в
  текущей схеме; но `UseFront()` — последний, поэтому новая схема должна резолвить движки из DI в
  рантайме (тогда плагины смогут регистрировать фабрики в `ConfigureWebApplicationBuilder`).

---

## 2. Целевая архитектура (to-be)

```
FrontsOption (опция в БД, таблица Options)
   └─ список FrontItem { Slug, Title, Url, Path, EngineId, Enabled }
          │  Path пусто → data/fronts/<slug>; иначе внешняя папка
          ▼
IFrontManager (singleton)  ──подписка на IEventManager OptionUpdate(nameof(FrontsOption))──
   │   актуальный список фронтов + кэш созданных движков (пересборка при изменении опции)
   ▼
единый динамический middleware в пайплайне (после админки/API)
   │   резолв фронта по URL-префиксу на КАЖДЫЙ запрос → только Enabled
   ├─ статика: <front>/wwwroot (PhysicalFileProvider)
   └─ рендер: IWebRenderEngine по EngineId через реестр IWebRenderEngineFactory (DI)
```

- **Реестр движков**: `IWebRenderEngineFactory` регистрируются в DI как `IEnumerable<>` — встроенные
  (Handlebars) + плагинные (плагин добавляет фабрику в `ConfigureWebApplicationBuilder`). Метаданные
  через `[Display]` (как у `TemplateManager`).
- **Список фронтов** редактируется в админке (страница настроек), изменения применяются сразу.
- **Хранение фронта**: папка `data/fronts/<slug>` (`_root.hbs`, `pages/`, `blocks/`, `layout/`,
  `wwwroot/`); поддерживается внешняя папка (`Path` = абсолютный путь).
- **Всё в файлах**: страницы, блоки и layout'ы — только файлы с атрибутами
  `@page`/`@layout`/`@cache`. Посты `page/template/block/layout` и `FrontOptions.HostHtml`
  больше не участвуют в рендере.

---

## 3. Фазы

Каждая фаза — отдельный коммит-набор, после каждой фазы система собирается и работает
(`dotnet build Mars.slnx` + прогон соответствующих тестов). Порядок фаз 0→7 соблюдается
(1 зависит от 0, 2 от 1, 3 от 2; 4 можно параллелить с 3; 5 и 6 после 4).

### Фаза 0. Доменная модель фронтов ✅ (2026-08-07)

**Цель**: настройка фронтов — опция в БД, а не appsettings.

- Новая опция `FrontsOption` в `src/Mars.Shared/Options/FrontsOption.cs`:
  `List<FrontItem>`, `FrontItem { Slug, Title, Url (нормализация как у `FrontOptionHostItem.Url`),
  Path ("" = по умолчанию), EngineId, Enabled }`.
- Интерфейс `IFrontManager` рядом с `IMarsAppProvider` (`src/Mars.Host.Shared/Services/`),
  реализация `FrontManager` в `src/Mars.WebApp/Services/`:
  читает `FrontsOption` через `IOptionService`, подписка на
  `IEventManager.OptionUpdate(nameof(FrontsOption))` (паттерн подписки — `WebTemplateService`),
  отдаёт актуальный список, резолвит физический путь (`ContentRoot/data/fronts/<slug>` либо `Path`),
  валидирует slug/Url.
- Регистрация: `RegisterOption<FrontsOption>()` в `src/Mars.Options/Mars.Options.Host/MainOptions.cs`.
- Миграция при старте: если `FrontsOption` пустая, а в appsettings есть секция `AppFront`
  (`Mode=HandlebarsTemplateStatic`) — перенести записи в опцию (Url/Path), чтобы существующие
  инсталляции продолжили работать. Setup-визард (`src/Mars.WebApp/Setup/SetupService.cs`) пока
  продолжает писать `AppFront` — он остаётся бутстрапом, миграция подхватывает.

**Готово когда**: опция зарегистрирована, `FrontManager` отдаёт фронты, миграция из appsettings
работает (проверка логом/тестом).

### Фаза 1. Реестр движков и динамический пайплайн ✅ (2026-08-07)

**Цель**: движок рендера выбирается в рантайме из настроек; новый URL/вкл-выкл без рестарта;
плагины могут добавлять свои движки.

- Новый интерфейс `IWebRenderEngineFactory` в `src/Mars.Modules/Mars.WebSiteProcessor/Interfaces/`:
  `string Id`, метаданные через `[Display(Name, Description)]`,
  `IWebRenderEngine Create(MarsAppFront appFront, IServiceProvider services)`.
  Встроенная фабрика для Handlebars поверх существующего
  `HandlebarsWebRenderEngine` (`src/Mars.Modules/Mars.WebSiteProcessor.Handlebars/`).
- `IFrontManager` кэширует созданные движки per-front (ключ: slug + ревизия настроек) и пересоздаёт
  при `OptionUpdate` — так смена `EngineId` применяется без рестарта.
- `StartupFront` переписывается: вместо `app.Map` на каждый URL — один общий fallback-middleware
  (после маршрутов админки/API): на запрос резолвит фронт по наиболее специфичному URL-префиксу
  (логика `MarsAppProvider.GetAppForUrl`), если фронт выключен/не найден — 404; иначе статика
  `<front>/wwwroot` (кэш `PhysicalFileProvider` per-front) и рендер движком.
  `app.Map(url) { MapControllers }` убрать — контроллеры уже мапятся глобально (`app.MapControllers()`
  в `MarsWebAppStartup.ConfigureApp`); проверить, что ничего из per-front контроллеров не теряется.
- `IWebRenderEngine` ужать до реально используемого контракта (`RenderPage`-перегрузка с
  `RenderEngineRenderRequestContext` остаётся — её зовёт `WebSiteRequestProcessor`).
- Убрать создание движков через `Features.Set<IWebRenderEngine>` из `UseWRE*`.
- Расширение плагинами: фабрика регистрируется в DI в `ConfigureWebApplicationBuilder`
  (вызывается до `Build()` — успеет в `IEnumerable<IWebRenderEngineFactory>`); дополнить
  `ai/PluginCreationGuide.md` примером.

**Готово когда**: фронт отдаётся через новый middleware; смена `EngineId`/`Enabled`/нового Url в
опции меняет поведение без рестарта; `dotnet build` зелёный; тесты `HandlebarsEngine` из
`tests/Mars.AppFrontEngines.Integration.Tests` переведены на новую схему и проходят.

> **Как реализовано (заметки)**: кэш движков живёт не в `FrontManager`, а в
> `WebRenderEngineLocator` (`Mars.WebSiteProcessor/Services`, интерфейс `IWebRenderEngineLocator`) —
> `Mars.Host.Shared` не может ссылаться на модуль. Локатор подписан на `FrontManager.Changed`
> и инвалидирует кэш per-front по diff'у полей. Пайплайн: статика раздаётся отдельным middleware
> `FrontStaticFilesMiddleware` в `StartupFront` (ВАЖНО: `MapFallback` имеет route-ограничение
> `:nonfile` и не матчит URL с точкой — в fallback рендер попадает только «не-файлы», как и в старой
> схеме). Рендер — `app.MapFallback` → `IWebSiteProcessor.Response`; `/api/{**slug}` — JSON-404.
> Движки создаются лениво на первом запросе к фронту. `MarsAppFront.Configuration` пока остаётся
> `AppFrontSettingsCfg` (заполняется из `FrontItem`, Mode=HandlebarsTemplateStatic) — тип
> пересматривать в Фазе 3 вместе с демонтажом DB-легаси. Blazor/DatabaseHandlebars движки не
> регистрируются (спят до своих фаз), `IMarsAppProvider` работает как раньше до Фазы 7.

### Фаза 2. Файловые фронты и стартовый шаблон ✅ (2026-08-07)

**Цель**: фронт = только папка с файлами; создание фронта в один клик.

- Конвенция `data/fronts/<slug>` — по образцу `data/plugins` (`PluginManager.PluginsDefaultPath`),
  базовый путь: keyed-`IFileStorage("data")`/`ContentRoot/data` (`src/Mars.Host/MainMarsHost.cs`,
  `UseFileStorages`).
- Стартовый шаблон: `src/Mars.WebApp/Res/front_templates/default/` — содержимое
  `MyMarsHandlebarsSiteTemplate` (`_root.hbs`, `pages/`, `blocks/`, `layout/`, `wwwroot/`; без
  `.git`, `*.ps1`, `appsettings.local.json`, `global.d.ts`). `Res/` — нередактируемые ресурсы.
- Действие «создать фронт»: копирование `Res/front_templates/default` → `data/fronts/<slug>`
  (сервис в `Mars.WebApp`, вызывается REST-эндпоинтом из админки, фаза 4).
- `WebTemplateService.ScanSite()` — только файловый источник: убрать подмешивание
  `WebTemplateDatabaseSource` и `FrontOptions.HostHtml`-как-`_root`
  (строки с `dbTemplateSource` в `.../Services/WebTemplateService.cs`).
- Внешняя папка: `FrontItem.Path` = абсолютный путь (сценарий: разработка на
  `MyMarsHandlebarsSiteTemplate` без копирования). FileSystemWatcher уже умеет произвольный путь.

**Готово когда**: чистый старт создаёт дефолтный фронт и сайт открывается на `/`; внешний путь
работает; FileSystemWatcher + hot-reload события продолжаются.

> **Как реализовано (заметки)**: стартовый шаблон — `src/Mars.WebApp/Res/front_templates/default/`
> (копия `MyMarsHandlebarsSiteTemplate` без служебных файлов; `Res/**` уже копируется в output).
> `FrontTemplateService` (Mars.WebApp/Services) копирует шаблон в `data/fronts/<slug>` с валидацией slug.
> Бутстрап `EnsureDefaultFront()` (в `AppFrontMigration`): вызывается в `ConfigureApp` после миграции;
> если `FrontsOption` пуст — создаёт фронт `default` из шаблона и пишет опцию; в тестах пропускается
> (`MarsStartupInfo.IsTesting`). `WebTemplateService.ScanSite()` — только файловый источник, подписки
> на пост-события и `FrontOptions` удалены. `{{#context}}` в шаблоне работает (реализован в движке:
> `MyHandlebarsContextFunctions`/QueryLang) — его развитие вне скоупа, но стартовый шаблон им пользуется.

### Фаза 3. Демонтаж DB-легаси ✅ (2026-08-07)

**Цель**: убрать «магическую» отрисовку постов и сами типы.

- Удалить из пайплайна и кода:
  - `WebTemplateDatabaseSource` (`.../WebSite/SourceProviders/`) и его подписки на события постов;
  - `WebDatabaseTemplateService` (`.../Services/`);
  - модуль `Mars.WebSiteProcessor.DatabaseHandlebars` (проект целиком) и вызов
    `AddWREDatabaseHandlebars/UseWREDatabaseHandlebars` из `StartupFront`;
  - `FrontRoutingOption` (`src/Mars.Shared/Options/FrontRoutingOption.cs`), страницу
    `SettingsFrontRouting.razor`, регистрацию в `MainOptions.cs` (опция нигде не читается рендером).
- Пост-типы `page/template/block/layout`: убрать из seeding/InitialSiteData
  (`src/Mars.Host/Handlers/InitialSiteDataViewModelHandler.cs` и связанные seed-данные);
  существующие записи в БД пользователей не трогаем (они просто перестают отрисовываться).
- Аудит ссылок на `Post.page` (`grep "Post.page"`, `SelectRelataionModel2`, `MaintenanceModeOption`
  с `EMaintenancePageSource.PostPage` → у `WebSiteRequestProcessor.RenderMaintenancePage` ветка
  `PostPage` берёт страницу из `template.Pages` — продолжит работать по файловым страницам, но
  `FrontRoutingOption`-зависимости вычистить).
- Тесты: `tests/Mars.AppFrontEngines.Integration.Tests/DatabaseHandlebarsEngine` — удалить;
  `BlazorEngine` — заморозить (Skip), движок не регистрируется до отдельной задачи.

**Готово когда**: рендер не читает посты; сборка и оставшиеся тесты зелёные; в админке нет страниц
про DB-фронт.

> **Как реализовано (заметки)**: удалены `WebTemplateDatabaseSource`, `WebDatabaseTemplateService`,
> проект `Mars.WebSiteProcessor.DatabaseHandlebars` (из slnx + из ссылок WebApp/Blazor).
> `BlazorWebRenderEngine` перебазирован на `HandlebarsWebRenderEngine` (файловые шаблоны; DB-Initialize
> удалён) — Blazor-рендер по-прежнему спит до своей задачи. Удалены `FrontRoutingOption` +
> `SettingsFrontRouting.razor` + регистрация. Из seeding убраны пост-типы `page/template/block` и их
> посты (остались `post` + NavMenu + hello-post). XActions-рецепты DB-шаблонов удалены, кнопка
> «create example» в `EditPostTypePresentationPage` убрана. Тесты `DatabaseHandlebarsEngine` удалены,
> `BlazorEngine` — Skip. Ссылка `Mars.Host.Data` из `Mars.WebSiteProcessor` убрана (добавлена явно в
> `Mars.WebSiteProcessor.Handlebars` — там реально используется `MarsDbContext` в help-блоке).
> `PostTypePresentationRenderHandler`/`PageRenderService` не тронуты — рендерят посты поверх файловых
> шаблонов. `Post.page`-специфика в админ-вьюхах posts оставлена (для уже существующих БД).

### Фаза 4. Админка — список фронтов (переделка SettingsHostHtml) ✅ (2026-08-07)

**Цель**: настройки фронта — простой список: путь, движок, вкл/выкл; клик → редактор.

- Страница `/Settings/Front` (`FrontSettingsPage.razor`) становится редактируемым списком
  `EditOptionForm<FrontsOption>` (компонент `src/AppFront.Main/Components/EditOptionForm.razor`):
  для каждого фронта: Slug, Title, Url, Path (пусто = `data/fronts/<slug>`), выбор `EngineId`
  из доступных фабрик, тумблер `Enabled`, кнопки «Открыть редактор», «Удалить»; сверху —
  «Создать фронт» (slug + стартовый шаблон).
- Новые эндпоинты (расширить `FrontController`/`OptionController` в `src/Mars.WebApp/Controllers/`):
  список доступных движков (метаданные фабрик), создание фронта из шаблона, удаление фронта
  (опционально с папкой), валидация внешнего пути. Клиент — `Mars.WebApiClient`
  (`IFrontServiceClient`/`IOptionServiceClient`).
- Удалить: `SettingsHostHtml.razor` (маршрут `/Settings/html`), использование
  `FrontOptions.HostItems/HostHtml` из рендера (класс `FrontOptions` можно оставить до фазы
  Blazor, но из `MainOptions` и меню убрать), пункт меню «Host html» в `ASideOptions.razor`
  заменить на «Фронты»; `SettingsTabs.razor` — поправить ссылку.
- Старую read-only `FrontSettingsPage` не дублировать — она и становится новой страницей.

**Готово когда**: список фронтов управляется из админки; изменения применяются без рестарта
(проверить вместе с фазой 1); `Settings/html` удалён.

> **Как реализовано (заметки)**: `/Settings/Front` — `EditOptionForm<FrontsOption>` с карточками
> фронтов (Title/Url/Path/движок select из `client.Front.Engines()`/вкл) + «Создать фронт»
> (`FrontController.CreateFront` → копия шаблона в `data/fronts/<slug>` + запись в опцию) +
> удаление (опция «с папкой»; внешние папки через API не удаляются). `SettingsHostHtml.razor`
> удалён, регистрация `FrontOptions` убрана из `MainOptions` (класс жив до Blazor-фазы),
> `OptionController.AppFrontSettings` и клиентский метод удалены; меню `ASideOptions` и
> `SettingsTabs` исправлены. Новые REST: `Engines`, `CreateFront`, `DeleteFront` в `FrontController`.
> Переход в редактор: `NavigateTo("front/editor/{slug}")`.

### Фаза 5. Редактор фронта ✅ (2026-08-07)

**Цель**: страница-редактор в стиле VSCode: дерево файлов | код | живой предпросмотр.

- Новая страница `src/AppAdmin/Pages/FrontEditor/FrontEditorPage.razor`, маршрут
  `/front/editor/{slug}`, layout — `BuilderLayout` (`src/AppAdmin/Shared/BuilderLayout.razor`).
- Три панели: слева дерево файлов фронта (переиспользовать подход
  `src/AppAdmin/Pages/PageViews/AppFrontThemeFilesViewer.razor` +
  `AppFrontTemplateViewPage.razor`), по центру `CodeEditor2` (`MarsCodeEditor2`, язык
  `handlebars`, `OnSave` уже есть — см. `SettingsHostHtml.razor` как пример), справа iframe с
  сайтом фронта (`NavigationManager.BaseUri` + Url фронта). Кнопка полноэкранного предпросмотра
  (скрывает дерево/редактор).
- Файловый CRUD REST в `FrontController`: `GetTree(slug)`, `GetFile(slug, relPath)` (частично есть:
  `FrontFiles()`/`GetPart()`), `SaveFile`, `CreateFile/CreateFolder`, `Rename`, `Delete` — всё через
  общий серверный `FrontFilesService` (нужен и для REST, и для ИИ-инструментов фазы 6) с защитой:
  роль Admin, пути только относительные, нормализация и проверка выхода за корень папки фронта.
- Live-reload: `WebTemplateService` уже шлёт `reload`/`refreshcss` в `ChatHub` при изменении файлов.
  Страница редактора подписывается (админка уже подключена к `/_ws/admin`) и перезагружает iframe
  (cache-busting query). Попутно починить `wwwroot/mars/js/hot-reload.js`: URL хаба `/_ws/ws` →
  `/_ws/admin` — тогда превью обновляется и само.

**Готово когда**: открыл фронт в редакторе, отредактировал `_root.hbs`, сохранил — iframe показал
изменения; файлы создаются/переименовываются/удаляются из дерева; полноэкранный предпросмотр
работает.

> **Как реализовано (заметки)**: страница `src/AppAdmin/Builder/FrontEditorViews/FrontEditorPage.razor`,
> маршрут `/front/editor/{Slug}` (layout `BuilderLayout` через `Builder/_Imports.razor`).
> Дерево — плоский список из `FrontController.FrontTree` (раскрытие папок в состоянии страницы),
> создание/переименование/удаление — файловый CRUD `FrontController`'а. Редактор — `CodeEditor2`
> (язык по расширению, Ctrl+S → `SaveFrontFile`). Превью — iframe `Q.BackendUrl + front.Url`,
> перезагрузка через `@key`-счётчик. Live-reload: `ClientHub` подписан на `reload`/`refreshcss`
> (событие `OnFrontReload`), `hot-reload.js` исправлен на `/_ws/admin`.
> `FrontFilesService` (Mars.WebApp/Services) — общий файловый слой с защитой путей
> (нормализация + `StartsWith` корня фронта) — его же использует Фаза 6 для ИИ-инструментов.

### Фаза 6. ИИ-чат в редакторе

**Цель**: ИИ правит файлы фронта, изменения сразу видны в предпросмотре.

- Новый класс инструментов `src/Mars.Modules/Mars.AiChat.Host/Tools/MarsFrontFilesTools.cs`:
  `list_front_files`, `read_front_file`, `write_front_file`, `create_front_file`,
  `delete_front_file` — через общий `FrontFilesService` из фазы 5 (защита путей наследуется);
  `[Description]` на методах и параметрах обязательны (см. `ai/AiChatGuide.md`).
- Подключение: в `AiChatAgentService.RunChatAsync` добавить инструменты в массив `tools`
  (`AIFunctionFactory.Create`); slug фронта брать из `PageContext` (URL страницы редактора содержит
  slug); правила работы с файлами фронта добавить в `AiChatPrompts.BaseInstructions`/контекстные
  инструкции (правки относительными путями, превью обновляется автоматически, после правок
  сообщать что изменено).
- Контекст страницы: редактор реализует `IAiChatPageHandler`
  (`src/Mars.Modules/Mars.AiChat.Front/Services/IAiChatPageHandler.cs`, паттерн `EditPostView`) —
  отдаёт `GetInfo` (фронт, открытый файл), чтобы агент понимал контекст без лишних вопросов.
- Глобальный терминал AiChat уже смонтирован в `App.razor` — отдельный UI чата не нужен.

**Готово когда**: на странице редактора просьба «поменяй заголовок сайта в шапке» приводит к правке
файла агентом, и изменение видно в iframe без ручных действий.

### Фаза 7. Чистка и документация ✅ (2026-08-07)

- Удалить мёртвый код: `StartupStaticHandlebars.cs` (если не используется новой схемой),
  `OptionController.AppFrontSettings()` и `IOptionServiceClient.AppFrontSettings()` (заменить на
  работу с `FrontsOption`), закомментированные куски в `HandlebarsWebRenderEngine`/`FrontOptions`.
- `IMarsAppProvider` оставить как фасад над `IFrontManager` (много потребителей: ноды
  `RenderPageNodeImpl`, `MarsHostRootLayoutRenderNodeImpl`, `PostTypePresentationRenderHandler`,
  `_Host.cshtml`, контроллеры) — либо переводить потребителей постепенно, начиная с простых.
- appsettings: секцию `AppFront` из `appsettings.json` убрать (остаётся только миграционное чтение).
- Документация: страница в `docs/dev_docs/` (конвенции `ai/DocsGuide.md`), обновить
  `ai/FeatureIntegrationGuide.md`/`ai/PluginCreationGuide.md` в части движков рендера и фронтов.

> **Как реализовано (заметки)**: `OptionController.AppFrontSettings`/клиентский метод были удалены
> ещё в Фазе 4. Теперь удалены: `StartupStaticHandlebars.cs` (мёртвый), `UseWREBlazor` и неиспользуемый
> параметр `apps` из `AddWREHandlebars/AddWREBlazor`; из `HandlebarsWebRenderEngine` — мёртвые
> `UseFront`/`GetRenderKey` и закомментированные куски (из `BlazorWebRenderEngine` — `override UseFront`
> и FrontOptions-комментарии, иначе не компилировалось); неиспользуемый резолв `IMarsAppProvider` в
> `MapWebSiteProcessor`. **`IMarsAppProvider` теперь read-only фасад**: `MarsAppProvider` лениво резолвит
> `IFrontManager`/`IWebRenderEngineLocator`; `GetAppForUrl`/`FirstApp` — из кэша локатора (тот же
> инстанс `MarsAppFront`, что видит пайплайн — важно для тестов, подменяющих `Features`),
> `Apps` — по включённым фронтам, `SetupMultiApps` = фронтов > 1. Статик `StartupFront.AppProvider`
> удалён; потребители (`PageRenderController`, `InfoCommand`, `E2EServerFixture.WarmupRenderer`)
> переведены на DI. Секция `AppFront` убрана из `appsettings.json`; миграционное чтение осталось
> (`AppFrontMigration`), `SetupService` при установке всё ещё пишет `AppFront` — миграция подхватывает
> (пересмотр вместе с Blazor-фазой). Класс `FrontOptions` оставлен до Blazor-фазы (на него осталась
> только ссылка-комментарий в замороженном Blazor-движке). Документация: новая страница
> `docs/dev_docs/AppFront/Fronts.md` (+ ссылка из `Startup.md`), секция «Фронты и движки рендера»
> в `ai/FeatureIntegrationGuide.md`; `ai/PluginCreationGuide.md` обновлён в Фазе 1.
>
> **Баг-фикс пайплайна (найден после Фазы 7)**: `app.MapFallback(FrontFallbackAsync)` из Фазы 1 —
> глобальный endpoint `{*path:nonfile}` — выбирался внешним `UseRouting` для ВСЕХ не-файл путей и
> перехватывал `/dev` (админка: ветка `MapWhen` с локальным `MapFallbackToPage("/_AdminHost")`).
> Исправление: рендер фронта — терминальный middleware `FrontRenderFallbackMiddleware` вместо
> endpoint'а: исполняется только если `context.GetEndpoint() is null` (ничего не сматчилось) и путь
> не похож на файл (семантика `:nonfile`). Ветки (`/dev` через `MapWhen`) завершают пайплайн раньше.
> `/api/{**slug}`-fallback остался endpoint'ом (у него специфичный префикс). Регрессия:
> тест `Basic_DevAdmin_ShouldNotBeInterceptedByFrontFallback` в `HandlebarsAppFrontTests`.

---

## 4. Вне скоупа (по условию задачи)

- Blazor-рендер (`Mars.WebSiteProcessor.Blazor`) — оценён 2026-08-10 и удалён целиком
  (модуль, тесты `BlazorEngine`, пример `BlazorTemplateExample`): серверный пререндер WASM
  не нужен, а сценарий «Blazor/SPA-фронт в папке» при необходимости закроет отдельный
  статический движок раздачи файлов (SPA + данные через API).
- Ноды `RenderPage`/`MarsHostRootLayoutRender` — не рефакторим, только следим чтобы компилировались.
- `{{#context}}`-eval блок страниц — не реализуем.

## 5. Риски и как их гасим

| Риск | Митигция |
|---|---|
| Много потребителей `IMarsAppProvider`/`MarsAppFront` | Сохраняем интерфейс как фасад над `FrontManager`; переводим постепенно |
| Динамический middleware заденет маршруты админки/API | Middleware ставится после существующих маршрутов; поведение «нет фронта — 404» совпадает с текущим для `Mode=None` |
| Перестанут работать интеграционные тесты движков | Тесты `HandlebarsEngine` переводим на новую схему в фазе 1; `DatabaseHandlebars` удаляем в фазе 3; `Blazor` замораживаем |
| Path traversal в файловом REST и ИИ-инструментах | Единый `FrontFilesService` с нормализацией путей; только относительные пути внутри корня фронта |
| Старые инсталляции с `AppFront` в appsettings | Авто-миграция в `FrontsOption` при первом старте (фаза 0) |
| `hot-reload.js` смотрит на несуществующий хаб `/_ws/ws` | Фикс на `/_ws/admin` в фазе 5 |

## 6. Верификация (сквозная, после фаз 5–6)

1. `dotnet build Mars.slnx` — без ошибок и новых предупреждений.
2. Тесты: `tests/Mars.AppFrontEngines.Integration.Tests` (Handlebars), `tests/Mars.Integration.Tests`,
   E2E `tests/Mars.E2E.Tests` (по `ai/E2ETestingGuide.md`).
3. Ручной чек-лист:
   - чистый старт: создался дефолтный фронт в `data/fronts/`, сайт открывается на `/`;
   - админка: создать фронт, включить/выключить — меняется без рестарта;
   - смена движка у фронта — без рестарта;
   - добавить фронт на `/app2` — работает без рестарта;
   - подключить внешнюю папку (`MyMarsHandlebarsSiteTemplate`) — рендерится;
   - редактор: правка файла → iframe обновился; создать/переименовать/удалить файл;
   - полноэкранный предпросмотр;
   - ИИ: попросить агента поменять файл фронта → правка видна в iframe;
   - апгрейд старой инсталляции: `AppFront` из appsettings переехал в опцию.
