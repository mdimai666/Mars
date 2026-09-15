# План: реворк DataSource — рабочий инструмент источника данных

Исходные материалы: [Prompts/FinalizeDatasourcePrompt.md](./Prompts/FinalizeDatasourcePrompt.md)
(постановка: ревизия → добить фичи → визуал → остальные хотелки), ревизия кода 2026-09-15 (разделы 2–4
ниже). Ветка — `ai/datasource-rework`. Гайда `ai/DatasourceGuide.md` пока нет: по закрытии инициативы
схлопнуть этот план в него ([PlanLifecycleGuide.md](./PlanLifecycleGuide.md)).

Статус на 2026-09-15: **ревизия сделана, работы не начаты.** Этапы пронумерованы в порядке выполнения.

## 1. Принятые решения

1. **Порядок: сначала контракт результата + починка багов, потом визуал** (решение пользователя
   2026-09-15). Новый UI на текущем `string[][]`-контракте не делаем — иначе придётся переделывать дважды.
2. **Правка ячейки: генерировать `UPDATE`, показывать SQL, применять по кнопке** (стиль phpMyAdmin/DataGrip).
   Автоприменение по Enter/blur (Airtable-стиль) отклонено — опасно на чужих БД.
3. **Многоячейковый ноутбук (Jupyter-стиль) отложен.** Если делать — явные ячейки + общее соединение на
   вкладку (чтобы `BEGIN` и temp-таблицы жили между блоками). Это же решение фиксирует, что
   `string[] _sql` в `DatasourceController` пока остаётся с `_sql[0]`.
4. **Приоритетные не-SQL источники: CSV/Excel-файл, Google Sheets/облачные таблицы, HTTP/REST как таблица.**
   Внутренние «таблицы» Mars (Docker, опции, медиа) — не приоритет.
5. **Дефолты, принятые без возражений** (меняются легко, но менять их надо явно):
   - «пагинация» = серверный `MaxRows` + кнопка «показать больше», без OFFSET-обёртки над произвольным SQL;
   - правка ячеек **выключена** для выборок без PK; никаких `ctid`/`rowid`;
   - подтверждение для `DROP/TRUNCATE/ALTER/DELETE без WHERE`; отдельного флага «только чтение» нет —
     редактор и сейчас принимает любой SQL (проверки первого слова нет, в отличие от AiChat).
6. Вне этого плана: ИИ-фичи, привязка метаполей к SQL, мультиязычие, «визуальные ноды вместо IDE»,
   редактирование sequences/ролей.

## 2. As-is: что есть сейчас

- **Модуль** `src/Mars.Datasource/` — 6 проектов: `Mars.Datasource.Abstractions` (контракты; единственный
  пакет в NuGet), `Mars.Datasource` (узел `SqlNode`), `Mars.Datasource.Front` (Razor-библиотека,
  `DatabaseQueryWorkspace.razor` — вся нынешняя рабочая область), `Mars.Datasource.Host` (singleton
  `DatasourceService`, `DatasourceController`, CLI, `SqlNodeImpl`, AI-провайдер схемы),
  `Mars.Datasource.Host.PostgreSQL|MsSQL|MySQL` (драйверы).
- **Регистрация:** только `Mars.WebApp` (`MarsWebAppStartup.cs:106,215`), фронт — в WASM-админке
  (`src/Mars.Admin/Program.cs:71,95`); `Mars.Admin.Host` DataSource не знает.
- **UI:** 4 страницы в `src/Mars.Admin/Builder/DataSourceViews/` (base href `/dev/`): `/datasource`
  (карточки), `/datasource/query`, `/datasource/config`, `/datasource/actions`. В меню — один пункт
  `src/Mars.Admin/Builder/BuilderASide.razor:19`.
- **Конфиги:** опция `DatasourceOption { List<DatasourceConfig> }`; `default` — синтетический slug из
  `ConnectionStrings:DefaultConnection` (`DatasourceService.ResolveEngine`).
