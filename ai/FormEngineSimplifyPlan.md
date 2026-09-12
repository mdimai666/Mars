# План: упрощение FormEngine (Mars.Forms)

> **Статус: R0–R10 выполнены (2026-09-12)** (базовые решения согласованы 2026-09-10: полный
> рефактор ядра, значения — в модели, раскладка — плоский список с маркерами секций). Ветка
> `ai/form-engine-simplify`, коммит на каждый шаг. План **заменяет** «этапы A–D» и фазы C/D
> из [FormEnginePlan.md](./FormEnginePlan.md); тот файл остаётся историей проектирования и
> as-is-инвентарём, работы ведутся по этому плану.

## Зачем

Рабочий прототип получился запутанным не из-за объёма, а из-за пяти наложившихся решений:

1. **Три описания одного поля** — `FormFieldDescriptor` (рендер), `FormFieldDefinition`
   (~35 свойств, правка), `FormFieldSettings` (хранение) плюс legacy `FormItem.Rules|Editor`
   и фолбэк `GetEffectiveSystemFields`.
2. **Две вселенные значений одновременно** — мешок `FormValues` для системных слотов и
   EAV-строки через каскады для метаполей; отсюда пять вложенных `CascadingValue`
   в `PostFormZone`, реестр `IHeavyMetaValueEditors` с pull-протоколом и
   `PostContentEditorHolder` ради ИИ-моста.
3. **Шесть реестров там, где хватает двух** — `IFormEditorLocator`, `IFormContainerLocator`,
   `IFormFieldTypeSettingsLocator`, `IFormRuleRegistry`, `IFormRulesContributor`,
   `IHeavyMetaValueEditors`; при этом контейнер зарегистрирован один (`section`), панель
   настроек — одна.
4. **Абстракции без потребителя** — `IFormDataProvider.ReadAsync/SubmitAsync`,
   `FormSubmitResult`, `CanReadValues/CanSubmit` (форма поста объявляет оба `false`),
   `FormContext.Parameters|LangCode`, `FormValues.OwnerId` (пишется, не читается).
5. **`FormValueCodec` — 452 строки «угадай тип»** (`TryReadLong` принимает bool/строку/
   decimal/double/raw, `TryReadString` — Guid/дату/число): не канонические кодировки,
   а толерантный парсер; здесь и живут баги.

## Принятые решения (2026-09-10)

1. **Объём — полный рефактор ядра.** Поведение и внешний вид формы сохраняются,
   переписывается внутренняя механика.
2. **Значения формы редактирования живут в модели; форма — проекция.** Единственный путь
   чтения/записи — адаптер владельца (образец — `PostEditModel.BuildFormValues/ApplyFormValues`).
   Мешок `FormValues` остаётся только сериализацией для серверных правил и будущих
   динамических провайдеров (фазы 4–6), из рендера поста уходит.
3. **Раскладка — плоский упорядоченный список с маркерами секций.** Секция — граница
   в списке (как `Tab` в ACF: поля после маркера принадлежат секции до следующего),
   а не узел дерева. Вложенность хранения не нужна.

## Целевая модель

```
Mars.Forms.Contracts
  FormConfig      { Zones[], Items[] }                    // хранится у владельца (post_types.Options["form"])
  ConfigItem      { Key, Zone, Width, Visible, SectionTitle? }   // SectionTitle != null — маркер начала секции
  FormItem        { Key, Zone, Width, Visible, Title?, SectionTitle?, Field? }
                                                          // рантайм; Kind/Items/Collapsed/Rules/Editor уходят
  FormField       { Key, Title, TitleKey?, Type, Required, ReadOnly, Multiple, Choices,
                    Min, Max, ModelName, Editor, Rules, Description? }   // ОДИН тип поля
  FormRuleDefinition { Type, Params }                     // остаётся как есть
  FormFieldValidator — чистые required/length/regex/min/max, общие для фронта и сервера

Mars.Forms.Front
  FormRenderer + FormFieldRow                             // секции открываются инлайн, без DynamicComponent
  IFormEditorLocator, FormFieldBinding (+ CommitAsync для тяжёлых редакторов)

Mars.Forms.Abstractions
  IFormRuleRegistry (только unique и правила провайдеров), IFormValidator
  IFormProvider.BuildFormAsync(ctx)                       // без Read/Submit
```

Параметры поля (`Editor`, `Rules`) хранятся там же, где и сейчас: у метаполя — в
`meta_fields.Options`, у системного слота — в `post_types.Options["systemFields"]`.
Два ключа `Options` сохраняются (две страницы-редактора не затирают друг друга),
но в коде это один тип: дескриптор с уже разрешёнными значениями.

## Шаги

### R0 — зачистка спекуляции (поведение не меняется)

