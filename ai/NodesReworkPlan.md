# План: реворк полей нод — от одного `Payload` к источникам значений

> **Статус: этап 1 выполнен (шаги 1–4 и 6), шаг 5 отложен; дизайн этапа 2 согласован — 2026-09-14, ветка `ai/nodes-rework`.**
> Задача-источник: запрос пользователя «придумать систему использования переменной или полей входящих данных»
> (2026-09-14) — у каждого входного поля ноды должно быть не только константное значение, но и выражение /
> ссылка на поле сообщения (как `typedInput` в Node-RED и UI-mapper в n8n). Черновик `InputSource<T>` и
> `enum InputType` уже лежали неиспользуемыми в `InjectNode.cs`.
> Дизайн-обсуждение 2026-09-14 (три раунда): эталоны Node-RED / n8n / Power Automate, две ортогональные оси
> («тип значения» vs «источник значения»), варианты семантики полей A/B/C.
> **Этап 1 — «издалека»:** у `InjectNode` вместо одного `Payload` появился список полей
> (`Key` / `VarType` / `Value`) с drag&drop; значения пока только константы. Механика обкатана на Inject,
> обобщение на другие ноды — этапы 2+.

## Принятые решения (2026-09-14)

1. **Семантика полей — вариант A («Context»)**: поле с `Key == "Payload"` (без учёта регистра) уходит в
   `msg.Payload`, все остальные — в `msg.Set(key, value)` (Context). Почему не все поля в объект `Payload`
   (вариант B): `FileWriteNodeImpl`, `TemplateNodeImpl`, `HttpRequestNodeImpl` ждут в `Payload`
   скаляр/строку/поток, а `NodeMsg.AsFullDict()` и `DynamicNodeMsgWrapper` уже читают Context как
   свойства `msg.<key>` — вариант B сломал бы их и существующие flows.
2. **Типы значений — словарь `VarNode`** (`int/long/float/double/decimal/bool/string/DateTime/Guid` +
   массивы, `VarNode.ListTypesSelect()`) **плюс `timestamp`**. Новый enum типов не заводим: `enum InputType`
   (`String, Number, Boolean, DateTime, Flow, Global`) смешивал две оси — «тип значения» и «источник
   значения»; `Flow/Global` — источники, `String/Number` — типы. Оси фиксируем как ортогональные, тип берём
   у `VarNode`, enum удалён.
3. **Legacy `Payload` удалён полностью** (решение пользователя 2026-09-14: «уберем легаси Payload, меня бесит
   столько кода ради него»). В `InjectNode` нет ни свойства `Payload`, ни JSON-хуков, ни fallback в impl;
   дефолт ноды — `Fields = [Payload]` типа `timestamp`, то есть «Inject без настройки отдаёт текущее время»
   сохранено, но уже явным типом, а не магией «пустая строка → сейчас».
   **Последствие:** у flows, сохранённых до этапа 1, ключ `payload` при чтении игнорируется (неизвестное
   свойство) — такая нода получит дефолтный `[Payload / timestamp]`. Миграции нет, решено сознательно.
4. **Порядок полей — только визуальный.** Drag&drop как UI-удобство и единообразие с `HtmlParseNodeForm`
   / `SwitchNodeForm` / `VariableSetNodeForm`; семантики порядок не несёт (Context — словарь).
5. **Имена элемента: `Key`, `VarType`, `Value`** — нейтральные, переживут добавление оси «источник»
   (этап 2): `Value` остаётся строкой, рядом появляется `ValueKind` (решения 9–10).
6. **Этап 1 — только `InjectNode`.** `TemplateNode`/`SwitchNode`/`StringNode` и остальные не трогаем.
7. **Тесты нод — с секциями `//Arrange` / `//Act` / `//Assert`** (указание пользователя 2026-09-14).
8. **Набор kind этапа 2 — `const | msg | flow | global | expression`** (позже `var`, `env`): дискретные
   источники полей дают дом `FieldPathPicker`, `expression` — свободные выражения вида
   `msg.Payload.Count() + 1`. Полный набор Node-RED (JSON/buffer/cred/…) не берём: большинство видов
   пока нечем наполнить.
9. **Хранение режима — плоско, строками: `ValueKind` + `Value`** (`ValueKind` — string в стиле `VarType`),
   без `InputSource<T>`. **Обратная совместимость не нужна** (решение пользователя 2026-09-14): ни
   `@`-алиаса в резолвере, ни миграций; `@`-конвенция умирает целиком при переезде нод на `ValueKind`.