- **Работает:** список таблиц + колонки из `DatabaseStructure`, произвольный SQL через `CodeEditor2`,
  7 pg-«полезных» запросов, backup/restore, CLI `ds backup|restore`, узел `SqlNode`
  (Static/Payload → `string[][]`), AI-агент в AiChat (список баз, схема, execute, включая `SqlNonQuery`).
- **Потребители, которые нельзя сломать:** `Mars.Datasource.Host/Nodes/SqlNodeImpl.cs` (payload =
  `result.Data`) и `Mars.Modules/Mars.AiChat.Host/Tools/MarsSqlTools.cs` (`FormatRows(string[][])`,
  `SqlNonQuery`). Оба вне дерева и оба ходят в `IDatasourceService`, не в HTTP.

## 3. Живые баги (входят в Этап 2)

- `src/Mars.Datasource/Mars.Datasource.Front/Components/PartDataSourceActions.razor:17` — хардкод
  `href="/dev/datasource/query?slug=mssql"`; у любой другой конфигурации чужой slug →
  `ArgumentNullException` из `ResolveEngine` (`DatasourceService.cs:83`), а `Load()` — `async void` без
  try/catch → падение вместо сообщения.
- Относительные ссылки `href="datasource/config"` (`DatabaseQueryWorkspace.razor:41`,
  `Nodes/EditForms/SqlNodeForm.razor:38`) → с `/dev/datasource/query` дают
  `/dev/datasource/datasource/config`.
- `DatasourceService.ExecuteAction` игнорирует свой `slug`: pg-запросы всегда к `"default"`
  (`DatasourceService.cs:170`), backup — к `_defaultConfig` (`:197`). При этом `DatasourceController`
  и клиент принимают `slug`.
- Slug не валидируется: дубли падают поздно в `ToDictionary` (`DatasourceService.cs:53`), конфиг со slug
  `default` затеняется (`:72-75`), сравнение регистрозависимое.
- Клиент теряет `DatabaseDriver`: `SqlQuery` десериализует в `UserActionResult<string[][]>`
  (`DatasourceServiceClient.cs:41`), хотя серверный DTO его несёт.
- Опция регистрируется без `onChangeHook` (`MainDatasource.cs:35`) → кэш конфигов живёт на сравнении
  ссылок (`DatasourceService.cs:59-70`), `InvalidateLocalDictCache` не вызывается ниоткуда.
- `async void` на всех обработчиках + `StateHasChanged` из `Task.Run` (`DatabaseQueryWorkspace.razor.cs`,
  `DataSourceInfoComponent.razor`, `PartDataSourceActions.razor`, `SqlNodeForm.razor`).
- Мёртвый код: `tests/Mars.Datasource.Integration.Tests/Engines/EnginesTests.cs` — ctor кидает
  `NotImplementedException` и держит устаревшие hardcoded-строки подключения.
- Дубль имени: `WebApiClientDatasourceClientExtensions` есть и в `Mars.Datasource.Front`, и в
  `Mars.Docker.Front` → CS0104 у потребителя с обоими using.
- Тестов на фронт, страницы и CLI нет вообще; `DataSourceTests.TestConnection` проверяет только «не кинуло».

## 4. Разрывы под хотелки

- **G1. Контракт `string[][]` с заголовком в строке 0** (`SqlQueryResultActionDto.Data`, заполняется в
  каждом драйвере как `rows[0] = _cols`). Нет типов, NULL неотличим от `""`, нет идентичности строки —
  значит нет ни правки ячейки, ни валидации значения, ни пагинации. Всё остальное разбивается об это.
- **G2. Нет пути для non-query.** `SqlNonQuery` реализован во всех трёх драйверах и в сервисе
  (`IDatasourceService.cs:16`), используется AiChat (`MarsSqlTools.cs:132`), но в
  `DatasourceController` метода нет и в клиенте тоже — UI физически не может ничего записать.
