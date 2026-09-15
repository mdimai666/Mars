# План: реворк DataSource — рабочий инструмент источника данных

Исходные материалы: [Prompts/FinalizeDatasourcePrompt.md](./Prompts/FinalizeDatasourcePrompt.md)
(постановка: ревизия → добить фичи → визуал → остальные хотелки), ревизия кода 2026-09-15 (разделы 2–4
ниже). Ветка — `ai/datasource-rework`. Гайда `ai/DatasourceGuide.md` пока нет: по закрытии инициативы
схлопнуть этот план в него ([PlanLifecycleGuide.md](./PlanLifecycleGuide.md)).

Статус на 2026-09-16: ревизия сделана, **этапы 1–5.5 выполнены и закоммичены** (`313fb04e`, `0068b272`,
`02757a32`, `15e7a327`, `5e5d6f4d`, `b43632ef` — доводка верстки/редактора, `e1b4089d` — вторая итерация
визуала грида, `4d7640d8` — ширина/типы/даты и модальная правка длинных значений). **Этап 7 (управление
вьюхами) сделан 2026-09-16** и пока не закоммичен, этап 6 (не-SQL провайдеры) не начат; визуал этапов 5.1–7
проверяет пользователь в браузере, ветка `ai/datasource-rework` не запушена.

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

### Этап 5. Визуал — сделано 2026-09-15

- [x] `FluentMultiSplitter`: слева дерево объектов (250px, тянется, складывается; поиск по имени,
      группировка по схемам, бейджи `view`/`matview`, кнопка обновления структуры), справа рабочая область.
- [x] Вкладки запросов: у каждой свой SQL, результат, серверный лимит строк, режим table/json и свои
      несохранённые правки; «+» добавляет вкладку, крестик закрывает.
- [x] Редактируемый грид `Components/QueryResultGrid.razor`: правка одной ячейки по клику (Enter/blur —
      зафиксировать, Esc — бросить), подсветка изменённых ячеек, «Отменить (N)» / «Сохранить (N)».
- [x] Сохранение через показ SQL: `Components/SqlPreviewDialog.razor` показывает готовые `UPDATE`
      (по одному на строку), выполнение только по кнопке, значения — параметрами; после успеха данные
      перечитываются. Правка доступна **только** в режиме просмотра таблицы и только при наличии PK,
      иначе грид read-only с пояснением (решение из п.1.5).
- [x] `RowUpdateBuilder` в Abstractions: сборка `UPDATE` с квотированием от драйвера и параметрами;
      7 юнит-тестов (составной ключ, NULL, отсутствие ключа, скобки MsSQL).
- [x] Пагинация: кнопка «Показать больше» (×5 к серверному лимиту); структура таблицы — раскрывающийся
      блок над редактором (колонки, типы, PK, not null).
- **Найдено и исправлено тестом (важное):** строковый параметр в psql уходил с типом `text`, поэтому
  `WHERE "id" = @p` по uuid-колонке падал с `42883: operator does not exist: uuid = text`. Это сломало бы
  правку в UI почти на всех таблицах Mars (у постов uuid-ключи). Теперь строковые параметры psql
  отправляются как `unknown` (`NpgsqlDbType.Unknown`, через хук `configure` в
  `QueryResultMapping.ApplyParameters`) — тип выводит сам Postgres. MsSQL/MySQL правка не нужна: они
  неявно приводят строку к типу колонки.
- **Отклонение от плана:** полоса вкладок сделана тонким своим элементом на `FluentButton`, а не
  `FluentTabs`. Причина: `FluentTab` рендерит собственную панель содержимого на каждую вкладку, а у нас
  контент общий (один редактор + один результат), и содержимое-без-tab-режим визуально не проверить.
- **Честно:** глазами в браузере не проверял (браузер открываю только по команде). Проверено: сборка
  решения 0 предупреждений, 71/71 тестов датасорса, HTTP-тесты. Посмотреть стоит: поведение сплиттера
  рядом с Monaco (нужен ли `AutomaticLayout` при ресайзе), sticky-заголовок грида, диалог подтверждения.
- [ ] Из хотелок осталось: автокомплит по схеме (свой completion provider в Monaco), сохранённые
      запросы/история, отмена запроса из UI (нужен `CancellationToken` в методах клиента), сортировка по
      клику на заголовок, «тайлы/иконки вместо дерева» — обсуждать отдельно.

### Этап 5.1. Визуал грида результата — вторая итерация — сделано 2026-09-15

- [x] Перегородки между ячейками (`th/td:not(:last-child)` + `border-right`) — строки и колонки не сливаются.
- [x] guid-колонки: ширина до 110px, значение показывается как «начало…конец» (8 + 6 символов,
      моноширинный 12px, середина скрыта). Определение колонки: категория `QColumnKind.Guid` из общей
      `QColumnMapping.Kind` (`uuid`/`uniqueidentifier`) **или** значения, парсящиеся в `Guid` — иначе в
      MySQL/старых схемах `char(36)` не узнать; выборка — первые 5 непустых значений.
