# План: Mars.Forms — общий механизм форм и заполнения данных

> **Статус: спроектировано 2026-09-09, не реализовано.**
> Инициатива выросла из задачи «при редактировании поста все поля — настраиваемые»
> (см. [MetaFieldsGuide.md](./MetaFieldsGuide.md), «Вектор развития») и расширена
> до платформенного механизма: одна форма определения и один транспорт значений
> для поста, нод визуального редактора, автономных виджетов и внешних SQL-таблиц.
> Первый потребитель — форма редактирования поста.

## Принятые решения (2026-09-09)

1. **Метаполе остаётся полем данных.** Системные поля поста (title, slug, status,
   даты, теги, категории, excerpt, язык, автор) **не материализуются** строками в
   `meta_fields`: порядок, видимость, структура формы и правила для них даёт
   отдельный слой. Метаполе в этом слое — лишь один из производителей дескрипторов.
2. **Новый модуль `Mars.Forms`** (`.Contracts` + `.Abstractions` + `.Front`),
   плоско в `src/Mars.Modules`, виртуальная папка в `Mars.slnx`, подключение
   `AddForms()`/`UseForms()` в `Mars.WebApp`. Общий знаменатель для Cms, Nodes и
   Datasource — `Mars.Contracts`, поэтому модуль не создаёт циклов.
3. **Форма — дерево контейнеров двух видов**: `field` (лист, ссылка на поле по
   ключу + дескриптор) и `section` (чистый layout: заголовок, дети, свёрнутость;
   значения и строки хранения у него нет). Единый упорядоченный список, у
   корневых элементов — атрибут `zone`.
4. **Дескриптор отдаёт провайдер**, а не общий статический каталог: рендерер
   доменно-независим, а источники (метаполя, системные поля, свойства нод,
   колонки внешних таблиц) резолвятся у себя дома.
5. **Хранение определения — у владельца.** Пост: `post_types.Options["form"]`
   (jsonb уже есть, миграция не нужна, ключ — `PostTypeOptionsCatalog.Form`).
   Автономные формы: своя таблица `forms` (фаза 4).
6. **Значения — универсальный JSON-мешок** `Dictionary<string, JsonNode?>`
   с каноническими кодировками по типу и кодеком (раздел 3.3).
7. **Наборы валидаторов, редакторов и видов контейнеров — свои у провайдера**:
   манифест провайдера объявляет зоны, поддерживаемые правила, редакторы и
   контейнеры; дизайнер формы показывает только их.
8. **Пост — первый потребитель.** Транспорт записи поста **не меняется**:
   `PostEditModel` сам раскладывает мешок по типизированным свойствам и
   собирает существующие `CreatePostRequest`/`UpdatePostRequest`. В прочих
   (динамических) местах потребитель принимает контейнер формы и делает с ним
   что нужно сам.
9. **Правила системных полей действуют на всех путях записи** (не только в
   админ-форме) — иначе их можно обойти через API.
10. **Фича типа = выключатель слота**: лист системного поля существует, пока
    включена соответствующая фича (`PostTypeConstants.Features`).
11. **Охват первой итерации — только посты**; users и post categories
    подключаются тем же механизмом позже (через `MetaValueOwnerCatalog`).
12. **Ноды не трогаем.** Существующие типизированные ручные формы нод остаются
    как есть; механизм применяется к **динамическим нодам** и к **нодам
    создания сущностей** — это будущие этапы.
13. **Общий эндпоинт отправки формы появится позже** — для динамических форм;
    на прототипе провайдеры вызываются путями своих модулей.
14. **Раскладка и визуал заранее не проектируются** — корректируются, когда
    будет рабочий прототип.

---

## 1. Как сейчас (as-is)