10. **Редактор expression на старте — строка с автокомплит-попапом** и вставкой полей из пикера;
    Monaco — позже, вместе со схемой.

## Как было (до этапа 1)

- `InjectNode` — `src/Mars.Nodes/Mars.Nodes.Core/Nodes/Common/InjectNode.cs`: одно строковое `Payload`
  (пустая строка = «подставить timestamp»), `RunAtStartup`, `StartupDelayMillis`, `IsSchedule`,
  `ScheduleCronMask`, `[Display(GroupName = "common")]`. В файле же лежали неиспользуемые черновики
  (`InputSource<T>` с `get()`/`sdsd()`, `InputSource`, `enum InputType`, `public class DrawNode`).
- `InjectNodeImpl` — `.../Mars.Nodes.Core.Implements/Nodes/Common/InjectNodeImpl.cs`: единственное действие —
  `input.Payload = string.IsNullOrEmpty(Node.Payload) ? DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() : Node.Payload`
  и `callback(input)`.
- Расписание/старт читают другие поля и не зависят от payload: `RunAtStartup`/`StartupDelayMillis` —
  `Mars.Nodes.Host/Services/NodeService.cs:399-403`; `IsSchedule`/`ScheduleCronMask` —
  `Mars.Nodes.Host/Scheduler/NodeSchedulerService.cs:31-61`.
- CLI `mars.exe node inject <IdOrName>` payload **не читает** — запускает ноду с пустым `NodeMsg`
  (`Mars.Nodes.Host/CommandLine/NodesCli.cs:79`), т.е. переход на список полей CLI не ломает.
- Форма — `.../Mars.Nodes.FormEditor/EditForms/Common/InjectNodeForm.razor`: один `FormItem2` на `Node.Payload`
  + чекбоксы. Единственная форма в проекте с `IStringLocalizer`, и её ключи (`delay millis`, `cron mask`,
  `RunAtStartup`, `IsSchedule`) в resx отсутствуют — локализация фактически не работает (вне объёма этапа 1).
- Готовые образцы списков: `FluentSortableList` + `ArrayUtil.MoveItem` (`Mars.Core/Utils/ArrayUtil.cs`) —
  `HtmlParseNodeForm.razor` (`Node.InputMappings`), `SwitchNodeForm.razor` (`Node.Conditions`),
  `VariableSetNodeForm.razor` (`Node.Setters`, с `IValidatableObject` и `[ValidateComplexType]`).

---

## Этап 1 — `InjectNode`: список полей

### Шаг 1. Модель ✅

- [x] `InjectNodeField` — в `InjectNode.cs`: `Key`, `VarType` (дефолт `"string"`), `Value` (дефолт `""`),
      `[Display]` на каждом; `[Required]` на `Key`.
- [x] `InjectNode.Fields` — `InjectNodeField[]` через backing-поле: геттер отдаёт массив, сеттер
      нормализует `null` в `[]` (защита от `"fields": null` в json без хуков десериализации);
      дефолт — `[new() { Key = PayloadKey, VarType = VarNode.TimestampTypeName }]`; `[ValidateComplexType]`.
- [x] `InjectNode : IValidatableObject` — непустой список, уникальность `Key` (OrdinalIgnoreCase), формат
      `Key` (идентификатор: буква/`_`, далее буквы-цифры-`_`), допустимость `VarType` (`VarNode.IsValidVarType`).
- [x] Черновики `InputSource<T>`, `InputSource`, `InputType`, `DrawNode` и `#pragma` вокруг них удалены.
- [x] `timestamp` — добавлен в `VarNode` (`public const string TimestampTypeName`, запись
      `[TimestampTypeName] = typeof(long)` в `_typesDict`, `TimestampTypeName => 0L` в `GetTypeDefault`);
      из массиво-вариантов `ListTypesSelect()` исключён (`timestamp[]` бессмыслен).

### Шаг 2. Legacy `Payload` — ✅ удалён (не разворачиваем)

- [x] Свойство `Payload`, три JSON-хука (`IJsonOnDeserializing`/`IJsonOnSerializing`/`IJsonOnDeserialized`)
      и `using System.Text.Json.Serialization` из ноды убраны.
