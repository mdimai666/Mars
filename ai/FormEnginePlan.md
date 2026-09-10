# План: Mars.Forms — общий механизм форм и заполнения данных

> **Статус: спроектировано 2026-09-09; фазы 0–3 выполнены 2026-09-09;
> этапы A–D (визуал админки и унификация рендера) спланированы 2026-09-10.**
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
    будет рабочий прототип. *(Снято 2026-09-10: прототип рабочий, визуал
    проектируется — решения 15–21 и этапы A–D.)*

## Принятые решения (2026-09-10) — визуал и унификация рендера

15. **Два контура настройки разделены.** Страница редактирования типа правит
    **параметры полей** (системные и метаполя — в разных группах), окно
    редактирования представления (`FormLayoutEditor`) — только **как поля
    отображаются**: порядок, зона, видимость, ширина, секции. Правила и
    переопределение редактора системных слотов уезжают из дизайнера в параметры.
16. **Параметры системных полей — отдельный ключ** `post_types.Options["systemFields"]`
    (`PostTypeOptionsCatalog.SystemFields`), не items раскладки: `Options["form"]`
    пишет страница презентации, `systemFields` — страница типа, поэтому два
    редактора одного jsonb не затирают друг друга. Запись точечная
    (`WithSystemFields`), как `WithFormLayout`. Для уже сохранённых в раскладке
    Rules/Editor — компат-фолбэк в `PostFormBuilder`.
17. **Редактор определений полей становится общим** и к метаполям не привязан:
    title/key/description/type/enabled/hidden/валидаторы/варианты правятся одним
    механизмом по описанию провайдера, а данные метаполей строятся из него.
    Доменные панели настроек типа (relation, file/image, варианты select,
    code-lang, query) — зарегистрированные компоненты.
18. **Отрисовка значений — только FormEngine.** `MetaFieldViews` как стек рендера
    значений ликвидируется; метаполя лишь регистрируют свои редакторы
    (relation, галереи/медиа, WYSIWYG, Monaco, EditorJS, color/url/email/date/time)
    в общем `IFormEditorLocator`. Примитивы уходят встроенным редакторам движка.
19. **Транспорт мета-значений не меняется — адаптер поверх EAV.**
    `MetaValueEditModel` остаётся source of truth (`Id`/`Index`/`MarkForDelete`),
    FormEngine получает абстракцию хранилища `IFormValueStore` с двумя
    реализациями (JSON-мешок и EAV-строки); доменные редакторы берут строки через
    «нативный payload» стора. Мешок как единственный транспорт — для будущих
    динамических провайдеров (фазы 4–6), где легаси-транспорта нет.
20. **Users и post categories переводятся в том же заходе** (провайдеры `user.*`,
    `postcategory.*`) — иначе `MetaFieldViews` не удалить. Решение №11 в части
    «позже» пересмотрено.
21. **Общий редактор определений живёт в `Mars.Admin.Framework/Components/Forms/`**
    (рядом с `FormLayoutEditor`), контракты — в `Mars.Forms.Contracts`:
    `Mars.Forms.Front` остаётся листом (Contracts + FluentUI) и не тянет
    админ-виджеты (`FormItem2`, `InputTags2`, `DFluentDeleteButton`,
    `GroupedSelectDropDown`).

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

### Фаза 0 — общий слой (без потребителей) — выполнено 2026-09-09

- Три проекта `Mars.Forms.Contracts` / `.Abstractions` / `.Front` (плоско в
  `src/Mars.Modules`, виртуальная папка в `Mars.slnx`), `AddMarsForms()` в
  цепочке `MarsWebAppStartup`. Хук `UseMarsForms()` не понадобился: на старте
  нечего регистрировать в пайплайне.
- Контракты из 3.1, кодек из 3.3, манифест из 3.2, нормализатор из 3.5,
  встроенные правила (required/regex/length/min/max).
- Тесты `tests/Mars.Forms.Tests`: кодек (прямой/обратный по всем типам, decimal
  и даты), нормализатор (идемпотентность, дубликаты, неизвестные ключи,
  недостающие, выключенные), валидатор правил, реестры редакторов/контейнеров.

Что добавилось по ходу реализации (уточнение контрактов):

- `FormLayoutSettings` + `FormLayoutJson` — хранимая раскладка отдельно от
  `FormDefinition`: дескрипторы не сохраняются (устаревают), их всегда отдаёт
  провайдер.
