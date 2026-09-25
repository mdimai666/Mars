# SiteEngine Rework — план

Инициатива: реворк `Mars.SiteEngine.*` — зависимости/контракты, устранение `MyHandlebars`,
общий фабричный слой с `Mars.TemplateEngine.*`, Scriban как второй движок сайта, движко-адаптеры
QueryLang. Старт 2026-09-26.

## Принятые решения (2026-09-26, пользователь)

- **Вариант B — общий фабричный слой, контракты раздельные.** `ITemplateEngine` (рендер
  строки для нод) и `IWebRenderEngine` (рендер сайта) НЕ унифицируются: разный смысл.
  Общим становится только слой создания/настройки движка библиотеки (Handlebars.Net, Scriban)
  в `Mars.TemplateEngine.Providers.*` + механизм контрибьюторов хелперов из DI.
- **Basic-хелперы только сайту.** Нода Template (`Core.Handlebars`) остаётся на чистом
  Handlebars.Net (+Json); все Mars-хелперы (eq/date/text/context/…) видны только сайт-движку.
  Плагин может добавить хелперы в любой движок через контрибьюторы.
- **QueryLang — только движко-адаптеры.** Семантика `key = ef.Entity.Where(...)` общая
  (исполнение — `IQueryLangProcessing`, QueryLang.Host); синтаксис свой на движок:
  блок `{{#context}}` в Handlebars, функция `context` в Scriban. Существующие шаблоны не ломаются.
- **WebPage — минимальная правка.** Матчинг остаётся в модели (сущность внутренняя, cohesive);
  убираются только протечки и косметика (см. 1.6).
- **Отложено (отдельная инициатива):** кэш-грабли (`WebTemplateService.ClearCache()` чистит весь
  глобальный MemoryCache; 30-мин кэш compiled-делегатов) и sync-over-async в `#context`/
  `RenderPostContent` (Handlebars.Net синхронен по природе).

### Отклонённые альтернативы

- Полная унификация `ITemplateEngine` + сайт (partial/кастомизация в общем контракте) — раздувает
  контракт ноды Template сайт-спецификой.
- Декларативные запросы `@data` в заголовке страницы — не покрывают запросы внутри partials/циклов;
  можно вернуться позже как дополнение.
- Препроцессор-директивы `<!--#query -->` — новый синтаксис + regex-проход на рендер, грабли.
- Миграция матчинга страниц на ASP.NET EndpointRouting — переписывание кэша/404/превью, несоразмерно.

## As-is (разведка 2026-09-26)

- Два параллельных Handlebars-стека: `Mars.TemplateEngine.Providers.Handlebars`
  (`HandlebarsTemplateEngine : ITemplateEngine`, без точек расширения — закрытый инстанс на
  каждый `CreateTemplate()`) и `Mars.SiteEngine.Handlebars` (`MyHandlebars : IMarsHtmlTemplator`,
  ~30 хелперов, partials, `#context`).
- `IMarsHtmlTemplator` мёртв как DI-абстракция: transient-регистрация в
  `MainSiteEngineHandlebars.cs` не потребляется; `HandlebarsWebRenderEngine` делает
  `new MyHandlebars()`; `RegisterContextFunctions()` вызывается на каждый рендер.
- Инверсия: `SiteEngine.Handlebars → SiteEngine.Host` — движок сам создаёт `WebTemplateService`
  (`HandlebarsWebRenderEngine.cs:55`, `appFront.Features.Set<IWebTemplateService>(wts)`).
  Все остальные потребители уже берут `IWebTemplateService` из `appFront.Features`.
- Мёртвые ссылки: `SiteEngine.Abstractions → Mars.Nodes.Core / Mars.Cms.Abstractions /
  Mars.Media.Abstractions`; `SiteEngine.Host → Mars.Data` (только заглушка
  `Templators/RenderRazor/RenderRazorHost.cs` с NotImplementedException); внешние
  `Mars.Media.Host → SiteEngine.Abstractions`, `Mars.SemanticKernel.CMS → SiteEngine.Abstractions`.
- `Mars.SiteEngine.Templators` — проект из одного файла `PaginatorHelper.cs` (нужен
  SiteEngine.Host и QueryLang.Host `EfStringQuery.cs:420`), без PackageId.
