# План: DataSource, этап 2 — kind'ы источников (file, REST, …)

Продолжение [DatasourceReworkPlan.md](./DatasourceReworkPlan.md): этапы 1–10 там закрыты (контракт
результата, баги, схема БД, JSON, визуал, вьюхи, лимит просмотра, сворачивание схем, структура модуля).
Этот план — про не-SQL источники. Ветка `ai/datasource-rework-stage2` (от `ai/datasource-rework`).
По закрытии обеих инициатив схлопнуть в один `ai/DatasourceGuide.md` ([PlanLifecycleGuide.md](./PlanLifecycleGuide.md)).

Статус 2026-09-16: план согласовывается, код не начат.

## 1. Направление

DataSource перестаёт быть SQL-клиентом и становится **реестром источников с каталогом операций**.
У каждого `Kind` свой способ описать запрос и свой язык, универсальными остаются четыре вещи:
конфиг+доступы, каталог (объекты/операции + схема параметров), результат (таблица или документ),
хранилище больших тел. SQL — один kind, самый развитый.

«Один универсальный способ получения данных» — это **не SQL**, а каталог операций с типизированными
параметрами и единый тип результата. Так же устроено у других: Postman/Insomnia (Environment +
Collection + форма параметров), Swagger UI/Redoc (форма генерируется из схемы параметров),
Power Platform custom connectors (OpenAPI + auth → поля рисует дизайнер), ToolJet/Retool/Budibase
(источник имеет тип, редактор запроса у каждого типа свой), Grafana Infinity (один источник,
`type: json|csv|xml|graphql`), n8n (resource/operation из схемы приложения).

## 2. Принятые решения (2026-09-16)

1. **Новый модуль не заводим.** Потребители (дерево, грид, вкладки, `SqlNode`, `MarsSqlTools`,
   CLI, backup, форма настроек) живут в `Mars.Datasource`; kind'ы наращиваются проектами
   `Mars.Datasource.Providers.*` рядом с существующими.
2. **Второй уровень абстракции — провайдер над драйвером**: `IDatasourceProvider` (kind),
   `IDatasourceDriver` опускается внутрь sql-провайдера, три существующих драйвера не меняются.
3. **`Kind` у sql — отдельный и явный** (`sql | file | rest | graphql | supabase | firebase | …`).
4. **DuckDB / SQL-федерация над файлами — отклонены** («забудь про DuckDB»).
5. **Отдельного редактора для REST не будет — у всех Monaco.** HTTP-запросы оформляются как
   `.http`-файл (синтаксис VS Code REST Client: разделитель `###`, `# @name`, `@var`, `{{var}}`) —
   перечень адресов, заголовков и JSON-тел в одном документе.
6. **Один документ на источник**: `data/datasource/<slug>/requests.http` (не коллекции).
7. **Большое — в `/data`** (концепт на всю систему): документы, каталоги, файлы источников.
   В опциях — только маленькие конфиги и ссылки. Позже закрепить в `ai/ProjectStructureGuide.md`.
8. **Wire-совместимость `Contracts` ломаем осознанно**: `SqlRequest` → `DatasourceRequest`.
9. **Порядок: file-kind первым** (без секретов и сети — короткий путь проверить каркас),
   **потом rest-kind на WordPress в Docker**.
10. **Запись будет** (POST/PUT/DELETE для rest, правка для file) — с подтверждением по образцу
    `SqlSafety`.
11. **Отложено**: кэш (сначала прототип), protobuf, GraphQL (делаем позже, но место в модели
    держим), секреты → **системный техдолг** (не задача этого модуля).
12. **Очередь будущих kind'ов**: GraphQL (introspection), Supabase, Firebase, protobuf.
13. **CSV-парсер — встроенный** (`Microsoft.VisualBasic.FileIO.TextFieldParser`), без новой зависимости;
    после тестов посмотрим, нужен ли `CsvHelper`.
14. **Строки file-kind — только Dynamic LINQ.** `RuntimeTypeCompiler`/генерацию типов не используем.
15. **Ноды и ИИ не трогаем**: `SqlNode` остаётся как есть, `DatasourceNode` и AiChat-инструменты
    поверх каталога — в конце инициативы.
16. **`.http` — по стандарту VS Code REST Client**, в редакторе пока `plaintext`; подсветка (свой
    monarch) — позже.

## 3. Хранение: три артефакта на источник

