# Mars.Forms — гайд по механизму форм для агента

> Один рендерер, одно определение формы и один транспорт значений: форма поста, формы пользователя
> и категории, дальше — виджеты, ноды и внешние SQL-таблицы. Провайдеры разные, контракты общие.
> Метаполя как источник полей — [MetaFieldsGuide.md](./MetaFieldsGuide.md).
> Пошаговое проектирование (фазы 0–3, этапы A–D, упрощения R0–R10, сетка L0–L5) в `ai/` больше не
> лежит: `git show 6e603cf0:ai/FormEnginePlan.md`, `…:ai/FormEngineSimplifyPlan.md`,
> `…:ai/FormLayoutGridPlan.md`. Всё влито в master одним сквошем `6e603cf0`.

## Где что лежит

- **Контракты** — `src/Mars.Modules/Mars.Forms.Contracts`:
  `FormDefinition.cs` (`OwnerModel` + зоны + элементы), `FormItem.cs` (+ `FormItemKind`,
  `FormItemWidths`), `FormFieldDescriptor.cs` (+ `FormChoiceOption`) — описание поля для рендера,
  `FormFieldDefinition.cs` (+ `FormDefinitionCapabilities`) — редактируемая проекция для страницы
  типа, `FormFieldType.cs`, `FormZoneDescriptor.cs`, `FormItemExtensions.cs`.
- **Раскладка** — там же: `FormLayoutSettings` (в `FormDefinition.cs`), `FormLayoutJson.cs`
  (camelCase-хранение + чтение легаси), `FormLayoutTree.cs` (единственный проектор плоского списка
  в дерево), `FormLayoutRules.cs` (матрица «кто в ком лежит»).
- **Значения и правила** — там же: `FormValueCodec.cs` (канонические форматы), `FormValues.cs`,
  `FormRuleDefinition.cs` (+ `FormRuleCatalog`), `FormRuleEvaluator.cs` (чистые правила),
  `FormEditorCatalog.cs` (ключи общих редакторов).
- **Сервисы** — `src/Mars.Modules/Mars.Forms.Abstractions`: `IFormDataProvider.cs` (+ локатор),
  `Services/FormDefinitionNormalizer.cs`, `Services/FormRuleRegistry.cs`, `Services/FormValidator.cs`,
  `Services/FormOwnerScopes.cs`, `IFormRulesContributor.cs`, `IFormValidator.cs`, `FormContext.cs`;
  DI — `FormsServiceCollectionExtensions.AddMarsForms()`.
- **Рендер** — `src/Mars.Modules/Mars.Forms.Front`: `FormRenderer.razor` (зоны → узлы),
  `FormLayoutNodeView.razor` (узел сетки), `FormFieldRow.razor` (подпись, описание, ошибки),
  `FormRenderContext.cs`, `FormFieldBinding.cs` (поле + стор + клиентская валидация),
  `IFormValueStore.cs` (+ `FormValuesModel.cs` — JSON-мешок), `FormCommitHooks.cs`,
  `FormLiveEditors.cs`, `FormEditors.cs` (`IFormEditorLocator` + встроенные),
  `FormFieldTypeSettings.cs` (`IFormFieldTypeSettingsLocator`), редакторы-примитивы — `Editors/*.razor`;
  DI — `MainFormsFront.cs` (`AddMarsFormsFront()` / `UseMarsFormsFront()`).
- **Провайдер поста (референс)**:
  - `Mars.Cms.Contracts/PostTypes/` — `SystemFieldsCatalog.cs` (слоты, зоны, фичи, read-only,
    + `PostFormEditors` — доменные ключи редакторов), `PostTypeOptionsCatalog.cs` (ключи опций типа),
    `PostTypeGridColumns.cs` (колонки грида, делят пространство ключей со слотами);
  - `Mars.Cms.Abstractions/Forms/` — `PostFormBuilder.cs` (раскладка по умолчанию + дескрипторы),
    `PostFormRules.cs` (+ `PostFormRulesContributor`), `PostFormRulesValidator.cs`
    (правила типа на путях записи), `MetaFieldsFormBuilder.cs` (формы пользователя и категории);
  - `Mars.Cms.Host/Services/PostFormProvider.cs`; регистрация — `MainCms.cs`
    (`AddKeyedScoped<IFormDataProvider, PostFormProvider>(PostFormBuilder.OwnerModelWildcard)`).