- Мёртвый код: `RenderRazorHost`, `TemplatorQueryLangCacheService` (пустой класс),
  `#if THINGS`-блок в `WebSiteRequestProcessor`.
- `Mars.QueryLang` зависит от `SiteEngine.Abstractions` (PageRenderContext, XInterpreter,
  TemplatorRegisterFunction); `IQueryLangProcessing` объявлен в `Mars.QueryLang/Services/`.
- Точки расширения для плагинов уже есть: `IWebRenderEngineFactory` (DI IEnumerable,
  `Create(MarsAppFront, IServiceProvider)`, метаданные `[Display]`, выбор строкой
  `FrontItem.EngineId`), `ITemplatorFeaturesLocator.Functions`, `IFrontRequestHandler`.

---

## Фаза 1 — зависимости, контракты, мёртвый код

- [x] 1.1 Удалить мёртвые ProjectReference: в `SiteEngine.Abstractions` (Nodes.Core,
      Cms.Abstractions, Media.Abstractions), в `SiteEngine.Host` (Mars.Data), внешние
      (`Mars.Media.Host`, `Mars.SemanticKernel.CMS` → SiteEngine.Abstractions).
- [x] 1.2 Удалить мёртвый код: `RenderRazorHost` (+ папка), `TemplatorQueryLangCacheService`,
      `#if THINGS` в `WebSiteRequestProcessor`.
- [x] 1.3 Схлопнуть `Mars.SiteEngine.Templators`: `PaginatorHelper` →
      `SiteEngine.Abstractions` (рядом с Templators-типами); удалить проект из `Mars.slnx`,
      обновить ссылки в SiteEngine.Host и QueryLang.Host.
- [x] 1.4 Развязать `SiteEngine.Handlebars → SiteEngine.Host`: создание `WebTemplateService`
      перенести в Host (`WebRenderEngineLocator` до вызова фабрики, либо
      `IWebTemplateServiceFactory` в Abstractions с реализацией в Host); движок берёт готовый
      сервис из `appFront.Features.Get<IWebTemplateService>()`. Убрать ProjectReference.
      (сделано: создание в `WebRenderEngineLocator.Build()`, движку сервис не нужен —
      `InitializeEngine`/`Initialize` удалены; тесты `HandlebarsEngineCacheTests`/
      `FrontRenderErrorTests` создают WTS явно, как локатор)
- [x] 1.5 Убрать ссылку `SiteEngine.Handlebars → Mars.Data`: `MyHandlebarsHelpBlock` переведён
      на `IDatabaseEntityTypeCatalogService.ListEntities()` (Cms.Abstractions) вместо рефлексии
      по `MarsDbContext`; в `Mars.SiteEngine.Tests` добавлена явная ссылка на SiteEngine.Host
      (раньше была транзитивной через Handlebars).
- [x] 1.6 Проверка: `dotnet build Mars.slnx` — 0 ошибок; `Mars.SiteEngine.Tests` — 62/62;
      `Mars.Integration.Tests` (namespace Services) — 97/97.

## Фаза 2 — контрибьюторы в Providers.Handlebars + перестройка сайт-движка

- [x] 2.1 `Mars.TemplateEngine.Providers.Handlebars`: фабрика настроенного `IHandlebars`
      (UseJson/UseNewtonsoftJson — одна точка конфигурации) +
      `IHandlebarsBuilderContributor { string? Scope; void Configure(IHandlebars hb); }`
      (DI IEnumerable, фильтрация по scope, `HandlebarsScopes.Core/Site`, null-scope = все).
      `HandlebarsTemplateEngine` переведён на фабрику (один общий инстанс вместо создания на
      каждый `CreateTemplate()`; scope core — без Mars-хелперов, TextEncoder=HtmlEncoder сохранён;
      беспараметрический конструктор оставлен для тестов/бенчмарков). Регистрация фабрики —
      в `AddMarsTemplateEngines()`.
- [x] 2.2 `Mars.SiteEngine.Handlebars`: `MyHandlebars` и `IMarsHtmlTemplator` удалены
      (включая мёртвую transient-регистрацию). Хелперы — контрибьюторы scope "site" в
      `Extensions/`: `SiteBasicHelpersContributor` (условия/даты/текст/циклы/site_head/site_footer/help
      + CustomDateTimeFormatter) и `SiteContextHelpersContributor` (mobile/!mobile/context/L/
      raw_block/iff/RenderPostContent). Контекстные хелперы регистрируются один раз на инстанс
      движка (rctx приходит через options.Data) — `RegisterContextFunctions()` на каждый рендер
      устранён. `ParseStringTimespan` перенесён в `MyHandlebarsContextFunctions`.
