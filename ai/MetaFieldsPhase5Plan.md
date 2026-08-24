# План мета-полей — Фаза 5: уникальность значений

> **Статус: выполнена 2026-08-24 (ред. 3 — по обкатке дизайна: без енама
> владельца, доступ к домену через провайдеры в keyed-DI-конвенции проекта,
> делегаты на ValueTask).**
> **Итог реализации**: валидатор `unique` в реестре `MetaFieldValueValidators`
> (делегат `ValueTask` с контекстом `MetaValueValidationContext`: ключ модели
> владельца, ид сохраняемого объекта, поле); проверка уникальности —
> `IMetaValueUniquenessProvider`, провайдеры доменов в keyed-DI
> (`MetaValueOwnerCatalog`: `Post`/`PostCategory`/`User`; по файлу в
> `Mars.Host/Handlers/` как провайдеры моделей связей, общая колоночная
> проверка — `MetaValueUniquenessTool`); `MetaValuesValidator` резолвит
> провайдер по ключу модели, провайдер не зарегистрирован — правило мягко
> пропускается; `IMetaValuesValidator` → `ValidateAsync`/`ValidateJsonAsync`;
> `MetaValuesValidationExtensions` — `CustomAsync` + модель владельца +
> селектор ид (все 6 валидаторов запросов, 2 валидатора JSON-записи,
> генераторы); каталог `MetaFieldValidatorCatalog` += `unique` и
> `For(MetaFieldType)`; форма поля `FormMetaField`: блок «Валидация» по
> `For(type)`, селект по отфильтрованному каталогу, снятие недоступных
> правил при смене типа поля. Без миграций (правило в `Options.validators`).
> Точечные тесты зелёные: юнит `Test.Mars.Host` валидатор/генераторы/
> валидаторы типов 37/37; интеграционные `PostUniqueValidatorTests` 2/2
> (дубль при создании → 400; своё значение при обновлении → 200; чужое → 400).
> Ветка `ai/metafields-rework`. Закрывает исходный план редизайна
> `ai/MetaFieldsPhase3Plan.md` (Фазы 1–4 выполнены; продолжение
> `ai/MetaFieldsPhase4Plan.md`).

## Контекст

На выходе Фазы 4: реестр валидаторов значений `MetaFieldValueValidators`
(Mars.Host.Shared: делегат `IEnumerable<string> Validator(object? value,
JsonObject? parameters)` БЕЗ контекста, встроенные `regex` и `length`,
расширение `Register`), каталог для UI `MetaFieldValidatorCatalog`
(Mars.Shared), правила в `Options.validators` (массив `{type, params}`),
вызов — `MetaValuesValidator` (scoped, `Validate`/`ValidateJson`) на всех
путях записи: посты, категории, пользователи (через
`MetaValuesValidationExtensions` во FluentValidation-валидаторах запросов),
JSON-API постов (`Create/UpdatePostJsonQueryValidator`), генераторы
(`MetaValuesGeneratorService.ApplyAsync`). Значения постов — типизированные
колонки таблицы `post_meta_values` (`MetaValueBase`: `string_short`,
`string_text`, `int`, `long`, `float`, `decimal`, `date_time`, …).

Задача Фазы 5 из плана Фазы 3: правило уникальности значения мета-поля.
Исходная формулировка предполагала динамический уникальный индекс; по
обсуждению 2026-08-24 скоуп радикально упрощён.

## Принятые решения (2026-08-24)

1. **Только проверка в валидаторе при сохранении. Без вмешательства в БД**
   (решение пользователя): никаких динамических индексов и DDL. Уникальность —
   пользовательская конструкция уровня валидатора, «как регулярка».
   Следствие: гонки параллельных записей и путь `RegenerateAsync` (пишет
   значения без валидации) остаются без гарантии — принимается.
2. **Скоуп — все владельцы значений** (ред. 2): правило доступно мета-полям
   постов, категорий и пользователей; проверка идёт по своей таблице
   (`post_meta_values` / `post_category_meta_values` / `user_meta_values`).
   Маркер поля `meta_field_id` уже привязан к типу владельца — скоуп
   «в пределах типа» получается автоматически.
