# План: DataSource, этап 2 — kind'ы источников (file, REST, …)

Продолжение [DatasourceReworkPlan.md](./DatasourceReworkPlan.md): этапы 1–10 там закрыты (контракт
результата, баги, схема БД, JSON, визуал, вьюхи, лимит просмотра, сворачивание схем, структура модуля).
Этот план — про не-SQL источники. Ветка `ai/datasource-rework-stage2` (от `ai/datasource-rework`).
По закрытии обеих инициатив схлопнуть в один `ai/DatasourceGuide.md` ([PlanLifecycleGuide.md](./PlanLifecycleGuide.md)).

Статус 2026-09-17: **этапы A (каркас kind'ов), B (file-источник) и C (rest-источник на WordPress)
сделаны целиком, включая фронт**; коммит `eb82a73d` + правки C7/C8. Проверено: `dotnet build Mars.slnx` —
0 ошибок/0 предупреждений; `Mars.Datasource.Integration.Tests` 455/455; `Mars.WebApiClient.Integration.Tests`
(Datasource) 19/19; rest на живом WordPress в Docker — 9/9 (`WordPressDatasourceTests`, скипнуты
константой `SkipTest`). На default-источнике страница работает как раньше (проверил пользователь).
**File- и rest-источник глазами не проверяли**: форма настроек, дерево и запрос ждут проверки в браузере.
Найденный при проверке баг «в запрос не подставляется `/wp-json/`» исправлен — см. грабли про префикс путей.
Редактор rest-источника переделан под решение пользователя 2026-09-17: один документ `.http` вместо
вкладки на операцию — см. C7; после проверки C7 пользователем — дерево по методам, форма операции
и отказ от авто-выполнения запроса кликом — см. C8. Открыто: что именно менять в «открытии `.http`-файла»
(вопрос 7) и запись файлового источника (решение: «добавим дальше»).
Отдельно 2026-09-17/18 пользователь назвал код этапа 2 излишне сложным и попросил план разбиения —
добавлен этап E: мир делится на два (SQL-страница и страница объектов), решения — один роут,
две страницы, общий `ResultTable`; SQL-словарь (таблица/колонка/схема) остаётся как есть, публичные
SQL-входы (`DatabaseStructure`/`Tables`/`Columns`/`RefreshStructure`) сносятся, AiChat-инструмент
схемы глушится до конца инициативы. Серверная чистка и переделки UI на общие примитивы в этап E
не входят.

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
| **Конфиг** | `DatasourceOption` (как сейчас) | `Kind`, `Driver`, `Title`, `Slug`, `Disabled`, `Settings`: у rest — `baseUrl`, `discovery`, `discoveryUrl`, `timeoutSec`, `authMode` и доступы; у file — `files`, `hasHeaders`, `delimiter`. Метаданных каталога (`origin`/`fetchedAt`/`hash`) в опции нет — не понадобились |
| **Документ пользователя** | `data/datasource/<slug>/requests.http` | именованные запросы, дубли, заготовки, переменные — то, что человек правит в Monaco (пока только файлом, см. открытый вопрос 7) |
| **Каталог discovery** | `data/datasource/<slug>/catalog.json` | группы операций в wire-модели `DatasourceCatalogGroup` (id/name/parameters/defaultQuery). Сырой swagger/wp-json (`catalog.source.json`) не сохраняем: он в десятки раз больше, а пересобрать каталог — один запрос |
| **Файлы источника** (file-kind) | медиа-хранилище Mars или диск хоста | решение 2026-09-16: это контент сайта, в data-корень их не копируем; в `Settings["files"]` — ссылки |

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
- **Документ — данные пользователя, каталог — regenerable.** Ре-импорт («обновить» в дереве)
  перезаписывает только `catalog.json` и никогда не трогает `requests.http`; дифф/merge как в Postman
  не делали — свои настройки операций пока нечего беречь (открытый вопрос 1).
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

### Этап A. Каркас kind'ов (ядро) — сделано 2026-09-16 (кроме фронта)

- [x] A1. `DatasourceConfig`: `Kind` (default `sql`), `Settings` (`Dictionary<string,string>`);
      `ConnectionString` остаётся полем sql-kind'а, `Driver` — внутри kind'а. Миграция старых
      конфигов: `DatasourceConfig.Normalize()` (пустой `Kind` → `sql`, sql без драйвера → `psql`),
      вызывает `DatasourceService.InvalidateLocalDictCache`. `[Required]` на `ConnectionString`
      пока оставлен — снимем вместе с формой настроек (A7), когда появятся не-sql источники в UI.
- [x] A2. `Contracts`: `DatasourceRequest { ObjectId?, Language, Query, Parameters, MaxRows,
      TimeoutSec }` вместо `SqlRequest`, `DatasourceParam` вместо `SqlParam`,
      `Language = sql | linq`; каталог `DatasourceCatalog/DatasourceCatalogGroup/
      DatasourceCatalogObject/DatasourceCatalogColumn/DatasourceOperationParameter` +
      `DatasourceCapabilities` + `DatasourceKind`/`DatasourceObjectType`/`DatasourceParameterIn`;
      `DatasourceDriverResponse` += `Kind`, `DisplayName`; `ConnectionStringTestDto` += `Kind`,
      `Settings`. **Инвариант:** `[JsonIgnore]`-проекция `QueryResultDto.Data` осталась — её читают
      `SqlNodeImpl` и `MarsSqlTools.FormatRows`. `Json`/`ResultShape` не добавляли: документных
      ответов пока нет, появятся в этапе C.
- [x] A3. `Abstractions`: `IDatasourceProvider { Capabilities, Catalog, Query, Modify }` +
      `ISqlDatasourceProvider { Driver }` + `IDatasourceProviderFactory { Kind, Driver, DisplayName,
      DefaultConnectionString, HelpLink, Create }` + `IDatasourceProviderRegistry`;
      `SqlDatasourceProvider`/`SqlDatasourceProviderFactory` — адаптер поверх существующих
      `IDatasourceDriverFactory` (три драйвера не изменились, кроме переименования `SqlRequest`).
      `Discover` отдельным интерфейсом не заводили — появится в этапе C, когда будет кому.
- [x] A4. Доступ к файлам источника: `IDatasourceFileSource` (Abstractions) + `DatasourceFileSource`
      (Host) — относительная ссылка читается из **медиа-хранилища** Mars (не-keyed `IFileStorage`),
      корневая — с диска хоста, а корневая без такого файла пробуется как медиа-путь (путь копируют
      из медиа, где он бывает с ведущим слэшем). **В data-корень файлы источников не копируются**
      (решение пользователя 2026-09-16: «лежат либо в медиа, либо где-то на хосте… да, видны как
      контент сайта»). Data-корень остаётся за большими служебными телами — документ `.http` и
      каталог discovery появятся там в этапе C (тогда же вернётся `IDatasourceStore`).
- [x] A5. Host: `DatasourceProviderRegistry` (sql-движки оборачиваются, остальные — свои фабрики),
      `DatasourceService` резолвит провайдера по `(Kind, Driver)`, `Catalog(slug)` с кэшем 30 с
      (сбрасывается вместе с кэшем структуры при смене опции и после DDL), `TestConnection`
      проверяет источник построением каталога (работает для любого kind'а), `GET api/Datasource/Catalog`.
- [x] A6. Front: рабочая область переведена на каталог — `DatasourceCatalog` вместо
      `QDatabaseStructureResponse`, в дереве группы (схемы) и объекты любого типа, `QueryTab.Table`
      → `QueryTab.Object` + `QueryTab.Schema` + `QueryTab.Language`/`SourceWritable`, открытие объекта
      (`OpenObjectAsync(CatalogEntry)`) для sql собирает browse-SQL как раньше, для остальных —
      пустой текст и объект в запросе. Язык Monaco — по типу источника (`sql` / `csharp`),
      компонент пересоздаётся по `@key`, `_editorReady` при этом сбрасывается (грабля CodeEditor2).
      Kind-зависимое в UI: «＋ вьюха» и кнопки вьюхи — только при `CanManageViews`, «AI help» —
      только для sql, «всего N» (`COUNT`) — только для sql, подтверждение `SqlSafety` — только для sql,
      подсказка под редактором своя для файла (примеры `Val.*`). Кнопка «обновить» зовёт
      `RefreshCatalog` (новый эндпоинт + метод клиента) — работает для любого типа источника.
      Реестр редакторов kind'а (`IDatasourceEditorLocator`) не понадобился: отличия оказались
      в языке Monaco и наборе кнопок, а не в отдельном компоненте. Вернёмся к нему, когда появится
      rest-источник с формой параметров.
- [x] A7. Форма настроек: выбор типа источника (`Kind`) с подписями, драйверы фильтруются по kind'у
      (у file их нет — поле скрыто), `ConnectionString` показывается только тем kind'ам, чей провайдер
      дал подсказку строки подключения, у file — свои поля (`file`, `delimiter`, «первая строка —
      заголовки») с записью в `Settings` (пустое значение из словаря удаляется = «по умолчанию»),
      смена kind'а чинит драйвер и строку подключения, инлайн-ошибка «Укажите строку подключения».
      `[Required]` с `ConnectionString` снят — он не может быть kind-зависимым.
      Ключи настроек — в `Contracts` (`DatasourceSettings`), чтобы форма и провайдер не расходились.
      `PartDataSourceActions` передаёт в `TestConnection` kind и settings (иначе файл проверялся бы как sql).
- [x] A8. Тесты: `DatasourceProviderRegistryTests` (10 — резолв по kind/driver, неизвестный kind,
      чужой драйвер, единственный вариант типа, устаревший драйвер у типа без вариантов, список
      вариантов, отказ `ResolveSql`, `Describe`), `CatalogMappingTests` (8 — группы по схемам,
      id `schema.table`, пустая схема, kind объекта, колонки по ordinal с PK/JSON/размером,
      capabilities, `Kind` из конфига), `DatasourceConfigTests` += `Normalize` (5), HTTP-контракт:
      `Catalog_Request_Success`, `Catalog_CalledTwice_ReturnsCachedInstance`,
      `RefreshCatalog_Request_Success`, обновлён `Drivers_Request_Success` (теперь ждёт и file).

### Этап B. file-kind — сделано 2026-09-16

- [x] B1. `Mars.Datasource.Providers.File`: CSV встроенным `Microsoft.VisualBasic.FileIO.TextFieldParser`
      (кавычки, разделитель внутри значений, BOM, автоопределение разделителя `,`/`;`/tab/`|` по
      первой строке вне кавычек, явный разделитель из настроек) + XLSX через `XLWorkbook(stream)`
      (листы → объекты каталога, значение ячейки — инвариантным текстом: `35`, `true`,
      `2024-03-01`, `2024-03-01T10:20:30`, пустая ячейка → null).
- [x] B2. Файлы источника — **ссылки в `Settings["files"]`** (через `;`), каждая указывает либо путь
      в медиа-хранилище (`2026/09/sales.csv`), либо абсолютный путь на хосте (`C:\data\book.xlsx`);
      читаются через `IDatasourceFileSource`. Идентификатор объекта — сама ссылка, у книги с
      несколькими листами — `ссылка#Лист` (разделитель `#`, а не `:` — его занимает диск в
      Windows-пути). В `Settings` также `hasHeaders` (default true) и `delimiter`. Загрузка файлов —
      существующая медиа-админка, своего UI загрузки нет; выбор файла из медиа в форме — позже.
- [x] B3. Запрос: `Language=linq`, текст = **предикат `Where`** (пустой — все строки), Dynamic LINQ
      со своим `ParsingConfig` (`FileQueryConfig`) и функциями приведения `Val.Num/Dec/Str/Date/Flag/Id`;
      вывод типов колонок — `FileTypeInference` (имена типов `bigint`/`double precision`/`boolean`/
      `timestamp`/`uuid`/`text`, то есть понятные `QColumnMapping` → подсветка типов и короткое имя
      в гриде работают для файлов без отдельных правил).
- [x] B4. `MaxRows`/`Truncated` — те же, что для sql; жёсткий предел чтения `MaxSourceRows = 100_000`
      (файл читается в память целиком). `Modify` — внятный отказ «только для чтения».
- [x] B5. Front: Monaco `csharp` для linq-запроса, дерево из каталога (группа у файла одна и без
      имени, поэтому заголовков групп нет), грид без изменений, правка ячеек выключена —
      `QueryTab.CanEdit` требует `SourceWritable`, а вместо «нет первичного ключа» показывает
      «источник только для чтения» (`QueryTab.EditDisabledReason`). Просмотр файла = 50 строк
      через `MaxRows`, «Показать больше» растит его ×5 (browse-SQL у файла нет).
- [x] B6. Тесты (`tests/Mars.Datasource.Integration.Tests/FileProviders/`): `CsvTabularFileReaderTests`
      (10 — кавычки/пустые, автоопределение `;`, явный `tab`, без заголовков, BOM, дубли заголовков,
      лимит, пустой поток, лишние поля, `CanRead`), `XlsxTabularFileReaderTests` (7 — все листы,
      выбор листа по имени и номеру, типы ячеек, лимит, без заголовков, пустой лист, `CanRead`),
      `ValFunctionTests` + `FileTypeInferenceTests` (18), `DatasourceFileSourceTests` (5 — медиа-путь,
      обратные слэши, корневой путь с диска, откат корневого пути в медиа, отсутствующий файл),
      `FileDatasourceProviderTests` (20 — каталог с листами и пропуска чужих форматов, вывод типов,
      пустой список файлов, запросы, предикаты, регистр `Val`, лимит, битый предикат, выбор объекта,
      Windows-путь с `:` в идентификаторе, `hasHeaders=false`, xlsx-лист, отказы, capabilities,
      DI-регистрация).

### Этап C. rest-kind (WordPress) — сделано 2026-09-17

- [x] C1. `Mars.Datasource.Providers.Rest` (новый проект, в `Mars.slnx` и в агрегаторе
      `AddDatasourceRest()`): два сборщика каталога за одним интерфейсом `IRestCatalogDiscovery` —
      `WordPressRestDiscovery` (индекс `/wp-json/wp/v2`: `routes → endpoints → args`, маршрут-регулярка
      `(?P<id>[\d]+)` → путь `{id}` + параметр path, GET-аргументы → query, остальные → body, группы —
      пространства имён) и `OpenApiRestDiscovery` (`Microsoft.OpenApi` 2.7.5: `paths → operations`,
      `parameters` + свойства схемы `requestBody`, группы — теги). Третий режим `discovery=none` —
      каталог только из документа пользователя. Manual/AI-заполнение не делали: его заменяет документ.
- [x] C2. Документ `.http` в `data/datasource/<slug>/requests.http` (вернулся `IDatasourceStore`,
      реализация `DatasourceStore` в Host поверх keyed `"data"` `IFileStorage`): парсер
      `HttpDocumentParser` — блоки `###`, имя `# @name` (иначе подпись разделителя), переменные
      `@name = value` (документные и блочные), подстановки `{{name}}`, системные `$guid/$timestamp/
      $isoTimestamp/$datetime/$randomInt` (переменных окружения нет намеренно — документ приходит
      из браузера), заголовки до пустой строки, тело — до следующего блока. Выполнение:
      `RestRequestBuilder` → `RestExecutor` → `HttpClient` c `AuthFlowHandler`/`AuthConfig`
      (`Mars.HttpSmartAuthFlow`), клиенты кэширует `RestHttpClientCache` (ключ — отпечаток настроек,
      `PooledConnectionLifetime` 5 мин). Ответ: `RestResponseMapping` — массив объектов → колонки
      и строки, объект → одна строка + документ в `QueryResultDto.Json`, не-JSON → ячейка `response`.
- [x] C3. Запись: метод из текста запроса решает всё — `Query` выполняет любой метод, `Modify` отдаёт
      `SqlNonQueryResultActionDto`; `RestSafety.IsWrite`/`FirstMethod` (в Abstractions, рядом с
      `SqlSafety`) — по ним фронт показывает подтверждение «Запрос POST меняет данные источника».
      Тело для записи без тела собирается из параметров (числа и булевы — своим типом), `Content-Type`
      берётся из документа, иначе `application/json`.
- [x] C4. `X-WP-Total` → `QueryResultDto.Total` (новое поле) → `QueryTab.Total` → «строк: N из M»
      в гриде; `page`/`per_page` приходят из схемы параметров операции в форму под редактором.
- [x] C5. Front: дерево = операции discovery + группа «Запросы» из документа (объект — именованный
      запрос, `DefaultQuery` = текст его блока), открытие операции подставляет заготовку
      `GET {{baseUrl}}/wp/v2/posts` в Monaco, язык `plaintext` (`http` в бандле нет; в
      `CodeEditor2.Language` добавлен `plaintext`), форма параметров операции (`in · type`, enum —
      select, placeholder — default) над редактором, подтверждение записи, ответ документом сам
      открывает json-вид, подсказка про синтаксис `.http`. Форма настроек: тип источника
      «REST API — WordPress, OpenAPI», `baseUrl` (с проверкой), способ сбора каталога, адрес описания,
      таймаут, доступ (none/basic/bearer/apiKey/cookieForm) с полями по режиму.
- [x] C6. Тесты: `tests/Mars.Datasource.Integration.Tests/RestProviders/` — парсер `.http` (30),
      discovery WordPress (13), `RestRequestBuilder` (23), `RestResponseMapping` (13), провайдер
      с подменными сетью/discovery/хранилищем (23), `DatasourceStoreTests` (8); HTTP-контракт —
      `Drivers_Request_Success` ждёт rest, `TestConnection_RestWithoutDiscovery_Succeeds`,
      `TestConnection_RestWithoutBaseUrl_ReportsMissingAddress`. Живой WordPress —
      `tests/ExternalServices.Integration.Tests/WordPressTests/WordPressDatasourceTests.cs` (8:
      discovery по индексу, сохранение и повторное чтение каталога, чтение постов таблицей,
      параметры в query string, одиночный пост документом, Basic Auth, 401 без доступа,
      создание и удаление поста) — скипнуты константой `SkipTest`, как остальные тесты стенда.
      Стенд починен: WordPress 5.6+ отвечает 401 на Basic Auth пользователя (встроенные Application
      Passwords перехватывают заголовок) — отключены mu-плагином `MountFiles/disable-app-passwords.php`,
      а недоклонированный каталог плагина (один `.git`) фикстура теперь пересоздаёт.

- [x] C7. **Документ запросов правится из UI** (решение пользователя 2026-09-17: «в режиме `.http`
      будет только Monaco и там плашмя все эндпоинты, а левое дерево — просто удобный прыжок к методу»).
      Сервер: `GET api/Datasource/Requests(slug)` и `POST …/SaveRequests` (`RequestsDocumentDto`;
      строка в теле требует content-type, которого у ASP.NET Core нет по умолчанию — 415),
      `DatasourceService.RequestsDocument`/`SaveRequestsDocument` через `IDatasourceStore`
      (после сохранения кэш каталога сбрасывается — дерево показывает новые запросы).
      Каталог отдаёт границы блока: `DatasourceCatalogObject.Line`/`EndLine` (у операций discovery — 0),
      парсер считает их по значимым строкам блока. Фронт: вкладка-документ одна на источник
      (кнопка «.http», текст грузится с сервера только при открытии — дальше это текст редактора,
      чтобы не терять несохранённые правки), клик по объекту дерева переходит к его блоку
      (`CodeEditor2.RevealLinesAsync`), а операции из описания API дописываются заготовкой в конец;
      «Выполнить блок» выполняет запрос под курсором (`CodeEditor2.GetCursorLineAsync`),
      «дубль» копирует блок с новым именем (`posts` → `posts (2)`), «удалить блок» убирает его,
      Ctrl+S сохраняет документ. Операции над текстом — `DocumentText` в `Contracts`
      (блоки, границы, append/duplicate/remove, имя блока) с 18 тестами: ими пользуются и фронт, и тесты.

- [x] C8. **Дерево по методам, форма операции, отказ от авто-выполнения** (правки пользователя 2026-09-17
      после проверки C7). Дерево rest-источника группируется по методу запроса: `RestCatalogTree.ByMethod`
      раскладывает операции discovery и запросы документа в группы `GET/POST/PUT/…` (порядок методов
      фиксирован, остальное — «прочее»), строка получает имя без метода (путь или имя запроса), `Id`
      остаётся `METHOD path` — по нему работают переход к блоку и параметры. Раньше группы были из
      описания API (namespace WordPress, теги OpenAPI). Форма параметров операции снова видна на вкладке
      документа: у неё появилось поле `QueryTab.Operation` (к какой операции перешло дерево) — раньше
      `tab.Object = null` на документе гасил форму, и операцию можно было использовать только текстом.
      Клик по операции ничего не выполняет: объект с `DefaultLanguage = http` никогда не идёт по пути
      «открыть и выполнить» (для sql/файла просмотр объекта по-прежнему выполняется сразу).
      Ошибка загрузки/обновления каталога больше не подменяет всю рабочую область одним alert'ом —
      это полоса над деревом, а при пустом каталоге — alert с кнопкой «Повторить».

### Этап D. Очередь (в этой ветке не делаем, место в модели держим)

GraphQL (introspection; язык `graphql` в бандле есть) · Supabase (по сути rest-kind: PostgREST
отдаёт OpenAPI, пресет auth `apikey` + Bearer) · Firebase (настоящий отдельный kind:
коллекции/документы) · protobuf (подсветка в бандле есть) · кэш (TTL/manual, ключ
`slug+objectId+hash(params)`) · секрет-слой (системный техдолг) · `DatasourceNode` для нод
(параметры операции = входные поля с `ValueKind`, см. `ai/NodesReworkPlan.md`) · AiChat-инструменты
поверх каталога · паковка провайдеров (бывший 6.4).

### Этап E. Два мира: SQL-страница и страница объектов — уточнено 2026-09-18

Ход обсуждения: пользователь сначала сказал «код излишне сложен… разбить, 400–500 строк максимум»,
затем отверг единый словарь: «мне не нравится. Подумаем может просто разделим мир на два? SQL
открывает свой редактор, все остальные свой. В SQL имена остаются. А в другом менять?», и далее —
«давай полностью разные редакторы страницы. У SQL даже дерево таблиц будет отличаться. И таблица
результатов тоже». Итоговые решения: **один роут**, **две страницы**, **`ResultTable` общий**.

Граница — `Kind == sql`. SQL-страница: таблица/колонка/схема, вьюхи, browse-SQL, правка ячеек.
Страница объектов: файл/лист, операция discovery, запрос документа, поле/параметр, форма параметров,
документ `.http`, результат-таблица или документ. Общий компонент с ветвлениями по kind
(`DatabaseQueryWorkspace`) исчезает — вместо него диспетчер и две страницы.

**Подход — перенос и вынос, без переделки вида.** Разметка, CSS-классы и поведение сохраняются:
блоки уезжают в свои страницы/подкомпоненты с `[Parameter]`/`EventCallback`, логика — в классы.
Улучшения вида и замены самописного дерева/грида на `DTreeView`/`FluentDataGrid` — **вне этапа**
(отдельным заходом с проверкой в браузере). Серверная часть (мёртвый код, дубли провайдеров, разрез
`DatasourceService`, базовый ADO-драйвер, объединение парсеров `.http`) — тоже вне этапа E.

Правило размера: компонент целиком (`.razor` + `.razor.cs`) — не больше ~400–500 строк, каждый
новый файл после разреза — ориентировочно ≤250 строк.

**Статус 2026-09-18: E1–E5 сделаны; E7 закрыт частично; E6 не делали.**
Фактическая раскладка (все компоненты ≤500 строк, было 1016+327 / 432 / 409+118):
`Front/QueryPage.razor(.cs)` — диспетчер по `Kind` (в `SelectDatasourceDto` добавлено поле `Kind`);
`Front/Workspaces/QueryWorkspaceBase.cs` — общая машинерия (каталог, вкладки, редактор, запуск
запроса с хуками `AllowsEmptyQuery`/`ConfirmRunAsync`/`AfterResult`); `Front/Workspaces/Sql/
SqlQueryWorkspace.razor(.cs)` (142+340) — дерево схем/таблиц, browse-SQL, «всего N», вьюхи, правка
ячеек; `Front/Workspaces/Objects/ObjectsQueryWorkspace.razor(.cs)` (149+266) — файл и REST, документ
`.http`, форма параметров; `Front/Workspaces/WorkspaceTreeState.cs` — фильтр, свёрнутые группы,
вычисления дерева; `Front/Workspaces/Shared/` — `DatasourceHeader`, `ObjectsTree`,
`ObjectStructurePanel`, `OperationParametersPanel`, `ObjectsHint`; `Front/Components/ResultTable
.razor(.cs)` — рендер строк/ячеек (правка осталась в `QueryResultGrid`); `Components/RestSourceForm`,
`Components/FileSourceForm` + `Services/DatasourceSettingsEditor` (хелперы настроек и подписи).
Отклонения от замысла: вместо `SqlResultGrid`/`RestResultView` — один `ResultTable` + один
`QueryResultGrid` (правка гейтится `Tab.CanEdit`) — меньше копипасты; правила дерева уехали
в `WorkspaceTreeState`, а не в страницы.

Сделано по E7: снесены `IDatasourceService.{Columns,Tables,DatabaseStructure,RefreshStructure}`,
эндпоинты `DatasourceController.{Columns,Tables,DatabaseStructure,RefreshStructure}`, методы клиента,
DTO `QTableResponse`/`QTableColumnResponse`/`QTableSchemaResponse`/`QDatabaseStructureResponse`,
`Mappings/DataSourceMapping`, кэш структуры в `DatasourceService`; `IDatasourceAIToolSchemaProviderHandler`
вычищен от мёртвого кода; AiChat-инструмент схемы убран из `SqlToolset` и `MarsSqlTools` (глушение —
вернуть его на каталог в конце инициативы); `QColumnMapping`/`QColumnKind` → `FieldTypeMapping`/
`FieldKind` (единственная протечка SQL-лексики в общий мир), тест — `FieldTypeMappingTests`.

**Осталось по E7:** `QueryColumn` → `DatasourceField`, `QueryResultDto.Columns` → `.Fields`,
`DatasourceCatalogColumn` → `DatasourceCatalogField`, `QueryResultDto.DatabaseDriver` → `.SourceDriver`
(17 файлов в `src` + `tests`: `Contracts/Models/{QueryResultDto,DatasourceCatalogColumn,
DatasourceCatalogObject,SqlNonQueryResultActionDto,SqlQueryJsonResultActionDto}.cs`,
`Abstractions/Models/{CatalogMapping,QueryResultMapping}.cs`, три sql-драйвера,
`Providers.Rest/{RestDatasourceProvider,RestResponseMapping}.cs`, `Providers.File/FileDatasourceProvider.cs`,
`Front/Components/QueryResultGrid.razor`, тесты `FileDatasourceProviderTests`, `RestDatasourceProviderTests`,
`RestResponseMappingTests`). `IDatasourceDriver.Tables()/Columns()` оставлены сознательно — это
внутренний интерфейс sql-мира, им пользуются тесты движков.

**Не делали (E6):** диалект `SqlDialectMapping.Dialect(source?.Driver)` в SQL-странице вычисляется
в нескольких местах; спиннеры и вывод ошибок в страницах — свои (не переведены на `SharedLoader2`/
`ExceptionMessage`).

Проверки после работы: `dotnet build Mars.slnx` — 0 ошибок; `Mars.Datasource.Integration.Tests` —
455/455. **UI в браузере не проверяли** (sql / file / rest: дерево, вкладки, выполнение, документ
`.http`, форма параметров, правка ячейки) — это следующий шаг.

- [ ] E1. Диспетчер `Front/QueryPage.razor(.cs)` (~120): читает опцию и `Kind`, грузит каталог,
      показывает полосу ошибки и «Повторить» при пустом каталоге, дальше отдаёт работу странице.
      Роут `/datasource/query?slug=` не меняется — точка входа остаётся одна
      (`src/Mars.Admin/Builder/DataSourceViews/DatasourceQueryPage.razor:3` рендерит её).
- [ ] E2. SQL-страница `Front/Workspaces/Sql/` — своё дерево, свой тулбар, свой результат:
      `SqlQueryWorkspace.razor(.cs)` (~180, композиция) · `SqlCatalogTree.razor(.cs)` (~170: схемы →
      таблицы/вьюхи/матвьюхи, PK и размеры, «＋ вьюха», «обновить»; сейчас `.razor:64-125` и
      `LoadAsync/RefreshCatalogAsync/ApplySchemaDefaults/IsSchemaExpanded/ToggleSchema/Filtered/
      Schemas/DisplayName/FindObject`) · `SqlQueryToolbar.razor(.cs)` (~110: «Выполнить», «всего N»,
      AI help, «определение»/«удалить»; `.razor:130-222`) · `SqlResultGrid.razor(.cs)` (~200:
      типы и `QColumnKind`-раскраска, guid-сжатие, правка ячеек и `UPDATE`, «Показать больше»,
      `SqlSafety`-подтверждение) · `SqlTabRunner.cs` (~170: `RunActiveTabAsync:346-436`,
      `RunMoreAsync`, `StartTotalCount`, `CountTotalAsync`, `ParseTotal`, browse-SQL и `COUNT`) ·
      `SqlViewActions.cs` (~110: `CreateViewAsync:810-835`, `ShowViewDefinitionAsync:837-876`,
      `DropViewAsync:878-906`, `ExecuteViewDdlAsync:908-941`). SQL-лексика (таблица/колонка/схема)
      остаётся здесь без переименований; `Q*`-типы не трогаем.
- [ ] E3. Страница объектов `Front/Workspaces/Objects/` — для file/rest/GraphQL/Supabase и прочих
      не-sql kind'ов: `ObjectsQueryWorkspace.razor(.cs)` (~150: композиция, выбор редактора по `Kind`) ·
      `ObjectsCatalogTree.razor(.cs)` (~120: у файла одна группа без имени, у rest — группы по
      HTTP-методам) · `ObjectsTabBar` + `ObjectsToolbar` (~90: «Выполнить блок», «Сохранить», «дубль»,
      «удалить блок», «.http») · `Editors/File/*` (~140: LINQ-редактор, подсказки `Val.*`, выбор листа) ·
      `Editors/Rest/*` (~330 на троих: документ `.http`, `OperationParametersForm` — переносится как
      есть из `.razor:229-268`, переходы по блокам). Логика документа переезжает из
      `DocumentTabActions` (`OpenDocumentAsync:589-630`, `OpenDocumentBlockAsync:632-676`,
      `SaveDocumentAsync:687-711`, `DuplicateDocumentBlockAsync:713-738`, `RemoveDocumentBlockAsync:740-758`,
      `CursorLineAsync`, `OnEditorSaveAsync`).
- [ ] E4. Общий низкий уровень `Front/Shared/` — `ResultTable.razor(.cs)` (~150: строки, ячейки,
      `data-full`, json-переключение, копирование; сейчас разметка `QueryResultGrid.razor:41-110`) ·
      `QueryTabBase.cs` (Title/Loading/Error/Result, от него `SqlTab`/`FileTab`/`RestTab`; общий
      `QueryTab` остаётся только базой) · чистые классы, вынесенные из старого грида: `GuidColumnDetector`
      (guid-детект `:88-134`), `CellEditState` (ручная стейт-машина правки `:159-236`), `QueryResultJson`
      (json-проекция `:339-396`), `RowUpdateSaver` (`SaveAsync:244-295`), `ColumnIndex/GetValue`
      (`:339-356`) — нужны только `SqlResultGrid`/`RestResultView`.
      Попутно (те же грабли, что были в общем компоненте): тройка `_editorReady/_editorNeedsSync/_editorLang`
      (`:61-66`, пишется из 8 мест) — один `SyncEditorAsync`; сброс вкладки в `OpenObjectAsync:551-558`
      заменить на существующий `QueryTab.Reset()`; двойной запуск загрузки (сеттер параметра `:41` +
      `OnInitialized:187`) — один; `record CatalogEntry` (`:108`) и поле `_viewSource` (`:799`) убрать
      из середины класса к полям. `CodeEditor2` пересоздаётся по `@key="EditorLang"` и сбрасывает
      `_editorReady` — при выносе панелей `@ref` редактора не трогать; вкладку передавать параметром,
      а не ссылкой на страницу.
- [ ] E5. `Components/EditDatasourceOptions.razor` (432) — по веткам разметки: `DatasourceKindForm`
      (kind/driver/connectionstring, `:37-83`), `RestSourceForm` (baseUrl, каталог, таймаут, доступы —
      `:84-202`), `FileSourceForm` (файлы, разделитель, заголовки — `:203-227`), `@code:254-433`
      (13 предикатов kind/auth, `RepairUnknownKinds`) — в `.razor.cs`. Попутно: `opt = opt;` (`:353-357`)
      — трюк перерисовки; валидация slug (`:367-376`) дублирована в `PartDataSourceActions.razor:33`;
      ~15 одинаковых пар «label + поле настроек» — вынести в один маленький `SettingField`.
- [ ] E6. Мелкие дубли Front: общий шаблон диалога (`CellValueDialog`, `CreateViewDialog`,
      `SqlPreviewDialog`, `ViewDefinitionDialog` — 4 копии обвязки Header/Body/Footer/`OkAsync`),
      спиннеры 3× → `SharedLoader2`, ошибки → `ExceptionMessage`, подтверждения
      (`ConfirmChangeAsync:1001-1013`, `DropViewAsync:890-899`) → готовый
      `DialogExtensions.MarsDeleteConfirmation`, `SqlDialectMapping.Dialect(source?.Driver)` (6 точек:
      `DatabaseQueryWorkspace.razor.cs:166,780,791,813,882`, `QueryResultGrid.razor.cs:334`) — один
      член/хелпер. Проверено, что всё перечисленное существует в `src/Admin/Mars.Admin.Framework`.
- [ ] E7. Словарь и снос SQL-входов из публичной поверхности (решение пользователя: «снести совсем,
      ничего не оставлять»). Снести: `IDatasourceService.{DatabaseStructure,RefreshStructure,Columns,Tables}`,
      эндпоинты `DatasourceController.{Columns,Tables,DatabaseStructure,RefreshStructure}` (`:40-54,92`),
      методы `IDatasourceServiceClient`/`DatasourceServiceClient` (`:18,27,30-49`),
      `QTableResponse`/`QTableSchemaResponse`/`QTableColumnResponse`/`QDatabaseStructureResponse`,
      `Mappings/DataSourceMapping`, закомментированный хвост `DatasourceService.cs:387-450`; в
      `IDatasourceDriver` убрать `Tables()/Columns()` (`:20-21`) — они нужны только самому sql-драйверу.
      AiChat глушим: `src/Mars.Modules/Mars.AiChat.Host/Tools/MarsSqlTools.cs:64-85` (вызов
      `DatabaseStructure` и обход `Tables/Columns`) снимается, инструмент переедет на каталог в конце
      инициативы (решение 15) — сам модуль AiChat не развиваем.
      Переименовать только «протечки» SQL-лексики в общий мир: `QColumnMapping`/`QColumnKind` →
      `FieldTypeMapping`/`FieldKind` (ими пользуются `FileTypeInference` и `JsonTypeInference`),
      `DatasourceCatalogColumn` → `DatasourceCatalogField`, `QueryColumn` + `QueryResultDto.Columns` →
      `DatasourceField` + `.Fields`, `QueryResultDto.DatabaseDriver` → `SourceDriver`.
      **Не переименовывать:** SQL-словарь внутри `Workspaces/Sql` и серверных провайдеров (`QTable*`,
      `QDatabaseStructure*`, `BrowseSqlBuilder`, `SqlDialectMapping`, `SqlSafety`, `ViewDdlBuilder`,
      `RowUpdateBuilder`, `QTableKind`); `SqlNode` (`TypeId => GetType().FullName!`,
      `src/Mars.Nodes/Mars.Nodes.Core/Node.cs:17` — иначе сохранённые flows дадут `UnknownNode`);
      `DatasourceOption` (ключ опции в БД = `typeof(T).Name`, `OptionService.cs:79,255-256`, и по имени
      класса секреты закрыты в `MarsOptionsTools.cs:23`); имена файлов `requests.http`/`catalog.json`.
      Тесты: `Mars.WebApiClient.Integration.Tests/Tests/Datasources/DataSourceTests.cs` (методы
      зафиксированы `nameof` — снос ломает компиляцию), `Datasource.Integration.Tests/Engines/*`,
      `WordPressDatasourceTests` (правится на `Fields`).
- [ ] E8. Проверка: `dotnet build Mars.slnx` (0/0); точечно — `Mars.Datasource.Integration.Tests`
      по `-namespace …Engines|…FileProviders|…RestProviders` и HTTP-контракт в
      `Mars.WebApiClient.Integration.Tests`; руками — `/datasource/query` на источниках sql/file/rest
      (`Workspaces/Sql`, `Workspaces/Objects`): дерево, вкладки, выполнение, документ `.http`, вьюхи,
      форма параметров, правка ячейки. Автотестов на razor в репо нет (UI проверяется при разработке,
      отдельным прогоном не гоняем); для вынесенной чистой логики (`GuidColumnDetector`,
      `QueryResultJson`) тесты можно добавить. Если правки задели js/css — bump `MarsAppVersion`.

## 6. Проверка

- Сборка: `dotnet build Mars.slnx`.
- Новые юнит-тесты: парсеры (CSV/XLSX/`.http`), normalizer `Kind`, Dynamic LINQ-запрос, хранилище
  документа и каталога на `InMemoryFileStorage`, discovery WordPress и OpenAPI, построитель
  HTTP-запроса и разбор ответа.
- `tests/Mars.Datasource.Integration.Tests` — новые провайдеры (движковые тесты не трогаем).
  Запуск с фильтром: `…\Mars.Datasource.Integration.Tests.exe -namespace …RestProviders`
  (MTP xUnit v3 понимает `-filter "/сборка/namespace/класс/метод"`, `-class`, `-namespace`, `-method`;
  `--filter` и `--treenode-filter` — нет).
- HTTP-контракт новых эндпоинтов: `tests/Mars.WebApiClient.Integration.Tests/Tests/Datasources/DataSourceTests.cs`.
- rest-kind на живом API: `tests/ExternalServices.Integration.Tests/WordPressTests/WordPressDatasourceTests.cs`
  (нужны Docker, интернет, `git`; скипнуты константой `SkipTest` — на время фазы снимать
  `[IntegrationFact(Skip = …)]` у своих тестов, обратно не забывать: обнуление `SkipTest` включает
  и нагрузочный тест на 1000 запросов).
- UI — визуально при разработке фазы (тестов на Razor-компоненты в репо нет), отдельным прогоном не проверять.

## 7. Грабли и инварианты

- **Dynamic LINQ: текст запроса к файлу — предикат `Where`, не цепочка.** `rows.Where("…")` в
  тексте не разбирается: `No generic method 'Where' on type 'System.Linq.Queryable' is compatible…`
  (парсер берёт стандартный `Queryable.Where`, а не строковую перегрузку Dynamic LINQ). При этом
  `rows.OrderBy("…")` и `rows.Select("new (…)")` в цепочке работают. Поэтому сортировка/проекция —
  не текстом запроса, а отдельными средствами (клик по заголовку грида, поля запроса позже).
- **`[DynamicLinqType]` ненадёжен**: дефолтный провайдер типов сканирует уже загруженные сборки и
  кэширует результат на первом разборе, поэтому `Val` из сборки провайдера находится через раз.
  Симптом: вместо «неизвестный тип» — `No property or field 'age' exists in type 'Char'` (имя
  приняли за член строки). Лечится явной регистрацией: `FileQueryConfig` держит свой `ParsingConfig`
  с `DefaultDynamicLinqCustomTypeProvider(config, [typeof(Val)], true)`.
- **Свои типы Dynamic LINQ разбирает с учётом регистра**: `Val.Num(age)` работает, `val.num(age)` —
  нет (и ошибка при этом про другое место). В доках и подсказках писать точный регистр.
- **`Convert.ToInt64(col)` в предикате падает на пустой ячейке** (`FormatException: The input string
  '' was not in a correct format`) — поэтому свои `Val.*`, которые на непарсимом и пустом дают null.
- **Реестр провайдеров: единственный вариант типа источника → драйвер в конфиге игнорируется.**
  У `DatasourceConfig.Driver` дефолт `"psql"`, и он остался бы в настройках file/rest-источника,
  созданных через `new DatasourceConfig()`; строгое сопоставление ломало бы их.
- **Пин `System.IO.Packaging` в csproj файлового провайдера**: ClosedXML тянет уязвимый 6.0.0
  (NU1903), как и в `Mars.Excel.Host`. Пакет `System.Linq.Dynamic.Core` уже был запинен в
  `Directory.Packages.props:119` (лежал без использования).
- **Namespace тестов `…Integration.Tests.File` затеняет `System.IO.File`** во всём проекте
  (`File.WriteAllText` начинает искаться в нашем namespace) — папка и namespace зовутся `FileProviders`.
- **`FluentSelect` без `OptionValue` пишет в значение подписью.** У селекта типа источника стоял
  только `OptionText=@KindLabel`, и в `config.Kind` уезжало «SQL — база данных» вместо `sql`:
  сервер отвечал «Провайдер источников "SQL — база данных" не подключён», а форма прятала драйвер и
  строку подключения (`HasDrivers`/`UsesConnectionString` по несуществующему kind'у). Лечение —
  `OptionValue=@(kind => kind)` + починка неизвестного kind'а при открытии формы (`RepairUnknownKinds`).
- **Большое — в `/data`**, в опциях — только маленькие конфиги и ссылки (инвариант концепта).
  Уточнение 2026-09-16: это про **служебные** тела (документ запросов, каталог discovery). Файлы
  источников туда не копируются — они лежат в медиа или на хосте, где их положил пользователь.
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
- Страница `/datasource/actions` (`DataSourceInfoComponent`) показывает pg-кнопки для источника
  любого типа: сервер отвечает внятной ошибкой «доступно только для PostgreSQL, а источник … — file».
  Скрывать кнопки по kind'у не стали — компонент не знает тип источника, а ошибка безопасна.
- В data-корне у источника только служебные тела — `datasource/<slug>/requests.http` и
  `datasource/<slug>/catalog.json` (`IDatasourceStore`); файлы file-источника лежат в медиа или на
  хосте (`IDatasourceFileSource`), загрузки их через админку нет — путь вводят руками.
- `Tables`/`Columns`/`DatabaseStructure`/`RefreshStructure` остались (их пользуют AiChat-схема и тесты),
  фронт рабочей области ходит только в `Catalog`/`RefreshCatalog`.

Грабли rest-kind (2026-09-17):

- **Пути операций склеиваются с корнем REST API — иначе 404.** Описание API задаёт пути не от адреса
  сайта: индекс WordPress по адресу `/wp-json/wp/v2` отдаёт маршруты вида `/wp/v2/posts`, OpenAPI —
  от корня своего `servers[0]`. Префикс считает `RestRoutePrefix.FromAddress` (адрес минус путь его
  namespace): для WP — `/wp-json`, для `servers: https://api.example.org/v1` — `/v1`. Первая версия
  подставляла путь как есть, и заготовка из дерева (`GET {{baseUrl}}/wp/v2/posts`) давала 404; живой
  тест `Rest_DraftRequestOfCatalogOperationWorks` выполняет теперь именно заготовку из каталога.
  Побочный случай: `RestSourceSettings.Combine` удваивал префикс, если `baseUrl` указан вместе с ним
  (`http://site/wp-json` + `/wp-json/wp/v2/posts`) — общий первый сегмент из пути вырезается.
  **Сохранённые до фикса `catalog.json` содержат старые пути — нужно «обновить» дерево.**
- **Описание параметра бывает и на схеме**: в swagger `description` чаще внутри `schema`, а не у самого
  параметра, поэтому берём `parameter.Description ?? parameter.Schema?.Description`.
- **`Microsoft.OpenApi` 2.x — не тот API, что в 1.x.** Типы живут в `Microsoft.OpenApi` (namespace
  `.Models` удалён), `OpenApiFormat` исчез: чтение — `OpenApiDocument.LoadAsync(stream, format: null,
  settings, ct)` → `ReadResult { Document, Diagnostic }`; `Paths` — `IDictionary<string, IOpenApiPathItem>`,
  `Operations` — `Dictionary<System.Net.Http.HttpMethod, OpenApiOperation>`, `Tags` — `ISet<OpenApiTag>`,
  `parameter.In` — `ParameterLocation?`, `parameter.Required` — `bool`, а обязательность полей тела —
  `requestBody.Schema.Required` (`ISet<string>`), `schema.Type` — `JsonSchemaType?`. Версию пиним в
  `Directory.Packages.props` (2.7.5, приходит транзитивно от Swashbuckle). Состав API проверяли
  рефлексией по DLL из nuget-кэша через pwsh — быстрее, чем угадывать по сборке.
- **`[GeneratedRegex]` требует модификатор доступа**: `static partial Regex X()` без `private` даёт
  CS8796 (ошибка прилетает и из сгенерированного файла).
- **`RestSourceSettings.Combine` обязан работать с не-URI baseUrl**: в заготовке операции адрес —
  переменная `{{baseUrl}}`, и `new Uri("{{baseUrl}}/")` падал `UriFormatException` на ровном месте.
- **`Content-Type` принадлежит телу**: `StringContent` ставит `text/plain`, а повторный
  `TryAddWithoutValidation("Content-Type", …)` по занятому заголовку молча возвращает false —
  mediaType надо передавать в конструктор `StringContent`, а из перебора заголовков его пропускать.
- **JSON по умолчанию экранирует кириллицу** (`\u041f\u0440…`): тело запроса, значения json-колонок
  и сохранённый каталог сериализуем с `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` (`RestJson`).
- **`ConnectionStringTestDto` с `default!` в non-nullable свойствах → HTTP 400**: ASP.NET Core
  считает non-nullable ссылочные свойства обязательными, и проверка rest-подключения без `Driver` и
  `ConnectionString` не доходила до сервиса («One or more validation errors occurred»). Дефолты — `""`.
- **`TestConnection` форсирует discovery** (`IDatasourceDiscoverableProvider.Discover`), иначе
  сохранённый `catalog.json` временного конфига (`test_<kind>`) показывал бы успех при нерабочих настройках.
- **Каталог discovery сохраняется при первом же `Catalog()`**, а не только в `Discover()`: дерево и
  `TestConnection` не должны ходить в сеть на каждое открытие, «обновить» — единственный принудительный
  ре-импорт. Документ `requests.http` при этом перечитывается каждый раз — он данные пользователя.
- **Параметры запроса — это переменные документа**: `{{name}}` и `{name}` в тексте важнее формы,
  а не упомянутые в тексте параметры уходят в query (чтение) или в JSON-тело (запись). Дубль
  исключают проверки «имя упомянуто в тексте» и «ключ уже есть в query».
- **`HttpRequestMessage` освобождает провайдер** (`using`): в тестах запрос надо копировать
  (метод, URL, тело, заголовки) внутри `HttpMessageHandler`, а не читать после выполнения.
- **Стенд WordPress: Basic Auth пользователя не работает из коробки.** WordPress 5.6+ сам разбирает
  `Authorization: Basic` как Application Passwords и отвечает 401, поэтому плагин WP-API/Basic-Auth
  бесполезен, пока встроенный механизм включён — отключаем mu-плагином. Вторая грабля стенда:
  прерванный `git clone` оставляет каталог с одним `.git`, фикстура считает его готовым, и WordPress
  сообщает «The 'basic-auth' plugin could not be found» уже после успешной установки.
- **Ошибка каталога в рабочей области не должна подменять её целиком**: условие `@if (errorMessage…)`
  перед разметкой гасило и дерево, и вкладки, оставляя один alert (пользователь: «исчезает вся
  страница»), а при ошибке «обновить» сообщение вообще не показывалось — каталог-то уже загружен.
  Теперь это полоса над деревом, а пустой каталог показывает alert с «Повторить».
- **`tab.Object = null` на вкладке документа гасил форму параметров операции** — форму пришлось
  привязать к отдельному полю `QueryTab.Operation` (операция, к которой перешло дерево). Держать
  в одном поле «объект каталога» и «объект документа» нельзя: у вкладки документа свой смысл жизни.
- **Клик по объекту каталога не должен выполнять http-запрос**: путь «открыть объект и сразу показать
  данные» годится для таблицы sql и файла, но не для rest-операции. Признак берётся не только из
  `catalog.Kind` (`IsRest`), но и из самого объекта (`DefaultLanguage = http`) — иначе источник
  с неверно записанным `Kind` снова начнёт стрелять запросами по клику.

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

1. ~~Ре-импорт каталога: merge/replace и показ диффа~~ — сделано replace: «обновить» в дереве зовёт
   `Discover` и перезаписывает `catalog.json`, диффа нет. Вернёмся, если появится кастомизация
   операций (свои имена/описания), которую жалко терять при ре-импорте.
2. Концепт «большое в `/data`» — внести в `ai/ProjectStructureGuide.md` отдельной правкой?
3. Выбор файла источника из медиа в форме настроек (пикер вместо ручного ввода пути) — делать ли.
4. Сортировка/проекция для file-kind: раз предикатом `Where` цепочку не выразить (грабли),
   делаем ли клик по заголовку грида серверным `OrderBy` для всех kind'ов.
5. Подсветка `.http` своим monarch — когда (после прототипа).
6. `SqlNode` показывает все источники, включая file и rest: запрос с `Language=sql` к файлу даст
   ошибку разбора предиката, к rest-источнику — ошибку разбора `.http`. Чинится в шаге про ноды
   (решение 15 — ноды пока не трогаем): нужен `Kind` в `SelectDatasourceDto` и фильтр в форме узла.
7. ~~Документ `requests.http` из UI~~ — сделано в C7, доработано в C8 (дерево по методам, форма операции,
   отказ от авто-выполнения). **Открыто:** как должно выглядеть «открытие `.http`-файла» — пользователь
   назвал текущее решение плохим, но что именно менять, ещё не сказал (варианты: клик по операции
   не должен дописывать заготовку в документ; документ не должен быть отдельной вкладкой/кнопкой).
   Также открыт вопрос о месте вставки заготовки операции, которой в документе нет: в конец (сейчас)
   или в позицию курсора. Форму параметров оставляем — решение пользователя: «можно и в редакторе
   писать, или просто формы заполнять».
8. Доступы rest-источника (пароль, apiKey, clientSecret) лежат в опции открытым текстом — как
   connection string sql-источника. До системного секрет-слоя так и остаётся; в сообщениях об ошибках
   и в логах их не выводим, ключ кэша клиентов — отпечаток (SHA256), а не сами настройки.
