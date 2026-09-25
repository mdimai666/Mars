# Mars — фронты: как работает рендер сайтов

Гайд по фронтовой подсистеме после фронт-реворка (завершён в августе 2026).
Пользовательская документация — `docs/dev_docs/AppFront/Fronts.md` и `docs/dev_docs/AppFront/Handlebars/`.
Общая структура решения — `ai/ProjectStructureGuide.md`.

## Что такое фронт

Фронт — это папка с файлами-шаблонами и статикой:

- `_root.hbs`/`_root.sbn` — корневой шаблон (расширение = движок фронта: `*.hbs` — Handlebars, `*.sbn` — Scriban);
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
- `Mars.SiteEngine.Handlebars` — встроенный движок рендера Handlebars (фабрика `HandlebarsRenderEngineFactory`);
  сайт-хелперы — контрибьюторы `Extensions/SiteBasicHelpersContributor` + `SiteContextHelpersContributor`.
- `Mars.SiteEngine.Scriban` — встроенный движок рендера Scriban (фабрика `ScribanRenderEngineFactory`,
  Id `scriban`); сайт-функции — `Extensions/SiteScribanFunctionsContributor`.
- `Mars.TemplateEngine.Providers.Handlebars`/`.Scriban` — фабричный слой движков библиотек
  (`IHandlebarsEngineFactory`/`IScribanEngineFactory`) и точки расширения
  (`IHandlebarsBuilderContributor`/`IScribanObjectContributor`, scope `core` = нода Template,
  `site` = сайт-движок, `null` = все). Тот же механизм используют плагины.
- Филлеры контекстных переменных (`SiteTmpCtx*`, `ITemplateContextVariablesFiller`) — в
  `Mars.SiteEngine.Abstractions/TemplateData` (общие для движков); парсер/процессор QueryLang-запросов
  (`DataQueryBodyParser`, `ContextQueryProcessor`) — в `Mars.QueryLang/Services`.
- Админка: страница `/Settings/Front` (список фронтов) и редактор
  `src/Mars.Admin/Builder/FrontEditorViews/FrontEditorPage.razor` (`/front/editor/{Slug}`).
- ИИ-инструменты: `Mars.AiChat.Host/Tools/MarsFrontFilesTools.cs`.
- Стартовые шаблоны: `Res/front_templates/<name>` в content root (в репо — `src/Mars.WebApp/Res/front_templates`:
  `default`, `landing`, `scriban` (движок Scriban, `*.sbn`), служебный `admin`).
  Движок шаблона определяется по наличию `*.sbn`-файлов (`FrontTemplateService.DetectTemplateEngine`).

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

- Реестр фабрик `IWebRenderEngineFactory` в DI (`IEnumerable<>`). Встроенные —
  `HandlebarsRenderEngineFactory` (`Mars.SiteEngine.Handlebars`) и `ScribanRenderEngineFactory`
  (`Mars.SiteEngine.Scriban`, Id `scriban`); плагины могут добавлять свои фабрики.
  Метаданные движка — через `[Display]`.
- Движок фронта выбирается по `FrontItem.EngineId`. `WebRenderEngineLocator` кэширует движки
  per-front, создаёт лениво на первом запросе и пересоздаёт по diff'у полей при изменении
  `FrontsOption` (подписан на `FrontManager.Changed`). `WebTemplateService` создаёт ХОСТ
  (`WebRenderEngineLocator.Build`) и кладёт в `appFront.Features` — движки его не создают.
- Движок библиотеки (экземпляр `IHandlebars` / глобальный `ScriptObject`) движки сайта получают
  из фабрик провайдеров (`TemplateEngine.Providers.*`) с scope `site` — хелперы/функции
  регистрируются контрибьюторами один раз на инстанс.
- Конвенции Scriban-шаблонов: layout выводит страницу через `{{ body }}` (двухстадийный рендер:
  страница → обёртка root+layout); блоки — `{{ include 'blocks/name' }}`; QueryLang —
  `{{ context "posts = ef.Post.Take(3)" key? cache? }}`; `$`-переменные недоступны из глобалов
  (`$errors` → `errors`); списки — `{{ x.size }}`, не `.Count`.
- `IMarsAppProvider` — read-only фасад над фронт-менеджером для старых потребителей
  (`GetAppForUrl`, `Apps` и т.д.).
- Демонтировано в реворк: DB-рендер (посты типов `page/template/block/layout`, `HostHtml` как `_root`)
  и Blazor-рендер. Шаблоны — только файлы.
- **Маунт-фронты**: `FrontItem.Url` нормализуется в сеттере (ведущий `/` добавляется, `"/"` → `""`);
  страницы объявляют url ОТНОСИТЕЛЬНО маунта (`@page "/"`, `@page "/second"`) — префикс срезается
  `WebSiteRequestProcessor.StripMount` перед матчингом (`_req.Path`, кэш-ключ и route-переменные
  тоже фронто-относительные). API by-url принимает и полный, и относительный url.