3. **Правило `unique`** — обычный валидатор реестра: запись
   `{ "type": "unique" }` в `Options.validators`, в каталоге — «Уникальное
   значение», в форме поля — строка в списке валидаторов (как регулярка).
   Параметров нет; сообщение фиксированное: «значение уже занято».
4. **Контракт валидаторов расширяется контекстом** (поле, владелец, доступ
   к проверке): делегат — `ValueTask<IEnumerable<string>> Validator(object?
   value, JsonObject? parameters, MetaValueValidationContext context,
   CancellationToken ct)`. `MetaValueValidationContext` (Mars.Host.Shared):
   `ModelName` (ключ модели владельца — строка, не енам), `OwnerId` (ид
   сохраняемого объекта при обновлении — для исключения себя), `Field` и
   `UniquenessProvider` (заполняются валидатором).
5. **Проверка — провайдеры доменов в keyed-DI** (ред. 3; паттерн
   провайдеров моделей связей и генераторов): интерфейс
   `IMetaValueUniquenessProvider` (Mars.Host.Shared):
   `IsOccupiedAsync(field, value, excludeOwnerId, ct)` → `ValueTask<bool>`;
   реализации по доменам `Post`/`PostCategory`/`User` — файлы в
   `Mars.Host/Handlers/`, регистрация `AddKeyedScoped` под ключами
   `MetaValueOwnerCatalog`; `MetaValuesValidator` резолвит провайдер по
   `ModelName` контекста; провайдер не зарегистрирован — правило мягко
   пропускается. Пустые значения не проверяются. Неподдерживаемый тип /
   неожиданный тип значения → «не занято» (не блокировать).
6. **Поддерживаемые типы полей** — одиночные скаляры со своей колонкой:
   String, Text, Int, Long, Float, Decimal, DateTime. Bool не предлагается
   (бессмысленно), Select — пока нет (денормализация ключа варианта в
   `string_short` — отдельное решение), Relation/File/Image/SelectMany —
   мульти-значения, Query — без значения.
7. **Валидация становится асинхронной по всей цепочке**: `IMetaValuesValidator`
   → `ValidateAsync`/`ValidateJsonAsync` (+ контекст владельца);
   `MetaValuesValidationExtensions` → `CustomAsync` (+ модель владельца и
   селектор ид владельца из запроса); обновляются 6 валидаторов запросов
   (посты/категории/пользователи, создание/обновление), 2 валидатора
   JSON-записи постов (модель `Post`, ид из запроса; у `CreatePostJsonQuery`
   ид тоже передаётся — он там nullable), генераторы (модель `Post`, ид нет).
8. **Каталог фильтруется по типу поля**:
   `MetaFieldValidatorCatalog.For(MetaFieldType)` — String/Text:
   regex+length+unique; скалярные нестроковые: только unique; остальное —
   пусто. Ключи доменов значений — `MetaValueOwnerCatalog`
   (Mars.Host.Shared): `Post`/`PostCategory`/`User` (строки, как ключи
   провайдеров моделей связей).
9. **Без миграций**: правило в существующем jsonb `Options.validators`.
   Контракты запросов/ответов не меняются.

## Шаги

### Шаг 1 — контракты (Mars.Shared, Mars.Host.Shared)

- `MetaFieldValidatorCatalog`: `Unique = "unique"` в `All`;
  `For(MetaFieldType type)` — фильтрация по п. 6/8.
- `MetaValueOwnerCatalog` (Mars.Host.Shared/Services): ключи доменов
  `Post`/`PostCategory`/`User`.
- `IMetaValueUniquenessProvider` (Mars.Host.Shared/Services).
- `MetaValueValidationContext` (Mars.Host.Shared/Dto/MetaFields).
- `MetaFieldValueValidators`: делегат `ValueTask` с контекстом;
  `ValidateAsync(rule, value, context, ct)`; встроенные `regex`/`length` —
  синхронная логика в `ValueTask.FromResult`; обработчик `unique`
  (пустое значение → нет ошибок; иначе — `context.UniquenessProvider`).
