# План: упрощение FormEngine (Mars.Forms)

> **Статус: согласовано 2026-09-10.** Пользователь выбрал полный рефактор ядра, значения —
> в модели (форма как проекция), раскладка — плоский список с маркерами секций. Шаги R0–R6.
> План **заменяет** «этапы A–D» и фазы C/D из [FormEnginePlan.md](./FormEnginePlan.md);
> тот файл остаётся историей проектирования и as-is-инвентарём (ссылаться на него за
> обоснованием «почему так вышло»), но работы ведутся по этому плану.

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

### R1 — один тип поля

- `FormFieldSettings` растворяется в дескрипторе; признак `SettingsOnForm` уходит
  (параметры всегда берутся у владельца поля).
- Legacy `FormItem.Rules|Editor`, перенос их нормализатором, `GetEffectiveSystemFields`
  и компенсационная запись в `UpdatePresentation` — удаляются. Старые раскладки читаются
  однократным мерджем в `systemFields` (jsonb, миграция БД не нужна).
- `FormFieldDefinition` остаётся только моделью редактора определений (не вторым описанием
  поля рантайма); набор capabilities сокращается.

Проверка: `PostFormBuilderTests`, `FormDefinitionNormalizerTests`, `PostTypeOptionsCatalogTests`.

### R2 — значения в модели

- `PostFormValueStore : IFormValueStore` поверх `PostEditModel` (системные слоты —
  типизированные свойства, метаполя — строки `MetaValues`).
- `PostFormZone` теряет все пять `CascadingValue` и сводится к `<FormRenderer/>`.
- Контент регистрируется обычным ключом редактора, `PostContentEditorHolder` удаляется.
- Вместо `IHeavyMetaValueEditors` с pull-протоколом — один `CommitAsync` у биндинга,
  который собирает рендерер.
- `FormValues`/`FormValueCodec` остаются серверным правилам и будущим динамическим формам.

Проверка: `Mars.Server.Tests` (Forms), E2E `CreatePostTests` (здесь ловился цикл тегов),
`HandlebarsAppFrontTests` (регрессия рендера фронта).

### R3 — плоский конфиг и рендерер

- Дерево → плоский список с маркерами секций; `FormDefinitionNormalizer` из 126 строк
  сжимается до ~40 («отбросить неизвестные, применить порядок, дописать недостающие»).
- Рендерер: `FormRenderer` открывает секции инлайн; удаляются `FormItems`, `FormSectionBlock`,
  `FormRenderContext.FieldTemplate`, `IFormSelfLabeledEditor`.
- Дизайнер `FormLayoutEditor` переписывается под плоский список (вверх/вниз, зона, ширина,
  видимость, вставить/удалить/переименовать секцию); уходят маппинг дерево↔`LayoutRow`
  и логика «забрать/выпустить поле из секции».

Проверка: E2E рендера админ-формы + переписанные тесты нормализатора.

### R4 — правила на фронте

- `FormFieldValidator` в `Contracts`: клиент показывает ошибки мгновенно по простым правилам,
  сервер вызывает те же функции в `FormValidator`.
- Шов `PostFormRulesValidator` → `GeneralPostQueryValidator` (FluentValidation) сохраняется.
- `IFormRuleRegistry` — только `unique` и правила провайдеров.

### R5 — общие редакторы вместо постовых

- `PostTagsEditor` → `core.input.tags` (`string[]`), `PostAuthorEditor` → `core.display.author`
  в `Mars.Admin.Framework`, без чтения `PostEditModel`.
- Пикер категорий остаётся доменным редактором провайдера, но настройки берёт из дескриптора
  (`ModelName`), а не из каскада; постовая папка `Forms/` от них очищается.

### R6 — строгий кодек

- Убрать толерантные ветки чтения (`TryReadLong` с bool/double/raw, `TryReadString`
  с Guid/датой/числом): на проводе значение уже канонично, на неизвестную форму — внятная
  ошибка формата. 452 строки → ~100.

Проверка: `FormValueCodecTests` переписываются на строгие случаи + случаи отказа.

## Гардайлы

- Типизированные формы нод (`Mars.Nodes.FormEditor`) не трогаем.
- Фазы 4–6 исходного плана (автономные формы, динамические ноды, внешние SQL) не начинаем,
  пока R0–R3 не закрыты.
- Верификация точечная по затронутым областям + полная сборка `dotnet build Mars.slnx`.
- Внешний вид и поведение формы поста сохраняются: ключи полей (`title`, `slug`, …) —
  стабильные идентификаторы, на них завязаны E2E-селекторы.