- [x] Правка ячейки — **только по двойному клику** (`@ondblclick`, было «клик = правка, второй клик =
      копирование»; копирование по клику убрано по просьбе пользователя 2026-09-15). Полное значение
      усечённой ячейки доступно в `title`, в редакторе по двойному клику и при копировании выделения —
      `copy`-обработчик подменяет усечённый текст полным по атрибуту `data-full`
      (`wwwroot/MarsDatasourceFrontJsInterop.js`, регистрация в `OnAfterRenderAsync` грида). Тот же
      атрибут ставят json-ячейки: раньше при копировании в них попадало слово «json». Подсказка про
      двойной клик — в строке состояния грида.
- [x] Цвета типов по категориям: `QColumnKind` (`Text`/`Number`/`Boolean`/`DateTime`/`Json`/`Guid`) и
      `QColumnMapping.Kind` в Abstractions (переиспользует существующий маппинг `ClrType`; незнакомый
      провайдерский тип показывается как строковый), в гриде — `ds-type-*` классы и палитра на CSS-переменных
      с вариантом для `body.dark` (и для системной тёмной темы). Красится и **значение ячейки**, и подпись
      типа в заголовке: числа — синие, даты — зелёные, bool — оранжевый, json — фиолетовый; строковые
      значения остаются цветом по умолчанию (их большинство, приглушать основной текст нельзя), а подпись
      строкового типа — серая. Покрыто `QColumnMappingTests` (7 теорем, 30 кейсов).
- [x] Ширина таблицы по контенту: убран `min-width: 100%` (из-за него узкие таблицы растягивались на всю
      область), осталось `width: max-content`; заголовок колонки остался липким.
- [x] Cache-busting: `MarsAppVersion` → `0.8.3-alpha.17`; `Mars.Admin.styles.css` теперь подключается
      с версией — до этого scoped css RCL в браузере не инвалидировался. Заодно исправлена дев-оболочка
      `src/Mars.Admin/wwwroot/index.html`: она ссылалась на `AppAdmin.styles.css` (имя до реструктуризации),
      поэтому при запуске через WASM-dev-server scoped css вообще не грузился; версии там по-прежнему нет
      — нужен хард-релоад.
- Проверено: `dotnet build Mars.slnx` — 0 ошибок, 0 предупреждений; `QColumnMappingTests` — 30/30,
  `QueryResultMappingTests` — 9/9; модуль разбирается node (экспорт `registerFullValueCopy`). Глазами в
  браузере не проверял: проверить стоит липкий заголовок с новыми `border-right` (в `border-collapse: collapse`
  линии при скролле может «терять» Chrome) и читаемость палитры типов на тёмной теме.

### Этап 5.2. Найдено пользователем при проверке в браузере — сделано 2026-09-15

- [x] **Ячейка показывала старое значение после правки.** Грид рисовал значение только из `Result.Rows`,
  а `Tab.Changes` использовался лишь для подсветки: пока не нажмёшь «Сохранить» (и запрос не перечитается),
  в ячейке висело старое значение. Теперь отображаемое значение = несохранённая правка, если она есть,
  иначе значение из результата; повторное открытие редактора тоже показывает правку. Сравнение с
  оригиналом в `CommitEdit` не изменилось — вернул исходное значение, правка снимается.
- [x] **`character varying[]` показывался как `System.String[]`.** Общий `QueryResultMapping.Format`
  не знал про коллекции и уходил в `value.ToString()`; в psql массивы приходят как `string[]`/`int[][]`.
  Теперь массив форматируется литералом Postgres — `{a,"b,c"}` (как показывает psql; такое значение
  можно вернуть в `UPDATE` параметром), пустой элемент и слово `NULL` берутся в кавычки, вложенные
  массивы рекурсивно. `byte[]` по-прежнему base64 (проверяется до массива), словари (hstore) не трогаем —
  они тоже `IEnumerable`. Покрыто `QueryResultMappingTests` (9 кейсов).

### Этап 5.3. Ширина значений и страницы — сделано 2026-09-15

- [x] Значение обычной ячейки ограничено 330px с многоточием (`.ds-cell-text`), развёрнутый json в ячейке —
      тоже 330px и внутренний скролл: у viewer'а ключи `flex-shrink: 0`, поэтому одна json-колонка распирала
      таблицу. Полный текст по-прежнему достаётся редактором (двойной клик) и копированием выделения.