- **Админка**:
  - `src/Admin/Mars.Admin.Framework/Components/Forms/` — дизайнер `FormLayoutEditor.razor(.cs)` +
    `FormLayoutDraft.cs` (все операции над раскладкой) + `FormLayoutRoot.razor` / `FormLayoutCell.razor`,
    редактор определений `FieldDefinitionsEditor.razor` / `FieldDefinitionRow.razor`,
    панель `MetaFieldSettingsPanel.razor` (скоуп `meta`), рендер мета-формы `MetaValuesForm.razor`,
    доменные редакторы значений — `Editors/`;
  - `src/Admin/Mars.Admin.Framework/Components/MetaFieldViews/MetaValueStore.cs` — единственный
    перевод EAV-строк в значения формы и обратно;
  - `src/Mars.Admin/` — `Components/SystemFieldSettingsPanel.razor` (скоуп `post.systemfields`),
    `Pages/PostsViews/Forms/PostFormValueStore.cs`, `Startups/StartupFormEditors.cs` (регистрация
    редакторов и панелей), страницы `Pages/PostsViews/EditPostView.razor` (+ ИИ-хендлер),
    `EditPostTypePage`, `EditPostTypePresentationPage`, `ManagePostView`, `PostTypeGridSettingsEditor`,
    `Pages/UserViews/EditUserPage`, `Pages/PostCategoryViews/EditPostCategoryView`.

## Поток: определение → рендер → запись

1. **Провайдер собирает определение.** `IFormDataProvider.GetFormAsync(FormContext)` ищется
   keyed-DI по `ownerModel`: точный ключ (`form.feedback`) или шаблон `prefix.*` (`post.*`).
   Резолв — `FormOwnerScopes.Of`: точный ключ, затем шаблоны от длинного к короткому
   (`sql.ds1.orders` → `sql.ds1.*` → `sql.*`). Пост: `PostFormProvider` → `PostFormBuilder.Build`
   = слоты `SystemFieldsCatalog` по включённым фичам типа + метаполя типа (без `Disabled` и `Query`,
   на клиенте ещё и без `Hidden`), порядок = историческая разметка формы.
2. **Нормализатор сводит сохранённую раскладку с полями провайдера**
   (`IFormDefinitionNormalizer.Normalize(saved, defaults)`, 5 шагов в
   `FormDefinitionNormalizer.cs`): порядок и настройки из сохранённого, неизвестный ключ поля
   отбрасывается, узел без допустимого родителя переезжает в корень зоны (недопустимый в корне —
   удаляется, дети осиротевают в корень), недостающие поля дописываются в конец, свободные элементы
   оборачиваются в ряд с колонкой. Циклы разрываются, порядок в списке не важен, дескрипторы всегда
   свежие, идемпотентен. Формы пользователя и категории сохранённой раскладки не имеют —
   `MetaFieldsFormBuilder` нормализатор не вызывает (порядок = `MetaField.Order`).
3. **Раскладка хранится у владельца, отдельно от дескрипторов.** У типа поста —
   `post_types.Options["form"]` (порядок, зоны, видимость, узлы сетки) и `["systemFields"]`
   (правила и редактор слотов), ключи — `PostTypeOptionsCatalog`. Два ключа потому, что их пишут
   разные эндпоинты (`presentation/update` и `PUT api/PostType`) и они не затирают друг друга.
4. **Рендер.** `FormRenderer` идёт по зонам: контейнеров ≥ 2 → `FluentTabs`, один → его дети,
   нет контейнеров → свободные узлы (`FormRenderer.razor:8`). Узел рисует `FormLayoutNodeView`
   (ряд → колонки, поле → `FormFieldRow`, `Heading` → `<h6>`, `Divider` → `<hr>`). Подписи,
   описания и ошибки — в строке поля; редактор отвечает только за ввод. Дерево и рендеру, и
   дизайнеру даёт один `FormLayoutTree.Build`.
5. **Значения — модель владельца, форма — проекция.** Редактор получает `FormFieldBinding`
   (дескриптор + `IFormValueStore`) и работает с каноническими CLR-значениями. У поста стор
   (`PostFormValueStore`) читает и пишет свойства `PostEditModel`, метаполя идут через
   `MetaValueStore`; `FormValuesModel` (JSON-мешок) — для форм без типизированной модели.
   Транспорт записи не меняется: `PostEditModel` сам собирает `Create/UpdatePostRequest`.