- `FormFieldDescriptor.Rules` — правила от источника поля (например length из
  `ColumnSize` внешней таблицы); правила раскладки (`FormItem.Rules`) —
  пользовательские. Валидатор применяет оба набора.
- `FormFieldDescriptor.SettingsOnForm` — признак «правила и редактор живут в
  раскладке» (системные поля) против «на определении поля» (метаполя);
  нормализатор переносит `Rules`/`Editor` только для таких полей.
- `FormFieldDescriptor.ElementType` — тип одного элемента значения
  (`SelectMany` → `Select`).
- `FormError.Index` — индекс значения в множественном поле.
- `IFormEditorLocator.GetDefaultEditor` — встроенный редактор по типу, когда
  явный ключ не задан; `FormEditorCatalog` — ключи встроенных редакторов.
- `Mars.Forms.Front` пока не подключён к `Mars.Admin`: потребителей нет,
  подключение — в фазе 1.

### Фаза 1 — пост как референс-провайдер — выполнено 2026-09-09

- `Mars.Cms.Contracts`: `SystemFieldsCatalog` (ключи `title, slug, status,
  created_at, modified_at, author, tags, categories, excerpt, lang`; на слот —
  заголовок-ключ ресурса, `FormFieldType`, фича-гейт, ReadOnly, Multiple, InGrid,
  зона, ключ редактора) и `PostFormEditors` (ключи доменных редакторов), маппер
  `MetaFieldDto → FormFieldDescriptor` (`MetaFieldFormMapping`).
- Хранение и контракт: `PostTypeOptionsCatalog.Form` + `GetFormLayout`/
  `WithFormLayout`, `PostTypeDetail.Form`, `PostTypeDetailResponse.Form`,
  `PostEditViewModel.Form` (эффективное дерево), `PostFormBuilder` +
  `PostFormProvider` (keyed `post.*`).
- Админка: `EditPostView.razor` рендерится по дереву — по зоне на фрагмент
  `StandardEditContainer` (`PostFormZone`), лист маршрутизирует `PostFormField`
  (контент → свой редактор, метаполе → `FormMetaValueItem`, системный слот →
  `FormFieldRow`); `PostEditModel` держит мешок значений системных слотов
  (`BuildFormValues`/`ApplyFormValues`/`FillFormValues`), транспорт записи не
  изменился; `MetaValuesByIndex` остаётся при мета-значениях.
- Консолидация четырёх дублей базовых колонок — отдельным заходом.

Что добавилось/изменилось по ходу:

- **Контент остаётся своим компонентом** (`PostContentEditor`): пять веток
  редакторов, их `@ref` и мост ИИ-агента переехали в него целиком, а доступ
  страницы — через `PostContentEditorHolder` (контент рендерится внутри дерева,
  в любой зоне). Перевод тяжёлых редакторов контента на общий pull-контракт
  отложен: он ломал бы ИИ-мост без видимой пользы на прототипе.
- Реестр тяжёлых редакторов мета-значений вынесен из `FormMetaValue` в
  `IHeavyMetaValueEditors` — один реестр раздаётся всем зонам формы.
- `FormRenderContext.FieldTemplate` и `TitleResolver` — точки расширения
  рендерера: провайдер подмешивает доменные компоненты и локализует заголовки
  слотов (`FormFieldDescriptor.TitleKey`).
- Провайдер поста объявляет `CanReadValues = false`, `CanSubmit = false`
  (решение B(ii)): значения ходят типизированным транспортом, мешок живёт
  только внутри формы. `ReadAsync`/`SubmitAsync` в интерфейсе получили дефолтные
  реализации с `NotSupportedException` — провайдер не обязан уметь и то и другое.
- Слот `created_at` доступен всегда, а редактируется только с фичей
  `ModifyCreatedDate` (иначе read-only) — как в прежней разметке.
- Тесты: `tests/Mars.Server.Tests/Forms/PostFormBuilderTests.cs` (порядок и зоны
  по умолчанию, фича-гейты, исключения Disabled/Query/Hidden, контент,
  read-only даты, варианты статусов, сохранённая раскладка и правила, манифест).

### Фаза 2 — правила системных полей (выполнено 2026-09-09)

