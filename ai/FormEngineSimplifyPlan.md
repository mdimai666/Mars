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
Автотестами не покрыт рендер формы поста (UI) — проверяется визуально в запущенном приложении.

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