- [x] Страница перестала расширяться по данным — только скролл таблицы. Две причины:
  1. **Правила для элементов FluentUI из scoped css не применялись вовсе.** `b-`атрибут Blazor вешает
     только на элементы своего компонента, значит `.ds-splitter { flex: 1 1 auto; min-height: 0 }`
     (класс на корне `FluentMultiSplitter`) было мёртвым. Теперь сплиттер и панели задаются через
     `::deep` (`.ds-workspace ::deep .fluent-multi-splitter` / `.fluent-multi-splitter-pane`),
     класс `ds-splitter` из разметки убран — цепляемся за класс библиотеки.
  2. **`min-width: 0` по всей цепочке.** Без него flex-элемент не сжимается ниже контента, и min-content
     широкой таблицы уходил вверх до `main#Content` (flex-элемент лэйаута) → горизонтальный скролл страницы.
     Добавлено: `.ds-workspace` (flex-элемент `main#Content`), панели сплиттера, `.ds-pane`, `.ds-result`,
     `.ds-grid`, `.ds-scroll`.
- Проверяет пользователь в браузере (я его не открываю): ожидаемое поведение — таблица скроллится
  горизонтально внутри своей области, страница и сайдбар стоят на месте.

### Этап 5.4. Показ типов и дат — сделано 2026-09-15

- [x] Типы показываются коротким именем — `QColumnMapping.ShortTypeName` (Abstractions): `timestamp with
      time zone` → `timestamptz`, `timestamp without time zone` → `timestamp`, `time with/without time zone` →
      `timetz`/`time`, `character varying` → `varchar`, `character` → `char`, `double precision` → `float8`;
      суффикс массива `[]` сохраняется (`varchar[]`), незнакомые имена отдаются как есть в своём регистре
      (MsSQL `NVARCHAR` не трогаем). Используется и в заголовке грида, и в блоке «структура».
- [x] Даты/время в ячейках — без секунд, долей и часового пояса (`QueryResultMapping.DisplayDateTime`:
      `2026-09-15T10:30:00.0000000+03:00` → `2026-09-15 10:30`, `date` без времени и `time` без секунд так же).
      **Значение в контракте остаётся полным** — иначе правка ячейки вернула бы в базу усечённое время,
      а копирование потеряло бы пояс. Поэтому показ отличается от значения → на ячейке `title` и `data-full`
      (при копировании выделения уходит полное значение), редактор по двойному клику тоже полное.
- [x] Режим «json» показывает значения как есть (это «сырой» вид результата).
- [x] Тесты: `ShortTypeName` (12 кейсов) и `DisplayDateTime` (7 кейсов) — юнит-тесты рядом с форматтерами.
- Проверено: `dotnet build Mars.slnx` — 0 ошибок/предупреждений, 58/58 в юнит-наборах датасорса.

### Этап 5.5. Правка длинных значений в модалке — сделано 2026-09-15

- [x] Двойной клик по ячейке со значением длиннее 50 символов (`QueryResultGrid.InlineEditMaxLength`)
      открывает модалку `Components/CellValueDialog.razor` с Monaco (`CodeEditor2`), а не однострочный input:
      в ячейке такое значение всё равно не видно. Для json-колонки язык редактора `json`, для остальных —
      `plaintext`; ширина `min(960px, 90vw)`, высота редактора 50vh, закрытие по клику вне окна выключено
      (в модалке ценные данные).
- [x] Результат модалки идёт тем же путём, что инлайн-правка: `ApplyEdit` (вынесен из `CommitEdit`) кладёт
      значение в `Tab.Changes`, равное исходному — снимает правку. То есть «Сохранить (N)» работает одинаково.
- [x] Побочно: модалка годится и как «просмотрщик» длинного значения.
- Проверено: `dotnet build Mars.slnx` — 0 ошибок/предупреждений. Поведение Monaco в диалоге (раскладка
  при `AutomaticLayout`, которое в `CodeEditor2` включено) проверяет пользователь в браузере: в датасорсном
  фронте тестов на Razor-компоненты нет, а Monaco — JS.

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

### Этап 7. Управление вьюхами — сделано 2026-09-16

Решения пользователя (2026-09-15/16): скоуп — **обычные вьюхи** (определение + создание/замена + удаление),
матвьюхи и карта зависимостей не в этой фазе; «изменить» — `CREATE OR REPLACE` (psql/mysql) и
`CREATE OR ALTER` (mssql), а смена состава колонок остаётся ошибкой движка как есть (автоматический
drop+create не делаем); тело вьюхи — **проверка, что это один SELECT**, без обёртки в подзапрос
(определение должно остаться ровно таким, как его написал пользователь); UI — кнопка «＋ вьюха» у фильтра
дерева и кнопки «определение»/«удалить» в инфо-строке открытого объекта, без контекстных меню.

До этапа уже работало (не переделывали): вьюхи приходят в дерево с бейджем `kind`, колонки вьюхи описаны
тем же запросом, клик открывает `SELECT *`, правка ячеек у вьюхи выключается сама (нет PK), есть тест
`DatabaseStructure_CreatedView_AppearsWithViewKind`.