- **G3. Нет параметров.** `SqlQuery(string sql)` без параметров → сгенерировать
  `UPDATE ... WHERE pk=@p` нельзя. Плюс имена подставляются интерполяцией: MySQL
  `WHERE table_schema = '{database}'`, чтение колонок `SELECT * FROM "{tableName}"`; schema-qualified
  имён нет нигде.
- **G4. Нет лимита, отмены, таймаута.** `SELECT *` на большой таблице материализуется целиком в
  `string[][]`, а потом в столько же `<td>` (грид без пагинации). Свой cap есть только в AiChat
  (50 строк / 30k символов).
- **G5. JSON-режим полупостроен.** `SqlQueryJson` + `SqlQueryJsonResultActionDto`
  (`Abstractions/Models/SqlQueryResultActionDto.cs:15`) есть только в постгресовом драйвере
  (`DatasourcePostgreSQLDriver.cs:211`), в `IDatasourceDriver` не входит, эндпоинта нет, в MsSQL/MySQL
  нет; фактически используются backup-драйвером и одним тестом.
- **G6. Схема собирается N+1 соединениями:** `DatabaseStructure()` открывает соединение на каждую
  таблицу, а `Columns()` ходит `SELECT * FROM table` (полная выборка ради схемы). Вызывается на каждое
  открытие рабочей области, кэша нет.
- **G7. Только таблицы, без вьюх; Postgres жёстко `schemaname='public'`; в MsSQL/MySQL `SchemaName`
  пустой** → дерево не сгруппировать по схемам.
- **G8. Один запрос за вызов:** `DatasourceController.SqlQuery` берёт `_sql[0]` из `string[]`.
- **G9. `IDatasourceDriver` — SQL-образный** (`Tables/Columns/DatabaseStructure/SqlQuery`), CSV/Excel/HTTP
  в него не влезают; «ConnectionString» — тоже SQL-понятие.
- **G10. Секреты:** connection string открытым текстом в опции и в textarea админки; шифрования нет,
  логирования тоже нет (и не заводить).
- **G11. Три драйвера жёстко прилинкованы** в `Mars.Datasource.Host.csproj` → провайдер нельзя поставить
  пакетом.
- **G12.** Нет сохранённых запросов, истории, состояния вкладок; область пересоздаётся при смене slug.

## 5. Этапы

### Этап 1. Контракт результата (ядро) — сделано 2026-09-15

- [x] Новые модели в `Mars.Datasource.Abstractions/Models/`: `QueryColumn` (`Name`, `DataTypeName`,
      `ClrTypeName`, `IsNullable`, `IsJson`, `IsKey`), `QueryResultDto` (`Columns`, `Rows` как
      `string?[][]`, `Truncated`, `ElapsedMs`, `Command`), `SqlRequest` (`Sql`, `Parameters?`, `MaxRows`,
      `TimeoutSec?`), `SqlParam` (`Name`, `Value`).
- [x] Общие преобразования для провайдеров — `Models/QueryResultMapping.cs`: колонка из `DbColumn`,
      значение в строку (даты/время — инвариантно, чтобы правка ячейки возвращалась без потерь),
      чтение строк с лимитом, текст ошибки одной строкой, подстановка параметров.
- [x] `IDatasourceDriver`: `Query(SqlRequest, CancellationToken)`, `NonQuery(sql, parameters, ct)`,
      `QuoteIdentifier(string)`. Старые `SqlQuery`/`SqlNonQuery` **удалены** (не помечены
      `[Obsolete]`), `SqlQueryResultActionDto` удалён, `SqlNonQueryResultActionDto` и
      `SqlQueryJsonResultActionDto` вынесены в отдельные файлы. Все три драйвера мигрированы сразу.