6. **Запись.** Тяжёлые редакторы (WYSIWYG, код, блочный) не пишут на каждый ввод: страница
   обязана вызвать `FormCommitHooks.CommitAllAsync()` перед сохранением. Записать в такой редактор
   извне — `FormLiveEditors.Find(fieldKey)` (так работает ИИ-мост и Ctrl+S в редакторе кода).

## Раскладка: узлы и сетка

Узел один и для хранения, и для рантайма — `FormItem` (`Key`, `Parent`, `Zone` у корня зоны, `Kind`,
`Title`, `Visible`, `Width`; `Field` заполняет только провайдер). Хранится плоский массив:
порядок в массиве = порядок внутри родителя. Матрица допустимых родителей — `FormLayoutRules.CanContain`:

| Kind | Допустимый родитель | Рендер |
|---|---|---|
| `Container` | корень зоны | `FluentTabs`, если контейнеров ≥ 2 |
| `Row` | корень зоны, `Container`, `Column` | `row` |
| `Column` | только `Row`; ширина `full`/`half`/`third`/`quarter` = 12/6/4/3 доли | `col-*`; пустая колонка занимает место |
| `Field` | только `Column` (легаси — свободно, нормализатор обернёт) | `FormFieldRow` во всю ширину колонки |
| `Heading`, `Divider` | только `Column` | `<h6>`, `<hr>` |

- **Ширина живёт только у колонки** (`FormItemWidths.Span`, null и неизвестное = 12). Элементы живут
  только в колонках.
- **Раскладка по умолчанию — «один ряд и одна колонка на зону»** (`WrapElements`): свободные
  элементы родителя складываются в один ряд, соседи без ширины делят общую колонку, элемент с
  шириной получает свою. Ключ группировки — **родитель + зона** (корневые узлы разных зон
  неродственны, хотя родителя нет ни у тех, ни у других). Поэтому форма без сохранённой раскладки
  выглядит как до сетки (те же `col-12` / `col-md-6` / `col-md-4`).
- **Секций-маркеров больше нет**: вместо `SectionTitle` — узлы `Heading`/`Divider`. Легаси читается
  в `FormLayoutJson.Parse`: вложенное `items` (дерево до R3) становится заголовком, дети — соседями;
  плоский `sectionTitle` — тоже заголовком. Миграции нет.
- **Дизайнер** — `FormLayoutEditor` на `FormLayoutDraft`: черновик держит операции (добавить/удалить
  узел, перенести поддерево, сменить ширину, переподвеска детей, палитра неразмещённых полей),
  наружу отдаёт только `FormLayoutSettings` (`Draft.ToSettings()`). Перенос адресуется «перед узлом»:
  `Draft.Move(key, parent, beforeKey, zone)`. Сброс раскладки — `ValueChanged(null)` +
  `OnResetRequested` (порядок провайдера знает только сервер). Хосты — страница презентации типа и
  диалог из формы поста.
- **UI дизайнера согласован с пользователем — менять только осознанно** (`FormLayoutCell.razor`,
  `FormLayoutRoot.razor`, стили — `FormLayoutEditor.razor.cs` + `.razor.css`): «ряд» в интерфейсе
  называется **строкой** — надпись «строка» слева от колонок в фиксированных 50 px; ширина
  показывается числом (12/6/4/3) и только у колонки (`width:80px; min-width:50px`); элемент (поле) —
  строка с названием и иконками видимость/«убрать из раскладки», **без инпута заголовка и чекбокса**
  (заголовок поля правится в редакторе определений); инпуты текста есть у контейнера («Имя таба»,
  200 px) и у заголовка (180 px); кнопки добавления — в корне зоны «Строка» и «Контейнер», в строке
  «Колонка», в колонке «+ Строка / + Заголовок / + Разделитель»; кнопки узлов видны только при
  наведении (класс `layout-actions`); зона и контейнер — вертикальный список строк, флекс-слой без
  `gap` (отступы внутренней рамкой); бросок на узел вставляет переносимое **перед** ним у его
  родителя.

## Значения: форматы и сторы