Удаляем то, у чего нет потребителя: `IFormDataProvider.ReadAsync/SubmitAsync`,
`FormSubmitResult`, `FormProviderCapabilities.CanReadValues/CanSubmit`,
`FormContext.Parameters|Parameter|LangCode`, `FormValues.OwnerId`, `IFormContainerLocator`
(+ `FormContainerLocator`, инлайн-рендер секции в `FormItems.razor`), `Manifest.ContainerKinds`
и `Manifest.RuleTypes|EditorKeys`, если дизайнер их не читает.

Проверка: `dotnet build Mars.slnx` (0 ошибок) + `Mars.Forms.Tests`.
Кандидаты, которые остаются до своих шагов: legacy `FormItem.Rules|Editor` и
`FormFieldSettings` — R1 (нужна миграция чтения), `IFormFieldTypeSettingsLocator` — R1,
`FormValues`+кодек — R6.

Итог по `IFormFieldTypeSettingsLocator`: сохранён осознанно — см. «Решение: панели настроек —
не редакторы значений» в конце.

### R1 — один тип поля — выполнено 2026-09-10

- Убраны `FormFieldDescriptor.SettingsOnForm` и легаси `FormItem.Rules|Editor`: раскладка
  не несёт ничего, кроме представления (порядок, зона, видимость, ширина, секции), а правила
  и редактор всегда приходят в дескрипторе. Бейдж `meta` в дизайнере убран: параметры любого
  поля правятся в его собственном хранилище, различать «где» больше не нужно.
- `FormValidator` применяет только `field.Rules`; `PostFormRulesValidator.RulesOnly` отбирает
  слоты по ключу транспорта и наличию правил — без флага.
- `FormFieldSettings` остаётся **складской** записью параметров слота
  (`Options["systemFields"]`, три поля): это не второе описание поля, а его DTO,
  который растворяется в дескрипторе при сборке (`PostFormBuilder.SlotItem`).
- Легаси-материализация `GetEffectiveSystemFields` удалена вместе с компенсацией
  в `UpdatePresentation`: раскладка структурно не может нести правила, читать их неоткуда.
  **Следствие (сознательное):** правила, сохранённые только в раскладке окном между
  фазой 3 и этапом A и после этого не пересохранённые, при чтении не поднимутся — их нужно
  задать заново в параметрах поля.
- `FormFieldDefinition` остаётся моделью редактора определений (не вторым описанием рантайма).

Проверка: `dotnet build Mars.slnx` — 0 ошибок; `Mars.Forms.Tests` 82/82;
`Mars.Server.Tests` 476/476.

### R2 — значения в модели — выполнено 2026-09-10

- Форма поста больше не держит параллельный мешок значений: `PostFormValueStore` читает и пишет
  свойства `PostEditModel` напрямую (системные слоты — типизированные свойства, метаполя —
  строки `MetaValues`). Удалены `PostEditModel.BuildFormValues`/`FillFormValues`/`ApplyFormValues`
  и неиспользуемый `MetaValuesByIndex`.
- Каскадов в зоне формы поста: пять → два. Один типизированный `PostFormContext` (модель,
  значения, хуки записи, holder контента) и один `MetaValueContext` (значения + определения полей) —
  последний общий с формами пользователей и категорий.
- Реестр тяжёлых редакторов (`IHeavyMetaValueEditors` + `HeavyMetaValueEditorRegistry`) удалён:
  редакторы с отложенной записью регистрируют `CommitAsync` в `FormCommitHooks` рендерера
  (`FormRenderContext.Commits`), а страница забирает значения одним `CommitAllAsync()` перед
  сохранением. `MetaValuesForm.PullAsync` переименован в `CommitAllAsync`.
- Мета-редакторы (relation, file, children, примитивы) больше не читают значения и определения
  из каскадов: строки идут из стора (`Binding.Value`, `NativeValue`), определения — из
  `MetaValueContext`. Доменные редакторы (теги, категории, автор, контент) берут модель
  из `PostFormContext`.

Проверка: `dotnet build Mars.slnx` — 0 ошибок; `Mars.Forms.Tests` 82/82; `Mars.Server.Tests` 476/476.
Рендер формы поста и форма пользователя проверены E2E (`CreatePostTests`, `EditUserPageTests` — оба
зелёные, см. «E2E-проверка формы» в конце) — на первом прогоне они нашли два дефекта рефакторинга,
исправленных в R2–R5.

### R3 — плоский конфиг и рендерер — выполнено 2026-09-10

- `FormItem` стал плоским: `Kind`, `Items`, `Collapsed` убраны, вместо них маркер секции
  `SectionTitle` (`IsSectionHeader`). Секция — граница в списке, а не узел дерева.
- `FormDefinitionNormalizer`: 126 → ~70 строк, без вложенности, бакетов по зонам и предела
  глубины (`FormNormalizeOptions` с `MaxSectionDepth` удалён).
- Старые раскладки не теряют порядок: `FormLayoutJson.Parse` разворачивает вложенное дерево
  секций в маркеры — легаси-чтение собрано в одном месте.
- Рендерер: `FormItems` и `FormSectionBlock` удалены, `FormRenderer` сам открывает секции
  (заголовок во всю ширину, поля за ним). Свёрнутость секции ушла вместе с `Collapsed`.