- [x] **Инвариант совместимости:** `QueryResultDto.Data` — `[JsonIgnore]`-проекция
      (заголовки + строки, `NULL` → `""`); `SqlNodeImpl` и `MarsSqlTools` ходят в новые методы
      сервиса, но читают по-прежнему `.Data`. Записать это в гайд при схлопывании.
- [x] Лимит: читаем до `MaxRows`, при переполнении ставим `Truncated` и прекращаем чтение.
      **`MaxRows = 0` (по умолчанию) — без ограничения**: так `SqlNode` и поток-потребители не меняют
      поведение; UI (`DatabaseQueryWorkspace`) передаёт 500. Отмена: `CancellationToken` доходит до
      `ExecuteReaderAsync`/`ExecuteNonQueryAsync`.
- [x] Эндпоинты `Query` и `NonQuery` в `DatasourceController` + методы в `IDatasourceServiceClient` /
      `DatasourceServiceClient` (тело — `SqlRequest`).
- [x] Ошибки: стиль `Ok=false` + `Message` сохранён, текст обрезается до одной строки (500 символов),
      `DatabaseDriver` больше не теряется на клиенте.
- [x] Тесты: на каждом движке — лимит с `Truncated`, `NonQuery` с параметрами, чтение колонок результата;
      в `Mars.Datasource.Integration.Tests` появилась вставка строк (`SeedTodoAsync`) — раньше тест
      проверял только строку заголовков. В `WebApiClient.Integration.Tests` — `Query` и `NonQuery`.

### Этап 2. Баги и UX ошибок — сделано 2026-09-15

- [x] `PartDataSourceActions.razor`: `Query tool` ведёт на `slug=@Config.Slug` через `nav.NavigateTo`
      (было хардкод `slug=mssql`, из-за чего страница падала на чужом slug), кнопка заблокирована
      при невалидном slug, под ней текст ошибки.
- [x] Абсолютные ссылки от `nav.BaseUri` вместо относительных (`DatabaseQueryWorkspace`,
      `SqlNodeForm`): свойства `datasourceConfigUrl`.
- [x] `ExecuteAction` принимает slug: pg-«полезные» запросы и `BackupAsSQLFile` доступны только
      для psql-источника (иначе внятный отказ), backup идёт по выбранному источнику, а не по `default`.
      CLI `ds backup|restore` оставлен на `DefaultConfig`.
- [x] Slug: правила в `DatasourceConfig.ValidateSlug` (2–32, `[a-z0-9_-]`, `default` зарезервирован),
      инлайн-ошибка в `EditDatasourceOptions` (включая дубль, регистронезависимо). В `DatasourceService`
      битые и повторяющиеся slug **пропускаются** при построении словаря — раньше `ToDictionary` падал
      и «ломались» сразу все источники.
- [x] `async void` убран везде (`DatabaseQueryWorkspace`, `DataSourceInfoComponent`,
      `PartDataSourceActions`, `SqlNodeForm`), `StateHasChanged` больше не зовётся из `Task.Run`,
      добавлены try/catch + видимые ошибки (в рабочей области — вместо падения показывается alert).
- [x] `RegisterOption<DatasourceOption>` с `onChangeHook` → `InvalidateLocalDictCache`
      (`MainDatasource.cs`).
- [x] Подтверждение опасного SQL: `SqlSafety.IsDestructive` + диалог `DeleteConfirmationDialog`
      (FluentUI `ShowConfirmationAsync` живёт только на конкретном `DialogService`, поэтому взят
      репозиторный путь). Дополнительно к плану: подтверждения требует не только `DELETE` без `WHERE`,
      но и `UPDATE` без `WHERE`.
- [x] Чистка: `EnginesTests.cs` удалён; дубль-класс `WebApiClientDatasourceClientExtensions`
      переименован в `WebApiClientDockerClientExtensions` — это была опечатка в `Mars.Docker.Front`,
      из-за неё любой потребитель обоих модулей получал CS0104.