- **Все форматы — в `FormValueCodec`, одном месте.** Decimal — строкой в инвариантной культуре
  (числом JS теряет точность), DateTime — ISO-8601 `"O"` со смещением, `Select` — ключ варианта
  (не заголовок), `Relation`/`File`/`Image` — строка-Guid, `SelectMany` — массив строк-ключей,
  множественные поля — массив, где порядок = индекс. `Computed` не передаётся.
- **Чтение строгое**: одна каноническая форма на тип, отклонение — ошибка формата
  (`IsShapeValid`). «Угадывание типа» запрещено: оно скрывает рассинхрон клиента и сервера.
  Отсутствие ключа = «значение не задано», пустая строка ≠ отсутствие.
- Расхождение форматов лечится кодеком, а не провайдерами и не сторами.
- **Мешок `FormValues` сегодня нужен только серверной валидации** (`IFormValidator.ValidateAsync`)
  и будущим динамическим провайдерам; типизированные владельцы (пост) его не используют.

**Потребители сейчас**: форма поста (`EditPostView` + `PostFormValueStore`), форма пользователя
(`EditUserPage`) и форма категории (`EditPostCategoryView`) — обе через `MetaValuesForm` +
`MetaValueStore`. Дальше (фазы 4–6) — те же контракты с другими провайдерами.

## Реестры (три оси)

Все три — синглтоны из DI, `Register` доступен в любой момент, словарь собирается при запросе,
поэтому регистрация из плагина после старта тоже видна. Регистрации админки —
`src/Mars.Admin/Startups/StartupFormEditors.cs` (вызывается после сборки контейнера).

| Ось | Ключ | Как выбирается |
|---|---|---|
| Редакторы значений — `IFormEditorLocator` | ключ редактора + тип поля + кратность | `GetEditorComponent(editorKey, type, multiple)`; `EditorsFor` для UI-списка предлагает только регистрации **с названием** |
| Панели настроек — `IFormFieldTypeSettingsLocator` | скоуп + тип поля (`meta`, `post.systemfields`) | `PanelsFor` рисует общие панели скоупа (регистрация без типов) + панели типа; правят доменную модель через `FormFieldDefinition.Source`, не значение |
| Правила — `IFormRuleRegistry` | тип правила + скоуп владельца | встроенные (`FormRuleCatalog.BuiltIn`: required/regex/length/min/max) считает `FormRuleEvaluator` — один код на клиенте и сервере; в реестре живут только правила с данными владельца |

- **Дефолтный редактор типа** — `FormEditorLocator.DefaultKeys` / `GetDefaultEditor`: `SelectMany`
  без кратности → чекбоксы вариантов (`Choices`), множественные и `SelectMany` → `List`,
  иначе редактор по типу. Пустой `Editor` в дескрипторе = дефолт.
- **Ключи редакторов трёхчастные** `<происхождение>.<семейство>.<реализация>`: общие — `core.*`
  (`FormEditorCatalog`), доменные — у владельца (`PostFormEditors.Title` = `post.input.title`,
  `Categories` = `post.picker.categories`; `MetaFormEditors` = `core.meta.relation[.multi]`,
  `core.meta.file[.multi]`). Обычный текст — без ключа.
- **Повторная регистрация ключа перекрывает прежнюю** (последняя выигрывает).

## Валидация

- **Клиент** — `FormFieldBinding`: мгновенная подсветка тем же `FormRuleEvaluator`,
  `required` из проверки исключён намеренно (`FormFieldBinding.cs:109`) — пустое поле не
  подсвечивается до сохранения.
- **Сервер** — `FormValidator.ValidateAsync`: пропускает `Computed` и `ReadOnly`, проверяет
  обязательность, форму значения (`IsShapeValid`), `Min`/`Max` дескриптора и правила; встроенные —
  `FormRuleEvaluator`, остальные — через реестр. Незарегистрированное правило молча не срабатывает.
- **Правила типа действуют на всех путях записи поста**: `PostFormRulesValidator` вызывается из
  `GeneralPostQueryValidator`, который подключён и к `CreatePostQueryValidator`, и к
  `UpdatePostQueryValidator` — обойти через API нельзя. Ошибки маппятся в свойства транспорта
  (`PostFormRulesValidator.TransportProperty`).
