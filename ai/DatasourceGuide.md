# DataSource — реестр источников с каталогом операций

Гайд для агента. Модуль `src/Mars.Datasource/`: SQL-базы (включая основную БД Mars — slug `default`),
файлы CSV/XLSX и внешние REST API одним механизмом. Схлопнут из планов инициативы 2026-09-21;
история шагов — `git show 5fae4d91:ai/DatasourceReworkPlan.md` и `git show 5fae4d91:ai/DatasourceStage2Plan.md`.

## Суть модели

- **DataSource — не SQL-клиент, а реестр источников с каталогом операций.** У каждого `Kind`
  (`sql | file | rest`, очередь — graphql/supabase/firebase/protobuf/sheets) свой язык запроса;
  универсальны четыре вещи: конфиг+доступы, каталог (объекты/операции + схема параметров), результат
  (таблица или документ), хранилище больших тел. Образцы такого устройства: Postman, Swagger UI,
  Power Platform connectors, ToolJet/Retool, Grafana Infinity, n8n.
- **Новый источник = проект-провайдер + профиль.** Ядро, контроллер и фронт читают
  `DatasourceKindProfile` из каталога, а не сравнивают `Kind`; ветвлений по kind'у в общем слое нет.
- **Два мира в UI** (решение пользователя, не «улучшать»): один роут `/datasource/query?slug=`,
  диспетчер `QueryPage` по `Kind` → `Workspaces/Sql/SqlQueryWorkspace` или
  `Workspaces/Objects/ObjectsQueryWorkspace`; общий низ — `WorkspaceShell` + `ResultTable` +
  `QueryResultGrid`. SQL-словарь (таблица/колонка/схема, `Q*`, `BrowseSqlBuilder`) живёт только
  в sql-мире; в общем мире — объект/поле/операция.

## Где что лежит (10 проектов)

- **`Mars.Datasource.Contracts`** (NuGet, виден из WASM). Неймспейсы = папки, кросс-ссылки доменов
  внутри библиотеки — `GlobalUsings.cs`:
  - `Config/` — `DatasourceConfig`/`DatasourceOption`, **`DatasourceKindProfile`** (+ `DatasourceFeature`,
    `DatasourceEditorLanguage`, `DatasourceActionDescriptor`), `DatasourceSettingField`,
    `DatasourceSettings` (ключи настроек — единый источник формы и провайдера), `RestDiscovery`,
    `SelectDatasourceDto`, `ConnectionStringTestDto`;
  - `Catalog/` — `DatasourceCatalog`/`DatasourceCatalogGroup`/`DatasourceCatalogObject`
    (+`DatasourceOperation`)/`DatasourceField`/`DatasourceOperationParameter`/`DatasourceActionRequest`;
  - `Query/` — `DatasourceRequest`/`DatasourceParam` (+ константы `DatasourceKind`, `DatasourceLanguage`),
    `QueryResultDto`, `DatasourceModifyResult`, `ViewDefinitionResponse`;
  - `Document/` — `DocumentText` (+`DocumentBlock`, операции над текстом `.http`), `HttpBlockSync`,
    `DocumentDto`;
  - `Ai/` — `DatasourceSchemaText` (каталог → текст схемы для агента), `DatasourceParametersJson`
    (JSON имя→значение → `DatasourceParam`);
  - `Nodes/` — `SqlNode`; `Sql/` — чистые хелперы: `SqlSafety`, `RestSafety`, `BrowseSqlBuilder`,
    `RowUpdateBuilder`, `ViewDdlBuilder`, `SqlDialect(+Mapping)`, `FieldTypeMapping`, `QueryResultMapping`.
- **`Mars.Datasource.Abstractions`** (NuGet, серверный): `Interfaces/` — `IDatasourceProvider(+Factory,+Registry)`,
  `IDatasourceDriver(+Factory)`, `IDatasourceBackupDriver`, `IDatasourceFileSource`, `IDatasourceStore`;
  `Services/` — три грани одного singleton-сервиса `IDatasourceRegistry` / `IDatasourceService` /
  `ISqlDatasourceService`; `Sql/` — `SqlDatasourceDriverBase` (общий ADO-скелет: абстрактные
  `CreateConnection`/`CreateCommand` и тексты каталогов, виртуальные `ParameterPrefix`,
  `ConfigureParameter`, `MapField`, `Actions`/`ExecuteAction`), `SqlDatasourceProvider(+Profile)`,
  `AdoResultReader`, `BackupSettings`; `Mappings/` — `CatalogMapping`, `QDatabaseStructure*`, `QTable*`.
