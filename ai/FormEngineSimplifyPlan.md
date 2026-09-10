# План: упрощение FormEngine (Mars.Forms)

> **Статус: R0–R6 выполнены 2026-09-10** (согласовано в тот же день: полный рефактор ядра,
> значения — в модели, раскладка — плоский список с маркерами секций). Ветка
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
- Регистрация редакторов и панелей настроек — через DI (`AddFormEditor`,
  `AddFormFieldSettingsPanel`), а сами реестры (`FormEditorLocator`,
  `FormFieldTypeSettingsLocator`) — экземпляры-синглтоны с состоянием, собранным в конструкторе:
  статических реестров в слое форм не осталось, чтение потокобезопасно, тесты изолированы
  (каждый собирает свой реестр, а не мутирует общий).

Проверка: сборка 0 ошибок; `Mars.Forms.Tests` 82/82; `Mars.Server.Tests` 471/471;
`Mars.Admin.Framework.Tests` 34/34 (новый `MetaValueStoreTests`: соответствие типов, индексы,
ключи вариантов, сохранение `Id` строк); интеграционные `Controllers.PostTypes` 19/19 и
`Nodes` 14/14; E2E `CreatePostTests`, `EditUserPageTests`, новый `EditPostMetaFieldsTests`
(тип поста дополняется метаполями через API, значения правятся общими редакторами, сохраняются
в EAV) и новый `EditPostSystemFieldEditorTests` (системный слот `title` с редактором «Цвет»:
рендер и сохранение) — зелёные. Позже в R8 добавились тесты локаторов
(`FormEditorLocatorTests` — `Mars.Forms.Tests` 87/87, `FormFieldTypeSettingsLocatorTests`).

## E2E-проверка формы (рецепт)

Сьют `Mars.E2E.Tests` выключен по умолчанию: тесты помечены `[E2EFact]`, включение — переменная
окружения `MARS_E2E_TESTS=1` (как `MARS_DOCKER_TESTS` у контейнерных тестов). Порядок:

1. Задать окружение: в cmd — `set MARS_E2E_TESTS=1`, в pwsh — `$env:MARS_E2E_TESTS='1'` (в `pwsh -File test-all.ps1 -IncludeE2E` это делает сам скрипт).
2. **`dotnet build Mars.slnx`** — обязательно: WASM-админка не входит в сборку самого
   E2E-проекта, и без пересборки решения браузер получает старый бандл (в `_framework`
   fingerprint имён ассетов, устаревший манифест указывал бы на прежний `.wasm`).
3. `tests\Mars.E2E.Tests\bin\Debug\net10.0\Mars.E2E.Tests.exe -filter "/Mars.E2E.Tests/Mars.E2E.Tests.Tests/CreatePostTests/*"`
   (нужны Docker и системный Edge; окно браузера открывается видимым — так устроен сьют).

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