- **`unique` для slug** — `PostFormRules.Unique` через `IPostRepository.SlugOccupiedAsync`,
  сравнением `lower()`. Уникальных индексов на slug нет — только выражение-индексы
  `ix_posts_slug_lower` и `ix_posts_post_type_id_slug_lower`, поэтому уникальность гарантирует
  валидатор, а не БД (гонки параллельной записи приняты, как и у метаполей).
- Обязательность `title` и `slug` задана DataAnnotations транспорта, а не правилами формы
  (`SystemFieldsCatalog.IsRequired`).

## API и два контура настройки типа

| Что | Эндпоинт |
|---|---|
| Данные типа (есть `systemFields`, раскладки нет) | `GET api/PostType/{id}` |
| Параметры полей (страница типа) | `GET api/PostType/edit/{id}`, `PUT api/PostType` |
| Представление — только раскладка | `GET api/PostType/presentation/edit/{id}`, `PUT api/PostType/presentation/update` |
| Эффективное дерево для дизайнера | `GET api/PostType/form/{id}?saved=false` (false = раскладка по умолчанию / сброс) |
| Форма поста | `PostEditViewModel.Form` (`FormDefinition`) |

Не смешивать: **представление** (порядок, зона, видимость, ширина, узлы сетки) — в раскладке,
**параметры поля** (правила, редактор, язык кода) — в настройках владельца
(`Options["systemFields"]` для слотов, определение метаполя для метаполей).

## Как добавить

### Провайдер формы (новый владелец)

Образец — пост: `PostFormBuilder` + `PostFormProvider` + `PostFormValueStore`.

1. Ключ владельца: точный или `prefix.*`; при желании — константы и хелперы разбора, как
   `PostFormBuilder.OwnerModelPrefix/OwnerModelWildcard/OwnerModel(typeName)/PostTypeName(ownerModel)`.
2. Статический билдер: зоны (`FormZoneDescriptor`), `DefaultItems` (порядок, который форма имела бы
   без сохранённой раскладки), дескрипторы полей; в конце — `normalizer.Normalize(saved, defaults)`,
   если у владельца есть хранимая раскладка.
3. Класс `IFormDataProvider` (образец — 20 строк `PostFormProvider.cs`) и регистрация
   `services.AddKeyedScoped<IFormDataProvider, X>(key)` в Main-модуле.
4. Параметры полей без собственного определения (слоты, колонки таблицы) — `FormFieldSettings`
   в опциях владельца (`FormFieldSettingsJson` для хранения; пустой набор не хранится).
5. Значения на фронте: своя `IFormValueStore` поверх модели владельца либо `FormValuesModel`.
6. Правила с данными владельца — `IFormRulesContributor` (образец `PostFormRulesContributor`).
7. Права редактора определений — `FormDefinitionCapabilities`: для каталожных слотов
   `SystemFields` (правятся только редактор и правила), для полей источника — `MetaFields`.

### Редактор значения

1. Компонент с единственным параметром `FormFieldBinding` (образцы — `Mars.Forms.Front/Editors/*.razor`,
   доменные — `Mars.Admin.Framework/Components/Forms/Editors/`); на input-элементе —
   `Name="@Binding.Field.Key"` (см. «Грабли»).
2. `IFormEditorLocator.Register(editorKey, component, multiple, title, fieldTypes)`; `title = null` —
   редактор не предлагается в UI-выборе и доступен только явным ключом дескриптора.
3. Регистрация — `StartupFormEditors.cs` (админка) или свой startup модуля/плагина.
4. Значение — каноническое CLR (`FormValueCodec`), множественные — `GetList`/`SetList`.
5. Редактор с отложенной записью обязан зарегистрировать коммит в `FormCommitHooks`, а если в него
   можно писать извне — setter в `FormLiveEditors` (по ключу поля).

### Правило валидации

- Чистое (без данных владельца) → ключ в `FormRuleCatalog` (включая список `BuiltIn`) + ветка в
  `FormRuleEvaluator` — после этого правило одинаково считается на клиенте и на сервере.
- С данными владельца → `IFormRuleRegistry.Register(type, handler)` (глобально) или
  `Register(ownerModel, type, handler)` (скоуп, шаблоны `prefix.*` поддерживаются), взнос —
  через `IFormRulesContributor` в DI.

### Панель доменных настроек поля

