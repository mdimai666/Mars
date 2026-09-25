# Mars.SiteEngine — гайд по модулю

Как устроен модуль рендера сайтов: проекты, движки, точки расширения, QueryLang-адаптеры.
Продуктовый уровень (пайплайн запроса, админка, редактор, hot-reload, создание фронтов) —
`ai/FrontsGuide.md`. История SiteEngine-реворка (сентябрь 2026) —
`git show 17ea5ffe:ai/SiteEngineReworkPlan.md`.

## Структура (где что лежит)

- `src/Mars.Modules/Mars.SiteEngine.Abstractions` — контракты и модели: `IWebRenderEngine`,
  `IWebRenderEngineFactory`, `IWebTemplateService`, `PageRenderContext`, `WebSiteTemplate`/`WebPage`/
  `WebSitePart`, `Templators/` (`XInterpreter`, `TemplatorRegisterFunction`, `PaginatorHelper`),
  `TemplateData/` (филлеры переменных рендера `SiteTmpCtx*`, `ITemplateContextVariablesFiller`,
  `SiteBaseHref`).
- `src/Mars.Modules/Mars.SiteEngine.Contracts` — wire-DTO и опции (`FrontsOption`/`FrontItem`,
  `FCreateFrontRequest`, `FFrontTemplateResponse`).
- `src/Mars.Modules/Mars.SiteEngine.Host` — движконезависимое ядро: `WebSiteRequestProcessor`
  (матчинг `@page`, кэш, `StripMount`), `WebTemplateService` (скан `*.hbs`+`*.sbn`, hot-reload),
  `WebRenderEngineLocator` (кэш движков per-front + статика фронтов), `FrontManager`,
  `FrontTemplateService`, middleware — `MainSiteEngine`.
- `src/Mars.Modules/Mars.SiteEngine.Handlebars` — движок Handlebars: `HandlebarsWebRenderEngine`,
  сайт-хелперы — контрибьюторы в `Extensions/` (`SiteBasicHelpersContributor`,
  `SiteContextHelpersContributor`), функции — `HandlebarsFunc/`.
- `src/Mars.Modules/Mars.SiteEngine.Scriban` — движок Scriban: `ScribanWebRenderEngine`
  (двухстадийный рендер страница → layout), `WebSitePartsTemplateLoader` (include блоков/layout),
  сайт-функции — `Extensions/SiteScribanFunctions*`, `ScribanRenderContext` (rctx в
  `TemplateContext.Tags`).
- `src/Mars.Modules/Mars.TemplateEngine.Providers.Handlebars` / `.Scriban` — **общий фабричный слой
  движков библиотек** (используют и сайт-движки, и нода Template): `IHandlebarsEngineFactory`/
  `IScribanEngineFactory` создают настроенный инстанс (`IHandlebars` / глобальный `ScriptObject`)
  по scope; точки расширения — `IHandlebarsBuilderContributor`/`IScribanObjectContributor`
  (`Scope`: `core` = нода Template, `site` = сайт-движок, `null` = все). Регистрация —
  `AddMarsTemplateEngines()` (`Mars.TemplateEngine.Host`).
- QueryLang: семантика `key = ef.Entity.Where(...)` и исполнение — `Mars.QueryLang` +
  `Mars.QueryLang.Host` (`IQueryLangProcessing`, `EfStringQuery`); общий парсер тела —
  `DataQueryBodyParser`/`ContextQueryProcessor` в `Mars.QueryLang/Services`. Движки имеют только
  адаптеры синтаксиса: блок `{{#context}}` (Handlebars), функция `context(query, key?, cache?)` (Scriban).

## Поток рендера (коротко)

`WebSiteRequestProcessor` матчит страницу и готовит `PageRenderContext` → филлеры `SiteTmpCtx*`
заполняют `TemplateContextVariables` (общий словарь данных обоих движков) → движки добавляют
`site_base` (из `appFront.Front?.Url`, нормализация — `SiteBaseHref.FromFrontUrl`) →
`IWebRenderEngine.RenderPage`. Движок фронта выбирается по `FrontItem.EngineId` из реестра фабрик
`IWebRenderEngineFactory` (DI `IEnumerable`, метаданные `[Display]`); `WebRenderEngineLocator`
кэширует движки per-front и пересоздаёт по diff'у `FrontsOption`.

## Как добавить

- **Хелпер/функцию сайта** — контрибьютор scope `site` в `Extensions/` модуля движка
  (Handlebars: `RegisterHelper` в `IHandlebarsBuilderContributor`; Scriban: импорт в
  `IScribanObjectContributor`). Ядро не править. Хелперы ядра ноды Template — scope `core`.