| Область | Состояние | Что это даёт механизму |
|---|---|---|
| Форма поста | `src/Mars.Admin/Pages/PostsViews/EditPostView.razor` — хардкод-разметка в фиксированном порядке, гейты `context.FeatureActivated(...)`; зоны заданы фрагментами `StandardEditContainer`: `ChildContent`, `SectionSidePublish`, `SectionExtraSidebar` | Зоны уже существуют как контракт контейнера — дерево должно ложиться на них 1:1 |
| Метаполя в форме | `Mars.Admin.Framework/Components/MetaFieldViews/FormMetaValueItems.razor` — свой фильтр (`!Disabled`, `!= Query`, `!Hidden`, исключён контент) и единственная сортировка `OrderBy(s => s.Order)`; сервер порядок не отдаёт | Рендер по дереву заменяет и фильтр, и сортировку |
| Системные поля | Дескрипторов нет. Четыре независимых хардкод-списка: `PostTypeGridConstants.BaseColumns` + `KnownColumn` в `PostTypeGridSettingsEditor.razor.cs`, `GridColumn`/`GridColumnKind` в `ManagePostView.razor.cs`, `PostTypeViewService.BuildColumns`, список базовых полей в AI-хендлере `EditPostView.razor.cs`. Алгоритм слияния порядка продублирован (`RebuildFrom` / `RebuildColumns`) | Один каталог слотов + один нормализатор вместо четырёх списков и двух копий слияния |
| Валидация базовых полей | `GeneralPostQueryValidator` по `IGeneralPostQuery` (Title, Type, Slug, Tags, Status, UserId, Excerpt, Content, LangCode, CategoryIds); уже резолвит тип поста и применяет фича-гейты статусов/категорий. **Уникальность slug не проверяется нигде**, кроме подбора суффикса в `PostService.GetOrCreateSingleAsync` | Готовая точка подключения правил; дыра уникальности закрывается правилом `unique` |
| Правила метаполей | `MetaFieldValueValidators` — делегат `(object? value, JsonObject? params, MetaValueValidationContext, ct)`, открытый статический `Register`; встроенные regex/length/unique | Реестр value-agnostic — переносится в общий слой адаптером, не переписывается |
| Редакторы | Каталог ключей `MetaFieldEditorCatalog` (сервер) + `IMetaFieldEditorLocator`/`MetaFieldEditors.Register` (фронт), рендер `DynamicComponent` в `RowMetaValue` | Тот же паттерн становится общим `IFormEditorLocator` |
| Ноды | POCO со zwyклыми свойствами + DataAnnotations (`[Required]`, `[Display]`); ~50 ручных razor-форм в `Mars.Nodes.FormEditor/EditForms`, привязка `[NodeEditFormForNode]`, поиск через `INodeFormsLocator`, рендер `DynamicComponent` в `EditForm` + `ObjectGraphDataAnnotationsValidator`; хранение — весь граф одним JSON `nodes/flows.json` (`NodeService`) | Ручные типизированные формы **остаются**; механизм нужен динамическим нодам и нодам создания сущностей |
| Внешние SQL | `IDatasourceDriver`: `Columns(table) → Dictionary<string, QTableColumn>` (CLR-тип, `IsKey`, `IsAutoIncrement`, `IsUnique`, `ColumnSize`), `Tables()`, `DatabaseStructure()`, `SqlQuery`, `SqlNonQuery`. `SqlNode` только читает | Готовый источник дескрипторов и путь записи |
| XActions | `XActionArgument {Name, Label, Type(String/Number/Bool/Choice), Required, DefaultValue, Options, OptionsSource}` + `XActionFormDialog.razor` — единственный работающий генератор «схема → форма» | Доказательство паттерна; позже частный случай общего механизма |
| Options | Атрибут → razor-форма (`OptionEditFormForOptionAttribute`, `IOptionsFormsLocator`) | Тот же паттерн «ручная форма по типу», потенциальный потребитель |

---

## 2. Референсы (проверено по документации продуктов)

- **Payload**: `Collapsible` — «presentational-only and only affects the Admin
  Panel»; `Tabs` — то же, **но** именованный таб группирует данные в объект.
  То есть layout и хранение разведены явно — берём это разделение.
- **ACF**: `Tab`/`Accordion` значения не хранят и группируют все поля, идущие
  после, до следующего таба; вложенность данных делает только `Group` (у нас
  удалён сознательно — решение №2 гайда метаполей).