- Правила из дерева применяются на всех путях записи поста:
  `GeneralPostQueryValidator` (Create/Update) и JSON-путь `PostJsonService`
  (он валидирует те же `CreatePostQuery`/`UpdatePostQuery`). Запись поста идёт
  только через `PostService.Create/Update`, а они валидируют запрос — других
  путей нет.
- Правило `unique` для slug через `IPostRepository.SlugOccupiedAsync(typeName,
  slug, exceptId)` — сравнение по `lower()`, как требуют индексы `posts.slug`;
  `exceptId` исключает свой пост при обновлении. Закрывает существующую дыру
  (раньше уникальность slug не проверялась нигде, кроме подбора суффикса в
  `GetOrCreateSingleAsync`). **Включает администратор типа в раскладке формы** —
  по умолчанию правила нет: индексы slug не уникальные, дубли в существующих
  данных допустимы.
- DataAnnotations на транспорте (`[Required] Title`, `[StringLength]`) остаются
  полом; пер-тип правила добавляются сверху, а не заменяют его: серверный
  валидатор снимает с дескрипторов слотов `Required`/`Min`/`Max` и оставляет
  только правила раскладки, поэтому без сохранённых правил поведение записи
  не меняется.

Что добавилось/изменилось по ходу:

- `PostFormRulesValidator` (Cms.Abstractions) — перенос значений системных слотов
  из `IGeneralPostQuery` в мешок `FormValues` кодеком и прогон через общий
  `IFormValidator`; имя свойства в ошибке — свойство транспорта
  (`TransportProperty`: `lang` → `LangCode`, `author` → `UserId`, …).
- Проверяются только слоты, которые несёт транспорт записи: `created_at` и
  `modified_at` в `Create/UpdatePostQuery` не приходят, поэтому правила для них
  на сервере не применяются (вернутся вместе с редактированием даты создания).
- Видимость элемента раскладки на проверку не влияет: правило защищает данные,
  а не разметку.
- Правила, которым нужны данные владельца, провайдеры вносят через
  `IFormRulesContributor` (общий слой применяет взносы при создании реестра) —
  `unique` поста зарегистрирован в скоупе `post.*` и срабатывает только на slug.
- Правила метаполей по-прежнему применяет `MetaValuesValidator` — форма их не
  дублирует (дескриптор метаполя не несёт `Rules`, `SettingsOnForm = false`).
- Тесты: `tests/Mars.Server.Tests/Forms/PostFormRulesValidatorTests.cs`
  (правила раскладки, пол транспорта, отсечение дат и метаполей, `unique` на
  создании и обновлении, сообщение из параметров правила, сквозной прогон через
  `CreatePostQueryValidator`); общая фикстура формы — `PostFormTestHost`.

### Фаза 3 — дизайнер формы в админке (выполнено 2026-09-09)

- Дизайнер — общий компонент `FormLayoutEditor` (+ строка `FormLayoutRow`) в
  `Mars.Admin.Framework/Components/Forms`: правит дерево провайдера по зонам —
  порядок (стрелки, как в редакторе колонок грида), зона, видимость, ширина,
  секции (добавить, переименовать, свернуть, удалить, забрать поле, выпустить
  поле), переопределение заголовка и редактора, правила системного слота.
  Зоны, правила и редакторы берутся из манифеста провайдера, поэтому компонент
  доменно-независим: показывает только объявленное провайдером.
- Drag-drop не делали: порядок стрелками по образцу `PostTypeGridSettingsEditor`
  (решение №14 — визуал правим по рабочему прототипу).
- Наружу дизайнер отдаёт только `FormLayoutSettings` (дескрипторы не хранятся);
  null — сброс к раскладке провайдера. Дерево по умолчанию собирает сервер
  (`GET api/PostType/form/{id}?saved=false`), поэтому сброс — это запрос,
  а не локальный пересчёт.
- Хосты: карточка «Форма редактирования поста» на странице презентации типа
  (`/EditPostType/Presentation/{id}`, рядом с колонками грида) и быстрый вход из
  меню действий формы поста — диалог `PostFormLayoutDialog`, который после
  сохранения подменяет дерево формы на месте, не трогая несохранённые правки поста.
- Транспорт: `UpdatePostTypePresentationRequest/Query.Form` → `post_types.Options["form"]`
  (`WithFormLayout`); чтение — `PostTypePresentationEditViewModel.Form` (дерево)
  и `.FormLayout` (сохранённая раскладка как есть — чтобы сохранение страницы без
  правок дизайнера не затирало раскладку).
