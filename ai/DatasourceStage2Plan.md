# План: DataSource, этап 2 — kind'ы источников (file, REST, …)

Продолжение [DatasourceReworkPlan.md](./DatasourceReworkPlan.md) (этапы 1–10 закрыты: контракт результата,
баги, схема БД, JSON, визуал, вьюхи, лимит просмотра, структура модуля). Ветка `ai/datasource-rework-stage2`
(от `ai/datasource-rework`, в master не влита). По закрытии инициативы оба плана схлопываются в один
`ai/DatasourceGuide.md` ([PlanLifecycleGuide.md](./PlanLifecycleGuide.md)).

**Статус 2026-09-18: этапы A, B, C, E и F сделаны.** Проверено: `dotnet build Mars.slnx` — 0 ошибок;
`Mars.Datasource.Tests` 429 (без Docker), `Mars.Datasource.Integration.Tests` 31 (движки + backup),
HTTP-контракт `DataSourceTests` 13, живой WordPress в Docker 9 (скипнуты константой `SkipTest`).
**UI глазами после этапов E/F не проверялся** — это следующий шаг (список в §6 «Проверить в браузере»).
Открыты: очередь kind'ов (этап D), вопросы про `.http`-документ (§9); E6 закрыт, отложенное F
(перегруппировка папок, DEBUG-тест) доделано 2026-09-21 — E и F закрыты.
**2026-09-21: этап G — AiChat поверх каталога — сделан (G1–G5)**: инструменты агента, два скилла,
снос `AiScenario`, page bridge рабочей области (решения 19–24, детали в §5.G); живая проверка
(полигон `aichat send`, браузер) — за пользователем.

## 1. Направление и как устроено сейчас

DataSource — **реестр источников с каталогом операций**, а не SQL-клиент. У каждого `Kind` свой язык
запроса; универсальны четыре вещи: конфиг+доступы, каталог (объекты/операции + схема параметров),
результат (таблица или документ), хранилище больших тел. SQL — один kind, самый развитый. Так же устроено
у Postman/Insomnia, Swagger UI, Power Platform connectors, ToolJet/Retool, Grafana Infinity, n8n:
«один универсальный способ получения данных» — это каталог операций с типизированными параметрами, а не SQL.

**Новый источник = проект-провайдер + профиль.** Ядро, контроллер и фронт читают профиль, а не сравнивают
`Kind`; ветвления по kind'у из общего слоя убраны (этап F).

Карта модуля `src/Mars.Datasource/` (10 проектов):

- **`Mars.Datasource.Contracts`** (NuGet, виден из WASM) — wire-модели по доменным папкам
  (2026-09-21, неймспейсы следуют папкам, кросс-ссылки доменов — через `GlobalUsings.cs`):
  `Config/` — `DatasourceConfig`/`DatasourceOption`, **`DatasourceKindProfile`** (+ `DatasourceFeature`,
  `DatasourceEditorLanguage`, `DatasourceActionDescriptor`), `DatasourceSettingField`, `DatasourceSettings`,
  `RestDiscovery`, `SelectDatasourceDto`; `Catalog/` — `DatasourceCatalog`/`…Object`(+`DatasourceOperation`)/
  `DatasourceField`/`DatasourceOperationParameter`/`DatasourceActionRequest`; `Query/` —
  `DatasourceRequest`/`DatasourceParam` (+ `DatasourceKind`, `DatasourceLanguage`), `QueryResultDto`,
  `DatasourceModifyResult`, `ViewDefinitionResponse`; `Document/` — `DocumentText` (операции над текстом
  `.http`), `HttpBlockSync`, `DocumentDto`; `Ai/` — `DatasourceSchemaText`, `DatasourceParametersJson`;
  `Nodes/` — `SqlNode`; чистые хелперы в `Sql/` — `SqlSafety`, `RestSafety`, `BrowseSqlBuilder`,
  `RowUpdateBuilder`, `ViewDdlBuilder`, `SqlDialect(+Mapping)`, `FieldTypeMapping`, `QueryResultMapping`.
- **`Mars.Datasource.Abstractions`** (NuGet, серверный) — интерфейсы `IDatasourceProvider(+Factory,+Registry)`,
  `IDatasourceDriver(+Factory)`, `IDatasourceBackupDriver`, `IDatasourceFileSource`, `IDatasourceStore`;
  грани сервиса `IDatasourceRegistry` / `IDatasourceService` / `ISqlDatasourceService`; `Sql/` —
  `SqlDatasourceDriverBase` (общий ADO-скелет), `SqlDatasourceProvider(+Profile)`, `AdoResultReader`,
  `BackupSettings`; `Mappings/` — `CatalogMapping`, `QDatabaseStructure*`, `QTable*`.