- [x] 2.3 `HandlebarsWebRenderEngine` получает `IHandlebars` из фабрики (scope "site") через
      конструктор (`IHandlebarsEngineFactory` резолвит `ActivatorUtilities` из rootServices);
      partials (`RegisterTemplate`) и кэш compiled-делегатов (30 мин) остались в движке.
      `AddMarsSiteEngineHandlebars` — `TryAddSingleton<IHandlebarsEngineFactory>` + регистрация
      контрибьюторов. Disposal движка при evict не добавляли (in-flight рендеры; вместе с
      отложенным кэш-реворком).
- [x] 2.4 Типы `IXTFunctionContext`/`TemplatorRegisterFunction`/`XInterpreter`/
      `TemplatorHelperInfoAttribute` остались в Abstractions (их использует QueryLang и Nodes).
- [x] 2.5 Тесты: `SiteHandlebarsTestFactory` (Mars.SiteEngine.Tests) вместо `new MyHandlebars()`;
      новые контракт-тесты фабрики/контрибьюторов `HandlebarsEngineFactoryTests`
      (Mars.Server.Tests) — scope-фильтрация, null-scope, case-insensitivity, NoEscape-колбэк
      (грабли: отсутствующий хелпер Handlebars.Net рендерит пустой строкой, не бросает).
      `Mars.SiteEngine.Tests` 62/62; `Mars.Server.Tests` (TemplateEngines) 57/57;
      `Mars.Integration.Tests` (Services) 97/97; `Mars.SiteEngine.Integration.Tests` (Docker,
      полный рендер фронта) 19/19.

## Фаза 3 — Scriban как движок сайта + QueryLang-адаптер

- [x] 3.1 `Mars.TemplateEngine.Providers.Scriban`: `ScribanScopes` (Core/Site),
      `IScribanObjectContributor { Scope; Configure(ScriptObject) }`,
      `IScribanEngineFactory.CreateGlobalObject(scope)` + реализация;
      `ScribanTemplateEngine` переведён на фабрику (общий global ScriptObject, scope core —
      пустой; беспараметрический конструктор сохранён). Регистрация фабрики —
      в `AddMarsTemplateEngines()`.
- [x] 3.2 Парсер тела `#context` обобщён: `DataQueryBodyParser` (FunctionBodyParse +
      ParseTimespan) и `ContextQueryProcessor` — в `Mars.QueryLang/Services` (НЕ в
      SiteEngine.Abstractions: QueryLang сам ссылается на Abstractions, была бы циркулярка).
      Handlebars `ContextBlock` переведён на них; старые `HandlebarsContextHelperFunctionBodyParser`
      и `HandlebarsContextBlockProcessor` удалены. Филлеры TemplateData переехали в
      `SiteEngine.Abstractions/TemplateData` и переименованы `HandlebarsTmpCtx*` → `SiteTmpCtx*`
      (общие для обоих движков).
- [x] 3.3 Новый модуль `src/Mars.Modules/Mars.SiteEngine.Scriban` (плоско, в slnx — папка
      SiteEngine): `ScribanRenderEngineFactory` (Id "scriban", `[Display(Name="Scriban")]`) +
      `ScribanWebRenderEngine` — двухстадийный рендер: страница → layout-обёртка,
      конвенция layout'ов — переменная `{{ body }}`; include блоков/лейаутов через
      `WebSitePartsTemplateLoader` (ITemplateLoader поверх WebSiteTemplate.Parts);
      кэш скомпилированных Template (30 мин, как у Handlebars); rctx — через
      `TemplateContext.Tags` (`ScribanRenderContext`). Host сканирует шаблоны `*.sbn`
      наравне с `*.hbs` (WebFilesReadFilesystemService, watcher, _updateFile).