- Дизайнер переписан под плоский список: «вверх/вниз» — сосед по зоне, «удалить секцию» просто
  убирает маркер (поля остаются на местах); операций «в секцию/из секции», пересчёта зон детей
  и нормализации глубины больше нет. `FormLayoutEditor` 313 → 246 строк, `FormLayoutRow` без
  рекурсии. Поля секции по-прежнему рисуются с отступом (`EntriesOf` считает depth после маркера).

Проверка: `dotnet build Mars.slnx` — 0 ошибок; `Mars.Forms.Tests` 79/79;
`Mars.Server.Tests` 476/476. Визуальная проверка дизайнера — в запущенном приложении.

### R4 — правила на фронте — выполнено 2026-09-10

- Встроенные правила (required, regex, length, min, max) перенесены в
  `Mars.Forms.Contracts.FormRuleEvaluator` — чистые функции без доступа к данным: **один код
  на клиент и сервер**. `BuiltInFormRules` удалён, в `IFormRuleRegistry` остались только правила
  провайдеров (`unique` и подобные), `FormValidator` считает встроенные напрямую, а неизвестные
  типа ищет в реестре.
- Фронт проверяет мгновенно: `FormFieldBinding.ValidationErrors` прогоняет правила поля тем же
  `FormRuleEvaluator`, `FormFieldRow` показывает сообщения рядом с полем. `required` на клиенте
  намеренно не проверяется — пустое поле не подсвечивается до сохранения, обязательность
  обеспечивает серверный валидатор.
- Шов записи не менялся: `PostFormRulesValidator` → `GeneralPostQueryValidator` (FluentValidation).

Проверка: сборка 0 ошибок; `Mars.Forms.Tests` 82/82 (добавлены `FormRuleEvaluatorTests`);
`Mars.Server.Tests` 476/476.

### R5 — общие редакторы вместо постовых — выполнено 2026-09-10

- `PostTagsEditor` → `FormTagsEditor` в `Mars.Admin.Framework` под ключом `core.input.tags`:
  значение — `string[]` из стора, привязки к посту нет.
- `PostAuthorEditor` → `FormTextDisplayEditor` под ключом `core.display.text` (только чтение):
  стор поста отдаёт для слота автора отображаемое имя, а не Guid. Пикер пользователя появится
  отдельным редактором-связью.
- `PostFormEditors` сократился до постовых ключей (`title`, `status`, `categories`); пикер
  категорий остаётся редактором провайдера — он привязан к типу поста. Постовая папка `Forms/`
  больше не держит редакторов значений.

Проверка: `dotnet build Mars.slnx` — 0 ошибок; `Mars.Forms.Tests` 82/82;
`Mars.Server.Tests` 476/476. Теги и автор в форме поста проверены E2E `CreatePostTests` (теги
сохраняются) — см. «E2E-проверка формы».

### R6 — строгий кодек — выполнено 2026-09-10

- Толерантные ветки чтения убраны: `TryReadString` больше не принимает Guid/даты/числа/булевы,
  `TryReadLong` — decimal/double/строку/булево, `TryReadDecimal` — json-число, `TryReadDate` —
  уже разобранные `DateTime`/`DateTimeOffset`. Одна каноническая форма на тип, отклонение —
  ошибка формата с внятным текстом («ожидается строка», «ожидается строка-Guid»,
  «ожидается дата ISO-8601 строкой», «ожидается строка с числом (decimal передаётся строкой)»).
- `TryReadRawDecimal` («угадай число из json-представления») удалён.
- Целые принимаются как `long` или `int`, вещественные — как `double` или `float`:
  это один и тот же json-номер, а не другая кодировка.
- Файл: 452 → 390 строк, и главное — из него ушёл код, который молча приводил неверную форму
  значения к «похожей».

Проверка: `dotnet build Mars.slnx` — 0 ошибок; `Mars.Forms.Tests` 82/82 (три теста
толерантности заменены на проверки отказа); `Mars.Server.Tests` 476/476.

## Итог R0–R6 (2026-09-10)

| Что было | Что стало |
|---|---|
| `FormProviderManifest` + `FormProviderCapabilities` (нигде не читались) | зоны прямо в `FormDefinition.Zones` |
| `IFormDataProvider.Read/Submit`, `FormSubmitResult` | один `GetFormAsync` |
| Флаг `SettingsOnForm` + легаси rules/editor в раскладке + `GetEffectiveSystemFields` | раскладка — только представление |
| Пять каскадов в `PostFormZone`, мешок значений рядом с моделью | `PostFormContext` + `MetaValueContext`, значения — свойства модели |
| `IHeavyMetaValueEditors` + `IHeavyMetaValueEditor`-реестр | `FormCommitHooks` в контексте рендера |
| Дерево секций, `Kind`/`Items`/`Collapsed`, 126-строчный нормализатор | плоский список с маркерами, ~70 строк |
| `FormItems` + `FormSectionBlock` + `DynamicComponent` для секции | один `FormRenderer` |
| Встроенные правила в реестре (только сервер) | `FormRuleEvaluator` — общий код фронта и сервера |
| Постовые редакторы тегов/автора | `core.input.tags`, `core.display.text` в общей библиотеке |
| `FormValueCodec` 452 строки «угадай тип» | 390 строк строгой канонической конверсии |