`IFormFieldTypeSettingsLocator.Register(scope, component, fieldTypes)`; без типов — показывается на
всех полях скоупа. Панель принимает `FormFieldDefinition` и правит доменную часть (цель связи, папка
загрузки, варианты, генератор) через `.Source`; общие параметры (заголовок, ключ, обязательность,
правила, редактор) рисует `FieldDefinitionsEditor` — не дублировать.

### Узел раскладки

`FormItemKind` → `FormLayoutRules.CanContain` → рендер в `FormLayoutNodeView.razor` → дизайнер
(`FormLayoutCell.razor` / `FormLayoutRoot.razor` / палитра в `FormLayoutEditor.razor`) → чтение и
запись в `FormLayoutJson.cs` → тесты `FormLayoutTreeTests`, `FormDefinitionNormalizerTests`,
`FormLayoutJsonTests`.

## Тесты

- `tests/Mars.Forms.Tests` — контракты и ядро: `Codec/FormValueCodecTests`,
  `Normalization/FormDefinitionNormalizerTests`, `Validation/FormRuleEvaluatorTests`,
  `Validation/FormRuleRegistryTests`, `Validation/FormValidatorTests`, `FormLayoutJsonTests`,
  `FormLayoutTreeTests`, `FormItemExtensionsTests`, `Front/FormEditorLocatorTests`,
  `Front/FormFieldTypeSettingsLocatorTests`, `FormDataProviderLocatorTests`.
- `tests/Mars.Server.Tests/Forms` — сборка формы поста и правила записи (`PostFormBuilderTests`,
  `PostFormRulesValidatorTests`, `MetaFieldsFormBuilderTests`, `MetaFieldTypeFormMappingTests`,
  фикстура `PostFormTestHost`); рядом `Dto/PostTypeOptionsCatalogTests`, `Dto/PostTypeGridColumnsTests`,
  `Dto/PostTypeGridSettingsJsonTests`, `Services/PostTypeViewServiceSqlTests`.
- `tests/Mars.Admin.Framework.Tests` — `Components/FormLayoutDraftTests`, `Components/MetaValueStoreTests`.
- Интеграционные (`tests/Mars.Integration.Tests`): `Controllers/PostTypes/*` (включая
  `UpdatePostTypePresentationTests`), `Controllers/Posts/*` (включая `PostUniqueValidatorTests`,
  `PostMultiplicityValidatorTests`).
- E2E (`tests/Mars.E2E.Tests`, Playwright + системный Edge, Postgres в контейнере): включаются
  `MARS_E2E_TESTS=1`, перед прогоном обязательна полная сборка `dotnet build Mars.slnx`. Набор формы:
  `CreatePostTests`, `EditPostMetaFieldsTests`, `EditPostRelationFieldTests`,
  `EditPostSystemFieldEditorTests`, `EditPostLayoutGridTests` (сетка задаётся через API),
  `EditUserPageTests`; запуск —
  `Mars.E2E.Tests.exe -filter "/Mars.E2E.Tests/Mars.E2E.Tests.Tests/CreatePostTests/*"`.
- **Как писать ассершены по форме**: после нормализации первым в `Items` идёт структурный узел с
  сгенерированным ключом, поэтому поле ищут по ключу (`definition.Field(key)` из
  `FormItemExtensions`), а зону и порядок проверяют у самого поля, а не по позиции в списке.

## Грабли

- **WASM-бандл админки не входит в E2E-проект** — без `dotnet build Mars.slnx` перед прогоном E2E
  тестирует старую сборку. Юнит-слой рендер не ловит вовсе: и подмену редактора в реестре, и цикл
  рендера нашёл именно E2E.
- **`CommitAllAsync()` перед сохранением обязателен.** Тяжёлые редакторы держат значение у себя;
  без вызова `FormCommitHooks.CommitAllAsync()` (страницы: `EditPostView`, `EditUserPage`,
  `EditPostCategoryView`, `MetaValuesForm.CommitAllAsync`) значение молча теряется.
- **`FluentDropZone` требует `StopPropagation="true"` на каждом уровне**, иначе бросок уходит внешней
  зоне; у `FluentDragContainer.OnDropEnd` void-делегат — внутри `_ = DropAsync(args)`
  (`FormLayoutEditor.razor.cs:122`).
- **Дескрипторы не хранятся.** В сохранённой раскладке `Field = null`, дескриптор каждый раз отдаёт
  провайдер: не складывать `FormFieldDescriptor` в опции владельца и не читать его из раскладки.