- [x] Сверх плана (найдено по ходу): `DatasourceConfig.GetDatabaseName()` больше не падает на пустой
      строке подключения; убрана мусорная ведущая `"` в дефолтной строке MSSQL; удалён отладочный
      `Console.WriteLine("Zee")` и мёртвый `DriverChanged`; в результате запроса появились заголовки
      колонок и строка «rows / ms / truncated» (раньше таблица выводилась и вовсе без заголовков),
      удалён нерабочий offcanvas-блок из разметки рабочей области.
- [x] Тесты: `SqlSafetyTests` (опасные/безопасные операторы, комментарии, `FirstWord`) и
      `DatasourceConfigTests` (правила slug, устойчивость `GetDatabaseName`). `SqlSafety` переехал в
      `Mars.Datasource.Abstractions`, чтобы покрываться тестами без ссылки на Razor-библиотеку.
- [ ] Отмены запроса из UI пока нет: нужен `CancellationToken` в методах клиента и кнопка в новой
      рабочей области (Этап 5). Отмена по разрыву HTTP-соединения уже работает — токен доходит до БД.

### Этап 3. Схема БД — сделано 2026-09-15

- [x] Структура собирается одним-двумя запросами на движок, обход таблиц с открытием соединения на
      каждую убран. SQL по движкам: psql — `pg_class`/`pg_namespace`/`pg_attribute`/`pg_index`;
      MsSQL — `INFORMATION_SCHEMA` по `TABLE_CATALOG` (= база из строки подключения); MySQL —
      `information_schema` по `TABLE_SCHEMA`.
- [x] `Columns(table)` больше не делает `SELECT * FROM t`: одна выборка метаданных по таблице
      (или по всей базе для структуры), PK — из каталога, а не из reader-схемы.
- [x] Вьюхи и матвьюхи: `QTableSchema.Kind` (`table`/`view`/`matview`), в psql — все пользовательские
      схемы (`pg_namespace`), в MsSQL/MySQL `SchemaName` теперь заполняется (раньше был пустым).
- [x] `QTableColumn.IsNullable` добавлено, `IsKey` приходит из каталога (PK) — на этом будет стоять
      правка ячеек в Этапе 5. Общая сборка структуры — `QDatabaseStructureBuilder` + `QTableMeta`/
      `QColumnMeta` в Abstractions: три драйвера больше не дублируют логику.
- [x] Кэш структуры: `DatasourceService` держит её 30 секунд, сбрасывает при смене опции; кнопка
      «обновить» в дереве таблиц → `RefreshStructure` (эндпоинт + метод клиента), минуя кэш.
- [x] Фронт (в старой рабочей области, Этап 5 перерисует): группировка дерева по схемам, бейдж
      `view`/`matview`, маркеры PK и `not null` в списке колонок, кнопка обновления структуры.
- [x] Тесты: PK/`IsNullable`/`Kind` на всех трёх движках, вьюха в структуре на psql, кэш
      (`BeSameAs`) и `RefreshStructure` в HTTP-тестах.
- [ ] Не сделано осознанно: отдельного `Views()`-метода нет — вьюхи приходят тем же `Tables()`
      с признаком `Kind` (одно дерево вместо двух списков). Материализованные вьюхи проверены
      только на psql — в MsSQL их нет, в MySQL тоже.

### Этап 4. JSON-режим — сделано 2026-09-15

- [x] `IsJson` приходит и из результата запроса (`QueryColumn.IsJson`), и из схемы
      (`QTableColumn.IsJson` + `QTableColumnResponse.IsJson`) — общий признак в `QColumnMapping.IsJson`
      (`json`, `jsonb`).
- [x] json-viewer подключён: дев-оболочка `src/Mars.Admin/wwwroot/index.html`, боевой список скриптов
      `Mars.SiteEngine.Host/WebSite/Scripts/AppAdminSpaHtmlScripts.cs` (добавлен `version: appVersion`,
      раньше версии в URL не было вовсе), `MarsAppVersion` → `0.8.3-alpha.15`.