- **`Mars.Datasource.Host`** — `DatasourceService` (три грани одним singleton'ом), `DatasourceProviderRegistry`,
  `DatasourceController`, `DatasourceStore`, `DatasourceFileSource`, backup, CLI `ds`, `SqlNodeImpl`
  (AI-схема внутренней БД отсюда уехала — G3, схема у агента теперь из каталога).
- **`Mars.Datasource.Providers.PostgreSQL|MsSQL|MySQL`** — драйвер от `SqlDatasourceDriverBase` + фабрика +
  хук `AddDatasource<X>()`; у PG ещё backup-драйвер и `PostgreSqlUsefulQueries` (действия источника).
- **`Mars.Datasource.Providers.File`** — CSV/XLSX, Dynamic LINQ, `FileTypeInference`, `Val.*`.
- **`Mars.Datasource.Providers.Rest`** — discovery (WordPress/OpenAPI), парсер `.http`, executor, маппинг ответа.
- **`Mars.Datasource.Front`** (Razor-библиотека, WASM) — `QueryPage` (диспетчер по `Kind`),
  `Workspaces/QueryWorkspaceBase`, `Workspaces/Shared/WorkspaceShell` + панели, `Workspaces/Sql/`,
  `Workspaces/Objects/`, `Components/` (`QueryResultGrid`, `ResultTable`, диалоги, `EditDatasourceOptions`,
  `DatasourceSettingFields`, `AuthConfigEditor`), свой API-клиент.
- **`Mars.Datasource`** — агрегатор (`AddDatasource`/`UseDatasource`): единственная точка подключения в WebApp.

Поток запроса: конфиг из опции → реестр резолвит провайдера по `(Kind, Driver)` → `Catalog(slug, refresh)`
(кэш 30 с, discovery-каталог сохраняется в data-корень) → профиль в каталоге управляет деревом/редактором/
формой настроек → `Query(slug, DatasourceRequest)` → `QueryResultDto` (`Fields`/`Rows`/`Json`/`Total`/
`Truncated`) → запись через `Modify(slug, request)` → документ пользователя через `Document`/`SaveDocument`.

## 2. Решения

1. **Новый модуль не заводим** — kind'ы наращиваются проектами `Mars.Datasource.Providers.*`.
2. **Провайдер над драйвером**: `IDatasourceProvider` (kind), `IDatasourceDriver` — внутри sql-провайдера.
3. **`Kind` явный**: `sql | file | rest` (+ очередь: graphql, supabase, firebase, protobuf, key-value).
4. **DuckDB / SQL-федерация над файлами — отклонены** («забудь про DuckDB»).
5. **Отдельного редактора для REST нет — у всех Monaco**; HTTP-запросы живут как `.http`-документ
   (синтаксис VS Code REST Client: `###`, `# @name`, `@var`, `{{var}}`).
6. **Один документ на источник**: `data/datasource/<slug>/requests.http`, не коллекции.
7. **Большое — в `/data`**: в опциях только маленькие конфиги и ссылки (концепт на всю систему,
   уточнение — про служебные тела; файлы источников туда не копируются).
8. **Wire-совместимость `Contracts` ломаем осознанно** — ветка не влита в master.
9. **Порядок: file-kind первым** (короткий путь проверить каркас без сети и секретов), потом rest на WordPress.
10. **Запись будет** — с подтверждением по образцу `SqlSafety` (`RestSafety.IsWrite`).
11. **CSV-парсер встроенный** (`Microsoft.VisualBasic.FileIO.TextFieldParser`), XLSX — `ClosedXML`.
12. **Строки file-kind — только Dynamic LINQ**, без `RuntimeTypeCompiler`/генерации типов.
13. **Ноды и ИИ не трогаем**: `SqlNode` как есть, `DatasourceNode` и AiChat-инструменты поверх каталога —
    в конце инициативы (AiChat-инструмент схемы БД из `SqlToolset` убран, вернуть на каталог).
14. **`.http` по стандарту VS Code REST Client**; языка `http` в бандле Monaco нет, поэтому он
    зарегистрирован своим монархом в `MarsCodeEditor2JsInterop.js` (`registerHttpLanguage`, образец —
    TextMate-грамматика humao.restclient) + CodeLens «▶ выполнить» над строкой запроса (команда
    `mars.http.run` → `CodeEditor2.OnRunRequest(line)` → `RunBlockAtLineAsync`). Профиль rest:
    `EditorLanguage = http`.
15. **Два мира в UI** (этап E): один роут `/datasource/query?slug=`, диспетчер по `Kind`, две страницы
    (`Workspaces/Sql` и `Workspaces/Objects`), общий низ (`WorkspaceShell`, `ResultTable`, `QueryResultGrid`).
    SQL-словарь (таблица/колонка/схема, `Q*`, `BrowseSqlBuilder`, `SqlSafety`, `ViewDdlBuilder`) остаётся
    в sql-мире без переименований. Публичные SQL-входы (`DatabaseStructure`/`Tables`/`Columns`/
    `RefreshStructure` + `Q*Response` + `DataSourceMapping`) **снесены**.
    **Не переименовывать:** `SqlNode` (`TypeId` = полное имя типа, иначе сохранённые flows → `UnknownNode`),
    `DatasourceOption` (ключ опции в БД = имя класса, по нему же закрыты секреты в `MarsOptionsTools`),
    имена файлов `requests.http`/`catalog.json`.
16. **Профиль источника — свой тип в `Contracts`**, не на `Mars.Forms`; **доступы общие** — источник хранит
    `AuthConfig` (`Mars.HttpSmartAuthFlow`) одним JSON-значением настройки `auth`; **API-клиент модуля
    остаётся в `.Front`**; **тесты разделены**: юниты — `tests/Mars.Datasource.Tests`, Docker —
    `tests/Mars.Datasource.Integration.Tests`.
17. **Отложено**: кэш запросов, protobuf, секреты (системный техдолг, не задача модуля).
18. **Размер компонента**: `.razor` + `.razor.cs` — не больше ~400–500 строк, новые файлы после разреза ≤250.
19. **AiChat поверх каталога** (этап G, 2026-09-21): форма инструментов (a) — `execute_sql` остаётся
    свободным «harness-инструментом» для sql-источников и `default`, плюс универсальная пара
    `get_source_schema(slug, filter)` и `run_query(slug, objectId?, query?, parametersJson?)` поверх
    `DatasourceRequest`; kind-специфику знает провайдер, не агент.
20. **Схема любого источника включая `default` — через `Catalog(slug)`**; EF-хендлер
    `IDatasourceAIToolSchemaProviderHandler` удаляется (не переносится).
21. **`AiScenario` упразднён — генерация/дописывание SQL только через чат**: кнопки «AI help»
    (`SqlQueryWorkspace`) и AI-кнопка `SqlNodeForm`, `MarsSQLQueryPromptHelper` удаляются,
    `AiScenario` уходит из профиля.
22. **Лимиты агента**: не более 25 строк результата + `Total`, когда провайдер его вернул; текст схемы —
    с бюджетом размера и обязательным фильтром.
23. **Скиллов два**: `mars-sql` (SQL: `default` + sql-источники, безопасность) и `mars-datasource`
    (каталог, file/rest); роутер грузит `mars-sql` при `EnableSqlAccess` как раньше,
    `mars-datasource` — на страницах `/datasource/*`.
24. **Page bridge на странице запросов**: рабочая область реализует `IAiChatPageHandler` — агент видит
    контекст (slug, kind, вкладка) и читает/пишет текст редактора; ссылка `Datasource.Front →
    AiChat.Front` односторонняя, цикла нет.

## 3. Хранение: три артефакта на источник

Опция — это **одна строка JSON на тип** (`OptionService`), и `EditDatasourceOptions` тянет её в WASM целиком,
поэтому большое там хранить нельзя (индекс WP `/wp-json/wp/v2` — 358 КБ).

| Артефакт | Где | Содержимое |
|---|---|---|
| Конфиг | `DatasourceOption` | `Kind`, `Driver`, `Title`, `Slug`, `Disabled`, `Settings`: у rest — `baseUrl`, `discovery`, `discoveryUrl`, `timeoutSec`, `auth`; у file — `files`, `hasHeaders`, `delimiter`. Метаданных каталога в опции нет |
| Документ пользователя | `data/datasource/<slug>/requests.http` | именованные запросы, заготовки, переменные — правится из UI в Monaco |
| Каталог discovery | `data/datasource/<slug>/catalog.json` | группы операций в wire-модели; сырое описание API не сохраняем — пересобрать каталог один запрос |
| Файлы источника (file) | медиа-хранилище или диск хоста | ссылки в `Settings["files"]`, копии в data не делаем |

- Хранилище — keyed `"data"` `IFileStorage` (регистрирует `MainServer.UseFileStorages`, фолбэк —
  `ApplicationPluginExtensions`, в тестах `InMemoryFileStorage`); `Abstractions` его не видит → свой
  `IDatasourceStore`, реализация `DatasourceStore` в `Host`.
- **Документ — данные пользователя, каталог — regenerable**: «обновить» в дереве перезаписывает только
  `catalog.json`, документ не трогает; дифф/merge как в Postman не делали.
- Дерево и форма параметров грузятся эндпоинтом `Catalog(slug, refresh)` — в опцию и в WASM большой JSON
  не попадает никогда.

## 4. Что переиспользуем из репо

- **Доступы**: `Mars.HttpSmartAuthFlow` — `AuthConfig` (Basic / Bearer+OAuth / CookieForm / CookieEndpoint /
  ApiKey), `AuthStrategyFactory`, `AuthFlowHandler`, парсинг логин-формы (тест на форме WordPress).
- **Стенд WordPress**: `tests/ExternalServices.Integration.Tests/WordPressTests/` — Testcontainers
  `wordpress:latest` + `mysql:8.0`, wp-cli, плагин Basic-Auth, 10 постов через Bogus; тесты скипнуты
  константой `SkipTest` (включать как E2E, на время фазы).
- **Discovery WP без OpenAPI**: индекс отдаёт `routes → endpoints[] → args` (type/default/enum/required),
  `X-WP-Total`/`X-WP-TotalPages` ложатся в `QueryResultDto.Total`.
- **Пакеты**: `System.Linq.Dynamic.Core`, `ClosedXML`, `Microsoft.OpenApi` 2.x — уже запинены;
  `Microsoft.CodeAnalysis.CSharp.Scripting` (Roslyn) — если понадобится полноценный C# (кешировать делегаты!).
- **Monaco** (BlazorMonaco): есть `graphql`, `protobuf`, `csharp/sql/json/yaml/ini/plaintext`, **нет `http`**.
- **Реестр «модуль приносит свой UI»**: `INodeFormsLocator`/`IOptionsFormsLocator` + `RegisterAssembly`
  (плагины) — образец, если провайдеры станут плагинами.

## 5. Этапы

### A. Каркас kind'ов — сделано 2026-09-16 (`c0681dae`, `43392f31`, `8040e945`)

`DatasourceConfig.Kind` (+`Normalize()` для старых конфигов) и `Settings`; wire-контракты запроса и каталога;
`IDatasourceProvider` (`Profile/Catalog/Query/Modify`) + фабрики + `DatasourceProviderRegistry` (sql-движки
оборачиваются, остальные — свои фабрики); `TestConnection` = построение каталога (работает для любого kind'а);
`IDatasourceFileSource` (относительная ссылка → медиа, корневая → диск хоста с откатом в медиа);
рабочая область переведена на каталог; форма настроек выбирает kind, фильтрует драйверы и поля
(ключи настроек — в `DatasourceSettings`, чтобы форма и провайдер не расходились).

### B. file-kind — сделано 2026-09-16

CSV встроенным `TextFieldParser` (кавычки, BOM, автоопределение `,`/`;`/tab/`|`, явный разделитель из настроек),
XLSX через `XLWorkbook(stream)` (листы → объекты каталога, значение ячейки инвариантным текстом).
Файлы — ссылки через `;` в `Settings["files"]`; идентификатор объекта — ссылка, у книги `ссылка#Лист`
(разделитель `#`: `:` занят диском Windows-пути). Запрос: `Language=linq`, текст = **предикат `Where`**
(пустой — все строки), свой `ParsingConfig` + `Val.Num/Dec/Str/Date/Flag/Id`; `FileTypeInference` выдаёт
имена типов как у sql (`bigint`/`timestamp`/`uuid`/`text`) — общая подсветка типов работает без отдельных
правил. `MaxSourceRows = 100_000`; **только чтение** (запись добавим позже), в UI правка выключена с причиной
«источник только для чтения». Загрузка файлов — существующая медиа-админка, пикера в форме нет.
Примеры условий (2026-09-21): меню «примеры» в тулбаре у linq-вкладки (`FileQueryExamples` во фронте,
по образцу `HttpDocumentExamples`); пустой редактор — вставка, непустой — дописывание через `&&`
(текст запроса файла одно выражение, замена потеряла бы набранное). Каждый пример исполняется
Dynamic LINQ в юнит-тесте (`FileQueryExamplesTests`, для него тесты ссылаются на `.Front` —
прецедент `Mars.Forms.Tests`).

### C. rest-kind (WordPress) — сделано 2026-09-17 (`eb82a73d`, `a82cc3cd`, `6dbf6afc`)

- Discovery за одним `IRestCatalogDiscovery`: `WordPressRestDiscovery` (индекс `/wp-json/wp/v2`, маршрут-регулярка
  `(?P<id>[\d]+)` → путь `{id}` + параметр path, GET-аргументы → query, остальные → body) и
  `OpenApiRestDiscovery` (`paths → operations`, группы — теги); третий режим `discovery=none` — каталог только
  из документа пользователя.
- `.http`-документ: `HttpDocumentParser` (блоки `###`, `# @name`, переменные `@name = value`, подстановки
  `{{name}}`, системные `$guid/$timestamp/$isoTimestamp/$datetime/$randomInt`; переменных окружения нет
  намеренно — документ приходит из браузера). Выполнение: `RestRequestBuilder` → `RestExecutor` → `HttpClient`
  с `AuthFlowHandler`/`AuthConfig`, клиенты кэширует `RestHttpClientCache` (ключ — SHA256-отпечаток настроек).
  Ответ: массив объектов → колонки и строки, объект → одна строка + документ в `QueryResultDto.Json`,
  не-JSON → ячейка `response`.
- Запись: метод из текста решает всё — `Query` выполняет любой метод, `Modify` отдаёт `DatasourceModifyResult`;
  `RestSafety.IsWrite` → подтверждение «Запрос POST меняет данные источника».
- `X-WP-Total` → `Total` → «строк: N из M»; `page`/`per_page` из схемы параметров операции → форма под редактором.
- UI: дерево **по HTTP-методам** (`RestCatalogTree.ByMethod`, `Id` остаётся `METHOD path`), документ правится
  из UI (вкладка-документ одна на источник, текст грузится с сервера только при открытии — иначе терялись бы
  несохранённые правки), клик по объекту переходит к его блоку (`RevealLinesAsync` по границам из каталога),
  операция из описания API дописывается заготовкой в конец, «Выполнить блок» выполняет запрос под курсором,
  дубль/удаление блока, меню «примеры» (заготовки GET/POST/PUT/DELETE и запрос с переменной —
  `HttpDocumentExamples`, дописываются в конец), Ctrl+S сохраняет. Клик по операции **не выполняет** запрос (признак — и `Kind`, и
  `DefaultLanguage = http`). Форма параметров привязана к отдельному полю `QueryTab.Operation`.
- Сворачивание дерева (2026-09-21): профиль объявляет `DefaultGroup = "GET"` и порог
  `CollapseGroupsAbove = 25` — на каталоге больше порога `WorkspaceTreeState.ApplyDefaults` сворачивает
  все группы методов кроме GET, на маленьком каталоге дерево раскрыто целиком (у sql порог 0 — прежнее
  поведение: не-дефолтная схема свёрнута всегда).
- Форма параметров следует за блоком под курсором (2026-09-21): `HttpBlockSync` в Contracts (чистые
  правила, виден из WASM) — разбор строки запроса блока, матчинг «блок → операция каталога» (метод +
  хвост пути, шаблоны `{id}` совпадают с конкретным сегментом и с `{{var}}`; побеждает самый конкретный;
  объекты без схемы параметров — запросы пользователя — форму не получают), чтение/запись значений
  (query, path, header, body-JSON; пустое значение убирает параметр; не-JSON тело не трогается;
  `{{var}}` живут в тексте как есть). Фронт: `DocumentBlockBinding` + события `CodeEditor2.OnCursorLine`
  (смена строки) и `OnContentChanged` (дебаунс 400 мс в JS) — фокус на другом блоке строго
  перепривязывает форму (нет известной операции — форма закрывается), правка текста мягко (не мигает
  при дописывании адреса); правка поля формы перезаписывает блок через `ReplaceLinesAsync`
  (`executeEdits` — курсор и undo сохраняются); перед выполнением блока форма перепривязывается на него
  (хук `BeforeRunDocumentBlock` —fix утечки параметров чужой операции в запрос). Текст документа —
  источник правды.
- Стенд WordPress починен: Application Passwords (перехватывают Basic Auth → 401) отключены mu-плагином
  `MountFiles/disable-app-passwords.php`, недоклонированный каталог плагина фикстура пересоздаёт.

### D. Очередь (в этой ветке не делаем, место в модели держим)

GraphQL (introspection; язык в бандле есть) · Supabase (по сути rest: PostgREST отдаёт OpenAPI, пресет auth
`apikey` + Bearer) · Firebase (настоящий отдельный kind: коллекции/документы) · protobuf · Google Sheets
(чтение CSV-экспортом, запись — OAuth отдельной фазой) · кэш запросов (TTL/manual, ключ `slug+objectId+hash(params)`) ·
секрет-слой (системный техдолг) · `DatasourceNode` для нод (параметры операции = входные поля с `ValueKind`,
см. `ai/NodesReworkPlan.md`) · AiChat-инструменты поверх каталога · паковка провайдеров и загрузка плагином.

### E. Два мира: SQL-страница и страница объектов — сделано, кроме E6 (`cdee2c50`, `e5a9e93b`)

Повод: пользователь назвал код излишне сложным и отверг единый словарь — «разделим мир на два: SQL открывает
свой редактор, все остальные свой», «у SQL даже дерево таблиц и таблица результатов будут отличаться».
Подход — перенос и вынос без переделки вида (улучшения вида и замены дерева/грида на `DTreeView`/`FluentDataGrid`
вне этапа). Результат: `QueryPage`-диспетчер, `QueryWorkspaceBase` (общая машинерия с хуками
`AllowsEmptyQuery`/`ConfirmRunAsync`/`AfterResult`), `Workspaces/Sql/SqlQueryWorkspace` (дерево схем,
browse-SQL, «всего N», вьюхи, правка ячеек), `Workspaces/Objects/ObjectsQueryWorkspace` (файл и REST, документ
`.http`, форма параметров), `WorkspaceTreeState` (фильтр, свёрнутые группы), `Shared/` (`WorkspaceShell`,
`DatasourceHeader`, `ObjectsTree`, `ObjectStructurePanel`, `OperationParametersPanel`), `Components/ResultTable`.
Отклонение от замысла: вместо `SqlResultGrid`/`RestResultView` — один `ResultTable` + один `QueryResultGrid`
(правка гейтится `Tab.CanEdit`) — меньше копипасты. Снесены публичные SQL-входы (§2.15), переименования
общего мира сделаны в F3a.

**E6 (2026-09-21, частично сделано):** `ResetTabForObject` вынесен в `QueryWorkspaceBase` (страницы дают
только хвост: Sql — browse-SQL, Objects — пустой текст); спиннеры → `SharedLoader1` (страница/панель)
и `SharedLoader2` (дерево, панель результата); подтверждения → `MarsDeleteConfirmation` (удаление вьюхи,
удаление блока `.http` — последнему подтверждение добавлено). **Не делать:** ошибки → `ExceptionMessage`
(пользователь снял: компонент — страничный «Oops» со стектрейсом, для инлайн-`alert` не годится).
Общая обвязка четырёх диалогов (`CellValueDialog`, `CreateViewDialog`, `SqlPreviewDialog`,
`ViewDefinitionDialog`) — **решение 2026-09-21: диалоги остаются как есть** (boilerplate паттерна
`IDialogContentComponent`, новых не предвидится); `ConfirmChangeAsync` тоже оставлен
(свой заголовок «Подтверждение запроса», семантика не удаление). E6 закрыт.

### F. Рефакторинг организации кода — сделано 2026-09-18 (8 коммитов `refactor [datasource]`)

Повод: ревью прототипа перед следующими kind'ами («нет ли велосипедов, именование, не усложнили ли, правильно
ли соединены миры SQL и REST»). Вывод: провайдерская триада + фабрики + реестр правильные, но kind-специфика
осела в общем слое (члены сервиса на kind, SQL-слова в универсальных контрактах, ветки `IsSql`/`IsRest` во фронте,
hardcode подписей kind'ов), поэтому новый источник требовал правок ядра. Пять решений пользователя — §2.16.

- **F1** чистка: снесён мёртвый `IDatasourceDriver.QuoteIdentifier`, `SqlQueryJson` заменён скалярным
  `Query` в `PgDumpBinPath` и удалён вместе с DTO и тестом, вычищены закомментированные хвосты
  (PG-драйвер, ~60 строк в `DatasourceService`, backup-драйвер), `Console.Error` → `ILogger`.
- **F2** `SqlDatasourceDriverBase`: абстрактные `CreateConnection`/`CreateCommand` и тексты каталогов,
  виртуальные `ParameterPrefix`, `ConfigureParameter`, `MapField`, `Actions`/`ExecuteAction`.
  Драйверы: MsSQL 239→56, MySQL 235→52, PostgreSQL 258→104 строки.
- **F3a** единый `DatasourceField` вместо `QueryColumn` + `DatasourceCatalogColumn` (`.Fields`, добавлен
  `Ordinal`), `SqlNonQueryResultActionDto` → `DatasourceModifyResult`, `DatabaseDriver` → `Kind` + `Driver`.
- **F3b** `DatasourceKindProfile` вместо `DatasourceCapabilities` + `DatasourceDriverResponse`: подписи,
  язык запроса и подсветки, тексты (`Hint`, `EmptyRequestMessage`), поведение (`OpensAsDocument`,
  `DocumentName`, `DefaultGroup`, `AiScenario`), `Features` (набор строк) и `Settings` (дескрипторы полей).
  Профиль приходит в `DatasourceCatalog.Profile`; `IsSql`/`IsRest`/`IsHttpObject`/`KindLabel`/`ObjectsHint`
  из фронта удалены, форма настроек рисуется по дескрипторам (`RestSourceForm`/`FileSourceForm` удалены).
- **F4a** `IDatasourceService` разрезан на три грани (один singleton): `IDatasourceRegistry` /
  `IDatasourceService` / `ISqlDatasourceService`; `NonQuery` → `Modify(slug, request)` (язык больше не
  форсится в sql); `RequestsDocument` → общий `Document(slug, name)` с проверкой имени по `Profile.DocumentName`;
  действия источника объявляет провайдер (`IDatasourceActionProvider` + `IDatasourceDriver.Actions`,
  pg-утилиты в `PostgreSqlUsefulQueries`), backup — действие хоста в `DatasourceCatalog.Actions`, кнопки
  `DataSourceInfoComponent` рисуются из каталога; `Catalog` + `RefreshCatalog` — один эндпоинт `Catalog(slug, refresh)`.
- **F4b** чистые хелперы уехали в `Contracts/Sql/`, ADO-часть осталась в Abstractions как `AdoResultReader`;
  **ссылка `Front → Abstractions` снята** (WASM-граница по гайду соблюдена).
- **F5** `Workspaces/Shared/WorkspaceShell.razor` — шапка, сплиттер, дерево, вкладки, тулбар, редактор и
  результат один раз; страницы мира дают четыре фрагмента (`TreeActions`, `ToolbarActions`, `AboveEditor`,
  `Editor`); Sql 145→45 строк, Objects 153→47. Monaco намеренно остался у страницы (`@ref` + `_editorReady`).
- **F6** `tests/Mars.Datasource.Tests` (юниты, 0.7 с, без Docker) отделён от `…Integration.Tests`
  (движки в Docker + backup); `InternalsVisibleTo` на оба.
- **F7** (сверх плана) геометрия блоков `.http` одна: `DocumentText.EndLine` используют и фронт, и парсер
  (тест `Parse_BlockBoundsMatchDocumentText`).

**Отложено — сделано 2026-09-21 (E и F на этом закрыты):** перегруппировка папок — `Contracts`:
`Models/`+`Dto/` → домены `Config/` (конфиг, опция, профиль, настройки, discovery-режимы, DTO выбора),
`Catalog/` (каталог, объекты, поля, параметры операций, действия), `Query/` (запрос, результат, modify,
ответ определения вьюхи), `Document/` (`DocumentText`, `HttpBlockSync`, `DocumentDto`); неймспейсы
следуют за папками, кросс-доменные ссылки внутри библиотек — через `GlobalUsings.cs` (Contracts,
Abstractions); `Abstractions/Models` → `Sql/` (драйвер-база, sql-провайдер/профиль, `AdoResultReader`,
`BackupSettings`) + `Mappings/` (`CatalogMapping`, `Q*`). Потребители по репо обновлены механически
(143 файла, clean renames). DEBUG-only `DriverPostgreSQLTests` с хардкодом локальных строк подключения
(и паролями) — удалён: драйверы покрыты Testcontainers-тестами движков. `ResetTabForObject` — в E6.
~~Переезд `IDatasourceAIToolSchemaProviderHandler`~~ — G3 (удалён).

**Отложено (осталось, нужны глаза или другая инициатива):** Bootstrap-вставки → FluentUI
(`OperationParametersPanel`, ячейка грида, кнопки дерева/тулбара) — делать вместе с проверкой UI;
общий редактор доступов на нодовую форму (`AuthConfigEditor` и `AuthFlowConfigNodeForm` пока свои:
нода правит `AuthFlowConfigNode`, общий компонент требует двустороннего маппинга; естественно делать
при работе над `DatasourceNode`). Цена решения про auth: `.Front` тянет `Mars.HttpSmartAuthFlow`, а с ним
AngleSharp в WASM-пакет (если размер станет критичен — выносить `AuthConfig` в контрактный проект).

### G. AiChat — SQL как harness-инструмент + внешние источники поверх каталога — сделано 2026-09-21 (G1–G5)

Постановка пользователя: агент должен делать SQL-запросы к системе Mars и внешние запросы к подключённым
источникам; SQL — универсальный harness-инструмент модели «делать что угодно», поэтому удобный
(схема под рукой, внятные ошибки, лимиты). Дописывание/генерация SQL — только через чат, в т.ч. в редактор
открытой страницы (решения 19–24). Опорные точки: `MarsSqlTools`/`SqlToolset` (AiChat.Host уже ссылается
на `Datasource.Abstractions`), `DatasourceRequest.ObjectId/Parameters` уже в контракте, `PageSkillRouter`,
скилл `mars-sql`, полигон `aichat send` (AiChatGuide).

- **G1 инструменты — сделано 2026-09-21** (тулсет `SqlToolset` и флаг `EnableSqlAccess` без изменений):
  - маппер каталога `DatasourceSchemaText.Build(catalog, filter, maxChars)` — чистая функция в
    `Contracts/Ai/`: объекты с полями (`*` — ключ), операции с параметрами (in/тип/обязательный/enum/
    default), записывающие операции помечены `(запись)` по `RestSafety.IsWrite`; фильтр по подстроке,
    бюджет 20 КБ с подсказкой сузить фильтр (+6 юнитов `DatasourceSchemaTextTests`);
  - `get_source_schema(slug, filter)` — `Catalog(slug)` (кэш 30 с) → текст маппера;
  - `run_query(slug, objectId?, query?, parametersJson?)` — универсальный вызов file/rest: `ObjectId`
    + `Parameters` из JSON-объекта имя→значение (не-строки — JSON-текстом), язык из
    `Profile.DefaultLanguage` (профиль — по kind+driver из `Providers()`); sql-источники отсекаются
    с направлением в `execute_sql`;
  - `list_data_sources` — добавлены `kind` и `features` из профиля;
  - `execute_sql` — `MaxRows` 50→25; общий `FormatResult(QueryResultDto)`: строки по `Fields`/`Rows`,
    `total` при наличии, ответ документом (`Json`) при пустых строках, заметки об обрезке;
  - запись: подтверждение `ask_user` — правило скилла (серверная сторона не меняется; `Modify` уже общий).
  Проверено: сборка AiChat.Host + Contracts, `Mars.Datasource.Tests` 470 зелёных. Полигон `aichat send`
  и UI — на совести G5.
- **G2 скиллы — сделано 2026-09-21**: bundled `mars-sql` переписан (схема только через
  `get_source_schema`, LIMIT 25 + `total`, диалект по драйверу, отсылка к `mars-datasource` для file/rest);
  новый `mars-datasource` (каталог/операции, предикаты file с `Val.*` и грабли цепочек, rest-параметры
  и пагинация по `total`, запись rest только после `ask_user`, file только чтение). `PageSkillRouter`:
  сегмент `datasource` в URL страницы → preload `mars-datasource` (+ `mars-sql` при `EnableSqlAccess` как сейчас).
- **G3 снос старого AI-сценария — сделано 2026-09-21** (решение 21): кнопки «AI help» в
  `SqlQueryWorkspace.razor` и AI-кнопка в `SqlNodeForm.razor` (+ инъекция `IAIToolAppService`),
  `AiScenario`/`_aiTool` в `QueryWorkspaceBase`, `AiScenario` в `DatasourceKindProfile` и
  `AiScenarioName`/присваивание в `SqlDatasourceProfile`, `MarsSQLQueryPromptHelper`,
  `IDatasourceAIToolSchemaProviderHandler` + регистрация в `MainDatasource` удалены; снята ссылка
  `Datasource.Host → Mars.Data` (EF-модель была нужна только хендлеру схемы) — закрывает отложенный
  пункт F «перенос хендлера в AiChat/Cms» (удалён, а не перенесён). Сборка решения + 470 юнитов зелёные.
- **G4 page bridge — сделано 2026-09-21**: ссылка `Datasource.Front → Mars.AiChat.Front`;
  `IAiChatPageHandler` реализует не `QueryPage`, а сама рабочая область — `QueryWorkspaceBase` сделан
  partial, мост в отдельном файле `Workspaces/QueryWorkspaceAiChat.cs` (база уже ~470 строк, лимит §2.18):
  `GetInfo` — slug, kind, язык, вкладки и активная (объект/документ, строки, total, ошибка),
  `GetFields`/`SetField("editor")` — текст редактора (`SetValue` только при `_editorReady`, иначе
  `_editorNeedsSync` — грабли §7), `Save` — `RunActiveTabAsync` (подтверждения опасного SQL/записи rest
  остаются на месте) с ответом «строк: N, total»; регистрация в `AiChatPageHandlerHolder.Current`
  в `OnAfterRenderAsync(firstRender)`, снятие в `Dispose` (паттерн `EditPostView`). Сборка решения зелёная.
- **G5 проверка — юниты сделаны 2026-09-21**: парсинг `parametersJson` вынесен в чистый хелпер
  `DatasourceParametersJson` (`Contracts/Ai/`, +5 юнитов `DatasourceParametersJsonTests`), сборка решения
  и `Mars.Datasource.Tests` 479 зелёных. **Осталось пользователю**: headless-полигон `aichat send`
  (схема+select на `default`, предикат на file, операция с параметрами на rest — тратит токены и может
  попасть в живой инстанс, без команды не запускать) и page bridge визуально в браузере.

## 6. Проверка

- Сборка: `dotnet build Mars.slnx`.
- Юниты (без Docker, 0.7 с): `dotnet build tests/Mars.Datasource.Tests` →
  `tests\Mars.Datasource.Tests\bin\Debug\net10.0\Mars.Datasource.Tests.exe`.
- Движки и backup (Testcontainers, нужен Docker): `…\tests\Mars.Datasource.Integration.Tests\bin\Debug\net10.0\
  Mars.Datasource.Integration.Tests.exe`, фильтр по MTP: `-namespace`, `-class`, `-method`
  или `-filter "/сборка/namespace/класс/метод"` (`--filter` и `--treenode-filter` не работают).
- HTTP-контракт: `tests/Mars.WebApiClient.Integration.Tests/Tests/Datasources/DataSourceTests.cs`
  (методы зафиксированы `nameof` — снос эндпоинта ломает компиляцию).
- rest на живом API: `tests/ExternalServices.Integration.Tests/WordPressTests/WordPressDatasourceTests.cs`
  (нужны Docker, интернет, `git`; скипнуты `SkipTest` — обнулять только на время фазы и возвращать обратно:
  константа включает и нагрузочный тест на 1000 запросов).
- UI — визуально при разработке (тестов на Razor-компоненты в репо нет), отдельным прогоном не проверять.
- Если правки задели js/css — bump `MarsAppVersion` в `Directory.Build.props`.

**Проверить в браузере (после E/F менялись форма настроек и обе страницы запросов):**

- `/datasource/config`: подписи типов источника и драйверов теперь из профилей («PostgreSQL» вместо `psql`),
  поля file/rest рисуются по дескрипторам, доступы — `AuthConfigEditor`, «Проверить подключение».
- `/datasource/query` на sql: дерево по схемам, «＋ вьюха», AI help, определение/удаление вьюхи, правка
  ячейки и сохранение через показ SQL, «показать больше».
- на file: открытие файла, предикат Dynamic LINQ, подсказка из профиля, «источник только для чтения».
- на rest: дерево по методам, переход к блоку документа (границы строк!), «Выполнить блок», форма параметров
  операции, сохранение/дубль/удаление блока, подтверждение записи, «строк: N из M».
- `/datasource/actions`: кнопки из каталога действий (у sql — pg-утилиты и backup).

## 7. Грабли

Общие:

- **Большое — в `/data`**, в опциях только маленькие конфиги и ссылки; опция = один JSON-ряд на тип, любое
  разрастание `DatasourceConfig` бьёт по каждому сохранению настроек и по загрузке админки.
- `IFileStorage`: пути относительные, разделитель `/`, абсолютные и выход за корень запрещены; keyed
  `"data"` регистрирует `MainServer`, в хостах без него нужен фолбэк, в тестах `InMemoryFileStorage`.
- В data-корне у источника только служебные тела (`requests.http`, `catalog.json`); файлы file-источника
  лежат в медиа или на хосте, загрузки через админку нет — путь вводят руками.
- WASM-админка **не может** ссылаться на агрегатор `Mars.Datasource` (`NETSDK1082`: `FrameworkReference
  Microsoft.AspNetCore.App` vs `browser-wasm`) — только `Contracts` + `Front`.
- `SqlDialectMapping.Dialect` для незнакомого драйвера молча возвращает `Postgres` — новый sql-драйвер с другим
  диалектом получит `"…"`/`LIMIT` без предупреждения.
- Connection string и токены не логировать и не выводить в ошибках; AiChat их читать не даёт
  (`MarsOptionsTools` `ReadDenied`) — не сломать.
- `CodeEditor2`: дефолтный `ContainerCssStyle` = `80vh` (при встраивании — `height:100%`), `SetValue/GetValue`
  до создания JS-редактора падают, пересоздание по `@key` сбрасывает `_editorReady`, события изменения нет.
- C# 14: `field` — контекстное ключевое слово внутри аксессоров; `Fields.Select(field => …)` в
  `QueryResultDto.Data` не компилируется (лямбда переименована в `item`).
- Профиль — singleton на фабрику и попадает во все каталоги: мутировать нельзя; действия хоста (backup)
  добавляются в `DatasourceCatalog.Actions`, а не в профиль.
- Старые rest-настройки с плоскими `authMode`/`authUsername`/… после F3b теряют доступы (настройка `auth` пуста) —
  миграцию не писали (ветка не влита), источник пересохраняется в форме.

file-kind / Dynamic LINQ:

- **Текст запроса к файлу — предикат `Where`, не цепочка**: `rows.Where("…")` не разбирается
  (`No generic method 'Where' … compatible`), а `rows.OrderBy("…")`/`rows.Select("new (…)")` в цепочке
  работают. Сортировка/проекция — не текстом, а отдельными средствами.
- **`[DynamicLinqType]` ненадёжен** (провайдер типов кэшируется на первом разборе) — лечится явной
  регистрацией: `FileQueryConfig` держит свой `ParsingConfig` с `DefaultDynamicLinqCustomTypeProvider(config,
  [typeof(Val)], true)`. Симптом: `No property or field 'age' exists in type 'Char'`.
- Свои типы Dynamic LINQ разбирает **с учётом регистра**: `Val.Num(age)` работает, `val.num(age)` — нет.
- `Convert.ToInt64(col)` в предикате падает на пустой ячейке → свои `Val.*`, дающие null на непарсимом.
- Реестр провайдеров: **единственный вариант типа источника → `Driver` в конфиге игнорируется** (дефолт
  `"psql"` остался бы в настройках file/rest, созданных через `new DatasourceConfig()`).
- Пин `System.IO.Packaging` в csproj файлового провайдера: ClosedXML тянет уязвимый 6.0.0 (NU1903).
- Namespace тестов `…Integration.Tests.File` затеняет `System.IO.File` во всём проекте — папка зовётся
  `FileProviders`. ClosedXML грузит книгу целиком в память → лимит строк/размера для XLSX.

rest-kind:

- **Пути операций склеиваются с корнем REST API — иначе 404**: описание API задаёт пути не от адреса сайта
  (индекс WP по адресу `/wp-json/wp/v2` отдаёт `/wp/v2/posts`). Префикс считает `RestRoutePrefix.FromAddress`;
  `RestSourceSettings.Combine` не удваивает общий сегмент, если `baseUrl` задан вместе с префиксом.
  **Сохранённые до фикса `catalog.json` содержат старые пути — нужно «обновить» дерево.**
- Описание параметра в swagger чаще внутри `schema` → берём `parameter.Description ?? parameter.Schema?.Description`.
- **`Microsoft.OpenApi` 2.x — не тот API, что 1.x**: типы в `Microsoft.OpenApi` (namespace `.Models` удалён),
  `OpenApiFormat` исчез, чтение — `OpenApiDocument.LoadAsync(stream, format: null, settings, ct)` →
  `ReadResult { Document, Diagnostic }`, `Paths` — `IDictionary<string, IOpenApiPathItem>`, `Operations` —
  `Dictionary<System.Net.Http.HttpMethod, OpenApiOperation>`, обязательность полей тела —
  `requestBody.Schema.Required` (`ISet<string>`), `schema.Type` — `JsonSchemaType?`. Состав API проверяли
  рефлексией по DLL из nuget-кэша через pwsh.
- `RestSourceSettings.Combine` обязан работать с не-URI `baseUrl` (`{{baseUrl}}` → `UriFormatException`).
- **`Content-Type` принадлежит телу**: `StringContent` ставит `text/plain`, а повторный
  `TryAddWithoutValidation` по занятому заголовку молча возвращает false → mediaType передавать в конструктор
  `StringContent` и пропускать в переборе заголовков.
- **JSON по умолчанию экранирует кириллицу** → сериализуем с `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` (`RestJson`).
- `ConnectionStringTestDto` с `default!` в non-nullable свойствах давал HTTP 400 (ASP.NET Core считает их
  обязательными) — дефолты `""`.
- **`TestConnection` форсирует discovery**, иначе сохранённый каталог временного конфига показывал бы успех
  при нерабочих настройках. **Каталог discovery сохраняется при первом же `Catalog()`** — дерево и проверка
  подключения не должны ходить в сеть на каждое открытие; документ `requests.http` перечитывается всегда.
- **Параметры запроса — это переменные документа**: `{{name}}`/`{name}` в тексте важнее формы, не упомянутые
  уходят в query (чтение) или в JSON-тело (запись).
- **На сервер блок уходит отдельным текстом, а не всем документом**: переменные из шапки файла
  (`document.Variables` серверного парсера) до него не доезжают — глобальные переменные видел только
  первый блок. Фронт подставляет строки объявлений в начало через `DocumentText.DocumentVariables`
  (до первого `###` и до первой строки запроса — та же граница, что у серверного парсера).
- `HttpRequestMessage` освобождает провайдер (`using`) — в тестах копировать запрос внутри `HttpMessageHandler`.
- **Стенд WordPress**: Basic Auth пользователя не работает из коробки (WP 5.6+ разбирает его как Application
  Passwords → 401, отключаем mu-плагином); прерванный `git clone` оставляет каталог с одним `.git`, и фикстура
  считает его готовым.
- **Ошибка каталога не должна подменять рабочую область целиком** (пользователь: «исчезает вся страница») —
  полоса над деревом, а при пустом каталоге alert с «Повторить».
- **`tab.Object = null` на вкладке документа гасил форму параметров** — операция живёт в `QueryTab.Operation`;
  держать «объект каталога» и «объект документа» в одном поле нельзя.
- `DocumentText.Blocks` не считает блоком текст до первого `###` (там переменные документа) — но если в
  преамбуле есть запрос (первый запрос `.http` вправе идти без `###`), она становится первым блоком:
  иначе фронт не находил блок под курсором и дублировал первый запрос в конец. Правило границ (`EndLine`)
  общее с парсером.

фронт:

- **Scoped-CSS страницы не дотягивается до внутренностей `WorkspaceShell`**: CSS-изоляция Blazor даёт
  родительский b-атрибут только корневому элементу дочернего компонента, поэтому правила `.ds-editor`/
  `.ds-result`/`.ds-tabs` в `Sql|ObjectsQueryWorkspace.razor.css` молча не применялись к элементам оболочки
  (редактор без ограничения высоты, результат наезжал сверху). Все `.ds-*` стили оболочки живут в
  `WorkspaceShell.razor.css`, у страниц мира своих CSS больше нет.
- **CodeLens в standalone-Monaco**: `editor.addAction` регистрирует команду в глобальном реестре
  с префиксом экземпляра (`<editorId>:<actionId>`, видно в минифицированном бандле BlazorMonaco 3.5.0),
  а lens выполняет команду по голому id → «command not found». Провайдер обязан собирать id как
  `editor.getId() + ':' + actionId` (редактор ищется по `monaco.editor.getEditors()` за модели).
  Вторая грабля монарха: `@word` внутри регекса — ссылка на атрибут определения языка, литеральный
  `@` писать классом `[@]` (иначе «language definition does not contain attribute 'name'»).
- **Редактор и результат разделены вертикальным `FluentSplitter`** (по умолчанию 50/50, `Panel1Size="50%"`,
  min 60px на панель). Грабли: `split-panels` — grid-хост, высоту имеют только slot-элементы в рядах,
  а слотнутые `div[slot="1"/"2"]` остаются `height:auto` и растягиваются контентом за экран — в
  `WorkspaceShell.razor.css` им заданы `height:100%; min-height:0; overflow:hidden` через `::deep`.
- **`FluentSelect` без `OptionValue` пишет в значение подписью**: в `config.Kind` уезжало «SQL — база данных»
  вместо `sql`. Лечение — `OptionValue=@(kind => kind)` + починка неизвестного kind'а при открытии формы.
- `DatasourceSettingsEditor`: пустое значение настройки удаляется (`SetSetting`) и означает «по умолчанию»;
  флажок (`Flag`) — снят только при `"false"`, пустое значение считается отмеченным (провайдер читает так же:
  `FileSourceSettings.HasHeaders`); битый JSON в настройке `auth` читается как «без доступа».
- Enum `AuthMode` в JSON настроек пишется строкой: `JsonStringEnumConverter` нужен **и во фронте, и в провайдере**,
  иначе доступ молча становится «без доступа».
- Страница `/datasource/actions` показывает кнопки из каталога действий; раньше pg-кнопки рисовались для
  источника любого типа (сервер отвечал внятной ошибкой — безопасно, но шумно).
- **`@for` захватывает переменную цикла в лямбду события jedną на все итерации**: к моменту клика
  `rowIndex == Rows.Length`, и `@ondblclick` правки ячейки отправлял несуществующую строку (инпут не
  рендерился, старый `FocusAsync` падал на пустой ссылке). В лямбдах событий — только локальные
  переменные тела цикла (`cell`, `column`), не `rowIndex`/`i`. `foreach` не подвержен (своя переменная
  на итерацию).
- **Общий `@ref` на условно рендеримый элемент очищается в default при удалении элемента** (Blazor
  сбрасывает поле в том же дифф-проходе, порядок вставка/удаление не гарантирован) — `FocusAsync` на
  пустой `ElementReference` в WASM даёт «ElementReference has not been configured correctly». Инпут
  правки ячейки поэтому отдельный компонент `CellEditInput`: фокусирует свой `@ref` на своём первом
  рендере, чужой дифф его не сбросит.

## 8. Отклонено

- **DuckDB / SQL-федерация над файлами** — нативная зависимость в Docker-образе, оффлайн-загрузка расширений,
  нет write-back. Альтернатива: file-kind с Dynamic LINQ.
- **Отдельный модуль для не-SQL источников** — потребители в `Mars.Datasource`, иначе дубль UI.
- **Отдельный REST-редактор вместо Monaco** — у всех Monaco, REST живёт как `.http`-документ; форма
  параметров — дополнение, а не замена редактора.
- **Своя таблица в БД под каталоги** — EF-сущность + конфигурации на четыре провайдера + миграции ради
  прототипа; вернёмся, если понадобится индекс/поиск по каталогам (тела останутся файлами).
- **Свой формат запросов вместо `.http`** — держимся синтаксиса VS Code REST Client, чтобы файл открывался
  в VS/VS Code/Postman и обратно.
- **Свой мини-язык фильтров** для файла — вместо него Dynamic LINQ (уже запинен).
- **Единый словарь для SQL и объектов** (таблица/объект, колонка/поле) — пользователь отверг; миры разделены.
- **Ручное/AI-заполнение каталога rest** — его заменяет документ пользователя.

## 9. Открытые вопросы

1. **Как должно выглядеть «открытие `.http`-файла»**: пользователь назвал текущее решение плохим, но что
   именно менять — не сказал. Кнопка «весь requests.http» из дерева убрана (2026-09-20) — документ
   открывается кликом по операции. Осталось решить: клик по операции не должен дописывать заготовку в
   документ; куда вставлять заготовку операции, которой в документе нет — в конец (сейчас) или в позицию
   курсора. Форму параметров оставляем.
2. **Запись file-источника** — решение «перезаписи пока не будет, добавим дальше»; как именно (правка ячейки
   с генерацией файла? только добавление строк?) — не обсуждали.
3. Выбор файла источника из медиа в форме настроек (пикер вместо ручного ввода пути).
4. Сортировка/проекция для file-kind: предикатом `Where` цепочку не выразить (грабли) — делаем ли клик по
   заголовку грида серверным `OrderBy` для всех kind'ов.
5. ~~Подсветка `.http` своим monarch — когда.~~ Сделано 2026-09-20 (решение 14): монарх + CodeLens
   «выполнить» над запросом. Вложения тел (JSON/XML как в rest-client через nextEmbedded) — не делали.
6. `SqlNode` показывает все источники, включая file и rest: запрос с `Language=sql` к файлу даст ошибку разбора
   предиката, к rest — ошибку разбора `.http`. Нужен фильтр по `Kind` в форме узла (решение 13 — ноды в конце).
7. Концепт «большое в `/data`» — закрепить в `ai/ProjectStructureGuide.md` отдельной правкой?
8. Доступы rest-источника лежат в опции открытым текстом (как connection string sql) — до системного
   секрет-слоя; в логах и ошибках не выводим, ключ кэша клиентов — отпечаток (SHA256).
9. ~~AiChat-инструмент схемы БД убран из `SqlToolset` — вернуть на каталог в конце инициативы.~~
   Запланировано 2026-09-21: этап G (решения 19–24) — схема через `Catalog(slug)`, инструменты поверх
   каталога, `AiScenario` снесён, всё через чат.
10. Из плана этапа 1 не сделаны и не перенесены в очередь D: SQL-автокомплит по схеме (свой completion
    provider Monaco), сохранённые запросы/история/состояние вкладок, отмена запроса из UI
    (`CancellationToken` в методах клиента), `CREATE` в списке опасных операторов `SqlSafety`.