- [x] Примеры в `Mars.Nodes.Core/Examples/Nodes/` (9 мест в 8 файлах), задававшие `Payload = "..."`,
      переведены на новый формат: `Fields = [new() { Key = "Payload", Value = "..." }]`.
- [x] Проверено, что других потребителей `InjectNode.Payload` нет: `InjectNodeImpl` переписан, CLI payload
      не читает, плагинов с этим свойством нет (отсутствие выявила сборка — 9 ошибок только в примерах).

### Шаг 3. Исполнение (`InjectNodeImpl`) ✅

- [x] Итерация по `Node.Fields`: `Key == "Payload"` (OrdinalIgnoreCase) → `input.Payload`, иначе
      `input.Set(Key, value)`.
- [x] Конвертация по `VarType`: `string` — литерал «как есть»; числовые/`bool`/`DateTime`/`Guid`/массивы —
      `JsonSerializer.Deserialize` по `VarNode.ResolveClrType`; `timestamp` — пусто → «сейчас», иначе unix-millis.
- [x] Ошибка разбора → `NodeExecuteException` с именем поля и значением.
- [x] Спец-случаев нет: пустая строка остаётся пустой строкой, пустой `Fields` ничего не делает,
      `Payload` в списке отсутствует — `input.Payload` не трогается.

### Шаг 4. Форма ✅

- [x] `FluentSortableList` по `Node.Fields` (`Handle=true`, `OnUpdate` → `ArrayUtil.MoveItem`, `@key=item`):
      колонки хендл · `Key` · `VarType` (селект из `VarNode.ListTypesSelect()`) · `Value` · удалить.
- [x] Редактор `Value` по типу: `bool` — селект `true/false`; остальные — текст с placeholder по типу
      (`timestamp` — «empty = now (unix millis)»).
- [x] Строки валидации на элемент: `FluentValidationMessage` для `Key`, `VarType`, `Value`; кнопка «Add».
- [x] Подсказка в форме: `Payload` → `msg.Payload`, остальные ключи → `msg.<key>`.
- [x] Существующие контролы (`RunAtStartup`, `StartupDelayMillis`, `IsSchedule`, `ScheduleCronMask`) не тронуты.

Отличия от исходного наброска: для `bool` выбран `FluentSelect` вместо `FluentCheckbox` (значение хранится
строкой, двусторонняя привязка потребовала бы конвертеров); разворот в Monaco для массивов/объектов отложен
до этапа 3; `timestamp` пока только unix-millis.

### Шаг 5. Примеры и документация — ⏸ отложен

Отложено пользователем 2026-09-14 («мы пока протипируем и смысла писать доки нет»). Когда вернёмся:

- [ ] `Mars.Nodes.Core/Examples/Nodes/` — отдельный пример Inject с несколькими полями
      (`Payload` + `status` + `timestamp`), а не только переписанные существующие.
- [ ] `Mars.Nodes.FormEditor/wwwroot/docs/InjectNode/InjectNode.md` и `.ru.md` — список полей, типы,
      семантика Context, CLI `node inject`. `NodesDocTests` проверяет только наличие файлов.

### Шаг 6. Тесты и проверка ✅

- [x] `tests/Mars.Nodes.Tests/Nodes/InjectNodeTests.cs` — 14 тестов (`//Arrange`/`//Act`/`//Assert`):
      `Payload` → `msg.Payload`; прочие ключи → Context; типы (`int`/`double`/`bool`/`int[]`);
      дефолтная нода → timestamp; `timestamp` пустой → now и с значением → это значение; пустая строка
      остаётся пустой строкой; отсутствие поля `Payload` не трогает payload; пустой `Fields` не меняет
      сообщение; json без `fields` даёт дефолтное поле; round-trip; валидация (дубликат ключа, плохой ключ,
      неизвестный тип, дефолтная нода без ошибок).
- [x] Проверка: `dotnet build Mars.slnx` — 0 warnings / 0 errors; `Mars.Nodes.Tests` — **432/432**.
- [ ] UI — визуально при ручной проверке (`/dev/nodered`), отдельным прогоном не гоняем.

---

## Этап 2 — ось «источник значения» (дизайн согласован 2026-09-14, не в работе)

Два кастомных компонента (FluentUI не подходит: нужен гибрид input и дерева/попапа), оба тупые и
node-agnostic, место — `Mars.Nodes.FormEditor/EditForms/Components/`:

- **`FieldPathPicker`** — выбор поля. Пропсы: `Root` (сейчас только `msg`, позже `flow`/`global`),
  `Value` — путь без префикса (`Payload`, `items[0].name`), `Schema` (дерево, опционально),
  `ExpectedType` (фильтр по типу). UI: input + собственный попап (дерево из Schema + свободный ввод).
  Применения: правая часть kind=`msg`/`flow`/`global`, вставка в expression-редактор; целевая сторона
  (Key нод, VariableSet) — позже, `InjectNodeField.Key` пока остаётся простым идентификатором.
- **`ValueSourceEditor`** — источник значения. Слева кнопка-селект kind, справа редактор по kind:
  `const` — редактор по `VarType` (логика этапа 1), `msg`/`flow`/`global` — `FieldPathPicker`
  с фиксированным корнем, `expression` — строка с автокомплит-попапом и вставкой полей.
  `VarType` ортогонален: ожидаемый тип на выходе (конвертация/валидация); он же `ExpectedType` пикера.

Хранение: плоско, строками — `ValueKind` (`const|msg|flow|global|expression`) + `Value`; никакого
`InputSource<T>` (решение 9). Обратная совместимость не нужна: резолвер без `@`-алиаса, миграций нет;
`@`-конвенция (`VariableSetNodeImpl.ReadFieldAsExpression`, сейчас `FileWriteNodeImpl.FilePath` и
`HttpRequestNodeImpl.Url`) удаляется при переезде нод на `ValueKind`.

Резолвер — одна точка: скоуп вынимается из `VariableSetNodeImpl.CreateInterpreter` (`msg` через
`DynamicNodeMsgWrapper`, `flow`, `global`, `var`, `env`) в общий `ResolveScope`; kind `msg`/`flow`/
`global` компилируются в выражение `msg.<path>` / `flow.<path>` / `global.<path>`, т.е. весь резолвинг —
один путь через движок выражений; `const` — парсинг по `VarType` (логика из `InjectNodeImpl` выносится
в общее место). `msg.Payload.Count() + 1` — на прототипе проверить LINQ-extensions движка
(string как `IEnumerable<char>`).

Куда жить резолверу: `Mars.Nodes.Core` не ссылается на DynamicExpresso, а `XInterpreter` лежит в
`Mars.SiteEngine.Abstractions` (архдолг: ноды зависят от SiteEngine). Кандидат — новый проект
`Mars.Nodes.Expressions` (резолвер + скоуп + обёртка над движком); выбор движка (DynamicExpresso против
Roslyn `CSharpScript` из `FunctionNode`) — на старте шага 1.

Схема для пикера: фаза A — свободный ввод + автокомплит по JSON последнего входящего сообщения ноды
(INPUT-панель как в n8n; debug-сообщения уже несут JSON); фаза B — типизированные выходы (этап 4) и
фильтр по `ExpectedType`; ключи `flow`/`global` — из живого инстанса (Blazor Server in-proc), позже.

Прототип-порядок:

1. `ValueKind` у `InjectNodeField` + резолвер (`const`/`expression`) + тесты.
2. kind `msg` + `FieldPathPicker` (текст + простой автокомплит без схемы).
3. `ValueSourceEditor` в форме Inject.
4. Переезд на `ValueKind`: `SwitchNode.Conditions`, `EvalNode.Input` (сейчас «строка — всегда
   выражение»), `FileWriteNode.FilePath`, `HttpRequestNode.Url`; `@`-конвенция удаляется.

## Этап 3 — UI источников: добор (набросок)

Панель INPUT по последнему сообщению ноды, автокомплит `msg.` / `flow.` / `global.` / `VarNode.`,
Monaco для expression и для массивов/объектов в `const`. База (компоненты, резолвер) — из этапа 2.

## Этап 4 — типизированные выходы (набросок)

`NodeOutput.Type` / schema (`EndpointJsonSchemaTool.SimpleJsonSchema` уже есть для endpoint-нод),
вывод типов payload по графу проводов, валидация выражений до запуска; схема для `FieldPathPicker.Schema`.

## Грабли и риски

- `DynamicNodeMsgWrapper` поднимает свойства `Payload` на верхний уровень и **приоритет у свойств payload**
  над Context (`_allProperties` заполняется и теми, и другими) — Context-ключ с именем, совпадающим со
  свойством payload-объекта, будет затенён. Для Inject (payload — строка/число) не проявляется, но выстрелит
  при payload-объекте (кандидат в этап 3/4).