- **Strapi**: «Configure the view» — per content-type drag-and-drop порядка
  полей (включая реляционные) без контейнеров; настройки видa хранятся на типе.

Вывод: контейнер-секция — layout-сущность без значения и без вложенности
хранения; порядок и видимость настраиваются на тип; форма конфигурируется
диалогом, а не правкой модели данных.

---

## 3. Целевая модель

### 3.1 Контракты

```
Mars.Forms.Contracts        (WASM-чистый, ссылается на Mars.Contracts)
  FormDefinition            OwnerModel, Items, Manifest
  FormItem                  Kind(field|section), Key, Zone?, Title?, Visible,
                            Width?, Collapsed?, Items[], Field?, Rules[], Editor?
  FormFieldDescriptor       Key, Title, FormFieldType, Required, ReadOnly, Multiple,
                            Description?, Editor?, Choices[], ModelName?, Min?, Max?,
                            Default?, Options(JsonNode?)
  FormFieldType             String, Text, Bool, Int, Long, Float, Decimal, DateTime,
                            Select, SelectMany, Relation, File, Image, Object, Computed
  FormRuleDefinition        { Type, Params(JsonObject?) }   — та же форма, что у метаполей
  FormValues                OwnerModel, OwnerId?, Dictionary<string, JsonNode?>
  FormSubmitResult          Ok, Errors[FormError(Key, Message)], Id?
  FormProviderManifest      см. 3.2
  FormValueCodec            канонические кодировки + конверсия (см. 3.3)

Mars.Forms.Abstractions     (серверные контракты)
  IFormDataProvider         GetFormAsync / ReadAsync / SubmitAsync
  IFormDataProviderLocator  keyed-DI по OwnerModel (паттерн IMetaRelationModelProviderHandler)
  IFormRuleRegistry         Register(scope, type, handler) + встроенные required/regex/length/min/max
  IFormValidator            проверка payload по определению (форма значения + правила)
  IFormDefinitionNormalizer общий merge: сохранённый порядок → отброс неизвестных →
                            дописывание недостающих в конец (один на грид и форму)

Mars.Forms.Front            (Blazor-рендерер)
  FormRenderer              обход дерева по зонам
  FormFieldRow              DynamicComponent через IFormEditorLocator
  IFormEditorLocator        (editorKey, FormFieldType, ownerModel) → компонент, открытый Register
  IFormContainerLocator     kind → компонент контейнера (field/section + свои у провайдера)
  примитивные редакторы     input, textarea, number, bool, date, select
```

### 3.2 Манифест провайдера — свои наборы у каждого

```
FormProviderManifest
  OwnerModel, Title
  Zones           [{ Key, Title }]           // пост: main/publish/extra; ноды и виджет: одна
  ContainerKinds  ["field", "section"]       // провайдер может добавить свои
  RuleTypes       ["required","regex","length","unique", …]
  EditorKeys      ["core.input.url", "core.wysiwyg.quilljs", …]
  Capabilities    CanReorder, CanAddSections, CanHide, CanEditRules, CanAddFields
```

Реестры остаются глобальными по реализациям, но ** скоупятся по владельцу**:
`Register(ownerModel, type, handler)` для своих правил/редакторов провайдера
плюс глобальные для общих. Каталог, который видит дизайнер формы, =
(глобальные ∩ манифест) ∪ свои провайдера. Это же решает «у разных провайдеров
разные наборы валидаторов и контейнеров отрисовки».

CMS-адаптер: существующие `MetaFieldValueValidators` и `MetaFieldEditorCatalog`
не переписываются, а регистрируются в общих реестрах для скоупа `post.*`
(паттерн открытого статического `Register` уже есть в обоих).

### 3.3 Значения: JSON-мешок против «универсальных полей»

Взвешивались два варианта.