Фазы 4–6 исходного плана (автономные формы, динамические ноды, внешние SQL) не начинались —
теперь их можно вести на упрощённом ядре.

## R7 — контент в системные поля — выполнено 2026-09-11

Контент был метаполем с маркером `featureKey=content`, но значение всегда жило в колонке
`posts.Content` — двойная природа тянула спец-кейсы во все слои. Теперь это обычный системный
слот; решено с пользователем: настройки редактора — в карточке «Системные поля», фича «Контент»
остаётся переключателем.

- `SystemFieldsCatalog`: слот `content` (`AppRes.Content`, `Text`, зона `main`, фича `Content`,
  редактор по умолчанию — блочный). Ключ раскладки тот же — сохранённые порядок/зона/видимость
  работают без правок.
- Параметры слота — в `post_types.Options["systemFields"]`: `editor` (обычный текст, WYSIWYG (Quill),
  код (Monaco), блочный (Editor.js)) и новый `codeLang` в `FormFieldSettings`. Страница типа
  показывает их в существующей строке «Системные поля»; язык кода — панель
  `PostContentSettingsPanel` (реестр панелей определений, скоуп `post.systemfields`).
- Редакторы контента стали редакторами общего слоя: `PostContentEditor` зарегистрирован под теми же
  тремя ключами `core.*` для `FormFieldType.Text` и работает через `FormFieldBinding`; ветку выбирает
  ключ редактора, язык кода приезжает дескриптором (`Options.codeLang`). Пустой ключ рисует штатный
  многострочный редактор.
- Удалены: маркер `FeatureFieldsCatalog.Content*` и `PostTypeFeatureFields.ApplyFeatureContent`
  (+ автосоздание поля при включении фичи), требования полей контента в валидаторах типа,
  `contentFieldKey` во всех сервисах (`MetaValuesValidator`, `MetaValuesEnricher`, `PostJsonService`,
  `PostService.StripContentFieldValue`, `PostMetaColumnsService`, `MetaValuesGeneratorService`),
  `FeatureFieldsCatalog.PostImage`-соседи не тронуты, `FormFieldDefinition.KeyLocked` (осталось без
  производителей), `PostFormField.razor` (контент больше не ветка шаблона — зона рендерит `FormRenderer`
  как все), контент из сидов.
- Правила слота контента теперь работают и на сервере: `PostFormRulesValidator` знает свойство
  транспорта `CreatePostQuery.Content` (раньше слот был исключён из-за мета-природы).
- **Миграции данных нет** (решение пользователя): переносить настройки из старых строк
  `meta_fields` не требуется. Следствие: на БД, где поле контента уже создано, оно останется
  метаполем и продублирует слот — такое поле надо удалить в типах поста (или пересоздать БД).

Проверка: сборка 0 ошибок; `Mars.Forms.Tests` 82/82; `Mars.Server.Tests` 470/470 (7 тестов контента
удалены, 1 добавлен); интеграционные `Controllers.PostTypes` 19/19, `Nodes` 14/14; E2E
`CreatePostTests` и `EditUserPageTests` — зелёные. E2E нашёл регрессию рефакторинга: редактор контента
держал локальный буфер и синхронизировал его из стора, который до коммита пуст, — при любом ре-рендере
(ввод тегов) буфер затирался и контент сохранялся пустым. Буфер убран: блочный редактор пишет в
модель сразу (как раньше), WYSIWYG и код — при коммите перед сохранением.

## R8 — метаполя стали данными, редакторы — общими — выполнено 2026-09-11

Редакторы значения жили в двух мирах: у метаполей свой реестр (`MetaFieldEditors`, контракт
`Value`/`ValueChanged` с EAV-строкой) и девять компонентов, у системных слотов — общий реестр
формы (`FormEditorLocator`) и один компонент контента, ветвившийся по ключу внутри. Теперь
редактор значения один на все поля: общий реестр, контракт `Binding`, канонические CLR-значения
(`FormValueCodec`), а метаполе для формы — обычное поле данных.

- `MetaValueStore` (`Mars.Admin.Framework`) — единственное место перевода EAV-строк метаполя в
  канонические значения формы и обратно: строка/число/дата, ключ варианта `Select`
  (в EAV — `VariantId`), список ключей `SelectMany` (в EAV — `VariantsIds`), Guid связи,
  множественные — строками по индексу (`Id` строки сохраняется при записи, лишние строки
  снимаются). Строки остались носителем значения (`NativeValue`) — связи, медиа и списки
  объектов правят их напрямую; стор формы поста делегирует метаполя этому же классу.