- `NodeMsg.AsFullDict()` кладёт `Payload` поверх копии `Context` — ключ Context с именем `Payload` был бы
  перекрыт (у нас он и должен уходить в payload, поведение согласовано).
- Добавление типа в `VarNode._typesDict` без правки `GetTypeDefault`/`ResolveDefault` — `NotImplementedException`
  в валидации/дефолтах (учтено для `timestamp`).
- `VarNode.IsValidVarType` — `internal static`: доступен внутри `Mars.Nodes.Core`, форме не нужен
  (ей хватает публичного `ListTypesSelect()`).
- Форма в списке: `FormItem2` не применять (в `SwitchNodeForm` он стоит с фиктивным `For="() => Node.Conditions"`);
  рабочий путь — placeholder + `FluentValidationMessage` на элемент.
- В `Mars.Nodes.Core` есть `Globals.cs` с `global using Mars.Nodes.Core.Nodes.Common` — поэтому в примерах
  `InjectNodeField` доступен без using (проверено при переводе примеров).

## Отклонённые альтернативы

- **Вариант B (все поля в объект `Payload`)** — даёт `msg.Payload.username`, но ломает ноды, ожидающие в
  `Payload` скаляр/строку/поток (`FileWriteNodeImpl`, `TemplateNodeImpl`, `HttpRequestNodeImpl`), и
  существующие flows. Выбран вариант A.
- **Свой enum типов (`InputType` из черновика)** — дублировал бы `VarNode` (9 типов + массивы) и смешивал
  ось типа с осью источника. Тип берём у `VarNode`, источники — отдельная ось (этап 2); enum удалён.
- **`List<InjectNodeField>` вместо массива** — в проекте для списков полей используются массивы
  (`T[]` + пересоздание через `[.. a, item]`); стиль сохранён.
- **`FormItem2` внутри списка** — не работает без выражения `For` на конкретный элемент.
- **JSON-хуки для чтения старого `Payload`** (были реализованы, затем удалены по решению пользователя):
  «столько кода ради него» — не оправдывает себя, данные старых flows с одним payload сознательно не
  переносятся.
- **`FluentCheckbox` для `bool`-значения** — значение поля хранится строкой; взят селект `true/false`.
- **Магия «пустая строка `Payload` → текущее время»** — заменена явным типом `timestamp` (в т.ч. в дефолте ноды).
- **`@`-префикс внутри строки как маркер выражения** (исходная идея пользователя): литерал, начинающийся
  с `@`, потребовал бы экранирования, JSON перестал бы быть самодокументированным. Взято отдельное
  свойство `ValueKind`; `@` остаётся только старой конвенцией двух нод до их переезда (решения 8–9).
- **Механизмы обратной совместимости старых flows** (дефолты инициализаторов под старый JSON, `@`-алиас
  в резолвере, миграции) — отклонены пользователем 2026-09-14: «обратная совместимость не нужны».
- **Monaco сразу для expression** — отложен: строка с автокомплит-попапом дешевле в прототипе, Monaco
  приходит вместе со схемой (этап 3).
- **Полный набор kind Node-RED (JSON/buffer/cred/env/…)** — не берём в старт этапа 2 (решение 8).

## Открытые вопросы

- Формат значения `timestamp` при явном вводе: пока только unix-millis (пусто = «сейчас»); нужен ли ISO-8601?
- Нужен ли предпросмотр собранного JSON прямо в форме (как OUTPUT-панель n8n) — в этап 1 не брали.
- Чистим ли нерабочие ключи локализации в `InjectNodeForm` (`delay millis`, `cron mask`) — вне объёма этапа 1.
- Массивы/объекты в значении поля сейчас вводятся сырым JSON-текстом — редактор в Monaco отложен до этапа 3.
- Старые flows с одним `payload` теряют значение (нода отдаёт timestamp) — закрыто окончательно:
  обратная совместимость не нужна (решение 9), миграций не будет нигде.
- Автокомплит этапа 2: откуда форма достаёт JSON последнего входящего сообщения ноды (debug-сообщения
  несут JSON, но механизма хранения/запроса в форму ещё нет) — проработать на шаге 2 прототипа.