- [x] Blazor-обёртка `Mars.Datasource.Front/Components/JsonViewer.razor` — своего interop'а не нужно,
      это веб-компонент (shadow DOM), атрибуты передаются из разметки; в репо обёртки не было.
- [x] В рабочей области: переключатель «table / json» для всего результата (все колонки как объекты,
      json-колонки разворачиваются вложенными объектами) и разворот json-значения прямо в ячейке.
- [x] Тесты: json-колонка распознаётся и в результате, и в схеме — на psql (`jsonb`) и MySQL (`JSON`).
- **Отклонено:** портировать `SqlQueryJson` (постресовый `row_to_json` + префикс имени таблицы) в
  MsSQL/MySQL. Вложенный вид — это *представление* тех же плоских данных, поэтому он собран на клиенте:
  работает на всех движках и не требует от пользователя писать агрегаты. `SqlQueryJson` остаётся
  внутренним — его использует backup-драйвер.
- **Ограничение:** в MsSQL нет json-типа (данные лежат в `nvarchar`), поэтому автоопределение
  json-колонки там невозможно — подсветка в ячейках не появится, пока не появится явная настройка
  «считать колонку JSON».

### Этап 5. Визуал

Делать **после** Этапов 1–4, отдельным согласованием (пользователь хочет сначала обсудить концепцию).

- [ ] MultiSplitter: `FluentMultiSplitter`/`FluentMultiSplitterPane` есть в FluentUI 4.14.4, свой
      `.razor.js` в поставке — внешних зависимостей не нужно; в репо не используется нигде.
- [ ] Вкладки запросов на `FluentTabs`/`FluentTab` (используются в `FormLayoutEditor`,
      `FormRenderer`), а не на Bootstrap nav-pills, как сейчас.
- [ ] Редактируемый грид строить самим: готовых инлайн-редактируемых гридов в репо нет (все ~17 мест
      `FluentDataGrid` — read-only), нужен `TemplateColumn` + правка + «несохранённые изменения».
- [ ] Точка входа: сейчас `/datasource` — три карточки, `/datasource/query` — IDE-подобное дерево;
      обсудить альтернативу «тайлы/карточки таблиц вместо дерева» (идея пользователя) — она не
      противоречит сплиттеру, потому что рабочая область остаётся одним компонентом.
- [ ] Автокомплит по схеме — отдельная фича: Monaco (BlazorMonaco 3.5.0) даёт только токенизацию SQL,
      completion provider надо регистрировать свой.

### Этап 6. Не-SQL провайдеры

- [ ] `DatasourceConfig.Kind` (`sql` | `csv` | `xlsx` | `sheets` | `http`) + `Settings` (словарь/JSON);
      существующие конфиги — `Kind = "sql"`, `Driver` остаётся внутри SQL-провайдера.
- [ ] Провайдер-абстракция: список «таблиц» + `Query → QueryResultDto` (+ опционально запись);
      `IDatasourceDriver` становится SQL-провайдером под ней.
- [ ] CSV/Excel-файл: чтение файла из медиа/`IFileStorage`, первая строка = заголовки, типы выводятся;
      для xlsx есть `ClosedXML` в стеке (`src/Mars.Modules/Mars.Excel.Host`, сейчас только генерация отчётов).
- [ ] Google Sheets: сначала только чтение (CSV-экспорт), запись — OAuth + API отдельной фазой.
- [ ] HTTP/REST как таблица: метод, заголовки, авторизация, ответ-массив → строки (переиспользует
      разбор JSON из Этапа 4).
- [ ] Провайдеры как плагины: убрать жёсткие ссылки на три драйвера из `Mars.Datasource.Host.csproj`
      в пользу динамической регистрации, иначе не-SQL источник нельзя поставить пакетом.

## 6. Проверка