- [x] 3.4 Сайт-функции — `SiteScribanFunctionsContributor` (scope "site"):
      `context(query, key?, cache?)` (QueryLang-адаптер поверх общих 3.2), `L` (vararg через
      IScriptCustomFunction), `iff`, `raw_block`, `render_post_content`, `site_head`,
      `site_footer`, текст/даты (`text_excerpt`, `text_ellipsis`, `nl2br`, `youtube_id`,
      `striphtml`, `encode`, `tojson`, `to_humanized_size`, `date_format`, `parsedateandformat`).
      Условия/циклы не дублируются — в Scriban нативные (`if`/`for`/`==`/`>`).
      Переменная `mobile` — bool в данных рендера.
      Грабли: `$errors`/`$maui` в Scriban НЕ читаются из глобалов (`$name` — локальная
      переменная) — движок дублирует `$`-ключи алиасами без префикса (`errors`, `maui`).
      Грабли: списки — встроенный list-аксессор, размер `{{ x.size }}`, не `.Count`.
      Грабли: ParameterCount у IScriptCustomFunction — максимум 64 (int.MaxValue падает).
- [x] 3.5 Админка без изменений: `FrontSettingsPage` — `FluentSelect Items="engines"` из
      `FrontController.Engines()` → `GetAvailableEngines()` → `[Display]` фабрик; "Scriban"
      появляется автоматически. `FrontItem.ScribanEngine = "scriban"` добавлен в Contracts.
- [x] 3.6 Тесты: `ScribanRenderEngineTests` (Mars.SiteEngine.Tests, 11 unit: if/переменные/
      layout-body/include/mobile/errors/context/L/iff/raw_block/text-хелперы) +
      `ScribanAppFrontTests` (Mars.SiteEngine.Integration.Tests, Docker: фронт sbnTheme на
      маунте /sbn — index/second/include/404). Все сьюты зелёные: SiteEngine.Tests 73/73,
      SiteEngine.Integration.Tests 22/22, Integration.Tests Services 97/97,
      Server.Tests TemplateEngines 57/57.
      Грабли тестовой темы: у маунт-фронтов страницы матчатся ПОЛНЫМ url (Host не срезает
      маунт) — `@page "/sbn"`, `@page "/sbn/second"`; index детектится по имени файла
      `index.sbn` (Url != "/"); Page404 у маунта не детектится (ищется Url == "/404") —
      fallback на index со статусом 404.
- [x] 3.7 NuGet: PackageId НЕ добавляли — движки SiteEngine (Handlebars/Host) не пакуются
      (CI пакует только csproj с явным `<PackageId>`); новый проект следует конвенции.
      Удаление `Mars.SiteEngine.Templators` публикацию не ломает (PackageId не было).

## Фаза 4 — WebPage-чистка и закрытие

- [x] 4.1 `WebPage` минимальная чистка (`SiteEngine.Abstractions/WebSite/Models/WebPage.cs`):
      - `UrlSegmentCount` — поле → init-свойство;
      - `RouteTemplate`, `RoutePattern`, `TemplateMatcher` → приватные;
      - убрана мёртвая проверка `TemplateMatcher is null` и `_templateMatcherRouteValues ??= []`
        (поле удалено);
      - `TemplateMatcherUsedConstraints()` — приватный, считается один раз в конструкторе
        (`_usedConstraints`), `RouteConstraintMatch` переиспользует кэш;
      - новый метод `FillRouteVariables(PathString, Dictionary<string, object?>)` — заменил
        раскопки `page.RoutePattern.PathSegments` в `WebSiteRequestProcessor` (единственный
        внешний потребитель ASP.NET-внутренностей; из процессора убраны using'и
        Routing.Patterns/Template).
- [x] 4.2 Регрессия рендера: `Mars.SiteEngine.Tests` 73/73; `Mars.SiteEngine.Integration.Tests`
      (Docker, Handlebars+Scriban фронты) 22/22; `Mars.Integration.Tests` (Services) 97/97;
      `Mars.Server.Tests` (TemplateEngines) 57/57; `dotnet build Mars.slnx` — 0 ошибок.
- [x] 4.3 `ai/FrontsGuide.md` обновлён: два встроенных движка (Handlebars/Scriban), фабричный
      слой Providers и контрибьюторы, Scriban-конвенции (`{{ body }}`, include, context,
      `$`-алиасы, `.size`), скан `*.hbs`+`*.sbn`, грабли маунт-фронтов, тесты, история
      SiteEngine-реворка.