- `IMetaValuesValidator`: `ValidateAsync(values, context, ct)`,
  `ValidateJsonAsync(fields, meta, requireAll, contentFieldKey, context, ct)`.
- `MetaValuesValidationExtensions`: `ValidateMetaValues<T>(validator,
  ownerModel, Func<T, Guid?>? ownerIdSelector = null)` на `CustomAsync`.
- Валидаторы запросов: посты — `Post` + ид (`UpdatePostQuery.Id`,
  у создания — `CreatePostQuery.Id`); категории — `PostCategory`;
  пользователи — `User` (ид соответствующих Update-запросов).
- `Create/UpdatePostJsonQueryValidator`: `CustomAsync` + `ValidateJsonAsync`
  (модель `Post`, ид запроса).

### Шаг 2 — сервер (Mars.Host)

- `MetaValuesValidator`: конструктор с `IServiceProvider`; резолвит
  `IMetaValueUniquenessProvider` по ключу модели контекста
  (`GetKeyedService`); контекст владельца от вызывающего + `Field` и
  `UniquenessProvider` повалидно.
- Провайдеры доменов (по файлу в `Mars.Host/Handlers/`, как провайдеры
  моделей связей): `PostMetaValueUniquenessProvider`,
  `PostCategoryMetaValueUniquenessProvider`, `UserMetaValueUniquenessProvider`
  — таблица значений домена, `Any(...)` по колонке типа через общий
  `MetaValueUniquenessTool.CheckAsync` (`String` → `StringShort`, `Text` →
  `StringText`, `Int`/`Long`/`Float`/`Decimal`/`DateTime` — свои),
  `meta_field_id` + исключение ид сохраняемого владельца.
- DI рядом с `IMetaValuesValidator` (MainMarsHost.cs):
  `AddKeyedScoped` под ключами `MetaValueOwnerCatalog`.
- `MetaValuesGeneratorService.ApplyAsync`: `ValidateAsync(generated,
  контекст{Post, ид нет}, ct)`.

### Шаг 3 — фронт (AppFront.Main)

- `FormMetaField`: блок «Валидация» рендерится, если `For(field.Type)`
  непуст (вместо `field.IsString`); селект строки — по отфильтрованному
  каталогу; «Добавить правило» — строка с первым доступным типом; строка
  `unique` — без дополнительных вводов. При смене типа поля — снимаем
  ставшие недоступными правила. Страницы типов не меняются (правило
  доступно всем владельцам).
- `MetaFieldEditModel`: `SyncValidatorsToOptions` — для `unique` пустой
  `params` (как сейчас для всех, у кого нет своих параметров); чтение без
  изменений.

## Верификация (правило: точечно, не весь сьют)

- сборка `Mars.slnx`;
- юнит `Test.Mars.Host`: `MetaValuesValidatorTests` — переход на
  `ValidateAsync` + новые кейсы `unique` (занято / свободно / пустое
  значение / ид владельца в провайдере / домен без провайдера мягко
  пропускается / домен категорий через свой провайдер — с фейковым
  `IMetaValueUniquenessProvider` и подменой `IKeyedServiceProvider`);
- интеграционные точечно: посты — дубль значения при создании второго поста
  → 400; обновление поста со своим же значением → ок; дубль при обновлении
  → 400 (валидатор `unique` на поле типа);
- контракты `WebApiClient` не меняются (правило в существующем
  `Options.validators`) — прогон не требуется;
- админка — визуально при разработке.

## Вне скоупа

- динамический уникальный индекс и любое DDL (отклонено 2026-08-24 как
  избыточное для пользовательской конструкции; при потребности вернуться);
- чувствительность к регистру (сравнение точное, как хранится);
- уникальность Select (денормализованный ключ варианта), мульти-значений;
- гарантия от гонок параллельной записи и на пути `RegenerateAsync`;
- кастомное сообщение правила в UI.
