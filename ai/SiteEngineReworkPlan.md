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

- [ ] 2.1 `Mars.TemplateEngine.Providers.Handlebars`: фабрика настроенного `IHandlebars`
      (UseJson/UseNewtonsoftJson/HtmlEncoder/форматтеры — одна точка конфигурации) +
      `IHandlebarsBuilderContributor { string? Scope; void Configure(IHandlebars hb); }`
      (DI IEnumerable, фильтрация по scope). `HandlebarsTemplateEngine` переводится на фабрику;
      scope ноды (`Core.Handlebars`) — без Mars-хелперов (поведение ноды не меняется).
- [ ] 2.2 `Mars.SiteEngine.Handlebars`: удалить `MyHandlebars` и `IMarsHtmlTemplator`
      (интерфейс в Abstractions + мёртвая transient-регистрация в `MainSiteEngineHandlebars`).
      Хелперы — классы-контрибьюторы со scope "site": `BasicHelpersContributor`
      (MyHandlebarsBasicFunctions), `ContextHelpersContributor` (MyHandlebarsContextFunctions:
      mobile/context/L/raw_block/iff/RenderPostContent), `SitePartsContributor` (site_head/
      site_footer), `HelpContributor`. Регистрация контекстных хелперов — один раз на инстанс
      движка (rctx и так приходит через `options.Data["rctx"]`), убрать
      `RegisterContextFunctions()` из цикла рендера.
- [ ] 2.3 `HandlebarsWebRenderEngine` — получает инстанс `IHandlebars` из фабрики
      (scope "site"); partials (`RegisterTemplate` блоков/лейаутов) и кэш compiled-делегатов
      остаются в движке (на фронт).
- [ ] 2.4 Типы `IXTFunctionContext`/`TemplatorRegisterFunction`/`XInterpreter`/
      `TemplatorHelperInfoAttribute` остаются в Abstractions (их использует QueryLang и Nodes).
- [ ] 2.5 Тесты: переписать `MyFunctionsTests`/`MyHandlebarsContextFunctionsTests`/
      `BasicExpressionTests` на контрибьюторов; контракт-тесты `Mars.Server.Tests/TemplateEngines`
      расширить на фабрику/контрибьюторов; `HandlebarsAppFrontTests` (Docker, полный рендер фронта).

## Фаза 3 — Scriban как движок сайта + QueryLang-адаптер

- [ ] 3.1 `Mars.TemplateEngine.Providers.Scriban`: зеркальный механизм — фабрика
      `Scriban.Template`/`TemplateContext` + `IScribanObjectContributor` (глобальный
      `ScriptObject` с функциями).
- [ ] 3.2 Парсер тела `#context` (`HandlebarsContextHelperFunctionBodyParser`,
      key=value строки) обобщить и перенести в `SiteEngine.Abstractions` — он движко-независим.
- [ ] 3.3 Новый модуль `src/Mars.Modules/Mars.SiteEngine.Scriban` (плоско; виртуальная папка
      в `Mars.slnx`): `ScribanRenderEngineFactory : IWebRenderEngineFactory` (Id "scriban",
      `[Display]`) + `ScribanWebRenderEngine : IWebRenderEngine`. Partials/блоки/лейауты —
      через кастомный `IIncludeHandler` поверх `WebSiteTemplate.Parts`; сборка root+layout —
      аналог `{{#>layout}}` (контент страницы как переменная/include).
- [ ] 3.4 Сайт-функции Scriban — контрибьюторами (scope "site"): базовый набор (эквиваленты
      eq/date/text-хелперов), `context` (QueryLang-адаптер: multiline-строка аргументом →
      общий парсер 3.2 → `IQueryLangProcessing`), `L`, `mobile`, `site_head`, `site_footer`.
- [ ] 3.5 Админка не меняется (выбор движка строкой `FrontItem.EngineId`, список из
      `FrontController.Engines()`). Проверить отображение "Scriban" в UI выбора движка.
- [ ] 3.6 Тесты: unit (ScribanWebRenderEngine на тестовой теме), интеграционные — аналог
      `HandlebarsAppFrontTests` на минимальном Scriban-фронте; контракт-тесты провайдеров.
- [ ] 3.7 NuGet: PackageId по конвенции модулей; проверить, что `nuget-publish.yml` подхватывает
      новый пакет (и что удаление `Mars.SiteEngine.Templators` из 1.3 не ломает публикацию —
      у него не было PackageId).

## Фаза 4 — WebPage-чистка и закрытие

- [ ] 4.1 `WebPage` минимальная чистка (`SiteEngine.Abstractions/WebSite/Models/WebPage.cs`)
      — отложена в конец по решению пользователя:
      - `UrlSegmentCount` — публичное поле → init-свойство;
      - `RouteTemplate`, `TemplateMatcher` → private (снаружи не нужны);
      - убрать мёртвую проверку `TemplateMatcher is null` в `MatchUrl` и бессмысленный
        `_templateMatcherRouteValues ??= []`;
      - `TemplateMatcherUsedConstraints()` — вызывать один раз (кэшировать в init);
      - новое свойство/метод `RouteParameterNames` — и переписать
        `WebSiteRequestProcessor.cs:153–165` (единственный внешний потребитель
        `RoutePattern.PathSegments`) на него.
- [ ] 4.2 Прогнать регрессию рендера: `HandlebarsAppFrontTests` (Docker) + лёгкие фронт-тесты
      `Mars.Integration.Tests` + `Mars.SiteEngine.Tests` + контракт-тесты `Mars.Server.Tests`.
- [ ] 4.3 Обновить `ai/FrontsGuide.md` (реестр движков: + Scriban; убрать упоминания MyHandlebars).
- Схлопывание плана в гайд — НЕ делать: правки продолжаются, схлопнуть по явной команде
  пользователя (решение 2026-09-26).

## Отложенный бэклог (не в этом реворке)

- `WebTemplateService.ClearCache()` чистит весь глобальный MemoryCache — нужна точечная
  инвалидация по ключам фронта.
- Sync-over-async в `#context`/`RenderPostContent` (`.GetAwaiter().GetResult()`) — упирается
  в синхронный контракт `IWebRenderEngine.RenderPage`; асинхронизация — отдельная инициатива.
- `@data`-запросы в заголовке страницы как дополнение к `#context` (движко-независимый уровень).

## Проверка (шпарлейка)

```
dotnet build Mars.slnx
# точечно (MTP-exe, dotnet test заблокирован):
dotnet build tests/Mars.SiteEngine.Tests && tests\Mars.SiteEngine.Tests\bin\Debug\net10.0\Mars.SiteEngine.Tests.exe
dotnet build tests/Mars.Integration.Tests && tests\Mars.Integration.Tests\bin\Debug\net10.0\Mars.Integration.Tests.exe
# Docker-интеграция фронта:
dotnet build tests/Mars.SiteEngine.Integration.Tests && tests\Mars.SiteEngine.Integration.Tests\bin\Debug\net10.0\Mars.SiteEngine.Integration.Tests.exe
```