- Схлопывание плана в гайд — НЕ делать: правки продолжаются, схлопнуть по явной команде
  пользователя (решение 2026-09-26).

## Фаза 5 — выбор движка при создании фронта + Scriban-стартер (запрос пользователя 2026-09-26)

- [x] 5.1 API создания фронта: `FCreateFrontRequest` += `EngineId`, `Path`;
      `FrontTemplates()` → `FFrontTemplateResponse { Name, EngineId }` (движок шаблона
      детектится по наличию `*.sbn`: `FrontTemplateService.GetStarterTemplateInfos`/
      `DetectTemplateEngine`); `CreateFront`: из шаблона — движок диктуется шаблоном,
      без шаблона — `EngineId` из запроса валидируется по `GetAvailableEngines()`;
      непустой `Path` — подключение существующей папки (проверка `Directory.Exists`).
- [x] 5.2 Стартовый шаблон `Res/front_templates/scriban` — порт default-темы:
      `_root.sbn` (@Body + site_head/site_footer + title через `page_title`), layout
      base/centered/fullwidth (конвенция `{{ body }}`), blocks header1/errors/paginator1,
      pages index/posts/post_detail/login/logout/help/404/500, wwwroot скопирован из default.
      Добавлена функция `help` (список сайт-функций; `ScribanRenderContext.Functions`).
- [x] 5.3 Админка `FrontSettingsPage`: переключатель источника («Из стартового шаблона» /
      «Существующая папка» + поле Path), селекты шаблона и движка; при создании из шаблона
      движок выбирается автоматически и селект задизейблен.
      Грабли: в FluentUI 4.14 `FluentSelect.Value` — **string** (значение опции), привязка
      к объекту-элементу не компилируется — биндить имя/Id и резолвить объект в коде.
- [x] 5.4 Тесты: `GetStarterTemplateInfos_DetectsEngine_BySbnFiles`; `scriban` в
      StarterFrontTemplatesTests (валидность WebSiteTemplate) + новый
      `ScribanStarterTemplate_AllFiles_ParseWithoutErrors` (парсинг каждого .sbn);
      Scriban-unit: multiline-строка в `context`, `help`, итерация словаря.
      Грабли Scriban: `for` — только одна переменная; словарь — `for kv in d` + `kv.Key`/`kv.Value`.
      Итог: SiteEngine.Tests 76/76, Integration Services 99/99+1 flake (DataContextTests,
      JWT iat на границе секунды — перезапуск 2/2 зелёный). `MarsAppVersion` → 0.8.3-alpha.32
      (cache-busting админки).
- [x] 5.5 Фикс маунт-фронтов (репорт пользователя 2026-09-26: «Url без слеша — ошибка при
      рендере; сайт в подпути рисует первый сайт»):
      - `FrontItem.Url` нормализуется в сеттере: ведущий `/` добавляется автоматически,
        `"/"` → `""` (корень). Без слеша `GetFrontForUrl` никогда не матчил маунт (падал
        на root-фронт), а `PathString` (RequestPath статики) бросал ArgumentException
        при построении фронта по slug. Сохранённые ранее кривые значения нормализуются
        при чтении опции (биндинг через сеттер).
      - `WebSiteRequestProcessor.StripMount(mountUrl, path)` — срезка префикса маунта
        (по границе сегмента) перед матчингом страниц; применена в `RenderRequest`
        (включая `WebClientRequest.replacePath` → `_req.Path`, кэш-ключ и
        `FillRouteVariables` — всё фронто-относительное) и в `PageRenderService.RenderUrl`
        (API by-url принимает и полный, и относительный url).
      - Страницы маунт-фронтов теперь объявляют url ОТНОСИТЕЛЬНО маунта (`@page "/"`,
        `@page "/second"`) — index/404/500 детектятся штатно; sbnTheme переведён на
        относительные url.
      - Тесты: `FrontItem_Url_NormalizesToLowerAndTrimsSlash` (расширен),
        `GetFrontForUrl_MatchesMount_SavedWithoutLeadingSlash`,
        `StripMount_CutsFrontPrefix_BySegmentBoundary`; Docker-сьют 22/22
        (Scriban-фронт на /sbn с относительными url, включая page_404_sbn),
        Integration Services 102/102.

## Фаза 6 — `site_base` для маунт-фронтов (запрос пользователя 2026-09-26)