- **Структурный узел не может занять ключ поля провайдера** — нормализатор такой узел отбрасывает
  (`Resolve` в `FormDefinitionNormalizer`), иначе поле исчезло бы из раскладки.
- **Ключи полей — стабильные идентификаторы** (`title`, `slug`, `created_at`, …): на них E2E-селекторы,
  `PostFormRulesValidator.TransportProperty` и ключи колонок грида. Не переименовывать.
- **slug — только через `lower()`** (`PostRepository.SlugOccupiedAsync`/`ExistAsync`): `ILike` и
  `string.Equals(StringComparison)` не дают планировщику использовать `ix_posts_post_type_id_slug_lower`.
- **Форма без сохранённой раскладки обязана выглядеть как раньше** — это контракт `WrapElements`
  и легаси-чтения `FormLayoutJson`; менять дефолт можно только вместе с E2E-проверкой колонок.
- **Легаси-БД: дублирующее метаполе контента.** Миграция
  `20260823215434_PostContentFeatureField` создавала строки `meta_fields` с
  `options.featureKey=content` для каждого типа с фичей `Content`; контент стал системным слотом
  (R7), и фильтра по этому маркеру в мета-потоках больше нет (`FeatureFieldsCatalog.GetFeatureKeyFor`
  знает только `PostImage`). На существующей БД такое поле рендерится обычным метаполем рядом со
  слотом `content` — его надо удалить в типах поста (или пересоздать БД). Миграции данных нет —
  сознательное решение.
- **Легаси-БД: правила, сохранённые только в раскладке, потеряны.** В окне между фазой 3 и этапом A
  правила системных полей писались в раскладку; при чтении они не поднимаются (раскладка правил не
  несёт) — их нужно задать заново в параметрах типа. Тоже сознательное следствие без миграции:
  жалобы «пропала валидация» на старых БД объясняются этим.
- **Тяжёлый редактор не должен держать локальный буфер, синхронизируемый из стора.** До коммита стор
  пуст, и любой ре-рендер формы (например ввод тегов) затирает буфер — контент сохранялся пустым;
  регрессию нашёл E2E, не юниты. Значение живёт в редакторе, наружу — только через
  `FormCommitHooks`, внутрь — только через `FormLiveEditors`.
- **Новый редактор обязан ставить `Name="@Binding.Field.Key"`** на input-элементе: E2E-селекторы ищут
  поле по ключу, `input[name]` — единственный стабильный крючок (атрибут уже терялся один раз,
  после чего селекторы пришлось переводить на ключи). Совет `Name="@nameof(model.FieldName)"` из
  `ai/E2ETestingGuide.md` для форм механизма не подходит.
- **FluentUI закреплён на 4.14.4** (`Directory.Packages.props`): компонентов `FluentDragItem` /
  `FluentDragItemsGroup` в этой версии нет, хотя вложенный пример drag&drop из документации собран
  именно на них. Рабочая песочница — `devstands/TestModules/Pages/FormsBuilderPage.razor`.
- **Неизвестное правило молча пропускается** (ни ошибки, ни лога) — при добавлении правила проверять
  регистрацию скоупа, а не только код обработчика.
- **Реестры открыты, кэшировать выборку нельзя**: `EditorsFor`/`PanelsFor`/`KnownTypes` собираются на
  каждый запрос, плагин может зарегистрировать компонент после старта.

## Инварианты (не нарушать без пересмотра)

- Раскладка — только представление: ни правил, ни редактора, ни дескрипторов в ней нет.
- Системные поля поста не материализуются строками в `meta_fields`: их значения живут в
  типизированных колонках `posts`.
- Транспорт записи поста не меняется: `PostEditModel` сам собирает `Create/UpdatePostRequest`,
  API и ноды не ломаются.
- Правила системных полей действуют на всех путях записи (`GeneralPostQueryValidator`), иначе их
  обходят через API.
- Типизированные формы нод (`INodeFormsLocator`) на эту схему не переводим.
- Панели настроек поля — не редакторы значений: они правят определение (`FormFieldDefinition.Source`),
  а не значение поля.
- Форматы значений меняются только в `FormValueCodec`, одним местом на все провайдеры.

## Отклонено (не предлагать снова без пересмотра)

