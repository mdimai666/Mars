# Mars — фронты: как работает рендер сайтов

Гайд по фронтовой подсистеме после фронт-реворка (завершён в августе 2026).
Пользовательская документация — `docs/dev_docs/AppFront/Fronts.md` и `docs/dev_docs/AppFront/Handlebars/`.
Общая структура решения — `ai/ProjectStructureGuide.md`.

## Что такое фронт

Фронт — это папка с файлами-шаблонами и статикой:

- `_root.hbs` — корневой шаблон;
- `pages/` — страницы с атрибутом `@page "/url"`;
- `blocks/`, `layout/` — блоки и layout'ы;
- `wwwroot/` — статика фронта.

Хранение: по умолчанию `data/fronts/<slug>`; если у фронта задан `Path` (абсолютный) — внешняя папка
(сценарий: разработка шаблона без копирования).

Мультифронт: фронтов может быть несколько, у каждого свой slug и Url-маунт (`/`, `/app2`, …).
Список фронтов — опция **`FrontsOption`** в БД (`Mars.SiteEngine.Contracts/Options/FrontsOption.cs`):
`FrontItem { Slug, Title, Url, Path, EngineId, Enabled }`. Всё динамическое: добавление фронта,
вкл/выкл, смена движка и URL применяются в рантайме без рестарта.

Особый фронт админки — `data/admin/front` (создаётся один раз при старте, `FrontTemplateService.EnsureAdminFront`).

## Где лежит код

- `Mars.SiteEngine.Abstractions` — контракты: `IFrontManager`, `MarsAppFront`, `IWebRenderEngine`,
  `IWebRenderEngineFactory`, `IWebRenderEngineLocator`, `IFrontRequestHandler`, `IFrontFilesService`.
- `Mars.SiteEngine.Contracts` — wire-DTO и опции (`FrontsOption`/`FrontItem`, `SEOOption`, `FaviconOption`).
- `Mars.SiteEngine.Host` — реализация: `FrontManager`, `WebRenderEngineLocator`, `FrontFilesService`,
  `FrontTemplateService`, `WebTemplateService`, `WebSiteRequestProcessor`; middleware пайплайна —
  в `MainSiteEngine` (`AddMarsSiteEngine`/`UseMarsSiteEngineStartup`/`UseMarsSiteEngine`).
- `Mars.SiteEngine.Handlebars` — встроенный движок рендера (фабрика `HandlebarsRenderEngineFactory`).
- Админка: страница `/Settings/Front` (список фронтов) и редактор
  `src/Mars.Admin/Builder/FrontEditorViews/FrontEditorPage.razor` (`/front/editor/{Slug}`).
- ИИ-инструменты: `Mars.AiChat.Host/Tools/MarsFrontFilesTools.cs`.
- Стартовые шаблоны: `Res/front_templates/<name>` в content root (в репо — `src/Mars.WebApp/Res/front_templates`:
  `default`, `landing`, служебный `admin`).

## Пайплайн запроса фронта

Регистрация — `MainSiteEngine.UseMarsSiteEngine(app)`, вызывается последним в пайплайне приложения.
Порядок внутри:

1. `robots.txt` — отдаётся из `SEOOption.RobotsTxt`.
2. Резолв фронта по URL-префиксу: `MarsAppFront` кладётся в `HttpContext.Items`.
3. `FrontRequestHandlersMiddleware` — обработчики `IFrontRequestHandler` из DI, по возрастанию `Order`.
   Запросы с endpoint'ом проходят только если помечены `FrontRenderEndpointAttribute` (публичное API рендера);
   файлы-ассеты идут мимо, кроме html. Пример: `MaintenanceFrontRequestHandler` (режим обслуживания,
   живёт в `Mars.Options.Host` — код фронтов про опцию ничего не знает).
4. `FrontStaticFilesMiddleware` — статика `<front>/wwwroot` (PhysicalFileProvider per-front).
   Системные префиксы `/dev`, `/_content`, `/_framework`, `/mars`, `/api`, `/_ws` никогда не
   обслуживаются из wwwroot фронтов.
5. `MapFallback("/api/{**slug}")` — JSON-404 для несуществующих API.
6. `FrontRenderFallbackMiddleware` — терминальный **middleware, а не endpoint**: глобальный
   `MapFallback` выбирался бы для всех не-файл путей и перехватывал `/dev` и прочие ветки с их
   локальными fallback'ами. Рендерит, только если запрос никто не обработал и путь «не файл»
   (семантика `:nonfile`). Нет фронта → 404; иначе `IWebSiteProcessor.Response` →
   `WebSiteRequestProcessor`: матчинг URL по страницам `@page`, кэш `@cache`/`@cache-force`,
   подготовка `PageRenderContext` → `renderEngine.RenderPage(...)`.

## Движки рендера

- Реестр фабрик `IWebRenderEngineFactory` в DI (`IEnumerable<>`). Встроенный —
  `HandlebarsRenderEngineFactory` (регистрируется в `Mars.SiteEngine.Handlebars`); плагины могут
  добавлять свои фабрики. Метаданные движка — через `[Display]`.