- **Свой движок сайта** — модуль `Mars.SiteEngine.<Name>`: реализация `IWebRenderEngine` +
  `IWebRenderEngineFactory` (Id, `[Display(Name=…)]`), регистрация фабрики в DI; в админке
  появится автоматически (селект движков строится из реестра). Движок библиотеки брать из
  фабрики провайдера, не создавать инстанс руками.
- **QueryLang-адаптер в движок** — парсинг тела через `DataQueryBodyParser`, исполнение через
  `ContextQueryProcessor` (см. реализации `ContextBlock`/`SiteScribanFunctions.Context`).

## Тесты

- `tests/Mars.SiteEngine.Tests` — юниты обоих движков, QueryLang, `SiteBaseHrefTests`.
- `tests/Mars.SiteEngine.Integration.Tests` — Docker-регрессия полного рендера
  (Handlebars root-фронт + Scriban маунт `/sbn`); обязательна при изменениях рендера.
- `tests/Mars.Server.Tests/TemplateEngines` — контракт фабрик/контрибьюторов Providers.
- Парсинг стартовых шаблонов — `StarterFrontTemplatesTests` в `Mars.Integration.Tests`.
- Прогоны: MTP-exe из `bin\Debug\net10.0` (`dotnet test` в репо заблокирован), Docker — `MARS_DOCKER_TESTS=1`.

## Грабли

- **Handlebars.Net**: отсутствующий хелпер рендерится пустой строкой, НЕ бросает — опечатки молчаливы.
- **Scriban**: `$name` — локальная переменная, `$`-ключи сайт-данных недоступны из глобалов — движок
  дублирует их алиасами без префикса (`errors`, `maui`); списки — `.size`, не `.Count`;
  `ParameterCount` у `IScriptCustomFunction` — максимум 64 (`int.MaxValue` падает); `for` — только
  одна переменная, словарь — `for kv in d` + `kv.Key`/`kv.Value`.
- **QueryLang `ef.`**: любая `ef.`-строка триггерит Roslyn-компиляцию Mto-моделей ВСЕХ пост-типов —
  одна битая модель (ключ метаполя = ключевое слово C#, дефис в TypeName) роняла `#context` на всех
  фронтах. Починено в `Mars.MetaModelGenerator` (коммит `c2e56d78`): экранирование `@`, PascalCase-
  нормализация имён классов, пропуск невалидных ключей с warning'ом, валидатор ключей.
- **Маунт-фронты**: страницы объявляют url относительно маунта; `_req.Path`, кэш-ключ и
  route-переменные — фронто-относительные (срезка `StripMount`); `site_base` корня — `/`, маунта —
  `/sbn/` (trailing slash обязателен).

## Инварианты

- `ITemplateEngine` (строка для нод) и `IWebRenderEngine` (сайт) — раздельные контракты, НЕ унифицировать.
- Basic-хелперы видны только сайту (scope `site`); нода Template — чистый движок библиотеки.
- Движки не создают `WebTemplateService` — его создаёт Host (`WebRenderEngineLocator.Build`),
  движки берут из `appFront.Features`.
- `Mars.SiteEngine.*` не пакуются в NuGet (нет `<PackageId>`); новые проекты модуля — так же.

## Отклонено (не пересматривать без причины)

- Унификация `ITemplateEngine` + сайт-контракта — раздула бы контракт ноды Template сайт-спецификой.
- `@data`-запросы в заголовке страницы — не покрывают запросы в partials/циклах (можно вернуться
  как дополнение к `#context`).
- Препроцессор-директивы `<!--#query -->` — новый синтаксис + regex-проход на рендер.
- Миграция матчинга страниц на ASP.NET EndpointRouting — переписывание кэша/404/превью, несоразмерно.
- Префикс `{{site_base}}` в каждой ссылке стартовых шаблонов — ссылки относительные, их резолвит
  `<base href="{{site_base}}">` (решение пользователя 2026-09-26).

## Бэклог (отдельная инициатива)

- `WebTemplateService.ClearCache()` чистит весь глобальный MemoryCache — нужна точечная инвалидация
  по ключам фронта; вместе с ней — disposal сайт-движков при evict из кэша `WebRenderEngineLocator`
  (не делается из-за in-flight рендеров).
- Sync-over-async в `#context`/`RenderPostContent` (`.GetAwaiter().GetResult()`) — упирается в
  синхронный контракт `IWebRenderEngine.RenderPage`.