- **`site_base`** — переменная данных рендера для `<base href="{{site_base}}">`: `""`/null (корень) →
  `/`, маунт `/sbn` → `/sbn/` (trailing slash обязателен — без него браузер режет последний сегмент
  base). Заполняют оба движка из `appFront.Front?.Url` после филлеров, нормализация —
  `SiteBaseHref.FromFrontUrl` (`SiteEngine.Abstractions/TemplateData`). Статика фронта уже
  обслуживается под маунтом (`RequestPath = front.Url` в `WebRenderEngineLocator.BuildStaticFiles`),
  так что относительные ассеты (`css/app.css`) с правильным base работают на маунте без правок.
  Фронтовые ссылки в шаблонах — относительные, БЕЗ ведущего слеша (`posts`, `img/...`, home — `./`):
  их резолвит `<base>`; `/posts` ушёл бы в корень домена мимо маунта. Системные роуты
  (`/dev`, `/mars/js/*`, `/api/*`) — всегда от корня, со слешем. Грабля относительных ссылок:
  на вложенных страницах (`posts/{slug}`) `posts` даст `posts/posts` — для ссылок с глубоких
  страниц использовать `{{site_base}}posts` (hbs) / `{{ site_base + 'posts' }}` (sbn).

## Шаблоны и hot-reload

- Файлы с атрибутами в шапке: `@page "/url"`, `@layout`, `@cache`/`@cache-force`, `@title`;
  модели `WebSitePart`/`WebPage`. Данные в шаблонах — QueryLang: `{{#context}}` (Handlebars)
  или `{{ context "..." }}` (Scriban); общий парсер/исполнение — `Mars.QueryLang`.
- `WebTemplateService` сканирует папку фронта (`*.hbs` и `*.sbn`, только файловый источник); `FileSystemWatcher`
  следит за изменениями и шлёт SignalR-события `reload`/`refreshcss` через ChatHub (`/_ws/admin`);
  `hot-reload.js` в wwwroot перезагружает страницы.
- **Важно**: движок кэширует скомпилированные шаблоны (~30 минут). Поэтому `FrontFilesService`
  после каждой изменяющей операции явно уведомляет движок (`IWebRenderEngineLocator.TryGetAppFrontBySlug`
  → `IWebTemplateService.NotifyFileChanged` — перечитывание + сброс кэша + `reload` сразу, без дебаунса).
  FileSystemWatcher — страховка для внешних правок.

## Админка

- `/Settings/Front` — список фронтов (`EditOptionForm<FrontsOption>`): карточки (Title/Url/Path/движок/вкл),
  «Создать фронт» — из стартового шаблона (движок диктуется шаблоном, селект задизейблен)
  или подключение существующей папки (пустой Path = `data/fronts/<slug>`, иначе внешняя папка;
  движок выбирается вручную, валидируется по реестру фабрик), удаление (опционально с папкой;
  внешние папки через API не удаляются).
  Грабля FluentUI 4.14: `FluentSelect.Value` — **string** (значение опции), привязка к
  объекту-элементу не компилируется — биндить имя/Id и резолвить объект в коде.
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

- `tests/Mars.SiteEngine.Tests` — юниты рендера (Handlebars + `ScribanEngine/ScribanRenderEngineTests`) и QueryLang.
- Docker-регрессия: `Mars.SiteEngine.Integration.Tests` — `HandlebarsAppFrontTests` +
  `ScribanAppFrontTests` (тема `ScribanEngine/sbnTheme`, маунт `/sbn`)
  (полный пайплайн: `/dev` не перехватывается фолбэком, maintenance, мгновенный рендер после записи).
- Лёгкий набор без Docker — см. память/гайды по фронтовым тестам (`FrontManagerTests`,
  `AiFrontFilesToolsTests` и др. в `Mars.Integration.Tests`).
- Прогоны (MTP-exe, `dotnet test` в репо заблокирован — собирать проект и запускать exe из
  `bin\Debug\net10.0`; Docker-сьют opt-in через `MARS_DOCKER_TESTS=1`):
  `Mars.SiteEngine.Tests` (юниты рендера/QueryLang), `Mars.SiteEngine.Integration.Tests`
  (Docker: Handlebars root-фронт + Scriban маунт `/sbn`), `Mars.Integration.Tests`
  (namespace Services — лёгкие фронтовые, включая парсинг стартовых шаблонов).

## SiteEngine-реворк: решения, грабли, бэклог

### Инварианты и отклонённые альтернативы

- **Контракты раздельные, общий только фабричный слой** (вариант B, решение 2026-09-26):
  `ITemplateEngine` (рендер строки для нод) и `IWebRenderEngine` (рендер сайта) НЕ унифицируются —
  разный смысл. Полная унификация отклонена: partial/кастомизация раздули бы контракт ноды
  Template сайт-спецификой.