- **`Mars.Datasource.Host`** — `DatasourceService` (три грани одним singleton'ом),
  `DatasourceProviderRegistry`, `DatasourceController`, `DatasourceStore` (data-корень),
  `DatasourceFileSource` (ссылки файла: относительная → медиа, корневая → диск хоста с откатом в медиа),
  backup, CLI `ds`, `SqlNodeImpl`.
- **`Mars.Datasource.Providers.PostgreSQL|MsSQL|MySQL`** — драйвер от `SqlDatasourceDriverBase` + фабрика
  + хук `AddDatasource<X>()`; у PG ещё backup-драйвер и `PostgreSqlUsefulQueries` (действия источника).
- **`Mars.Datasource.Providers.File`** — CSV (`TextFieldParser` из `Microsoft.VisualBasic.FileIO`) /
  XLSX (`ClosedXML`), Dynamic LINQ (`FileQueryConfig` со своим `ParsingConfig`, `Val.*`),
  `FileTypeInference` (имена типов как у sql — общая подсветка типов).
- **`Mars.Datasource.Providers.Rest`** — discovery (`IRestCatalogDiscovery`: `WordPressRestDiscovery`,
  `OpenApiRestDiscovery`, режим `none`), `HttpDocumentParser`/`RestRequestBuilder`/`RestExecutor`,
  `RestHttpClientCache` (ключ — SHA256-отпечаток настроек), `RestResponseMapping`, `RestRoutePrefix`.
- **`Mars.Datasource.Front`** (Razor-библиотека, WASM) — `QueryPage`, `Workspaces/`
  (`QueryWorkspaceBase` + partial `QueryWorkspaceAiChat`, `Shared/WorkspaceShell` + панели, `Sql/`,
  `Objects/`), `Components/` (грид, диалоги, `EditDatasourceOptions`, `DatasourceSettingFields`,
  `AuthConfigEditor`), свой API-клиент `IDatasourceServiceClient`. Ссылки: `Admin.Framework`,
  `AiChat.Front` (page bridge), `HttpSmartAuthFlow`, `MarsCodeEditor2`.
- **`Mars.Datasource`** — агрегатор (`AddDatasource`/`UseDatasource`): единственная точка подключения
  в WebApp; ядро не знает провайдеров (они регистрируются своими хуками из DI).

## Поток запроса

Конфиг из опции → `DatasourceProviderRegistry` резолвит провайдера по `(Kind, Driver)` →
`Catalog(slug, refresh)` (кэш 30 с; discovery-каталог сохраняется в data-корень при первом же вызове) →
профиль в каталоге управляет деревом/редактором/формой настроек → `Query(slug, DatasourceRequest)` →
`QueryResultDto` (`Fields`/`Rows`/`Json`/`Total`/`Truncated`) → запись через `Modify(slug, request)` →
документ пользователя через `Document`/`SaveDocument`. `TestConnection` = построение каталога
(работает для любого kind'а, форсирует discovery).

### Хранение: три артефакта на источник

Опция — одна JSON-строка на тип (`OptionService`), `EditDatasourceOptions` тянет её в WASM целиком —
большое там хранить нельзя (индекс WP `/wp-json/wp/v2` — 358 КБ).

| Артефакт | Где | Содержимое |
|---|---|---|
| Конфиг | `DatasourceOption` | `Kind`, `Driver`, `Title`, `Slug`, `Disabled`, `Settings` (rest: `baseUrl`, `discovery`, `discoveryUrl`, `timeoutSec`, `auth`; file: `files`, `hasHeaders`, `delimiter`). Метаданных каталога нет |
| Документ пользователя | `data/datasource/<slug>/requests.http` | именованные запросы, заготовки, переменные; правится из UI в Monaco |
| Каталог discovery | `data/datasource/<slug>/catalog.json` | группы операций в wire-модели; regenerable — «обновить» в дереве перезаписывает только его |
| Файлы file-источника | медиа-хранилище или диск хоста | ссылки в `Settings["files"]`; копии в data не делаем |

- Хранилище — keyed `"data"` `IFileStorage`; `Abstractions` его не видит → `IDatasourceStore`
  (реализация `DatasourceStore` в Host).
- Документ — данные пользователя: «обновить» его не трогает; дифф/merge как в Postman не делали.

### Языки запросов по kind'ам

- **sql**: свободный SQL (`Language=sql`); просмотр объекта — `BrowseSqlBuilder` (лимит 50 строк в самом
  SQL, `EnsureBrowseCap` поднимает серверный `MaxRows`), «всего N» — фоновый `COUNT` с таймаутом 5 с
  и только для нетронутого browse-запроса; «показать больше» — ×5 к лимиту. Правка ячейки =
  сгенерированный `UPDATE` (`RowUpdateBuilder`) + показ SQL + выполнение по кнопке (стиль phpMyAdmin;
  автоприменение отклонено), только при наличии PK. Вьюхи: `ViewDdlBuilder`, тело — первый SELECT
  из редактора, `CREATE OR REPLACE`/`CREATE OR ALTER`, автоматический drop+create не делаем.
- **file**: `Language=linq`, текст — **предикат `Where`** (пустой — все строки); только чтение;
  `MaxSourceRows = 100_000`; идентификатор объекта — ссылка на файл, у книги `ссылка#Лист`
  (разделитель `#`: `:` занят диском Windows-пути).
- **rest**: `Language=http`, текст — блок `.http` (синтаксис VS Code REST Client: `###`, `# @name`,
  `@var`, `{{var}}`, системные `$guid/$timestamp/$isoTimestamp/$datetime/$randomInt`; переменных
  окружения нет намеренно — документ приходит из браузера). `Query` выполняет любой метод;
  запись видна по `RestSafety.IsWrite` → подтверждение. `X-WP-Total` → `QueryResultDto.Total`.
  Вызов операции каталога без текста: `DatasourceRequest.ObjectId` (`METHOD путь`) + `Parameters`.

## AiChat-агент (этап G)

- Тулсет `SqlToolset` (AiChat.Host, флаг `AiChatOption.EnableSqlAccess`), инструменты в
  `Mars.Modules/Mars.AiChat.Host/Tools/MarsSqlTools.cs`: `list_data_sources` (slug/kind/driver/features),
  `get_source_schema(slug, filter)` (любой источник через `Catalog(slug)` → `DatasourceSchemaText`,
  бюджет 20 КБ), `run_query(slug, objectId?, query?, parametersJson?)` (file/rest; sql отсекается
  в `execute_sql`), `execute_sql` (свободный SQL — «harness-инструмент»; ≤25 строк модели + `total`).
- Скиллы (`ai-skills/`): `mars-sql` (preload при `EnableSqlAccess`) и `mars-datasource`
  (preload на страницах `/datasource/*` — `PageSkillRouter`); подтверждение записи через `ask_user` —
  правило скилла, серверная сторона запросы не гейтит.
- **`AiScenario` в профиле больше нет** — генерация/дописывание SQL только через чат; EF-хендлер схемы
  внутренней БД удалён (схема `default` — тоже из каталога), ссылка `Datasource.Host → Mars.Data` снята.
- Page bridge: `QueryWorkspaceBase` (partial, файл `Workspaces/QueryWorkspaceAiChat.cs`) реализует
  `IAiChatPageHandler` — `GetInfo` (slug/kind/вкладки/активная), `GetFields`/`SetField("editor")`
  (текст редактора), `Save` = выполнить активный запрос (родные подтверждения на месте); регистрация
  в `AiChatPageHandlerHolder.Current` на первом рендере, снятие в `Dispose`.

## Рецепты

**Новый kind** (очередь D — graphql, supabase (≈rest: PostgREST отдаёт OpenAPI, auth `apikey`+Bearer),
firebase, protobuf, sheets (чтение CSV-экспортом), кэш запросов (ключ `slug+objectId+hash(params)`)):

1. Проект `src/Mars.Datasource/Mars.Datasource.Providers.<Kind>/` плоско (в `Mars.slnx` — виртуальная
   папка); реализация `IDatasourceProvider` (`Profile/Catalog/Query/Modify`) + `IDatasourceProviderFactory`
   + хук `AddDatasource<Kind>()` (образец — `Providers.Rest/MainDatasourceProvider.cs`); подключить хук
   в агрегаторе `Mars.Datasource`.
2. Профиль: `Kind`, `Title`, `DefaultLanguage`/`EditorLanguage` (язык Monaco; своего языка нет в бандле —
   регистрировать монархом в `MarsCodeEditor2JsInterop.js`, образец `registerHttpLanguage`),
   `OpensAsDocument`/`DocumentName`, `DefaultGroup`/`CollapseGroupsAbove`, `Features`, `Settings`
   (дескрипторы полей — форма настроек нарисует сама), `Hint`/`EmptyRequestMessage`.
3. Ключи настроек — константами в `DatasourceSettings`, чтобы форма и провайдер не расходились.
4. Доступы — `AuthConfig` из `Mars.HttpSmartAuthFlow` одним JSON-значением настройки `auth`
   (`AuthStrategyFactory`/`AuthFlowHandler`; Basic/Bearer+OAuth/CookieForm/CookieEndpoint/ApiKey).
5. Тесты: юниты в `tests/Mars.Datasource.Tests`, Docker в `…Integration.Tests`.

**Новый sql-драйвер**: класс от `SqlDatasourceDriverBase` (переопределить `CreateConnection`/
`CreateCommand`, тексты каталогов; при другом диалекте — `SqlDialectMapping`, иначе молча будет
Postgres) + фабрика + хук. Действия источника — `IDatasourceDriver.Actions`/`ExecuteAction`
(образец `PostgreSqlUsefulQueries`); действия хоста (backup) — в `DatasourceCatalog.Actions`.

**Что переиспользовать**: пакеты `System.Linq.Dynamic.Core`, `ClosedXML`, `Microsoft.OpenApi` 2.x
запинены; Roslyn (`Microsoft.CodeAnalysis.CSharp.Scripting`) — если понадобится полноценный C#
(кешировать делегаты!); реестр «модуль приносит свой UI» — `INodeFormsLocator`/`IOptionsFormsLocator`
+ `RegisterAssembly` (образец, если провайдеры станут плагинами).

## Тесты

- Сборка: `dotnet build Mars.slnx`.
- Юниты (без Docker, ~2 с): `dotnet build tests/Mars.Datasource.Tests` →
  `tests\Mars.Datasource.Tests\bin\Debug\net10.0\Mars.Datasource.Tests.exe` (cwd = bin).
- Движки и backup (Testcontainers, нужен Docker): `…\tests\Mars.Datasource.Integration.Tests\bin\Debug\net10.0\
  Mars.Datasource.Integration.Tests.exe`; фильтр MTP: `-namespace`, `-class`, `-method` или
  `-filter "/сборка/namespace/класс/метод"` (`--filter`/`--treenode-filter` не работают).
- HTTP-контракт: `tests/Mars.WebApiClient.Integration.Tests/Tests/Datasources/DataSourceTests.cs`
  (методы зафиксированы `nameof` — снос эндпоинта ломает компиляцию).
- Живой WordPress: `tests/ExternalServices.Integration.Tests/WordPressTests/WordPressDatasourceTests.cs`
  (Docker, интернет, `git`; скипнуты константой `SkipTest` — обнулять только на время работы и
  возвращать: константа включает и нагрузочный тест на 1000 запросов). Стенд: Application Passwords
  отключены mu-плагином `MountFiles/disable-app-passwords.php` (иначе Basic Auth → 401).
- Агента: headless-полигон `aichat send` (см. `ai/AiChatGuide.md`) — тратит токены и может попасть
  в живой инстанс, запускать только по команде пользователя. UI — визуально при разработке
  (тестов на Razor-компоненты нет). Правки js/css — bump `MarsAppVersion` в `Directory.Build.props`.

## Грабли

Общие:

- **Большое — в `/data`** (служебные тела); в опциях только маленькие конфиги и ссылки — любое
  разрастание `DatasourceConfig` бьёт по каждому сохранению настроек и загрузке админки. Файлы
  file-источника в data не копируются (медиа или путь хоста, ввод руками — пикера нет).
- `IFileStorage`: пути относительные, разделитель `/`, абсолютные и выход за корень запрещены; keyed
  `"data"` регистрирует `MainServer`, в хостах без него — фолбэк `ApplicationPluginExtensions`,
  в тестах `InMemoryFileStorage`.
- WASM-админка **не может** ссылаться на агрегатор `Mars.Datasource` (`NETSDK1082`) — только
  `Contracts` + `Front`; `Front` не ссылается на `Abstractions`.
- Connection string и токены не логировать и не выводить в ошибках; AiChat их читать не даёт
  (`MarsOptionsTools` `ReadDenied`) — не сломать.
- Профиль — singleton на фабрику и попадает во все каталоги: мутировать нельзя.
- `CodeEditor2`: дефолтный `ContainerCssStyle` = `80vh` (при встраивании — `height:100%`),
  `SetValue/GetValue` до создания JS-редактора падают, пересоздание по `@key` сбрасывает `_editorReady`,
  события изменения контента из коробки нет (в модуль добавлены `OnCursorLine`/`OnContentChanged`).
- C# 14: `field` — контекстное ключевое слово внутри аксессоров; лямбда `Fields.Select(field => …)`
  в `QueryResultDto.Data` не компилируется (переименована в `item`).

SQL-мир:

- **Строковые параметры psql отправлять `NpgsqlDbType.Unknown`** (хук `ConfigureParameter`): иначе
  `text` против uuid-колонки — `42883: operator does not exist: uuid = text` (у постов uuid-ключи).
  MsSQL/MySQL приводят строку сами.
- **В контракте значение полное, показ — усечённый** (даты без секунд, guid «начало…конец», ячейка
  330px); полное — в `title`, `data-full` и редакторе, иначе правка ячейки вернула бы в базу
  усечённое время. psql-массивы форматируются литералом Postgres (`{a,"b,c"}`), `byte[]` — base64;
  значение обратимо параметром `UPDATE`.
- `ColumnSize` — `long?`, не `int?` (MySQL `longtext` = 4294967295 → `OverflowException`); регрессия
  `MySqlDatasourceTests.DatabaseStructure_LongTextColumn_SizeAboveInt32`.
- Postgres `CREATE OR REPLACE VIEW` не даёт убрать колонку (`42P16`) — выбранное поведение, тест есть.
- DDL сбрасывает кэш структуры (первое слово `CREATE|ALTER|DROP|REFRESH|TRUNCATE`) — иначе дерево
  до конца TTL показывает старое.
- `SqlSafety.IsDestructive`: `DROP/TRUNCATE/ALTER/GRANT/REVOKE` всегда, `DELETE`/`UPDATE` без `WHERE`;
  `CREATE` сознательно не там (см. «Не сделано»). Флага «источник только для чтения» нет — редактор
  и так исполняет любой SQL.

file / Dynamic LINQ:

- **Текст запроса — предикат `Where`, не цепочка**: `rows.Where("…")` не разбирается, а сортировка/
  проекция в цепочке работают — значит текстом их не задать (см. «Не сделано» про клик по заголовку).
- **`[DynamicLinqType]` ненадёжен** (провайдер типов кэшируется на первом разборе) — явная регистрация:
  `FileQueryConfig` держит свой `ParsingConfig` с `DefaultDynamicLinqCustomTypeProvider(config,
  [typeof(Val)], true)`. Симптом: `No property or field 'age' exists in type 'Char'`.
- Свои типы разбираются **с учётом регистра**: `Val.Num(age)` работает, `val.num(age)` — нет.
- `Convert.ToInt64(col)` падает на пустой ячейке → `Val.*` дают null на непарсимом.
- Реестр провайдеров: **единственный вариант типа источника → `Driver` в конфиге игнорируется**
  (дефолт `"psql"` остаётся в настройках file/rest, созданных через `new DatasourceConfig()`).
- Пин `System.IO.Packaging` в csproj файлового провайдера: ClosedXML тянет уязвимый 6.0.0 (NU1903).
- Namespace тестов `…Integration.Tests.File` затеняет `System.IO.File` — папка зовётся `FileProviders`.
  ClosedXML грузит книгу целиком в память → лимит строк/размера для XLSX.

rest:

- **Пути операций склеиваются с корнем REST API — иначе 404**: описание API задаёт пути не от адреса
  сайта (индекс WP по `/wp-json/wp/v2` отдаёт `/wp/v2/posts`). Префикс считает `RestRoutePrefix.FromAddress`;
  `RestSourceSettings.Combine` не удваивает общий сегмент и обязан работать с не-URI `baseUrl`
  (`{{baseUrl}}` → `UriFormatException`). Сохранённые до этого фикса `catalog.json` содержат старые
  пути — лечится «обновить» в дереве.
- `Microsoft.OpenApi` **2.x — не тот API, что 1.x**: типы в `Microsoft.OpenApi` (namespace `.Models`
  удалён), `OpenApiFormat` исчез, чтение — `OpenApiDocument.LoadAsync(stream, format: null, settings, ct)`
  → `ReadResult`, `Operations` — `Dictionary<System.Net.Http.HttpMethod, OpenApiOperation>`,
  обязательность полей тела — `requestBody.Schema.Required` (`ISet<string>`), `schema.Type` — `JsonSchemaType?`.
- Описание параметра в swagger чаще внутри `schema` → `parameter.Description ?? parameter.Schema?.Description`.
- **`Content-Type` принадлежит телу**: `StringContent` ставит `text/plain`, повторный
  `TryAddWithoutValidation` по занятому заголовку молча возвращает false → mediaType передавать
  в конструктор `StringContent` и пропускать в переборе заголовков.
- **JSON по умолчанию экранирует кириллицу** → сериализация с `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`
  (`RestJson`).
- **Параметры запроса — это переменные документа**: `{{name}}`/`{name}` в тексте важнее формы,
  не упомянутые уходят в query (чтение) или JSON-тело (запись).
- **На сервер блок уходит отдельным текстом, не всем документом**: переменные из шапки файла до него
  не доезжают — фронт подставляет объявления в начало через `DocumentText.DocumentVariables`
  (граница та же, что у серверного парсера).
- `DocumentText.Blocks`: текст до первого `###` не блок (там переменные), но запрос в преамбуле
  (первый запрос вправе идти без `###`) делает её первым блоком — иначе фронт не находил блок под
  курсором. Правило границ (`EndLine`) общее с парсером (тест `Parse_BlockBoundsMatchDocumentText`).
- `TestConnection` форсирует discovery, иначе сохранённый каталог временного конфига показывал бы
  успех при нерабочих настройках.
- `HttpRequestMessage` освобождает провайдер (`using`) — в тестах копировать запрос внутри
  `HttpMessageHandler`.
- Стенд WordPress: прерванный `git clone` оставляет каталог с одним `.git`, и фикстура считает его
  готовым (пересоздаёт сама).

фронт:

- **Ошибка каталога не должна подменять рабочую область целиком** (пользователь: «исчезает вся
  страница») — полоса над деревом; при пустом каталоге — alert с «Повторить». Инлайн-ошибки —
  `alert alert-danger` (решение 2026-09-21: `ExceptionMessage` из фреймворка — страничный «Oops»
  со стектрейсом, для инлайна не годится и не заменять).
- **Scoped-CSS страницы не дотягивается до внутренностей `WorkspaceShell`** (CSS-изоляция даёт
  b-атрибут только корневому элементу дочернего компонента): все `.ds-*` стили оболочки живут в
  `WorkspaceShell.razor.css`, у страниц мира своих CSS нет.
- **CodeLens в standalone-Monaco**: `editor.addAction` регистрирует команду с префиксом экземпляра
  (`<editorId>:<actionId>`), lens обязан собирать id как `editor.getId() + ':' + actionId`. Монарх:
  `@word` внутри регекса — ссылка на атрибут языка, литеральный `@` писать классом `[@]`.
- **`FluentSplitter`**: `split-panels` — grid-хост, слотнутые `div[slot]` остаются `height:auto` —
  в `WorkspaceShell.razor.css` им заданы `height:100%; min-height:0; overflow:hidden` через `::deep`.
- **`FluentSelect` без `OptionValue` пишет в значение подписью**: в `config.Kind` уезжало
  «SQL — база данных» вместо `sql`. Лечение — `OptionValue=@(kind => kind)` + починка неизвестного
  kind'а при открытии формы.
- `DatasourceSettingsEditor`: пустое значение настройки удаляется и означает «по умолчанию»; флажок
  снят только при `"false"` (провайдер читает так же: `FileSourceSettings.HasHeaders`); битый JSON
  в `auth` читается как «без доступа». Enum `AuthMode` пишется строкой: `JsonStringEnumConverter`
  нужен **и во фронте, и в провайдере**, иначе доступ молча становится «без доступа».
- **`@for` захватывает переменную цикла в лямбду события одну на все итерации** (к клику
  `rowIndex == Rows.Length`) — в лямбдах только локальные переменные тела цикла; `foreach` не подвержен.
- **Общий `@ref` на условно рендеримый элемент очищается в default при удалении элемента** (порядок
  вставка/удаление в диффе не гарантирован) → `FocusAsync` на пустой `ElementReference` падает.
  Инпут правки ячейки — отдельный компонент `CellEditInput` со своим `@ref`.
- **`tab.Object = null` на вкладке документа гасил форму параметров** — операция живёт в отдельном
  `QueryTab.Operation`; «объект каталога» и «объект документа» в одном поле держать нельзя.

## Инварианты

- **Не переименовывать**: `SqlNode` (`TypeId` = полное имя типа — иначе сохранённые flows →
  `UnknownNode`), `DatasourceOption` (ключ опции в БД = имя класса; по нему же закрыты секреты в
  `MarsOptionsTools`), имена файлов `requests.http`/`catalog.json`.
- `QueryResultDto.Data` — `[JsonIgnore]`-проекция (заголовки + строки, NULL → `""`); её читает
  `SqlNodeImpl` (агент — через `Fields`/`Rows`/`Json` в `MarsSqlTools.FormatResult`). Оба ходят
  в сервис, не в HTTP — проекцию не ломать.
- `DatasourceRequest.MaxRows = 0` — без ограничения (потоки и `SqlNode` не меняют поведение);
  лимиты задаёт вызывающий (UI, агент — 25).
- Публичных SQL-входов (`DatabaseStructure`/`Tables`/`Columns`/`RefreshStructure` + `Q*Response` +
  `DataSourceMapping`) больше нет — не возвращать: структура ходит только через каталог.
- Wire-совместимость `Contracts` до влития в master ломали осознанно; теперь master — источник правды,
  изменения wire-моделей = версия пакета.
- Компонент фронта: `.razor` + `.razor.cs` ≤ ~400–500 строк, новые файлы после разреза ≤250
  (поэтому page bridge — partial-файл `QueryWorkspaceAiChat.cs`).
- Legacy (инстансы, работавшие с веткой): rest-настройки с плоскими `authMode`/`authUsername`/…
  теряют доступы после F3b (настройка `auth` пуста) — источник пересохраняется в форме; миграцию
  не писали намеренно.

## Отклонено (не пересматривать без нового решения пользователя)

- **DuckDB / SQL-федерация над файлами** — нативная зависимость в Docker-образе, оффлайн-загрузка
  расширений, нет write-back. Вместо него file-kind с Dynamic LINQ.
- **Отдельный модуль для не-SQL источников** — потребители в `Mars.Datasource`, иначе дубль UI.
- **Отдельный REST-редактор вместо Monaco** — у всех Monaco, REST живёт как `.http`-документ;
  форма параметров — дополнение, не замена. **Свой формат запросов вместо `.http`** — держимся
  VS Code REST Client, чтобы файл открывался в VS/VS Code/Postman.
- **Своя таблица в БД под каталоги** — EF-сущность + конфигурации на четыре провайдера + миграции;
  вернёмся, если понадобится индекс/поиск по каталогам (тела останутся файлами).
- **Единый словарь для SQL и объектов** — пользователь отверг; миры разделены.
- **Автоприменение правки ячейки** (Airtable-стиль) — опасно на чужих БД; только показ SQL + кнопка.
- **OFFSET-обёртка для пагинации** (ломается на `WITH`, `;`, DDL), `ctid`/`rowid` вместо PK,
  флаг «только чтение» на источник, редактирование sequences/ролей, многоячейковый ноутбук (Jupyter).
- **Ручное/AI-заполнение каталога rest** — его заменяет документ пользователя.
- **Общая обвязка четырёх диалогов** (`CellValueDialog`, `CreateViewDialog`, `SqlPreviewDialog`,
  `ViewDefinitionDialog`) — boilerplate паттерна `IDialogContentComponent`, диалоги остаются как есть;
  `ConfirmChangeAsync` — со своим заголовком «Подтверждение запроса» (не удаление).
- **`AiScenario`-кнопки генерации SQL** — всё через чат (G3).

## Не сделано / открыто

- **Очередь kind'ов (D)**: GraphQL (introspection; язык в бандле Monaco есть) · Supabase (≈rest) ·
  Firebase (настоящий отдельный kind) · protobuf · Google Sheets (чтение CSV-экспортом, запись —
  OAuth отдельной фазой) · кэш запросов (TTL/manual) · паковка провайдеров и загрузка плагином.
- **`DatasourceNode`** для нод: параметры операции = входные поля с `ValueKind`
  (см. `ai/NodesReworkPlan.md`); заодно — фильтр по `Kind` в форме `SqlNode` (сейчас показывает все
  источники: sql-запрос к file/rest даст ошибку разбора) и общий редактор доступов на нодовую форму
  (`AuthConfigEditor` vs `AuthFlowConfigNodeForm`, нужен двусторонний маппинг).
- **UX `.http`-документа** (пользователь назвал текущее решение плохим, что менять — не сказал):
  клик по операции не должен дописывать заготовку в документ; куда вставлять заготовку отсутствующей
  операции — в конец (сейчас) или в позицию курсора. Кнопка «весь requests.http» из дерева убрана —
  документ открывается кликом по операции.
- **Запись file-источника** — «перезаписи пока не будет, добавим дальше»; механизм не обсуждён
  (правка ячейки с генерацией файла? только добавление строк?).
- Пикер файла из медиа в форме настроек (сейчас путь вводят руками).
- Сортировка/проекция для file-kind: делаем ли клик по заголовку грида серверным `OrderBy` для всех
  kind'ов (предикатом цепочку не выразить).
- Из этапа 1: SQL-автокомплит по схеме (свой completion provider Monaco — отложен сознательно),
  сохранённые запросы/история/состояние вкладок (где хранить — не решено), отмена запроса из UI
  (токен доходит до БД, но `CancellationToken` в методах клиента и кнопки нет), `CREATE` в списке
  опасных `SqlSafety` (сейчас `CREATE OR REPLACE VIEW` из редактора идёт без подтверждения —
  диалог вьюх подтверждает сам).
- Bootstrap-вставки → FluentUI (`OperationParametersPanel`, ячейка грида, кнопки дерева/тулбара,
  dropdown `data-bs-toggle` в `DatasourceHeader`/`SqlNodeForm`) — делать вместе с проверкой UI;
  учесть нелюбовь пользователя к flyout-меню в навигации.
- Секрет-слой: доступы rest лежат в опции открытым текстом (как connection string sql) — системный
  техдолг; в логах/ошибках не выводим, ключ кэша клиентов — SHA256-отпечаток.
- Концепт «большое в `/data`» — закрепить в `ai/ProjectStructureGuide.md` отдельной правкой?
- Цена решения про auth: `.Front` тянет `Mars.HttpSmartAuthFlow`, а с ним AngleSharp в WASM-пакет
  (если размер станет критичен — выносить `AuthConfig` в контрактный проект).