- [x] Ядро без Docker — `Abstractions/Models/ViewDdlBuilder.cs`: `ViewDialect` из `DatasourceConfig.Driver`,
      `ViewDdlResult { Sql | Error }`, `Create(schema, name, body, replace)`, `Drop(schema, name)`,
      квотирование по диалекту (`"` / `[]` / `` ` ``). `Inspect` проверяет тело: первое слово — одно из
      `SELECT|WITH|VALUES|TABLE`, «;» допускается только хвостовая и вне литералов/комментариев/
      dollar-quoting (лексер повторяет правила движка, включая `\`-экранирование в MySQL: иначе наша
      проверка и парсер разошлись бы). Хвостовую «;» builder убирает сам.
- [x] Драйверы: `IDatasourceDriver.ViewDefinition(schema, name)` + три реализации — PG `pg_get_viewdef`
      (поиск по `pg_class`/`pg_namespace`, без склейки `::regclass`), MsSQL `sys.sql_modules`,
      MySQL `information_schema.VIEWS`.
- [x] Сервер: `IDatasourceService.ViewDefinition` + `DatasourceService` + `GET
      Datasource/ViewDefinition(slug, schema, name)` → `Dto/ViewDefinitionResponse { Sql }` (пусто —
      движок не отдал текст) + метод в `IDatasourceServiceClient`/`DatasourceServiceClient`.
- [x] Кэш: `DatasourceService.NonQuery` сбрасывает `_structureCache` при DDL-первом слове
      (`CREATE|ALTER|DROP|REFRESH|TRUNCATE`) — раньше после DDL дерево обновлялось только кнопкой «Обновить».
- [x] UI: `Components/CreateViewDialog` (схема/FluentSelect из загруженной структуры, имя, чекбокс
      «заменить, если есть», живой предпросмотр DDL, кнопка OK гаснет при ошибке валидации) и
      `Components/ViewDefinitionDialog` (Monaco, кнопка «В редактор» возвращает текст в рабочую область).
      В дереве — кнопка «＋ вьюха» у фильтра; в инфо-строке открытой вьюхи — «определение»/«удалить»
      (подтверждение удаления — общий `DeleteConfirmationDialog` с показом `DROP VIEW`).
      Тело вьюхи берётся из редактора рабочей области, а не правится в диалоге: у `CodeEditor2` нет события
      изменения, а предпросмотр обязан совпадать с тем, что выполнится.
- [x] Сценарий «изменить»: «определение» → «В редактор» (запоминается объект) → «＋ вьюха» с
      предзаполненными схемой/именем и включённым «заменить». После успешного DDL вкладка перепривязывается
      к объекту из перечитанной структуры (после DROP — отвязывается), созданная вьюха открывается.
- [x] Ограничения фазы: кнопки показываются только для `kind == "view"` — у матвьюх другой DDL, и лучше
      не показывать действия, которые на них падают; `DROP ... CASCADE` не предлагаем (при зависимых
      объектах будет ошибка движка).
- [x] Тесты: `ViewDdlBuilderTests` — 39 кейсов (диалекты, квотирование, `replace`, хвостовая «;»,
      «;» в середине с позицией, «;» в строке/комментарии/dollar-quoting, MySQL-экранирование, не-SELECT,
      пустое имя/тело, имя со схемой, `Drop` в трёх диалектах); в каждом движке —
      `ViewDefinition_CreatedByBuilder_ReturnsBodyAndDrops` (DDL собирает сам builder, читаем определение,
      заменяем, удаляем); HTTP-контракт — `DataSourceTests.ViewDefinition_Request_Success`.
- Найдено тестом (это и есть выбранная семантика): в Postgres `CREATE OR REPLACE VIEW` не даёт **убрать**
      колонку — `42P16: cannot drop columns from view`; добавлять колонки можно. В MsSQL/MySQL замена с
      другим составом колонок проходит. Тест PG это фиксирует: ошибка движка показывается, вьюха не
      пересоздаётся за спиной пользователя.
- Проверено: `dotnet build Mars.slnx` — 0 ошибок/0 предупреждений; `ViewDdlBuilderTests` 39/39;
      движки (Testcontainers) — Postgres 11/11, MsSQL 7/7, MySQL 8/8; HTTP-контракт
      `Mars.WebApiClient.Integration.Tests` 11/11. Верстку диалогов и поведение Monaco проверяет
      пользователь в браузере: тестов на Razor-компоненты в репо нет.

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
7. `SqlSafety.IsDestructive` не относит `CREATE` к опасным, поэтому `CREATE OR REPLACE VIEW` в редакторе
   выполняется без подтверждения. Диалог вьюх (Этап 7) подтверждает сам; расширять общий список —
   отдельное решение.