В опции хранить большое нельзя: `OptionService` держит опцию как **одну строку JSON на тип**
(`src/Mars.Modules/Mars.Options.Host/Services/OptionService.cs:78-127` — `SaveOptionAsync` пишет
значение целиком, `GetOption<T>` десериализует всё в `localCache`), а `EditDatasourceOptions`
тянет опцию в WASM целиком. Реальный размер discovery: `GET https://wordpress.org/news/wp-json/wp/v2`
— **358 КБ** (проверено 2026-09-16).

| Артефакт | Где | Содержимое |
|---|---|---|
| **Конфиг** | `DatasourceOption` (как сейчас) | `Kind`, `Driver`, `Title`, `Slug`, `Disabled`, `Settings` (baseUrl, ссылка на auth-конфиг, язык запроса, таймауты) + **только ссылка** на каталог: `origin`, `fetchedAt`, `hash` |
| **Документ пользователя** | `data/datasource/<slug>/requests.http` | именованные запросы, дубли, заготовки, переменные — то, что человек правит в Monaco |
| **Каталог discovery** | `data/datasource/<slug>/catalog.json` (+ `catalog.source.json` — сырой swagger/wp-json) | операции со схемами: `name`, `method`, `path`, `params[{in,type,required,default,enum,description}]`, `responseShape` |
| **Файлы источника** (file-kind, позже) | `data/datasource/<slug>/files/…` | CSV/XLSX внешних таблиц — данные источника, не контент сайта, поэтому не в медиа |

- Хранилище — keyed **`"data"` `IFileStorage`**: регистрирует `MainServer.UseFileStorages`
  (`src/Server/Mars.Server/MainServer.cs:153-168`, `PhysicalPath = ContentRootPath/data`), фолбэк для
  хостов без него — `src/Plugin/Mars.Plugin/ApplicationPluginExtensions.cs:52-67`. Потребители:
  `NodeService.cs:49` (flows), `PluginService.cs:39`, `PluginZipInstaller.cs:20`. В тестах —
  `InMemoryFileStorage` (`src/Server/Mars.Storage/Services/InMemoryFileStorage.cs`).
- `Abstractions` не видит `IFileStorage` (ссылается только на `Mars.Core`/`Mars.Contracts`/
  `Datasource.Contracts`) → контракт хранилища объявляем своим интерфейсом, реализация в `Host`
  (он `Mars.Server.Abstractions` уже ссылается).
- Дерево и форма параметров грузятся своим эндпоинтом `GET api/Datasource/Catalog(slug)` — по образцу
  уже работающего `DatabaseStructure(slug)` с серверным кэшем. В опцию и в WASM большой JSON не
  попадает никогда.
- **Документ — данные пользователя, каталог — regenerable.** Ре-импорт не перезаписывает документ,
  а предлагается как дифф/merge (так в Postman: import → merge or replace). «Создать дубль и
  заготовку» — операция над документом, каталога не касается.
- Паттерн «маленькая ссылка + отдельное тело» в Mars уже есть: `HttpRequestNode.AuthConfig` —
  `InputConfig<AuthFlowConfigNode>` (`src/Mars.Nodes/Mars.Nodes.Core/Nodes/Network/HttpRequestNode.cs:24`),
  тело в config-узле, резолвится `RNS.GetConfig(...)` (`HttpRequestNodeImpl.cs:31`, `NodeRuntime.cs:198`).

## 4. Что уже есть в репо и переиспользуется

- **Доступы для REST готовы**: `src/Modules/Mars.HttpSmartAuthFlow/` — `AuthConfig` (Basic /
  Bearer+OAuth `TokenUrl,ClientId,ClientSecret,Scope` / CookieForm / CookieEndpoint / ApiKey),
  `AuthStrategyFactory`, `Handlers/AuthFlowHandler`, `CookieFormStrategy` с парсингом логин-формы.
  Тест парсинга **именно формы входа WordPress**: `tests/Mars.HttpSmartAuthFlow.Integration.Tests/ParseFormTests.cs:8-45`.
  → в `Settings` rest-источника держим ссылку на auth-конфиг, а не своё поле с токеном.
- **Стенд WordPress готов, но выключен**: `tests/ExternalServices.Integration.Tests/WordPressTests/WordPressFixture.cs`
  — Testcontainers `wordpress:latest` + `mysql:8.0` в одной сети, wp-cli качается в temp, плагин
  Basic-Auth клонируется с GitHub, установка `wp-install.sh` из `MountFiles`, Flurl-клиент
  `WithBasicAuth("admin","admin")`, 10 постов через Bogus. Тесты `WordPressPerformanceTests.cs`
  (`/wp-json/wp/v2/users/me`, `posts?_fields=id,title,slug`) **скипнуты** константой
  `SkipTest = "not require every time"` (`:13,21,34`) — для rest-kind включать так же, как E2E
  (временно обнулить константу), а не гонять каждый прогон.