- **Парные маркеры `row-start`/`row-end` вместо узлов сетки.** Дерево из парных маркеров нормализатор
  не проверит дёшево, пустой контейнер неотличим от забытого маркера, перенос поддерева превращается
  в перенос среза с балансировкой. Выбран плоский список с `Parent`.
- **«Универсальные носители» значений вместо JSON-мешка** (слоты как `MetaValueBase`): не влезают
  композитные значения (свойства нод, json-колонки, виджеты), набор колонок не расширяется без смены
  контракта, а escape-hatch всё равно нужен. Поэтому `FormValueCodec` + `FormValues`.
- **Панели настроек поля как редакторы значений** (слить два реестра в один): доменная модель
  недоступна общему слою (дескриптор — read-only record), источник — единственное хранилище, кодек
  переводит только общий поднабор параметров (отсюда `OnSourceChanged` + явный `StateHasChanged()`),
  скоуп необходим потому, что один визуальный вид живёт в разных хранилищах (`codeLang` из
  `MetaFieldEditModel.CodeLang` против `Options["systemFields"]`), панели нужен контекст страницы
  (каскад `MetaRelationModels`, `IDialogService`), и ось «выбор редактора» не подходит — доменные
  настройки рисуются безусловно, а не выбираются.
- **Раскладка как место хранения правил/редакторов.** Референсы проверены: Payload
  (`Collapsible`/`Tabs`) и ACF (`Tab`/`Accordion`) — presentational-only и значения не хранят
  (именованный таб Payload группирует данные в объект — у нас такого нет), Strapi «Configure the
  view» — per-type drag&drop без контейнеров, хранится на типе. Отсюда инвариант «раскладка — только
  представление».

## Что ещё не сделано

- **Фаза 4 — автономные формы (виджеты)**: `FormEntity`/таблица `forms` в общем `MarsDbContext`
  (`Id`, `Key` уникален, `Title`, `OwnerModel?`, `Definition` jsonb, `Disabled`, `Tags`, `CreatedAt`,
  `ModifiedAt`; FK — во fluent-конфигурации), провайдер `form.<key>`; куда пишутся значения — решает
  обработчик, привязанный к форме (XAction, поток нод, письмо, свой провайдер); позже рендер на
  публичном фронте — возможен, пока контракты остаются WASM-чистыми; общий эндпоинт отправки
  (`POST api/Form/{owner}/submit`) появляется здесь или позже.
- **Фаза 5 — динамические ноды и ноды создания сущностей**: существующие типизированные формы нод
  (`INodeFormsLocator`) остаются как есть; механизм применяется там, где форма определяется данными
  (нода создания сущности = провайдер `post.<typeName>`, та же валидация, что у админ-формы).
  `Mars.Nodes.FormEditor` ссылается на `Mars.Forms.Front`, значения динамической ноды пишутся в
  `nodes/flows.json` через `NodeService`, дескрипторы берутся из определения ноды.
- **Фаза 6 — внешние SQL-таблицы**: провайдер `sql.<slug>.<table>`, дескрипторы из `QTableColumn`
  (маппинг: тип колонки → `FormFieldType`, `IsKey`/`IsAutoIncrement` → readonly/hidden,
  `ColumnSize` → правило `length`, `IsUnique` → правило `unique`), чтение через `SqlQuery`, запись
  параметризованным INSERT/UPDATE. Блокер — `IDatasourceDriver.SqlNonQuery` не принимает параметров:
  сначала параметризованный non-query во всех трёх драйверах.
- **Свести XActions и формы опций на тот же механизм**: `XActionArgument` + `XActionFormDialog` —
  сегодня единственный работающий генератор «схема → форма», `OptionEditFormForOptionAttribute` /
  `IOptionsFormsLocator` — тот же паттерн. Без этого в платформе живут два параллельных генератора
  форм; в `ai/XActionsPlan.md` связи с `Mars.Forms` пока нет.
- **Пикер пользователя для слота `author`**: сейчас слот read-only с редактором `core.display.text`
  (подпись вместо значения), нужен отдельный редактор-связь.
- **Натуральная ширина колонки (`auto`)** — вторая итерация раскладки (сейчас `null` = full).
- **Косметика**: папка `MetaFieldViews` смешанная (модели + доменные редакторы значений +
  `MetaValueStore`), перенос файлов по слоям отложен.