| | JSON-мешок + канонические кодировки | Универсальные носители (слоты как `MetaValueBase`) |
|---|---|---|
| Композитные значения (свойства нод, JSON-колонки, виджеты) | естественно | не влезают, нужен JSON-слот-escape-hatch |
| Типизация на проводе | по дескриптору, проверяется кодеком | строгая |
| Прецедент в репо | `PostJsonService.Meta: Dictionary<string, JsonNode>` | `MetaValueBase` (9 типизированных колонок + `Index`) |
| Расширение без смены контракта | да | нет |
| Риск | точность decimal, формат дат | два способа выразить значение |

**Выбран JSON-мешок**, а «универсальные поля» реализуются как **универсальная
система типов** — фиксированная каноническая кодировка каждого `FormFieldType`
и один кодек на клиент и сервер:

| FormFieldType | JSON на проводе | CLR после кодека |
|---|---|---|
| String, Text | строка | `string` |
| Bool | `true`/`false` | `bool` |
| Int, Long | целое число | `long` |
| Float | число | `double` |
| Decimal | **строка** (инвариантная культура) | `decimal` — без потери точности в JS |
| DateTime | ISO-8601 строка со смещением | `DateTimeOffset` |
| Select | строка = **ключ варианта** (не заголовок) | `string` |
| SelectMany | массив строк-ключей | `string[]` |
| Relation, File, Image | строка-Guid | `Guid` |
| Object | объект/массив произвольной формы | `JsonNode` |
| Computed | не передаётся (резолвится на чтении) | — |
| `Multiple` у любого типа | массив значений, порядок = индекс | список |

Правила мешка: ключ отсутствует = значение не задано (сентинелов нет, как в
`meta_values`); `null` допустим только для необязательных полей; пустая строка
не равна отсутствию. `FormValueCodec` — единственное место, где живут форматы:
если decimal/даты поедут, правится кодек, а не провайдеры.

### 3.4 Хранение определений

- **Пост**: `post_types.Options["form"]`, ключ `PostTypeOptionsCatalog.Form`
  (каталог сейчас пуст), сериализация `PostTypeFormSettingsJson` — по образцу
  `PostTypeGridSettingsJson` (camelCase, битый json → null). Миграция не нужна.
- **Автономные формы (фаза 4)**: `FormEntity` / таблица `forms` в общем
  `MarsDbContext` — `Id, Key (уникален), Title, OwnerModel?, Definition (jsonb),
  Disabled, Tags, CreatedAt, ModifiedAt`; FK — только во fluent-конфигурациях
  (решение №4 гайда метаполей). Куда пишутся значения — решает обработчик,
  привязанный к форме (XAction, поток нод, письмо, свой провайдер).

### 3.5 Нормализация дерева

Сервер отдаёт **эффективное** дерево: все доступные поля ровно один раз,
неизвестные ключи отброшены, недостающие дописаны в конец видимыми,
`section` — один уровень вложенности. Алгоритм тот же, что уже дважды написан
для грида (`RebuildFrom`, `RebuildColumns`), поэтому извлекается в
`IFormDefinitionNormalizer` и переиспользуется гридом. Дефолтное дерево поста =
сегодняшний хардкод-порядок, так что фаза рендера ничего не меняет визуально до
первого сохранения настроек. Требование: нормализатор идемпотентен — сохранение
без правок не меняет порядок и не шумит в jsonb.

---

## 4. Фазы

### Фаза 0 — общий слой (без потребителей)

- Три проекта `Mars.Forms.Contracts` / `.Abstractions` / `.Front` (плоско в
  `src/Mars.Modules`, виртуальная папка в `Mars.slnx`), `AddForms()`/`UseForms()`.
- Контракты из 3.1, кодек из 3.3, манифест из 3.2, нормализатор из 3.5,
  встроенные правила (required/regex/length/min/max).
- Тесты `tests/Mars.Forms.Tests`: кодек (прямой/обратный по всем типам, decimal
  и даты), нормализатор (идемпотентность, дубликаты, неизвестные ключи,
  недостающие, выключенные), валидатор правил.

### Фаза 1 — пост как референс-провайдер