- Ширина элемента теперь действительно рендерится: `FormItems` оборачивает детей
  зоны в bootstrap-`row`, а элемент — в `col-12 / col-md-6 / col-md-4` (раньше
  классы ширины стояли на `FormFieldRow` без родителя-`row` и не работали).
- Попутно в нормализаторе: сохранённая секция без зоны получала `Zone = null` и
  пропадала из рендера зоны — теперь зона пересчитывается, как у полей
  (тест `SectionWithoutZone_GetsFirstZone`).
- Тесты: `tests/Mars.Server.Tests/Dto/PostTypeOptionsCatalogTests.cs` (опции типа:
  соседство ключей, null убирает ключ, round-trip зон/настроек/правил) и
  интеграционный `UpdatePostTypePresentation_WithFormLayout_StoresItInTypeOptions`
  (Docker-сьют; в этот прогон не запускался — Docker не поднят).

### Этапы A–D — визуал админки и унификация рендера (запланировано 2026-09-10)

Идут **до** фаз 4–6: закрывают решения 15–21. Первый визуальный шаг уже сделан
2026-09-10 на `EditPostTypePage`: статусы — сворачиваемая карточка в конце
основной колонки, «Видимость» типа — в боковой панели под `Disable`.

**Этап A — параметры системных полей + общий редактор определений (v1) — выполнено 2026-09-10**

- Контракты (`Mars.Forms.Contracts`): `FormFieldDefinition` — редактируемая
  проекция дескриптора (Key, Title, Description, Type, Required, ReadOnly,
  Hidden, Disabled, Multiple, Editor, Choices, Min/Max, ModelName, Tags, Order,
  `Options:JsonNode`, Rules, `State` = New/Existing/Deleted, `Id?`) и
  `FormDefinitionCapabilities` (CanAdd/CanClone/CanDelete/CanChangeType/
  CanEditKey/CanHide). `FormFieldDescriptor` остаётся read-only контрактом
  рендера; провайдер маппит определение ↔ дескриптор.
- UI (`Mars.Admin.Framework/Components/Forms/`): `FieldDefinitionsEditor.razor` +
  `FieldDefinitionRow.razor` — аккордеон в нынешнем стиле `FormMetaField`
  (заголовок: иконка типа, title, key, бейджи hidden/disabled/фича; тело: общие
  параметры + панели настроек типа + валидаторы). Состав доступных действий — из
  capabilities.
- Первый потребитель — системные поля: capabilities урезаны (без добавить/
  дублировать/удалить/сменить тип), состав строк = `SystemFieldsCatalog.All` ∩
  включённые фичи типа. Карточка «Системные поля» — перед «Meta fields» на
  `EditPostTypePage`.
- Хранение и транспорт: `PostTypeOptionsCatalog.SystemFields` +
  `GetSystemFields`/`WithSystemFields` + `FormFieldSettingsJson` (по образцу
  `FormLayoutJson`); `UpdatePostTypeRequest.SystemFields` → `UpdatePostTypeQuery`
  → точечная запись `Options` (обычный путь `UpdateEntity` Options не трогает,
  поэтому `form`/`grid` не затираются); чтение — `PostTypeDetail.SystemFields`
  (+ `PostTypeResponse`/`PostTypeDetailResponse`, `PostTypeEditModel`).
- `PostFormBuilder.SlotItem` берёт Rules/Editor/Required/ReadOnly из
  `SystemFields` с компат-фолбэком на сохранённую раскладку.
- `FormLayoutRow`: убрать Rules и Editor (остаются порядок, зона, видимость,
  ширина, секции, переопределение заголовка).
- Тесты: `PostTypeOptionsCatalogTests` (round-trip нового ключа, соседство с
  `form`, null убирает ключ), `PostFormBuilderTests` (применение параметров,
  фолбэк, фича-гейты), `PostFormRulesValidatorTests` (правила из нового
  источника доходят до валидации записи), интеграционный
  `UpdatePostType_WithSystemFields_StoresThemInTypeOptions` (Docker-сьют).

Что добавилось/изменилось по ходу реализации этапа A:

- Контракт параметров — общий, не постовый: `FormFieldSettings` +
  `FormFieldSettingsJson` в `Mars.Forms.Contracts` (ключ, переопределение
  редактора, правила). Пустой набор не хранится — ключ из Options убирается.