- Удалены: девять мета-редакторов (`MetaValue*Editor`), реестр `MetaFieldEditors`,
  `RowMetaValue`, `MetaValueRowEditor`, мёртвый `IHeavyMetaValueEditor`,
  `MetaValueEditModelLookup` и осиротевший маркер `IFormSelfLabeledEditor` (подпись и описание
  поля рисует только `FormFieldRow`). Встроенные редакторы общего слоя рисуют и метаполя, и
  системные слоты: текст, число, дата, выбор, чекбоксы вариантов, множественный список.
- `PostContentEditor` разбит на общие редакторы: `FormWysiwygEditor`, `FormCodeEditor`
  (язык из `Options.codeLang` + селект в редакторе, Ctrl+S сохраняет форму), `FormBlockEditor`
  (админка — EditorJsBlazored), `FormColorEditor`, `FormUrlEditor`, `FormEmailEditor`,
  `FormTimeEditor`, `FormDateTimeEditor`. Регистрируются для String/Text/DateTime, и
  **в выборе редактора поля предлагаются только зарегистрированные с названием** — встроенные
  дефолты и обёртки провайдеров в список не попадают (`EditorsFor` = предложение UI).
- Выбор редактора больше не захардкожен под контент: любой текстовый слот типа поста
  (`content`, `excerpt`, будущие) получает тот же список из реестра; `FormFieldDefinition.Editors`
  и `SystemFieldsCatalog.ContentEditors` удалены как дубль реестра.
- ИИ-мост: вместо `PostContentEditorHolder` — `FormLiveEditors` в контексте рендера (редактор
  поля регистрируется по ключу и принимает значение извне, там же `SaveRequest`); панель языка
  кода — `SystemFieldSettingsPanel` (скоуп `post.systemfields`), общая для любого системного
  слота с редактором «код»; `SystemFieldsCatalog.ContentEditorKey/ContentCodeLang` →
  `EditorKey`/`CodeLang` по ключу слота.
- В дескрипторе метаполя `Editor` = `Options.editor`, а для связей и медиа — доменные
  `MetaFormEditors` (`core.meta.relation[.multi]`, `core.meta.file[.multi]`): только у них нет
  типизированного значения. Обёртки примитивов (`core.meta.value[.multi]`) удалены.
- Каталог ключей один: `FormEditorCatalog` (`Mars.Forms.Contracts`) вместо мета-каталога —
  ключи редакторов (цвет, ссылка, Email, время, дата-время, WYSIWYG/код/блочный) и параметры
  поля (`Options.editor`/`Options.codeLang` с их ридерами) стали общими для всех провайдеров.
  Следствие: любой текстовый слот типа поста выбирает любой из них — например, для `title`
  доступен редактор «Цвет», а коллизия `core.input.date` (см. FormEnginePlan, этап C) снята:
  редактор даты один.
- Реестры (`FormEditorLocator`, `FormFieldTypeSettingsLocator`) — экземпляры-синглтоны из DI,
  как локатор типов нод (`NodesLocator`): зарегистрировать редактор или панель можно откуда
  угодно и когда угодно (`Register`), а словарь собирается в момент запроса и подменяется
  атомарно — поэтому регистрация из плагина после старта тоже видна. Статических реестров
  в слое форм не осталось, тесты изолированы (каждый собирает свой реестр).

Проверка: сборка 0 ошибок; `Mars.Forms.Tests` 82/82; `Mars.Server.Tests` 471/471;
`Mars.Admin.Framework.Tests` 34/34 (новый `MetaValueStoreTests`: соответствие типов, индексы,
ключи вариантов, сохранение `Id` строк); интеграционные `Controllers.PostTypes` 19/19 и
`Nodes` 14/14; E2E `CreatePostTests`, `EditUserPageTests`, новый `EditPostMetaFieldsTests`
(тип поста дополняется метаполями через API, значения правятся общими редакторами, сохраняются
в EAV) и новый `EditPostSystemFieldEditorTests` (системный слот `title` с редактором «Цвет»:
рендер и сохранение) — зелёные. Позже в R8 добавились тесты локаторов
(`FormEditorLocatorTests` — `Mars.Forms.Tests` 90/90, `FormFieldTypeSettingsLocatorTests`).

## R9 — метаполя полностью в FormEngine — выполнено 2026-09-11

Последним «вторым миром» был каскад `MetaValueContext`: редакторы связи и медиа брали из него
определения полей (`MetaFieldEditModel`) и правили EAV-строки (`MetaValueEditModel`) в обход
привязки. Теперь редактор значения один для всех полей: настройки — из дескриптора, значения —
через `FormFieldBinding`, про строки и определения не знает никто, кроме стора.

- Удалён `MetaValueContext` вместе с каскадами в `PostFormZone` и `MetaValuesForm` и свойством
  `PostFormContext.Meta`.
- Настройки полей связи/медиа читаются из дескриптора: `ModelName`, `Multiple`, `Type`
  (Image/File) и `Options.kind|removeMode|uploadFolder|dropZone|viewMode`;
  `MetaValueListHelper.ResolveRemoveMode` принимает `FormFieldDescriptor`.