- **Discovery WP не требует OpenAPI**: индекс `/wp-json/wp/v2` отдаёт `routes` → `endpoints[]` →
  `args` с `type/default/enum/required/description` (проверено на wordpress.org/news). Плюс
  `X-WP-Total`/`X-WP-TotalPages` ложатся в уже готовый `QueryTab.Total` (этап 8).
- **LINQ-строка**: `System.Linq.Dynamic.Core` 1.7.4 уже запинен (`Directory.Packages.props:119`) и
  нигде не используется.
- **Roslyn** (если понадобится полноценный C#): `Microsoft.CodeAnalysis.CSharp.Scripting` 5.9.0
  (`Directory.Packages.props:51`), работает в `FunctionNodeImpl.cs:63-77` и
  `Mars.MetaModelGenerator/RuntimeTypeCompiler.cs:104`.
- **XLSX**: `ClosedXML` 0.102.2 в стеке (`Directory.Packages.props:11`), но сейчас только генерация
  отчётов (`Mars.Excel.Host/Services/ExcelService.cs`, `ExcelNode` — шаблонный отчёт, не чтение).
- **OpenAPI**: `Microsoft.OpenApi` уже используется (`src/Server/Mars.Server/Startup/MarsSwagger.cs:13`
  и фильтры) — на генерацию; чтение чужого документа та же библиотека.
- **Monaco** (бандл BlazorMonaco 3.5.0): `graphql` ✔, `protobuf` ✔, `csharp/sql/mysql/pgsql/json/yaml/ini` ✔,
  **`http` ✘** (в monaco-editor его нет — это язык расширения VS Code). Белый список языков —
  `src/Modules/MarsCodeEditor2/CodeEditor2.razor.cs:26-40`.
- **Реестр «модуль приносит свой UI»** и он уже работает для плагинов: `INodeFormsLocator` /
  `IOptionsFormsLocator` + `RegisterAssembly`, регистрация ассемблей плагина —
  `src/Plugin/Mars.Plugin.Front/PluginFrontHelperExtensions.cs:26-36`.
- Общий слой результата уже ADO-обобщён и kind-независим: `QueryResultMapping.Column(DbColumn)` /
  `ReadRowsAsync(DbDataReader)` / `ApplyParameters(DbCommand,…)`, `QDatabaseStructureBuilder`;
  `SqlDialectMapping.Dialect` для незнакомого драйвера возвращает `Postgres`.

## 5. Этапы

### Этап A. Каркас kind'ов (ядро)

- [ ] A1. `DatasourceConfig`: `Kind` (default `sql`), `Settings` (`Dictionary<string,string>`);
      `ConnectionString` остаётся полем sql-kind'а, `Driver` — внутри kind'а. Миграция старых
      конфигов без кода: `DatasourceOption` → `INormalizableAfterReadValue` (пустой `Kind` → `sql`),
      хуки уже вызывает `OptionService`.
- [ ] A2. `Contracts`: `DatasourceRequest { ObjectId?, Language, Query?, Parameters, MaxRows,
      TimeoutSec }` вместо `SqlRequest` (wire ломаем, решение 8); `Language = sql | linq | csharp |
      graphql | http | none`; каталог `QCatalog/QGroup/QObject/QParameter`; `QueryResultDto` +
      `Json` (`JsonNode`) + `ResultShape (Table|Document|Both)`; `DatasourceDriverResponse` + `Kind`.
      **Инвариант:** `[JsonIgnore]`-проекция `QueryResultDto.Data` остаётся — её читают
      `SqlNodeImpl.cs:44` и `MarsSqlTools.FormatRows`.
- [ ] A3. `Abstractions`: `IDatasourceProvider { Kind, Capabilities, Catalog(ct), Execute(req,ct),
      Discover(settings,ct) }` + `IDatasourceProviderFactory { Kind, Driver, DisplayName, HelpLink,
      Create(config) }`; `Capabilities` (`CanSql/CanBrowse/CanEdit/CanExecute/ReadOnly`);
      `SqlDatasourceProvider` — адаптер поверх существующих `IDatasourceDriverFactory`
      (три драйвера не трогаем).
- [ ] A4. `IDatasourceDocumentStore` (Abstractions, без `IFileStorage`) + реализация в Host поверх
      keyed `"data"`: `Read/WriteDocument(slug)`, `Read/WriteCatalog(slug)`, пути
      `datasource/<slug>/…`.
- [ ] A5. Host: `DatasourceService` резолвит по `(Kind, Driver)`; кэш структуры обобщается до кэша
      каталога; контроллер — `Catalog(slug)`, `Document(slug)` (get/put), `Execute(slug, request)`.
- [ ] A6. Front: оболочка (дерево каталога вместо «только таблицы», результат, вкладки, строка
      состояния) + реестр редакторов kind'а (`IDatasourceEditorLocator` по образцу
      `INodeFormsLocator`) + `<DynamicComponent>`; sql-kind — текущий Monaco+грид без изменений.