- `Mars.Cms.Contracts`: `SystemFieldsCatalog` (ключи `title, slug, status,
  created_at, modified_at, author, tags, categories, excerpt, lang`; на слот —
  заголовок, `FormFieldType`, фича-гейт, ReadOnly, Multiple, InGrid, InForm,
  имя колонки) и маппер `MetaFieldDetailBase → FormFieldDescriptor`.
- Хранение и контракт: `PostTypeOptionsCatalog.Form`, `PostTypeFormSettingsJson`,
  поле `Form` в `PostTypeRequest`/`PostTypeResponse`/`PostTypeDetailResponse`
  и в `PostTypeEditModel`.
- `Mars.Cms.Host`: построитель эффективного дерева (фича-гейты + метаполя типа +
  сохранённый порядок) и манифест провайдера (зоны `main`/`publish`/`extra`).
- Админка: `EditPostView.razor` рендерится по дереву; `PostEditModel` держит
  адаптеры значений (системные слоты → типизированные свойства, мета-слоты →
  `MetaValuesByIndex`, который наконец получает потребителя); контент — обычный
  лист дерева, его тяжёлые редакторы регистрируются в pull-контракте
  `IHeavyMetaValueEditor`, и спец-кейс уходит из `FormMetaValueItems.razor` и
  из `BeforeSave`; `ToCreateRequest`/`ToUpdateRequest` не меняются.
- Консолидация четырёх дублей базовых колонок на `SystemFieldsCatalog` и замена
  двух копий слияния на общий нормализатор.

### Фаза 2 — правила системных полей

- Правила из дерева применяются на всех путях записи поста:
  `GeneralPostQueryValidator` (Create/Update) и JSON-путь `PostJsonService`.
- Правило `unique` для slug через `IPostRepository.ExistAsync(typeName, slug)` —
  сравнение по `lower()`, как требуют индексы `posts.slug`. Закрывает
  существующую дыру (сейчас уникальность slug не проверяется).
- DataAnnotations на транспорте (`[Required] Title`, `[StringLength]`) остаются
  полом; пер-тип правила добавляются сверху, а не заменяют их.

### Фаза 3 — дизайнер формы в админке

- Диалог по образцу `PostTypeGridSettingsEditor`: drag-drop (`FluentSortableList`
  уже используется для мульти-значений), три зоны, секции, видимость, ширина,
  редактор правил системного поля; доступные правила/редакторы/контейнеры —
  только из манифеста провайдера.
- Хосты: страница типа и быстрый вход из `SectionActions` формы поста.
- Раскладка и визуал (`Width`, вид секций, плотность) заранее не
  проектируются — корректируются по рабочему прототипу.

### Фаза 4 — автономные формы (виджеты)

- `FormEntity` + миграция, CRUD в админке, провайдер `form.<key>`: определение
  из таблицы, значения — в привязанный обработчик. Рендер сначала в админке,
  позже на публичном фронте (контракты остаются WASM-чистыми).
- Общий эндпоинт отправки (`POST api/Form/{owner}/submit`) появляется здесь или
  позже: до этого провайдеры вызываются путями своих модулей.

### Фаза 5 — динамические ноды и ноды создания сущностей (будущий этап)

- **Существующие типизированные формы нод не трогаем**: `[NodeEditFormForNode]`
  + `INodeFormsLocator` продолжают работать, перевод ~50 ручных форм на схему
  не планируется.
- Механизм применяется там, где форма определяется данными, а не
  скомпилированным типом: **динамические ноды** (дескрипторы — из определения
  ноды) и **ноды создания сущностей** — для них источник дескрипторов это форма
  целевого типа, то есть провайдер `post.<typeName>`: нода заполняет те же поля
  и проходит ту же валидацию, что и админ-форма.
- Запись — в `nodes/flows.json` через `NodeService`; `Mars.Nodes.FormEditor`
  ссылается на `Mars.Forms.Front`.

### Фаза 6 — внешние SQL-таблицы