- Значения — через привязку: `Binding.Value` (одиночная связь/файл) и
  `Binding.Values.SetList` (список идентификаторов, порядок = порядок значений). Компоненты
  `MetaValueRelationSingle`/`RelationMulti`/`FileMulti`/`ChildrenList` больше не создают и не
  правят строки `MetaValueEditModel`, а `MetaValueFileEditor` рисует пикер и без готовой строки.
- Заглушки «не выбрано» у необязательных связей — забота стора: `MetaValueStore` не пишет пустое
  значение (`SetValue`/`SetList` для nullable relation/file/image) и снимает уже существующие
  строки при чтении, поэтому отдельного шага «purge» в редакторах нет.
- `IFormValueStore.NativeValue` (шов под прямой доступ к строкам) удалён — после перевода
  редакторов на привязку у него не осталось потребителей: убран из интерфейса и всех трёх
  реализаций (`PostFormValueStore`, `MetaValueStore`, `FormValuesModel`).
- `MetaValuesForm` стал тонкой обёрткой над `FormRenderer` (дерево + стор + хуки записи).

Проверка: сборка `Mars.slnx` — 0 ошибок; `Mars.Forms.Tests` 90/90; `Mars.Admin.Framework.Tests`
37/37 (четыре новых теста стора: заглушка nullable-связи — не значение, очистка убирает строку,
обязательная связь строку сохраняет ради валидации, пустые элементы списка не хранятся);
`Mars.Server.Tests` 473/473. E2E: `CreatePostTests`, `EditPostMetaFieldsTests`, `EditUserPageTests`,
`EditPostSystemFieldEditorTests` и новый `EditPostRelationFieldTests` (поле-связь: рендер
редактора из дескриптора, выбор цели пикером, запись `ModelId` в EAV) — зелёные.

## R10 — раскладка уходит из контракта данных, параметры слотов остаются — выполнено 2026-09-12

`PostTypeDetailResponse` (`GET api/PostType/{id}`) — контракт данных типа, но он нёс два
«редакторских» поля: `Form` (сохранённая раскладка) и `SystemFields` (параметры слотов).
Потребители у них оказались разные, поэтому и решения разные.

1. **`Form` убран из ответа — читателей не было.** Раскладку пишет и читает страница презентации
   своей парой контрактов: `UpdatePostTypePresentationRequest.Form` → `GET presentation/edit`
   (`PostTypePresentationEditViewModel.FormLayout`); диалог раскладки из формы поста ходит туда же.
   `PostTypeDetailResponse.Form` заполнялся маппингом и не читался нигде — в контрактах раскладка
   была двойником.
2. **`SystemFields` в ответе оставлены (решение пользователя, 2026-09-12).** Контракт чтения
   симметричен записи: `CreatePostTypeRequest.SystemFields` и `UpdatePostTypeRequest.SystemFields`
   принимают параметры слотов, значит `GET` их отдаёт. Первоначальный вариант «параметры слотов
   тоже уехали в `PostTypeEditViewModel`» отменён: `PostTypeEditViewModel` вернулся к
   `{ PostType, MetaRelationModels }`, а `PostTypeEditModel.ToModel` берёт `SystemFields`
   из ответа, как раньше.
3. **Ключ редактора слота в форме поста берётся из дескриптора** (выбор при обсуждении R10).
   `PostFormBuilder.SlotItem` и раньше сводил `Options["systemFields"]` в
   `FormFieldDescriptor.Editor` (+`Options.codeLang`), поэтому сырые настройки в рендере не нужны:
   `EditPostView.ContentEditorKey` и ИИ-инструмент `MarsPostTools.CreatePost` читают
   `FormDefinition.Field(SystemFieldsCatalog.Content)?.Field?.Editor`. Это последнее место, где
   рендер поста читал настройки напрямую.

Что сделано:

- `PostTypeDetailResponse`: убрано `Form`, `SystemFields` оставлено (с комментарием про симметрию
  с запросами записи); удалён `PostTypeDetailResponseContentExtensions` — `ContentSettings` был
  мёртв, `ContentEditorKey`/`ContentCodeLang` заменены дескриптором.
- Хранильный `PostTypeDetail` не тронут (`SystemFields` нужны `PostFormBuilder.SlotItem`, `Form` —
  `PostFormBuilder.Build` и `GetPresentationEditModel`); легаси-комментарий про удалённый в R1
  `PostTypeOptionsCatalog.GetEffectiveSystemFields` убран.
- `PostTypeMapping.ToResponse(PostTypeDetail)` перестал маппить `Form` (одна строка).
- `Mars.Forms.Contracts`: `FormItemExtensions.Field(this FormDefinition, key)` — поиск листа-поля
  по ключу (нужен форме поста и ИИ-инструменту); покрыт `FormItemExtensionsTests`
  (поле находится среди структурных узлов сетки, структурный узел и неполевой элемент не находятся).