- Движок фронта выбирается по `FrontItem.EngineId`. `WebRenderEngineLocator` кэширует движки
  per-front, создаёт лениво на первом запросе и пересоздаёт по diff'у полей при изменении
  `FrontsOption` (подписан на `FrontManager.Changed`).
- `IMarsAppProvider` — read-only фасад над фронт-менеджером для старых потребителей
  (`GetAppForUrl`, `Apps` и т.д.).
- Демонтировано в реворк: DB-рендер (посты типов `page/template/block/layout`, `HostHtml` как `_root`)
  и Blazor-рендер. Шаблоны — только файлы.

## Шаблоны и hot-reload

- Файлы с атрибутами в шапке: `@page "/url"`, `@layout`, `@cache`/`@cache-force`, `@title`;
  модели `WebSitePart`/`WebPage`. Данные в шаблонах — `{{#context}}` и QueryLang.
- `WebTemplateService` сканирует папку фронта (только файловый источник); `FileSystemWatcher`
  следит за изменениями и шлёт SignalR-события `reload`/`refreshcss` через ChatHub (`/_ws/admin`);
  `hot-reload.js` в wwwroot перезагружает страницы.
- **Важно**: движок кэширует скомпилированные шаблоны (~30 минут). Поэтому `FrontFilesService`
  после каждой изменяющей операции явно уведомляет движок (`IWebRenderEngineLocator.TryGetAppFrontBySlug`
  → `IWebTemplateService.NotifyFileChanged` — перечитывание + сброс кэша + `reload` сразу, без дебаунса).
  FileSystemWatcher — страховка для внешних правок.

## Админка

- `/Settings/Front` — список фронтов (`EditOptionForm<FrontsOption>`): карточки (Title/Url/Path/движок/вкл),
  «Создать фронт» (из стартового шаблона), удаление (опционально с папкой; внешние папки через API не удаляются).
- `/front/editor/{Slug}` — редактор в стиле VSCode: дерево файлов | `CodeEditor2` (Ctrl+S) | iframe-превью
  сайта; полноэкранный предпросмотр; live-reload превью через ChatHub; защита от затирания
  (по событию `reload` открытый файл перечитывается, только если пользователь не вносил несохранённых правок).
- Файловый CRUD — `FrontController` через общий `FrontFilesService`: нормализация путей,
  только относительные, без выхода за корень папки фронта (эта же защита у ИИ-инструментов).

## ИИ в редакторе

- Инструменты `MarsFrontFilesTools` (list/read/write/create/rename/delete front files) подключаются
  к агенту только когда открыт редактор фронта: slug парсится из PageContext по URL `/front/editor/{slug}`.
- Правила работы — в промпте (структура фронта, относительные пути, «прочитай перед правкой»,
  удаление только через AskUser).
- Страница реализует `IAiChatPageHandler` (контекст: фронт, открытый файл).
- Правки видны в превью сразу: запись в файл → уведомление движка → событие `reload`.

## Создание фронта и бутстрап

- `FrontTemplateService` копирует шаблон `Res/front_templates/<name>` в `data/fronts/<slug>`
  (валидация slug; список доступных шаблонов — по папкам, кроме служебных).
- Бутстрап при старте (`UseMarsSiteEngineStartup`, после сидов): `MigrateAppFrontToOption`
  (легаси-секция `AppFront` из appsettings → `FrontsOption`), затем `EnsureDefaultFront`
  (опция пустая → создаётся фронт `default` из шаблона). В тестах (`IsTesting`) пропускается.
  Setup-визард пока пишет легаси-`AppFront` — миграция подхватывает.

## Тесты

- `tests/Mars.SiteEngine.Tests` — юниты рендера и QueryLang.
- Docker-регрессия: `HandlebarsAppFrontTests` в `Mars.SiteEngine.Integration.Tests`
  (полный пайплайн: `/dev` не перехватывается фолбэком, maintenance, мгновенный рендер после записи).
- Лёгкий набор без Docker — см. память/гайды по фронтовым тестам (`FrontManagerTests`,
  `AiFrontFilesToolsTests` и др. в `Mars.Integration.Tests`).

## Краткая история

Фронт-реворк (август 2026): настройки фронтов переехали из appsettings в опцию `FrontsOption`,
пайплайн стал полностью динамическим (без рестарта), фронты — только файлы в `data/fronts/`,
демонтирован DB-легаси (посты `page/template/block/layout`, Blazor-движок), добавлены список
фронтов в админке, редактор с live-превью и ИИ-чатом. Полный план с заметками по фазам —
в истории git (`ai/FrontReworkPlan.md`, файл схлопнут).

## Агентам

- Изменения рендера/пайплайна фронтов обязательно проверять Docker-регрессией `HandlebarsAppFrontTests`.
- Middleware SiteEngine — последние в пайплайне; порядок внутри `UseSiteEngineMiddlewares` не ломать
  (особенно: фолбэк-рендер — middleware, а не endpoint).
- Файловые операции над фронтами — только через `FrontFilesService` (защита путей);
  после изменяющих операций движок уведомляется явно (кэш шаблонов 30 минут).
- Опции фронтов/SEO/favicon — в `Mars.SiteEngine.Contracts.Options`, регистрации — в
  `UseMarsSiteEngineOptions`; конкретные модели опций в чужие пакеты не класть.
