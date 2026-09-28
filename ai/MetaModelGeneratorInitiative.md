# Mars.MetaModelGenerator — отчёт по обзору и направлениям переработки

Статус: **работа не начата** (2026-09-27), отчёт зафиксирован для будущего старта.
Модуль: `src/Mars.Modules/Mars.MetaModelGenerator` (5 компилируемых файлов + исключённый из сборки пример).

## Замысел модуля (подтверждён пользователем)

Мост «EAV из БД → типизированные CLR-модели в рантайме»:

1. `MetaEntityTypeProvider` читает активные PostTypes с MetaFields из БД.
2. `GenSourceCodeMaster` + `MtFieldInfo` генерируют исходник класса-`Mto`
   (`NewsMto : PostEntity, IMtoMarker`) — метаполе → strongly-typed свойство;
   relation-поля резолвятся через `metaModelTypesResolverDict`; `Display`-атрибуты
   делают модель самоописывающей.
3. Ключевой механизм — статическое поле `selectExpression`
   (`Expression<Func<PostEntity, XMto>>`), проецирующее EAV-строки в типизированные
   свойства в EF-транслируемой форме. Потребители: `QueryLangLinqDatabaseQueryHandler`,
   `MtoRelationMaterializer` (Cms.Host).
4. `RuntimeMetaTypeCompiler` компилирует пакет через Roslyn `CSharpScript` в
   `Dictionary<string, Type>`; типы кешируются в `MetaModelTypesLocator` (Cms.Host)
   с ручной инвалидацией.

Побочная линия: `GenerateMetaTypesSourceCode()` отдаёт сгенерированный C# наружу
(админка/AI-инструменты); `partial class` — задел на расширение моделей.

Инварианты, которые нельзя сломать при переработке:
- один битый PostType не должен ронять генерацию всех моделей (частичная деградация);
- форма `selectExpression` обязана оставаться EF-транслируемой
  (проверяется `ModelEfRequestRuntimeCompiledTests`).

Хранение: `MetaValueBase` (`src/Server/Mars.Data/Entities/MetaValueBase.cs`) — широкая
строка с типизированными nullable-колонками (`StringShort`, `Int`, `Decimal`, `VariantId`,
`ModelId`…) + FK `MetaFieldId` на `MetaFieldEntity`.

## Находки по текущему коду

### A. Структура и зависимости

1. Ссылка `Mars.QueryLang.Host → Mars.MetaModelGenerator` существует ради одной
   константы `GenSourceCodeMaster.selectExpressionGetterName` (`"selectExpression"`,
   `QueryLangLinqDatabaseQueryHandler.cs:151`). Контракт модуля (`IMetaEntityTypeProvider`,
   `MtoModelInfo`) уже живёт в `Mars.Cms.Abstractions` — перенос константы туда
   полностью разрывает связь Host→Host и закрывает исключение в `ai/ProjectStructureGuide.md`.
2. Мёртвый код: `EmitVariantExample.cs` (`Compile Remove`), «Концепт»
   `IGenExtraFieldFunction` + `PostStatusFieldFunction` (0 использований),
   приватный `TypeFieldsAsSourceCode` (не вызывается).
3. Рассинхрон имён файлов: `RuntimeTypeCompiler.cs` содержит класс
   `RuntimeMetaTypeCompiler`; `IRuntimeTypeCompiler.cs` (Cms.Abstractions) содержит
   `IMetaEntityTypeProvider` + `MtoModelInfo`.
4. Тест-проект `tests/Test.Mars.MetaModelGenerator` — единственный легаси `Test.Mars.*`
   в решении; конвенция — `Mars.MetaModelGenerator.Tests` (затронуты `Mars.slnx`, `test-all.ps1`).

### B. Надёжность

5. Нет защиты имён Mto-классов: `GetNormalizedTypeName` может дать коллизию
   (`"temp-page"` и `"temp page"` → `TempPageMto`) или невалидный идентификатор
   (`"1st"` → `"1stMto"`); один плохой PostType роняет компиляцию всех моделей
   (для ключей MetaField фильтр `SyntaxFacts.IsValidIdentifier` уже есть).
6. Непрозрачные ошибки компиляции: `CSharpScript.CreateDelegate()` бросает
   `CompilationErrorException`, диагностика Roslyn (`e.Diagnostics`) не логируется.
7. `throw new Exception(...)` в `MtFieldInfo` при неразрешённой relation (+ опечатка —
   незакрытая кавычка в сообщении): один битый MetaField роняет все модели.
8. Sync-over-async: `MetaModelTypesLocator.UpdateMetaModelMtoRuntimeCompiledTypes`
   (`.GetAwaiter().GetResult()`), TODO про инвалидацию кеша при изменении PostType
   (территория Cms.Host, смежная).
9. `CSharpScript` утекает: каждая `InvalidateCompiledMetaMtoModels()` оставляет
   невыгружаемую сборку (см. «Слой компиляции» ниже).

### C. Мелочи

10. Опечатка `PrepateData` → `PrepareData`.
11. Дублирование подготовки dict+resolver в `GenerateMetaTypes`/`GenerateMetaTypesSourceCode`.
12. `DateTime.Now` в заголовке генерируемого кода (недетерминированный вывод).
13. Неочевидные имена: `GenSourceCodeMaster`, `MtFieldInfo`, «Mto» нигде не расшифрован.

## Индустриальный контекст (web research, 2026-09-27)

Подходы к динамическим контент-типам:

1. **Физические таблицы на контент-тип** — Directus, Payload, Strapi: реальная таблица
   на тип, колонка на поле, схема синхронизируется миграциями. Тривиальный SQL,
   констрейнты. Цена: runtime-DDL при каждом изменении поля, миграции данных,
   диалекты под каждого провайдера.
2. **EAV с типизированными колонками** — Optimizely: `tblContentProperty` с колонками
   `String/LongString/Number/FloatNumber/Boolean/Date/ContentLink/GUID` — структурно
   то же, что `MetaValueBase` в Mars. Зрелая коммерческая CMS, 20 лет в проде.
   Цена: N полей → N подзапросов при чтении (наша боль «огромного SQL» присуща EAV).
3. **JSON-документ** — Strapi v5 documents, EF Core 8–10 owned/complex types с
   `ToJson()`: LINQ транслируется в `->>`/`@>`, на PG — jsonb + GIN/выраженческие
   индексы. Ограничение для Mars: полноценно поддержано Npgsql и SQL Server
   (`nvarchar(max)`, индексы слабее), MySQL-провайдер owned-JSON не умеет.
4. **Слой компиляции — консенсус:** `CSharpScript` непригоден для серверных сценариев
   (~50 МБ на компиляцию, сборка невыгружаемая → OOM при частых перекомпиляциях,
   медленный). Стандарт: `CSharpCompilation.Emit` → collectible `AssemblyLoadContext`
   (`LoadFromStream`, `[MethodImpl(NoInlining)]`, `Unload()` + верификация через
   `WeakReference`) + кеш скомпилированного по хешу исходника (паттерн Westwind.Scripting).
   Грабли: не кешировать `Type`/делегаты из ALC в долгоживущие поля; `MetadataReference`
   переиспользовать, но держать вне per-script ALC.

Вывод: хранение Mars ≈ хранение Optimizely — это индустриальная норма, не антипаттерн.
«Огромный SQL» — свойство EAV-чтения, а не дефект генератора.

## Направления переработки

### A. Та же архитектура, честная реализация (без изменений БД)

- **FK-константы вместо join'а:** генератор пишет `f.MetaField.Key == "price"` —
  join к MetaFields внутри каждого коррелированного подзапроса. FK `MetaFieldId`
  уже есть; подстановка `f.MetaFieldId == <guid-константа>` убирает join из всех
  полей сразу, SQL заметно худеет. (Потребуется инвалидация кеша типов при
  изменении MetaField — guid стабилен, но пересоздание поля меняет его.)
- `CSharpScript` → `CSharpCompilation` + collectible ALC + кеш по хешу исходника;
  внятная диагностика через `EmitResult.Diagnostics`.
- Переименование неочевидных классов, чистка мёртвого кода, фиксы из разделов A–C выше.
- Риск минимальный; потолок производительности EAV остаётся.

### B. Гибрид EAV + JSON-колонка

Источник истины — MetaValues, плюс поддерживаемая при записи jsonb-колонка на посте;
проекции читают `MetaJson->>'price'` — один скан вместо N подзапросов.
Кросс-БД неравномерно (PG отлично, MSSQL терпимо, MySQL деградирует),
синхронизация двух представлений — постоянная статья расходов.

### C. Физические таблицы на PostType (путь Directus/Payload)

SQL тривиален навсегда, но это переписывание ядра данных: runtime-DDL под три
провайдера, миграции при изменении полей, переезд существующих данных, перестройка
QueryLang/нод/админки. Фактически другая архитектура продукта.

## Открытые развилки (решения нет)

1. Боль №1 — размер/скорость SQL в проде (→ B/C) или качество генератора:
   память, имена, хрупкость (→ A)? A — обязательный минимум в любом сценарии.
2. Есть ли замеры: сколько MetaFields в типичном PostType, насколько проседает
   чтение? Возможный первый этап — замерить текущий SQL и план запроса.
3. Объём структурных правок: минимальный (константа в Cms.Abstractions + разрыв
   ссылки QueryLang.Host) vs переименование в `Mars.MetaModelGenerator.Host`
   vs полный сплит.
4. Политика отказоустойчивости при битых PostType/MetaField: пропуск с warning
   vs fail fast с диагностикой Roslyn.
5. Переименование `Test.Mars.MetaModelGenerator` → `Mars.MetaModelGenerator.Tests`.

## Источники

- https://dba.stackexchange.com/questions/334557/eav-or-jsonb-column-for-dynamic-data-that-needs-good-indexing
- https://coussej.github.io/2016-01-14/Replacing-EAV-with-JSONB-in-PostgreSQL/
- https://digitalpixie.co.uk/episerver-cms-11-useful-sql-queries-1
- https://docs.developers.optimizely.com/content-management-system/docs/dynamic-data-store
- https://payloadcms.com/docs/database/postgres
- https://payloadcms.com/docs/database/overview
- https://docs.strapi.io/cms/database-migrations
- https://directus.com/docs/guides/data-model/collections
- https://www.npgsql.org/efcore/mapping/json.html
- https://trailheadtechnology.com/ef-core-10-turns-postgresql-into-a-hybrid-relational-document-db/
- https://www.roji.org/efcore-pg-advanced-json
- https://carljohansen.wordpress.com/2020-05-09/compiling-expression-trees-with-roslyn-without-memory-leaks-2/
- https://www.strathweb.com/2019/01/collectible-assemblies-in-net-core-3-0/
- https://weblog.west-wind.com/posts/2022/Jun/07/Runtime-C-Code-Compilation-Revisited-for-Roslyn
- https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability
- https://github.com/dotnet/roslyn/issues/22219
- https://github.com/dotnet/roslyn/issues/49282