- [ ] A7. Форма настроек источника: поля из `Settings` по описанию провайдера (генерическая форма),
      для сложного — своя Razor-форма через `IOptionsFormsLocator`.
- [ ] A8. Тесты: реестр провайдеров и резолв по `(kind,driver)`, нормализация `Kind`,
      `InMemoryFileStorage`-хранилище документа/каталога, wire-контракт новых эндпоинтов.

### Этап B. file-kind (первым)

- [ ] B1. `Mars.Datasource.Providers.File`: CSV (`Microsoft.VisualBasic.FileIO.TextFieldParser` —
      разделитель/кавычки/BOM/пустые значения) + XLSX (`XLWorkbook(stream)`, листы → объекты каталога).
- [ ] B2. Файл источника — из `data/datasource/<slug>/files/…` (загрузка через админку позже);
      в `Settings` — имя файла, лист, «первая строка = заголовки».
- [ ] B3. Запрос `Language=linq` через Dynamic LINQ: фильтр/сортировка/проекция над строками;
      вывод типов колонок → `QColumnKind` (уже есть, используется гридом).
- [ ] B4. `MaxRows`/`Truncated` и отмена — те же, что для sql (общий `QueryResultDto`).
- [ ] B5. Front: Monaco `csharp` (или `plaintext`) вместо SQL, грид/ json-viewer без изменений,
      правка ячеек — по решению (есть PK-понятие только у sql; для файла — «read-only» в первом проходе).
- [ ] B6. Тесты: парсинг CSV/XLSX (юнит, файлы-фикстуры), LINQ-запрос, лимит строк, HTTP-контракт.

### Этап C. rest-kind (WordPress)

- [ ] C1. `Mars.Datasource.Providers.Rest`: discovery — OpenAPI (`Microsoft.OpenApi`), индекс
      `wp-json` (routes→endpoints→args), manual/AI как фолбэк; всё даёт один `QCatalog`.
- [ ] C2. Документ `.http` (синтаксис VS Code REST Client) + серверный парсер → операции; выполнение
      через `HttpClient` + `AuthFlowHandler`/`AuthConfig`; ответ: массив объектов → колонки+строки,
      иначе `Json`.
- [ ] C3. Запись (POST/PUT/DELETE) + подтверждение опасного действия (аналог `SqlSafety.IsDestructive`
      — по методу и наличию тела).
- [ ] C4. `X-WP-Total`/`X-WP-TotalPages` → `Total`/`TotalNote` в гриде; пагинация `page`/`per_page`
      из схемы параметров.
- [ ] C5. Front: дерево = именованные запросы документа (`# @name`), форма параметров из схемы
      каталога как дополнение к Monaco; язык `http` в бандле monaco отсутствует → `plaintext`
      (подсветка позже).
- [ ] C6. Тесты: парсер `.http` (юнит), discovery по индексу WP и выполнение — на
      `WordPressFixture` (Docker + интернет + `git` в PATH).

### Этап D. Очередь (в этой ветке не делаем, место в модели держим)

GraphQL (introspection; язык `graphql` в бандле есть) · Supabase (по сути rest-kind: PostgREST
отдаёт OpenAPI, пресет auth `apikey` + Bearer) · Firebase (настоящий отдельный kind:
коллекции/документы) · protobuf (подсветка в бандле есть) · кэш (TTL/manual, ключ
`slug+objectId+hash(params)`) · секрет-слой (системный техдолг) · `DatasourceNode` для нод
(параметры операции = входные поля с `ValueKind`, см. `ai/NodesReworkPlan.md`) · AiChat-инструменты
поверх каталога · паковка провайдеров (бывший 6.4).

## 6. Проверка

- Сборка: `dotnet build Mars.slnx`.
- Новые юнит-тесты: парсеры (CSV/XLSX/`.http`), normalizer `Kind`, Dynamic LINQ-запрос, хранилище
  документа на `InMemoryFileStorage`.