- Сборка: `dotnet build Mars.slnx`.
- Интеграционные тесты движков (Testcontainers, нужен Docker):
  `dotnet build tests/Mars.Datasource.Integration.Tests` →
  `tests\Mars.Datasource.Integration.Tests\bin\Debug\net10.0\Mars.Datasource.Integration.Tests.exe`.
- Контрактные тесты HTTP: `tests/Mars.WebApiClient.Integration.Tests/Tests/Datasources/DataSourceTests.cs`.
- Полный прогон — `pwsh -NoProfile -File test-all.ps1` (E2E/DockerImage не гоняем без явной команды).
- Новые тесты по этапам: писатели SQL-генерации (правка ячейки → `UPDATE`), лимит/`Truncated`, кэш
  структуры, вьюхи в дереве, `Kind`-провайдеры. Тестов на Razor-страницы в репо нет — если заводить,
  это отдельное решение ([feedback: точечная проверка](../QWEN.md)).

## 7. Грабли и инварианты

- `Data[0]` — строка заголовков у всех трёх драйверов; внутри репо на это опираются только
  `SqlNodeImpl` и `MarsSqlTools.FormatRows` (оба через сервис, не через HTTP) — легаси-проекция обязана
  продолжать работать.
- Админка — WASM: весь SQL ходит по HTTP, значит лимиты и таймауты обязаны быть серверными, иначе
  браузер повиснет на большой выборке ещё до отрисовки.
- Правка ячейки пишет в чужую (возможно, прод) БД: показ SQL и подтверждение — не «улучшение», а
  требование.
- Monaco: несколько живых инстансов одновременно в репо нигде не проверялись; `Lang=log` ставит тему
  глобально (`CodeEditor2`); при ресайзе сплиттера проверить `AutomaticLayout`, иначе редактор останется
  старого размера.
- `WaitHelper.WaitForNotNull(() => _editor, 2000)` в текущей рабочей области — редактор берётся по `@ref`;
  при пересоздании DOM (сплиттер/вкладки) ссылку нельзя считать живой.
- Connection string не логировать и не выводить в ошибках; AiChat его читать уже не даёт
  (`MarsOptionsTools` `ReadDenied`) — не сломать.
- После правок js/css, грузящихся в браузер, bump `MarsAppVersion` (`Directory.Build.props`).

## 8. Отклонено / отложено (с причинами)

- **Автоприменение правки ячейки** (Airtable/NocoDB-стиль) — опасно на внешних БД.
- **OFFSET-обёртка для пагинации** — ломается на `WITH`, `;`, DDL; вместо неё cap + явный SQL.
- **`ctid`/`rowid` вместо PK** — поведение «то работает, то нет».
- **Флаг «только чтение» на источник** — не нужен: редактор уже исполняет любой SQL.
- **Редактирование sequences/ролей** — не нужно (постановка пользователя).
- **ИИ-фичи** (генерация SQL — сейчас есть только кнопка «AI help» в редакторе) — в конце инициативы.
- **Связывание метаполей с SQL** — после ядра; задевал только design-note
  (`ai/MetaFieldsGuide.md:45`), кода нет.
- **Многоячейковый ноутбук** — отложен пользователем (см. решение 3).

## 9. Открытые вопросы

1. Сохранённые запросы и состояние вкладок: сущность в БД, `localStorage` или ничего на первом проходе.
2. Визуал Этапа 5: остаётся ли «дерево + редактор + результат» или пробуем тайлы/карточки таблиц.
3. JSON-режим: хватает ли подсветки/разворота ячеек или нужен ещё «дерево всего результата».
4. Нужен ли SQL-автокомплит по схеме (дороже, чем кажется: свой completion provider в Monaco).
5. Backup из UI: оставить только для psql или прятать кнопку для не-psql источников вовсе.
6. Google Sheets: чтение или сразу запись (OAuth) — влияет на объём фазы.