- `PostTypeEditModel`: `ContentEditorKey()`/`ContentCodeLang()` удалены как осиротевшие
  (страница типа читает редактор слота в панели настроек через `SystemFieldsCatalog`).
- `MarsPostTools` и `EditPostView` читают редактор из дескриптора; `ai/AiChatGuide.md` поправлен.
- У `PostTypeDetail`-extension оставлен только живой `ContentEditorKey` (`PostTransformer`,
  `BlockEditor1PostContentProcessor`, `PostTransformerTests`); мёртвые `ContentSettings`/
  `ContentCodeLang` удалены и там.

Проверка: `dotnet build Mars.slnx` — 0 ошибок; `Mars.Forms.Tests` 107/107;
`Mars.Admin.Framework.Tests` 50/50; `Mars.Server.Tests` 473/473; интеграционные
`Controllers.PostTypes.UpdatePostTypeTests` 3/3, `Services.PostTransformerTests` 1/1,
`Controllers.Posts` 32 (2 skipped). **Известный красный (не от R10):**
`UpdatePostTypePresentationTests.UpdatePostTypePresentation_WithFormLayout_StoresItInTypeOptions` —
ожидает `viewModel.Form.Items.First().Key == tags`, но после сеточной раскладки
(`fcc1308c…7b30268c`) элементы нормализуются в ряд+колонку, и первым идёт узел с сгенерированным
ключом. Ассершен старше сетки (последний коммит файла `e7b0920c` — предок `7b30268c`), правится
вместе с работой по `FormLayoutGridPlan.md`.

## Решение: панели настроек — не редакторы значений (зафиксировано 2026-09-12)

Вопрос, который план не фиксировал: почему доменные настройки поля
(`SystemFieldSettingsPanel`, `MetaFieldSettingsPanel`) — панели реестра
`IFormFieldTypeSettingsLocator`, а не «зарегистрированные редакторы» в `IFormEditorLocator`.
Незафиксированность дала расхождение: п.3 «Зачем» считал реестр панелей лишним, R0 отдал
`IFormFieldTypeSettingsLocator` в R1, целевая модель его не содержит — при этом он выжил и
работает (R7, R8), а в R1 про него не было ни слова. Ниже — решение и его техническая причина.

**Панель и редактор правят разные объекты.**

| | Редактор значения | Панель настроек |
|---|---|---|
| Что правит | значение поля в форме | настройку поля, точнее — доменную модель владельца |
| Контракт | один параметр `FormFieldBinding` (`Item` + `FormFieldDescriptor` + `IFormValueStore`) | `FormFieldDefinition` + `OnSourceChanged` |
| Ось регистрации | ключ редактора: `Register(editorKey, component, multiple, title, params FormFieldType[])` | скоуп + тип поля: `Register(scope, component, params FormFieldType[])` |
| Выбор | админ выбирает ключ (`FormFieldDefinition.Editor`, `EditorsFor`) | выбора нет: рисуются все панели типа (`PanelsFor`) |
| Слой | общий: `Mars.Forms.Front` → только `Mars.Forms.Contracts` | компонент админки/фреймворка, знает домен |
| Обратный вызов | значение уходит в стор, рендер приходит сам | `OnSourceChanged` + явный `StateHasChanged()` |

**Почему это нельзя сделать редактором** (не «не захотели», а не проходит):

1. **Доменная модель недоступна общему слою.** Панель берёт модель владельца из
   `FormFieldDefinition.Source` (`MetaFieldDefinitions.Fill` — `Source = field`;
   `PostTypeEditModel.BuildSystemFieldDefinitions` — `Source = this`) и пишет прямо в неё
   (`SetAsync(field => field.ModelName = value)`, `SetSystemFieldCodeLang`). Редактор значения
   получает `FormFieldDescriptor` — `record` с `init`-свойствами, то есть read-only снимок для
   отрисовки, — и живёт в слое, который не знает `MetaFieldEditModel`/`PostTypeEditModel`;
   протащить их в контракт общего слоя нельзя.
2. **Источник — единственное хранилище, определение — проекция.** Кодек
   `MetaFieldDefinitions.Apply`/`Fill` умеет переводить только общий поднабор параметров
   (Title/Key/Required/Editor/ModelName/Rules…); доменные вещи (цель связи с подтипами, `Kind`,
   `RemoveMode`, `ViewMode`, `DropZone`, генератор с префиксами по категориям, варианты,
   статусы, `codeLang`) в нём не описаны. Их правка обязана идти мимо определения — прямо в
   модель, а наружу вернуть «перечитай»: отсюда `OnSourceChanged` вместо `OnChanged` и явный
   `StateHasChanged()` в `FieldDefinitionRow.OnPanelChangedAsync` (параметры панели меняются
   по ссылке, без перерисовки строка осталась бы со старыми значениями).