- **Basic-хелперы только сайту** (scope `site`); нода Template — чистый движок библиотеки.
- **QueryLang — только движко-адаптеры**: семантика `key = ef.Entity.Where(...)` общая
  (`IQueryLangProcessing`, QueryLang.Host), синтаксис свой на движок (`{{#context}}` /
  `{{ context "..." }}`). Существующие шаблоны не ломаются.
- Отклонено: декларативные `@data`-запросы в заголовке страницы (не покрывают запросы внутри
  partials/циклов; можно вернуться как дополнение); препроцессор-директивы `<!--#query -->`
  (новый синтаксис + regex-проход на рендер); миграция матчинга страниц на ASP.NET
  EndpointRouting (переписывание кэша/404/превью, несоразмерно).
- `Mars.SiteEngine.*` не пакуются в NuGet (нет `<PackageId>`, CI пакует только явные) —
  новые проекты SiteEngine следуют той же конвенции.

### Грабли

- Отсутствующий хелпер Handlebars.Net рендерит пустой строкой, НЕ бросает — опечатки в именах
  хелперов молчаливы.
- Scriban: `$`-префиксные сайт-переменные не читаются из глобалов (`$name` — локальная переменная)
  — движок дублирует `$`-ключи алиасами без префикса; списки — встроенный list-аксессор
  (`.size`, не `.Count`); `ParameterCount` у `IScriptCustomFunction` — максимум 64
  (`int.MaxValue` падает); `for` — только одна переменная, словарь — `for kv in d` + `kv.Key`/`kv.Value`.
- Тестовая тема маунт-фронта: страницы объявляют url относительно маунта (`@page "/"`);
  index детектится по имени файла (`index.sbn`, Url != "/"); Page404 у маунта ищется по `Url == "/404"`.
- Любая `ef.`-строка в `#context` триггерит Roslyn-компиляцию Mto-моделей ВСЕХ пост-типов:
  одна битая модель (ключ метаполя = ключевое слово C#, дефис в имени пост-типа) роняла рендер
  любого сайта. Починено в `Mars.MetaModelGenerator` (экранирование `@`, нормализация имён,
  пропуск невалидных ключей, валидатор ключей) — см. коммит `c2e56d78`.

### Бэклог (не сделан, отдельная инициатива)

- `WebTemplateService.ClearCache()` чистит весь глобальный MemoryCache — нужна точечная
  инвалидация по ключам фронта; вместе с ней — disposal сайт-движков при evict из кэша
  `WebRenderEngineLocator` (сейчас не делается из-за in-flight рендеров).
- Sync-over-async в `#context`/`RenderPostContent` (`.GetAwaiter().GetResult()`) — упирается
  в синхронный контракт `IWebRenderEngine.RenderPage`; асинхронизация контракта — блокер.
- `@data`-запросы в заголовке страницы как движко-независимое дополнение к `#context`.

## Краткая история

Фронт-реворк (август 2026): настройки фронтов переехали из appsettings в опцию `FrontsOption`,
пайплайн стал полностью динамическим (без рестарта), фронты — только файлы в `data/fronts/`,
демонтирован DB-легаси (посты `page/template/block/layout`, Blazor-движок), добавлены список
фронтов в админке, редактор с live-превью и ИИ-чатом. Полный план с заметками по фазам —
в истории git (`ai/FrontReworkPlan.md`, файл схлопнут).

SiteEngine-реворк (сентябрь 2026): чистка зависимостей и мёртвого кода, `MyHandlebars`/
`IMarsHtmlTemplator` демонтированы — сайт-движки строятся на фабричном слое
`TemplateEngine.Providers.*` с контрибьюторами хелперов (точки расширения для плагинов),
добавлен движок Scriban (`*.sbn`), QueryLang-ядро общее (`Mars.QueryLang`), выбор движка при
создании фронта + Scriban-стартер, маунт-фронты (`site_base`, относительные ссылки).
План с заметками по фазам — в истории git (`git show 17ea5ffe:ai/SiteEngineReworkPlan.md`,
файл схлопнут).

## Агентам

- Изменения рендера/пайплайна фронтов обязательно проверять Docker-регрессией
  `Mars.SiteEngine.Integration.Tests` (Handlebars + Scriban фронты).
- Middleware SiteEngine — последние в пайплайне; порядок внутри `UseSiteEngineMiddlewares` не ломать
  (особенно: фолбэк-рендер — middleware, а не endpoint).
- Файловые операции над фронтами — только через `FrontFilesService` (защита путей);
  после изменяющих операций движок уведомляется явно (кэш шаблонов 30 минут).
- Опции фронтов/SEO/favicon — в `Mars.SiteEngine.Contracts.Options`, регистрации — в
  `UseMarsSiteEngineOptions`; конкретные модели опций в чужие пакеты не класть.