- `tests/Mars.Datasource.Integration.Tests` — новые провайдеры (движковые тесты не трогаем).
- HTTP-контракт новых эндпоинтов: `tests/Mars.WebApiClient.Integration.Tests/Tests/Datasources/DataSourceTests.cs`.
- rest-kind: `tests/ExternalServices.Integration.Tests/WordPressTests` (нужны Docker, интернет, `git`;
  тесты скипнуты константой `SkipTest` — на время фазы обнулять, обратно не забывать).
- UI — визуально при разработке фазы (тестов на Razor-компоненты в репо нет), отдельным прогоном не проверять.

## 7. Грабли и инварианты

- **Большое — в `/data`**, в опциях — только маленькие конфиги и ссылки (инвариант концепта).
- Опция = один JSON-ряд на тип: любое разрастание `DatasourceConfig` бьёт по каждому сохранению
  настроек и по загрузке админки.
- keyed `"data"` `IFileStorage` регистрирует `MainServer`; в хостах без него нужен фолбэк
  (`ApplicationPluginExtensions.cs:52-67`), в тестах — `InMemoryFileStorage`.
- `IFileStorage`: пути относительные, разделитель `/`, абсолютные и выход за корень запрещены.
- WASM-админка не может ссылаться на агрегатор `Mars.Datasource` (`NETSDK1082`,
  `FrameworkReference Microsoft.AspNetCore.App` vs `browser-wasm`) — только `Contracts` + `Front`.
- `SqlDialectMapping.Dialect` для незнакомого драйвера молча возвращает `Postgres` — новый sql-драйвер
  с другим диалектом получит `"..."`/`LIMIT` без предупреждения.
- `FunctionNodeImpl` компилирует скрипт на каждый вызов (делегат не кэшируется) — если пойдём в
  Roslyn для file-kind, кэшировать делегаты обязательно.
- ClosedXML грузит книгу целиком в память → лимит строк/размера файла для XLSX.
- `ColumnSize` — `long?`, не `int?` (у MySQL `longtext` = 4294967295); грабля этапа 1, регрессия
  `MySqlDatasourceTests.DatabaseStructure_LongTextColumn_SizeAboveInt32`.
- Connection string / токены не логировать и не выводить в ошибках; AiChat их читать не даёт
  (`MarsOptionsTools` `ReadDenied`) — не сломать.
- `CodeEditor2`: дефолтный `ContainerCssStyle` = `80vh` — при встраивании переопределять на
  `height:100%`; `SetValue/GetValue` до создания JS-редактора падают.
- После правок js/css bump `MarsAppVersion` (`Directory.Build.props`).

## 8. Отклонено (с причинами)

- **DuckDB / SQL-федерация над файлами** — решение пользователя 2026-09-16. Альтернатива: file-kind
  с Dynamic LINQ. Причины отказа: нативная зависимость в Docker-образе, оффлайн-загрузка расширений,
  write-back в файлы отсутствует.
- **Отдельный модуль для не-SQL источников** — потребители в `Mars.Datasource`, иначе дубль UI.
- **Отдельный REST-редактор вместо Monaco** — решение 5: у всех Monaco, REST живёт как `.http`-документ.
  Форма параметров — дополнение, а не замена редактора.
- **Своя таблица в БД под каталоги** (пока) — EF-сущность + конфигурации в PostgreSQL/MsSQL/MySQL/
  InMemory + миграции ради прототипа. Вернёмся, если понадобится индекс/поиск по каталогам;
  тела в любом случае останутся файлами.
- **Свой формат запросов вместо `.http`** — держимся синтаксиса VS Code REST Client, чтобы файл
  открывался в VS/VS Code/Postman и обратно.
- **Свой мини-язык фильтров** — вместо него Dynamic LINQ (стандартная библиотека, уже запинена).

## 9. Открытые вопросы

1. Ре-импорт каталога: merge/replace и показ диффа — в первом проходе rest-kind или позже.
2. Концепт «большое в `/data`» — внести в `ai/ProjectStructureGuide.md` отдельной правкой?
3. Загрузка файлов источника через админку (медиа-пайплайн vs своя загрузка в `data/…/files`).
4. Dynamic LINQ над строками: проверить первым же тестом, как обращаться к колонке
   (`it["col"]` / `it.col` на `ExpandoObject`) — от этого зависит форма строки в file-kind.
5. Подсветка `.http` своим monarch — когда (после прототипа).