- **Предусловие (блокер)**: `IDatasourceDriver.SqlNonQuery(string sql)` не
  принимает параметров — форма, пишущая в таблицу конкатенированным SQL, это
  инъекция. Сначала параметризованный non-query в контракте и во всех трёх
  драйверах (PostgreSQL/MsSQL/MySQL), отдельной задачей.
- Провайдер `sql.<slug>.<table>`: дескрипторы из `QTableColumn` (CLR-тип →
  `FormFieldType`, `IsKey`/`IsAutoIncrement` → readonly/hidden, `ColumnSize` →
  правило length, `IsUnique` → правило unique); чтение `SqlQuery`, запись
  параметризованным INSERT/UPDATE.

---

## 5. Границы — не делать без пересмотра

- Не материализовать системные поля строками в `meta_fields`.
- Не дублировать системные значения в `PostDetailResponse.MetaValues` —
  типизированные свойства остаются source of truth в API.
- Не переводить запись поста на мешок: API, `Mars.WebApiClient` и нода
  `AppEntityCreateNode` не должны сломаться.
- Не делать `section` полем данных: ни значений, ни строк хранения.
- Не вводить tabs/accordion в первой итерации — добавим как вид контейнера в
  манифесте провайдера при потребности.
- Не переносить CMS-каталоги (редакторы, валидаторы, генераторы) в общий слой —
  только адаптеры и регистрация.
- Не трогать forms-подобные механизмы users и post categories в этой итерации.
- Не переводить существующие типизированные формы нод на схему.
- Не вводить общий submit-эндпоинт до появления динамических форм.
- Не вылизывать раскладку и визуал до рабочего прототипа.
- Не начинать SQL-форму до параметризации `SqlNonQuery`.

## 6. Риски и грабли

- **Контент**: на сервере ~15 спец-кейсов ключа `content` (`PostService.
  StripContentFieldValue`, `MetaValuesEnricher`, `MetaValuesValidator`,
  генераторы, `PostMetaColumnsService`, `PostJsonService`,
  `GetOrCreateSingleAsync`). Фаза 1 трогает только клиентские пять — серверные
  остаются, пока значение контента живёт в колонке `posts.Content`.
- **Тяжёлые редакторы контента**: сейчас `BeforeSave` дочитывает значение из
  `@ref` редакторов мимо `PullAsync`. Перенос обязан сохранить порядок (pull до
  сохранения) и поведение Wysiwyg / Monaco / Editor.js. Заодно это место для
  отложенного «ленивая инициализация тяжёлых редакторов».
- **Зоны**: у `StandardEditContainer` три фрагмента; зоны провайдера должны
  совпадать с ними 1:1, иначе сайдбары разъедутся.
- **Одно пространство ключей** у грида и формы (`categories`, `author`,
  `created_at`) — семантика не должна различаться между потребителями.
- **Кодек**: точность decimal и формат дат фиксируются в одном месте; разные
  реализации на клиенте и сервере недопустимы (общий проект `Contracts`).
- **`FluentInputFile`**: при заданном `OnInputFileChange` встроенная обработка
  полностью отключается — в авто-режиме file/image-редакторов делегат не задавать.
- **Автор**: слот `author` появится в дереве, но редактора-пикера пользователя
  в админке может не оказаться — тогда слот в первой итерации read-only.

## 7. Верификация (точечная)

- `dotnet build Mars.slnx`.
- `tests/Mars.Forms.Tests` — кодек, нормализатор, правила (новый проект).
- `tests/Mars.Server.Tests` — пост-валидаторы, настройки типа, grid settings.
- `tests/Mars.WebApiClient.Integration.Tests` — при изменении
  `PostTypeRequest`/`PostTypeResponse` (добавление `Form` аддитивно, но
  контракты проверяем).
- Рендер формы — визуально в админке; при наличии сценария на пост —
  `tests/Mars.E2E.Tests`.
- Фазы 5–6 — тесты затронутых проектов (`Mars.Integration.Tests`,
  `Mars.Datasource.Integration.Tests`; для нод при необходимости новый
  `tests/Mars.Nodes.Tests`).