- [x] 6.1 Проблема: `<base href="/">` захардкожен в стартовых шаблонах — на маунт-фронте
      относительные ссылки/ассеты резолвятся от корня домена. Статика уже обслуживается под
      маунтом (`RequestPath = front.Url`), ломался только генерируемый HTML.
- [x] 6.2 Переменная данных рендера `site_base` (`SiteTmpCtxBasicDataContext.SiteBaseParamKey`):
      `SiteBaseHref.FromFrontUrl` (SiteEngine.Abstractions/TemplateData) — ""/null → "/",
      "/sbn" → "/sbn/" (trailing slash обязателен). Заполняется в обоих движках
      (`HandlebarsWebRenderEngine`/`ScribanWebRenderEngine.RenderPage`) из `appFront.Front?.Url`
      после филлеров — через `ITemplateContextVariablesFiller` нельзя (в контракте нет фронта).
- [x] 6.3 Стартовые шаблоны: `<base href="{{site_base}}" />` в default/landing `_root.hbs`
      и `<base href="{{ site_base }}" />` в scriban `_root.sbn`. Существующие фронты
      пользователей не трогаем — переменная просто доступна, правят сами.
- [x] 6.4 Тесты: `SiteBaseHrefTests` (нормализация + site_base в рендере обоих движков,
      mount/null); интеграционные фикстуры appTheme/sbnTheme — `<base>` в `_root`, assert'ы
      `"/"` (root) и `"/sbn/"` (маунт). Итог: SiteEngine.Tests 85/85,
      SiteEngine.Integration.Tests (Docker) 22/22, Integration.Tests 430/430 (4 skipped),
      `dotnet build Mars.slnx` — 0 ошибок. `MarsAppVersion` не bumpали — шаблоны файловые
      (Res/front_templates), не браузерная статика. `ai/FrontsGuide.md` дополнен.
- [x] 6.5 Ссылки в стартовых шаблонах — относительные, без ведущего слеша (решение пользователя;
      префикс `{{site_base}}` в ссылках отклонён): header1 (`./`, `posts`, `help`, `login`/`logout`,
      `img/...`), index, posts_page (`posts/{{Slug}}`, `img/...`), post_detail, 404/500 (`./`) —
      default+scriban+landing. Резолвит `<base href="{{site_base}}">`. Пагинаторы не тронуты
      (порядок `{{url}}{{url2}}` исходный, `url='/posts'`). Системные роуты (`/dev`, `/mars/js/*`)
      остались от корня. Грабля относительных ссылок на вложенных страницах (`posts/{slug}` →
      `posts/posts`) зафиксирована в FrontsGuide (лечение — `{{site_base}}posts`).
      Тест `Render_SiteBaseConcat_BuildsFrontRelativeUrl`; SiteEngine.Tests 86/86,
      Integration Services 102/102 (StarterFrontTemplates — парсинг hbs/sbn).

## Отложенный бэклог (не в этом реворке)

- `WebTemplateService.ClearCache()` чистит весь глобальный MemoryCache — нужна точечная
  инвалидация по ключам фронта.
- Sync-over-async в `#context`/`RenderPostContent` (`.GetAwaiter().GetResult()`) — упирается
  в синхронный контракт `IWebRenderEngine.RenderPage`; асинхронизация — отдельная инициатива.
- `@data`-запросы в заголовке страницы как дополнение к `#context` (движко-независимый уровень).
- Disposal сайт-движков при evict из кэша `WebRenderEngineLocator` (in-flight рендеры) —
  вместе с кэш-реворком.

## Проверка (шпарлейка)

```
dotnet build Mars.slnx
# точечно (MTP-exe, dotnet test заблокирован):
dotnet build tests/Mars.SiteEngine.Tests && tests\Mars.SiteEngine.Tests\bin\Debug\net10.0\Mars.SiteEngine.Tests.exe
dotnet build tests/Mars.Integration.Tests && tests\Mars.Integration.Tests\bin\Debug\net10.0\Mars.Integration.Tests.exe
# Docker-интеграция фронта:
dotnet build tests/Mars.SiteEngine.Integration.Tests && tests\Mars.SiteEngine.Integration.Tests\bin\Debug\net10.0\Mars.SiteEngine.Integration.Tests.exe
```