- Редактируемое определение — `FormFieldDefinition` (**класс**, а не record:
  нужна двусторонняя привязка в редакторе) + `FormDefinitionCapabilities` с
  пресетом `SystemFields`. Общий редактор — `FieldDefinitionsEditor` +
  `FieldDefinitionRow` в `Mars.Admin.Framework/Components/Forms`; хелпер
  параметров правил `FormRuleParams` вынесен из дизайнера раскладки.
  Визуал строки — сворачиваемая карточка (как `FormSectionBlock`), не
  `FluentAccordion`: раскрывать/скрывать можно без зависимости от internals FluentUI.
- `Mars.Admin.Framework` теперь ссылается на `Mars.Forms.Front` (нужен
  `IFormEditorLocator`, чтобы показывать только совместимые с типом редакторы).
  Направление допустимо: запрещены ссылки на чужие `.Host`, а `.Front`→`.Front`
  в репо уже есть (`Mars.Plugin.Front`/`Mars.Datasource.Front` → `Mars.Nodes.FormEditor`).
- Обязательность и read-only слота вынесены в каталог
  (`SystemFieldsCatalog.IsRequired`/`IsReadOnly`) — их используют и серверный
  `PostFormBuilder`, и клиентская `PostTypeEditModel`, иначе признаки разъехались бы.
- Легаси-совместимость с двух сторон: **чтение** — `GetEffectiveSystemFields()`
  материализует правила/редактор из старой раскладки, если ключа `systemFields`
  нет; **запись** — `UpdatePresentation` фиксирует их в `systemFields` до
  перезаписи раскладки (иначе сохранение представления их теряло).
  `FormItem.Rules`/`Editor` помечены как легаси, нормализатор их больше не
  переносит — раскладка стала чисто представлением.
- `UpdatePostTypeRequest.SystemFields`: `null` = «не прислали, сохранённые не
  трогаем» (обычный путь обновления Options раньше не писал вовсе), пустой
  список = «очистить». Создание типа пишет параметры сразу.
- Правила теперь приходят в **дескрипторе** слота (`FormFieldDescriptor.Rules`),
  а не в элементе дерева: `FormValidator` применяет `field.Rules.Concat(item.Rules)`,
  поэтому перенос без дублей, а `PostFormRulesValidator.RulesOnly` продолжает
  отбирать слоты по `SettingsOnForm`.
- Тесты: `PostTypeOptionsCatalogTests` (новый ключ, round-trip, легаси-материализация),
  `PostFormBuilderTests` (параметры применяются, раскладка их не несёт, чужой ключ
  игнорируется), `FormDefinitionNormalizerTests` (правила/редактор из раскладки
  больше не переносятся; дескрипторные выживают), `PostFormRulesValidatorTests`
  переведён на `SystemFields`, интеграционные `UpdatePostType_WithSystemFields_...`
  и `UpdatePostType_WithoutSystemFields_KeepsStoredOnes` (Docker не поднят —
  в этот прогон не запускались). Прогон: `Mars.Server.Tests` 474/474,
  `Mars.Forms.Tests` 84/84, `dotnet build Mars.slnx` — 0 ошибок.

**Этап B — метаполя на общем редакторе определений**

- Адаптер `MetaFieldEditModel` ↔ `FormFieldDefinition` в обе стороны (включая
  Options-мешок: editor/codeLang/kind/removeMode/viewMode/uploadFolder/dropZone/
  queryTarget/backReference/featureKey) и обратно в `Create/UpdateMetaFieldRequest`.
- Панели настроек типа как зарегистрированные компоненты
  (`IFormFieldTypeSettingsLocator`): relation (ModelName/Kind/RemoveMode/ViewMode/
  DropZone), file/image (UploadFolder/ViewMode/DropZone/Accept), select/selectMany
  (варианты — заменяет `EditMetaFieldVariants`), code (CodeLang), query (цель +
  обратная ссылка), min/max, валидаторы.
- Защита feature-полей (контент, картинка поста) — через capabilities + бейджи.
- Переключение трёх страниц типов (`EditPostTypePage`, `EditUserTypePage`,
  `EditPostCategoryTypePage`), затем удаление `FormMetaField.razor` (+ code-behind)
  и `EditMetaFieldVariants.razor`.

**Этап C — значения через FormEngine (post + user + postcategory)**