3. **Один визуальный вид — разные хранилища, поэтому нужен скоуп.** «Язык кода» читается из
   `MetaFieldEditModel.CodeLang` для метаполя и из `post_types.Options["systemFields"]` для
   системного слота (`SystemFieldsCatalog.CodeLang` / `SetSystemFieldCodeLang`). С единственным
   ключом редактора компонент не смог бы выбрать хранилище; скоупы `meta` и `post.systemfields`
   различают как раз одинаковые панели.
4. **У панели есть контекст страницы, у редактора — нет.** Панель метаполя получает каскадом
   `MetaRelationModels` (цели связей) и инжектит `IDialogService` (выбор папки загрузки).
   Редактор значения рендерится в любой форме (пост, пользователь, категория) и должен
   обходиться только дескриптором.
5. **Ось «выбор» не подходит.** Редакторов одного типа у поля может быть несколько, и админ
   выбирает ключ; доменные настройки типа безусловны (у Relation всегда цель связи, у File
   всегда папка загрузки), выбирать нечего — отсюда регистрация по типу поля и «рисуются все».

**Граница зафиксирована в коде:** общие параметры (заголовок, ключ, описание, обязательность,
кратность, редактор, правила, min/max, порядок, теги) правит строка `FieldDefinitionRow`;
панель правит только то, чего нет в общем наборе, и общих параметров не дублирует.

**Итог по реестру.** Из шести реестров п.3 «Зачем» удалены `IFormContainerLocator` и
`IHeavyMetaValueEditors`; `IFormFieldTypeSettingsLocator` сохранён осознанно: это не дубль
`IFormEditorLocator`, а вторая ось того же механизма (скоуп + тип поля вместо ключа редактора).
Механика у обоих одна и намеренно одинаковая — синглтон из DI, `Register` в любой момент,
словарь собирается при запросе и подменяется атомарно («аналогично локатору редакторов»),
поэтому регистрация панели из плагина после старта тоже видна. Пункт R0 про
`IFormFieldTypeSettingsLocator` закрыт решением «оставить», а не удалением.

## E2E-проверка формы (рецепт)

Сьют `Mars.E2E.Tests` выключен по умолчанию: тесты помечены `[E2EFact]`, включение — переменная
окружения `MARS_E2E_TESTS=1` (как `MARS_DOCKER_TESTS` у контейнерных тестов). Порядок:

1. Задать окружение: в cmd — `set MARS_E2E_TESTS=1`, в pwsh — `$env:MARS_E2E_TESTS='1'` (в `pwsh -File test-all.ps1 -IncludeE2E` это делает сам скрипт).
2. **`dotnet build Mars.slnx`** — обязательно: WASM-админка не входит в сборку самого
   E2E-проекта, и без пересборки решения браузер получает старый бандл (в `_framework`
   fingerprint имён ассетов, устаревший манифест указывал бы на прежний `.wasm`).
3. `tests\Mars.E2E.Tests\bin\Debug\net10.0\Mars.E2E.Tests.exe -filter "/Mars.E2E.Tests/Mars.E2E.Tests.Tests/CreatePostTests/*"`
   (нужны Docker и системный Edge; окно браузера открывается видимым — так устроен сьют).

Набор формы (прогонять каждый фильтром отдельно): `CreatePostTests` (title, slug, контент, теги),
`EditPostMetaFieldsTests` (метаполя-примитивы в EAV), `EditPostRelationFieldTests` (поле-связь:
пикер и `ModelId`), `EditPostSystemFieldEditorTests` (редактор системного слота), `EditUserPageTests`
(форма пользователя с метаполями).

На прогоне 2026-09-10 зелёные `CreatePostTests` и `EditUserPageTests`. Он же нашёл два дефекта
рефакторинга, которых не видит юнит-слой: `PostContentEditor` остался на удалённом каскаде
`PostEditModel` (NRE при рендере контента) и `IFormValueStore.SetValue` не писал множественные
слоты — редактор тегов отдаёт массив целиком через `Binding.Value`, а модельный стор умел только
`SetList` (теги молча терялись при записи).

## Гардайлы

- Типизированные формы нод (`Mars.Nodes.FormEditor`) не трогаем.
- Фазы 4–6 исходного плана (автономные формы, динамические ноды, внешние SQL) не начинаем,
  пока R0–R3 не закрыты.
- Верификация точечная по затронутым областям + полная сборка `dotnet build Mars.slnx`.
- Внешний вид и поведение формы поста сохраняются: ключи полей (`title`, `slug`, …) —
  стабильные идентификаторы, на них завязаны E2E-селекторы.

## Дальше: конструктор раскладки (сетка)

Раскладка вырастает из плоского списка полей в сетку ряды/колонки с drag&drop, контейнерами-табами
и неполевыми элементами (заголовок, разделитель); ядро R0–R9 при этом не пересматривается.
Отдельный план — [FormLayoutGridPlan.md](./FormLayoutGridPlan.md) (решения: плоское хранение узлов
с `Parent`, дерево — модель потребителей, ширина у колонки, секции → заголовок/разделитель).