- `IFormValueStore` в `Mars.Forms.Front`: GetValue/SetValue/GetList/SetList/
  AddItem/RemoveAt/Move + Errors + Changed + «нативный payload» для доменных
  редакторов. Реализации: поверх мешка (`FormValuesModel`) и `MetaValueStore`
  поверх `List<MetaValueEditModel>` (single = строка Index 0, multiple = строки по
  Index, колонка по `FormFieldType`; `Id`/`MarkForDelete` — в payload).
- Pull-протокол тяжёлых редакторов обобщается на уровень движка (вместо
  `IHeavyMetaValueEditors`/`IHeavyMetaValueEditor`).
- Встроенные редакторы: добрать SelectMany (чекбоксы вариантов) и поддержку
  `Choices` — сейчас в движке только `FormSelectEditor`/`FormListEditor`;
  примитивы (String/Text/Bool/Int/Long/Float/Decimal/DateTime/Select) уходят
  встроенным, `RowMetaValue` исчезает.
- Порт доменных редакторов на контракт `FormFieldBinding` и регистрация в
  `FormEditorLocator`: relation single/multi, children list, file multi и
  `FSelectMedia`, wysiwyg (Quill), code (Monaco), blockeditor (EditorJS),
  color/url/email/date/time — под ключами `MetaFieldEditorCatalog` и дефолтами
  для Relation/File/Image.
- Провайдеры `user.*` и `postcategory.*` (дерево = плоский список метаполей
  владельца, одна зона) → `EditUserPage` и `EditPostCategoryView` на `FormRenderer`.
- `PostFormField` худеет до `FormFieldRow`: контент становится зарегистрированным
  доменным редактором, а не веткой `if` (сам `PostContentEditor` и его ИИ-мост
  сохраняются).
- Резать по группам типов с проверкой сохранения на каждом шаге: примитивы →
  select/selectMany → relation → file/галереи → тяжёлые.

**Этап D — зачистка**

- Удалить `FormMetaValue`, `FormMetaValueItems`, `FormMetaValueItem`,
  `RowMetaValue`, `MetaValueRelationSelect` (сирота, потребителей нет) и всё, что
  не пережило порт.
- Перенести выжившее: `GroupedSelectDropDown` → общие компоненты (используют три
  формы нод в `Mars.Nodes.FormEditor`), модели `MetaFieldEditModel`/
  `MetaValueEditModel`/`MetaValueEditModelLookup`/`MetaFieldTypePresets`/
  `MetaValueListHelper` → в слой моделей Cms-админки.
- Слить реестры: `IMetaFieldEditorLocator`/`MetaFieldEditors` → `IFormEditorLocator`;
  `MetaFieldEditorCatalog` остаётся серверным источником ключей (его используют
  `PostTypeFeatureFields`, `BlockEditor1PostContentProcessor`, `MarsPostTools`,
  сиды и тесты).
- Публичный фронт не затрагивается: потребителей `MetaFieldViews` вне
  `Mars.Admin`/`Mars.Admin.Framework` нет (проверено 2026-09-10), кроме
  `GroupedSelectDropDown` в нодах.

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
  *(Пересмотрено 2026-09-10: переводятся на общем механизме на этапе C — решение №20.)*
- Не переводить существующие типизированные формы нод на схему.
- Не вводить общий submit-эндпоинт до появления динамических форм.
- Не вылизывать раскладку и визуал до рабочего прототипа.
  *(Снято 2026-09-10: этапы A–D.)*
- Не начинать SQL-форму до параметризации `SqlNonQuery`.
- Не менять транспорт мета-значений (`MetaValueEditModel`, EAV-строки, API) —
  на этапе C только адаптер хранилища (решение №19).
- Не оставлять редактирование параметров полей в дизайнере представления —
  после этапа A там только порядок/зона/видимость/ширина/секции (решение №15).

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
- Этапы A–D — `dotnet build Mars.slnx` + `tests/Mars.Forms.Tests` (стор, кодек,
  SelectMany/Choices, реестры) и `tests/Mars.Server.Tests` (`Forms/`,
  `Dto/PostTypeOptionsCatalogTests`); интеграции `tests/Mars.Integration.Tests`
  на этапе A (сохранение `systemFields`) и на этапе C (мета-значения трёх
  владельцев). Публичный фронт не затрагивается — `HandlebarsAppFrontTests` не нужны.
