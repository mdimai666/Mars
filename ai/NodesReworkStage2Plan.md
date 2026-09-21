# План: этап 2 — контракты выходов нод и провайдер подсказок в поле значения

> **Статус: фазы A–D сделаны (A/B 2026-09-17, C 2026-09-21 — `9f3bead8`, D 2026-09-21 — `a0fa8aeb`),
> фаза E (пересборка DebugMode) и фаза F (DebugNode хранит полный объект) — сделаны 2026-09-22,
> фаза G (спеки и умные поля Common/Network) — сделана 2026-09-22.**
> Задача-источник: запрос пользователя «как будем делать провайдера подсказок?» (2026-09-17).
> Продолжение [NodesReworkPlan.md](./NodesReworkPlan.md): этапы 1–2 того плана (список полей в `InjectNode`,
> ось «источник значения»: `ValueKind`, `InputValueResolver`, `FieldPathPicker`, `ValueSourceEditor`) выполнены,
> компонент поля — `Mars.Nodes.FormEditor/EditForms/Components/MarsValueInput.razor`.
> **Этот план поглощает этапы 3–4 соседнего плана** (UI источников и типизированные выходы) — их чек-листы
> переезжают сюда, в `NodesReworkPlan.md` остаются только отсылки.

## Принятые решения (2026-09-17)

1. **Источник кандидатов — контракты выходов нод, а не наблюдённые данные.** Нода объявляет, что отдаёт:
   `InjectNode` знает свои поля, `HtmlParseNode` знает результат; у статических нод тип выхода объявляется
   **атрибутом с типом** (не со списком имён), и атрибутов может быть больше одного.
   `Payload : object` — не признак каждой ноды, а **fallback накопления**: показывается, только если `Payload`
   не объявил никто по цепочке проводов, иначе транзитная нода затирала бы точный тип предшественника.
2. **Ничего не накапливаем по умолчанию.** Последние входящие сообщения не храним; хранение включается
   только **DebugMode**.
3. **DebugMode в нодах** — режим, в котором записываются последние входящие; в нём пикер показывает
   не только поля, но и **значения**.
4. **Кэш — InMemory, TTL 10 минут**, только для DebugMode.
5. **Значения обрезаем:** строку — по длине, JSON — по глубине и по числу первых элементов.
6. **Список переменных контекстов считается на клиенте** (при первом открытии редактора клиент знает,
   какие переменные есть в `flow`/`global`); сами значения не нужны. Позже — обновление списка переменных
   по SignalR.
7. **Пакет контрактов** — если понадобится, контракты (провайдер, типы кандидатов) выносим отдельным проектом.

### Форма контракта (уточнено 2026-09-17 после обсуждения)

Корень имени — `NodeOutputValueSpec` (везде `Output`, чтобы не путать со старым `NodeOutput` из
`Mars.Nodes.Core/NodeOutput.cs` — это порт/провод). Два носителя, оба дают один плоский список полей:

- **Динамика — интерфейс на модели** (`Mars.Nodes.Core`): `INodeOutputValueSpec.GetOutputValueSpec()`.
  `InjectNode` отдаёт свои `InjectNodeField` (`Key` + `VarType`), `TemplateNode` — имя слота из `Node.Property`.
  Считается в браузере: инстанс ноды в редакторе уже есть, обращение к хосту не нужно.
- **Статика — атрибут** `[NodeOutputValueSpec(typeof(XxxDto), Name = "…")]` на модели/impl. Атрибут объявляет
  **тип**, а не имена: плоский список путей разворачивает читатель из типа
  (`Payload.status : string`, `Payload.items[].name : string`). `Name` опционален, по умолчанию `Payload`
  (нужен нодам, которые пишут не в `Payload` — например `HttpRequestNodeImpl` кладёт свой объект в отдельный слот).

```csharp
public interface INodeOutputValueSpec { IEnumerable<OutputValueSpec> GetOutputValueSpec(); }

public sealed class NodeOutputValueSpecAttribute : Attribute      // несколько на тип разрешено
{
    public NodeOutputValueSpecAttribute(Type valueType);
    public string Name { get; init; } = "Payload";
}

public record OutputValueSpec(string Path, string VarType, string? Description = null);
```

Свойства «что нода делает с сообщением» (`Flow`: declared/passthrough/sink) в контракте нет: нода, которая
ничего не объявила, ничего и не добавляет, а тупик виден по отсутствию исходящих проводов — провайдер это и так знает.

- **Читатель** `NodeOutputValueSpecReader.Read(node)` / `.ReadStatics(Type)`: интерфейс → атрибуты → пусто;
  пусто — это «ничего не добавляет», а не ошибка. `Fallback` = `Payload : object` предлагает провайдер
  накопления, не читатель. **Разворачиватель типа** `OutputValueSpecExpander`: `ClrType` → `OutputValueSpec[]`,
  обрез по глубине, защита от циклов, `[]` в пути = элемент массива; `Dictionary`/`JsonElement`/`JsonNode`/
  `NodeMsg` не разворачиваем (ключи неизвестны, свойства — шум). Оба в `Mars.Nodes.Core` (browser-safe).
- **Транспорт — расширение `Load()`** (`INodeServiceClient.Load()` → `NodesDataResponse`,
  `src/Mars.Nodes/Mars.Nodes.Core/Contracts/Nodes/NodeResponse.cs`; сервер — `NodeController.Load` →
  `NodeService.GetNodesData`, `src/Mars.Nodes/Mars.Nodes.Host/Services/NodeService.cs:254`):
  - `OutputValueSpecs` — **сгруппировано по типу ноды** (`TypeId → OutputValueSpec[]`), только статика:
    для нод с интерфейсом данных нет, их считает клиент (у него есть инстанс). Источник статики — атрибуты
    на impl-классах (`NodeImplementFactory` знает сборки impl'ов), `Name` слота сохраняется.
  - `GlobalVariableNames` — **только имена** живых переменных глобального контекста, без значений
    (`VariablesContextDictionary.Keys`, `NodeRuntime.GlobalContext`).
  - Имена flow-переменных пока не отдаём — сначала проверить, реализован ли flow-контекст (см. вопросы).
- **Имена типов** — строкой (`int`, `string[]`), тот же словарь, что в формате хранения. Обратного маппинга
  `ClrType → имя` в `VarNode._typesDict` нет — добавляем публичный `VarNode.GetVarTypeName(Type)`
  (`null`, неизвестный тип, `object`, DTO → `VarNode.ObjectTypeName` = `"object"`). В сам `_typesDict` `"object"`
  **не** добавляем: он тянет `ResolveDefault` (`GetTypeDefault` кидает `NotImplementedException`) и попадает
  в `ListTypesSelect()`; `object` — имя для спеки, а не валидный тип хранения.
- **Передача — плоский список путей, не JsonSchema.** Пикеру нужен плоский список (schema всё равно пришлось бы
  флэттить на клиенте); существующие генераторы схем в репо рукописные и server-side
  (`Mars.Nodes.Core.Implements/Utils/EndpointJsonSchemaTool.cs`,
  `Mars.Modules/Mars.SemanticKernel.Abstractions/Generators/ModelJsonSchemaGenerator.cs`) — из Core не
  переиспользуются; JSON-schema-типы не совпадают с нашими именами (`Guid`, `DateTime`, `timestamp`, `decimal`),
  маппинг всё равно писать. Валидация значений по схеме (для DebugMode) возможна позже и контракт не меняет.

## Как есть сейчас (проверено по коду 2026-09-17)

- **Пикер данных не имеет.** `MarsValueInput.Candidates` не прокидывается ни из одной формы,
  `EffectiveCandidates` уходит в статические моки `MarsValueInputMocks.cs`; в `InjectNodeForm.razor` поля
  объявлены без `Candidates`. Провайдера как интерфейса в репозитории нет.
- **Контрактов выходов нет.** У `Node` нет описания того, что нода отдаёт: есть только `[Display(GroupName)]`
  и `[FunctionApiDocument]`.
- **Debug-сообщения — только эфир.** `Mars.Nodes.Abstractions/Hubs/BroadcastHub.cs` шлёт `DebugMsg` в группу;
  клиент складывает их в приватный плоский `List<DebugMessage>` (`Mars.Nodes.Workspace/NodeEditor1.razor.cs`),
  индекса по `NodeId` нет, JSON формирует **только** `DebugNode`
  (`Mars.Nodes.Core.Implements/Nodes/Common/DebugNodeImpl.cs`), после реконнекта всё теряется.
- **`flow`/`global` наружу не выведены.** Данные есть в серверном `NodeRuntime`
  (`Mars.Nodes.Host/Services/NodeRuntime.cs`: `GlobalContext`, `FlowContexts`, `VarNodesDict`), но ни
  эндпоинта, ни DTO, ни CLI над ними нет.
- **`VarNode` — единственный корень, доступный клиенту целиком**: ноды приходят из flows.json,
  тип берётся из `VarType` (`Mars.Nodes.Core/Nodes/Common/VarNode.cs`).
- **Типов из данных нет.** `EndpointJsonSchemaTool.SimpleJsonSchema` только валидирует; инференса схемы из
  примера JSON нет; обратного маппинга ClrType → отображаемое имя тоже нет.
- **`Mars.Nodes.FormEditor` не ссылается на `Mars.Nodes.Expressions`** — переиспользовать `RootPathRegex`,
  `DynamicNodeMsgWrapper`, `VarNode.ResolveClrType` на клиенте нельзя (отсюда дублирование списка корней
  в `MarsValueInput.razor`, помеченное комментарием).

## Фаза A — контракты выходов нод

Пилот (2026-09-17): размечаем только `InjectNode`, `SwitchNode`, `MqttInNodeImpl`; остальные ноды — когда
заработает провайдер.

- [x] `INodeOutputValueSpec` + `OutputValueSpec` + `NodeOutputValueSpecAttribute` (имя слота опционально) —
      в `Mars.Nodes.Core` плоско, рядом с `NodeOutput.cs`.
- [x] `NodeOutputValueSpecReader` (`Read(node)` / `ReadStatics(type)`, кэш статики, `Fallback`) и
      `OutputValueSpecExpander` (тип → плоские пути: вложенность, массивы, обрез по глубине, циклы, скип-лист).
- [x] Публичный маппер `ClrType → VarType` (`VarNode.GetVarTypeName`) + `VarNode.ObjectTypeName`.
- [x] Пилот, динамика: `InjectNode` — интерфейс, спек собирается из `Fields` (`Key` + `VarType`).
- [x] Пилот, статика: `MqttInNodeImpl` — `[NodeOutputValueSpec(typeof(string))]` (его `Payload` — строка).
- [x] Пилот, транзит: `SwitchNode` не объявляет ничего (`SwitchNodeImpl` в msg ничего не пишет, его условия —
      это порты), тест фиксирует пустой спек.
- [ ] Не в пилоте, позже: `TemplateNode` (`Node.Property`), `ForeachNodeImpl` (`Set(cycle)`),
      `HttpRequestNodeImpl` (`Set(requestInfo)`, слот через `Name`), `CounterNode`, `QueueNode`, `DirReadNode`,
      `HtmlParseNode`.
- [x] Тесты: ридер (интерфейс / атрибут / пусто / несколько атрибутов / слот `Name`), разворот типа
      (вложенность, массивы, глубина, цикл, скип-лист), маппер имён.
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (481 / 0 / 0).

## Фаза B — провайдер подсказок на клиенте (без значений) — сделано 2026-09-17

- [x] Контракт провайдера — в `Mars.Nodes.Front.Abstractions/Services/IValueFieldProvider.cs`:
      `IValueFieldProvider`, `IValueRootProvider` (+ `Order`), `ValueFieldInfo(Path, VarType, Source, Value?)`,
      `ValueFieldContext(Nodes, EditedNode)`. **Граф передаётся параметром**, а не инжектится: `INodeEditorApi`
      не в DI (создаётся вручную в `NodeEditor1`, компонентам приходит cascading), иначе провайдер был бы
      нетестируемым.
- [x] Композит `ValueFieldProvider` (Workspace): порядок по `Order` (msg 0, FlowContext 10, GlobalContext 20,
      VarNode 30), дедуп по `Path` — ближайший/первый побеждает.
- [x] Провайдер `VarNode`: из графа, тип из `VarType`.
- [x] Провайдеры `FlowContext` / `GlobalContext`: имена из `VariableSetNode.Setters[].ValuePath` + живые имена
      global из `Load` (`IHostValueHints`); значения не читаем.
- [x] Провайдер `msg`: обход проводов вверх от редактируемой ноды, фильтр по порту выхода
      (`OutputPort` / `AllOutputPorts`), ближайшее объявление побеждает по каждому пути, `Fallback` только если
      `Payload` не объявлен; статика из специй хоста, если своя сборка типа ничего не объявила.
- [x] Подсказки тянет сам `MarsValueInput`: `[CascadingParameter] NodeEditContainer1` (контейнер отдаёт себя
      через `CascadingValue Value="this"`) + `For="() => X"` (как у `FormItem2`, тип `Expression<Func<object?>>`) —
      адрес поля. Параметр `Candidates` остался для переопределения. Компонент зовёт
      `NodeEditContainer1.GetValueFields(fieldName)` → `IValueFieldProvider` (резолв через `IServiceProvider`;
      пусто, если провайдера нет — хост без NodeWorkspace, напр. пререндер). Формы больше ничего не знают:
      `InjectNodeForm.razor` и `EvalNodeForm.razor` (там `InputTextArea` заменён на `MarsValueInput`) просто
      ставят `For`. Из `MarsValueInputMocks.cs` удалены `Fields`, тип `ValueFieldInfo` переехал в
      `Front.Abstractions` (моки остались только для операций `ƒ`).
- [x] `For` → `ValueFieldContext.FieldName` (имя члена модели), чтобы провайдер знал, куда пишет поле.
- [x] `Load()` расширен: `NodesDataResponse.OutputValueSpecs` (по `TypeId`) + `GlobalVariableNames`;
      сборка на хосте — `NodeService.CollectOutputValueSpecs` (модели из `INodesLocator` + impl'ы из
      `INodeImplementFactory.Dict`, маппинг impl → `NodeBaseType` → `TypeId`); клиент заполняет
      `IHostValueHints` в `Mars.Admin/Builder/NodeViews/NodeRedPage.razor.cs` и
      `devstands/StandNodesApp/StandNodesApp.Client/Pages/NodeRedPageContent.razor.cs`.
- [x] Тесты: `tests/Mars.Nodes.Tests/ValueFields/ValueFieldProviderTests.cs` (13) — сбор полей вверх,
      транзит, fallback, ближайший побеждает, порты, специи хоста, цикл, источник, три корня, композит.
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (497 / 0 / 0).
- [ ] Осталось из фазы B: кандидаты из собственных свойств ноды (`HttpRequest.Url`/`UrlKind`) — см. дополнения.

**Бюджет интеропа (инвариант):** данные тянем при открытии попапа и по кнопке refresh, фильтрация —
локальная. Набор текста не должен порождать обращений к провайдеру (см. `perf [nodes] value input interop budget`).
Специи и имена global приходят одним ответом `Load()`, провайдер считается в браузере без обращений к серверу.

## Фаза C — DebugMode, значения и INPUT-панель

Решения (согласовано 2026-09-17 после обсуждения):

1. **Один глобальный флаг DebugMode, не сохраняется** — после перезагрузки процесса снова выключен
   (singleton на сервере, без записи в flows.json и без опций). Тумблер — в редакторе.
2. **Записывает исполнитель, а не нода отладки**: захват в `NodeTaskJob`, внутри `callbackNext(e, output)` — то есть
   сохраняется всё, что нода **отдала дальше**, у каждой ноды без исключений.
   Ключ — `(id ноды-отправителя, номер её выходного порта)`; это ровно та пара, которой ходит провайдер подсказок.
   Пишется копия сообщения (`e.Copy()`), потому что сборка снимка отложена троттлом.
3. **Ключ хранилища — с портом**: `nodeId + port`.
4. **Что хранит `DebugNode` — отдельный разговор** (пока он не пишет ничего; решение выше его не требует).
5. **Записываем не всё, а через троттл** — `src/Mars.Nodes/Mars.Nodes.Host/Helpers/SmartThrottleByKey.cs`
   (тот же, что уже использует `NodeService`). Троттл живёт внутри стора, нода зовёт один метод.
6. **Транспорт — только pull**: запрос снимков при открытии формы; SignalR/эфир не используем (эфир теряется
   при реконнекте, а JSON в `DebugMessage` формирует только `DebugNodeImpl`).
7. **Значения показываем и в списке подсказок, и в INPUT-панели** (в панели — `JsonObjectViewer.razor`,
   обёртка `andypf-json-viewer`).
8. Обрезка: строка — по длине, JSON — по глубине и первым N элементам.
9. `InjectNode` входящих подсказок не получит — у него нет входного порта; в его форме только `VarNode` /
   `FlowContext` / `GlobalContext`.

**Семантика захвата (согласовано 2026-09-17):** сохраняет исполнитель — всё, что нода отдала дальше,
под ключом `(нода, её выходной порт)`. Нода отладки в записи не участвует; что она будет хранить — обсудим
отдельно.

- [x] `INodeDebugMode` (Host, singleton, не персистится) + `INodeServiceClient.SetDebugMode` + эндпоинт
      `NodeController.SetDebugMode` + `Load()` отдаёт `DebugMode` в `NodesDataResponse`.
- [x] `NodeDebugSnapshot` (Core) + `NodeDebugSnapshotBuilder` (обрезка: строка 150, глубина 4, первые 50
      элементов; на пределе глубины значение становится обрезанной строкой, циклы не вешают разбор) +
      `INodeDebugStore` (Abstractions, impl `NodeDebugStore` в Host), TTL 10 минут, ключ `nodeId|port`,
      троттл `SmartThrottleByKey` (300 мс, сборка снимка внутри троттла).
      **Отклонение от плана:** не `IMemoryCache`, а `ConcurrentDictionary` + проверка `CapturedAt` при чтении —
      `IMemoryCache` не умеет перечислять ключи, а клиент просит пачку `nodeIds` сразу.
- [x] Захват в `NodeTaskJob.callbackNext` (проверка режима в исполнителе, до копирования сообщения).
- [x] Транспорт: `INodeServiceClient.DebugSnapshots()` + эндпоинт + запрос при открытии формы
      (`NodeEditContainer1.StartEditNode` → `IHostValueHints.SetDebugSnapshots`), `Version` в хинтах для
      сброса кэша подсказок.
- [x] Провайдер: `ValueFieldInfo.Value` из снимков, пути из живых данных добавляются отдельно
      (`msg.Payload.items[1].name` с индексом — вставляется как есть), `DebugSnapshotValues` режет значения до 80.
- [x] UI: тумблер `DEBUG` в панели редактора (`NodeEditor1.razor` → `NodeEditor1.razor.cs:OnToggleDebugMode`,
      состояние приходит параметром `DebugMode` от страницы), значение в строке подсказки
      (`.mvi-value` в `MarsValueInput.razor` + стили в `style.less`/`style.css`), INPUT-панель над консолью
      (`EditorParts/NodeInputViewer.razor`: предшественники выбранной ноды + `JsonObjectViewer`, пометка
      `stale` старше минуты, стили в `Mars.Nodes.Workspace/wwwroot/styles.css`).
- [x] Тесты: `tests/Mars.Nodes.Tests/Debug/NodeDebugStoreTests.cs` (снимок: обрезка/глубина/цикл; стор:
      выключенный режим, порты, троттл, TTL, фильтр по нодам), `NodeDebugCaptureTests.cs` (захват через
      реальный job: включённый режим даёт снимок каждой ноды цепочки, выключенный — ничего;
      в `NodeServiceUnitTestBase` добавлены `DebugMode`/`DebugStore` для тестов) и тесты в `ValueFields`
      (значения из снимка, пути только из данных, чужой порт, разбор JSON).
      Проверка: `dotnet build Mars.slnx` + стенд + `Mars.Nodes.Tests.exe` (523 / 0 / 0).
      `MarsAppVersion` поднят до `0.8.3-alpha.15` (правлены `style.css` / `styles.css`).

## Фаза D — `MarsPathInput`: поле пути свойства (решения 2026-09-21)

Задача: компонент для выбора/ввода **пути к свойству** (`msg.Payload.User.Name`, `GlobalContext.var1`,
относительный `Payload.User.Name`), в отличие от `MarsValueInput` — поля **значения**. Потребители:
`TemplateNode.Property`, `DebugNode.PropertyPath`, поля `InjectNode`, `VariableSetNode.ValuePath`.
`MarsValueInput` для этого не подходит: ось `const/@expr`, валидация значения и дата-пикер — шум для пути;
`FieldPathPicker` — тупой dropdown без дерева, типов и debug-значений, кандидатов ему никто не передаёт.

Решения:

1. **Отдельный компонент `MarsPathInput`** (`Mars.Nodes.FormEditor/EditForms/Components/`), тот же HUD-стиль
   и те же приёмы, что `MarsValueInput`; css/js переиспользуются (общий js-модуль `NodeFormEditorJsInterop`,
   стили — общий префикс или `.mvi-*` там, где совпадает). Дерево подсказок (`FieldTreeNode` + tree-popup)
   вынести в общую часть, чтобы не дублировать.
2. **Путь без `@`, не выражение.** Чистый путь: `msg.Payload.x` или относительный `Payload.x`.
   `DebugNode.PropertyPath` переводим с expr-формата (`@msg.Payload`) на путь; старые значения с ведущим `@`
   impl понимает (снимает `@`) — совместимость flows.
3. **Корни — параметр компонента:** `Roots` (список разрешённых, полный путь с корнем) либо `Root` +
   `ShowRoot` (фиксированный корень-чип слева, ввод относительного пути — приём из `FieldPathPicker`).
4. **Политика нод (согласовано 2026-09-21):** Template — только `msg` (относительный; запись в контексты —
   работа `VariableSetNode`, у dataflow-ноды не должно быть невидимых сайд-эффектов: снимки фазы C
   `(нода, порт)` и провайдер подсказок запись в контекст не видят; существующие значения `"Payload"`/`"Url"`
   остаются валидными, миграция не нужна). Inject — только `msg` (относительный). VariableSet — все 4 корня
   (рантайм `VariableSetNodeImpl` уже умеет). Debug — все 4 корня (чтение безопасно; в DebugMode в подсказках
   видны значения). Расширение Template на контексты позже — дешёвое (маршрутизация по образцу
   `VariableSetNodeImpl`), обратное — миграция flows, поэтому начинаем с ограничения.
5. **Подсказки — тот же `IValueFieldProvider`** (кандидаты через `NodeEditContainer1.GetValueFields`,
   `For`-адресация как у `MarsValueInput`), фильтр по разрешённым корням, дерево с типами; в DebugMode —
   значения рядом с путём. **Свободный ввод несуществующего пути разрешён** (цель записи может ещё не
   существовать); валидация только синтаксическая: сегменты-идентификаторы через точку, `VarNode` — ровно
   два сегмента, индекс `[N]` допустим.
6. **Массивы:** дерево показывает `[]`-узлы из специй, вставка без индекса; поддержка индексов/`[]`
   в сеттерах записи — отдельная задача (см. «Дописано в план»).

- [x] `MarsPathInput.razor` + стили; вынос общего из `MarsValueInput`: дерево — `Components/ValueFieldTree.cs`
      (`FieldTreeNode` + `Build`/`Rows`), корни и синтаксис пути — `Mars.Nodes.Core/ValuePath.cs` (единый
      источник: из него же строится `InputValueResolver.RootPathRegex` и регексы `MarsValueInput`).
      Стили переиспользуют `.mars-value-input` (корневой класс компонента), добавлен только
      `.mars-path-input { width:auto; flex:1 1 auto }`; js — те же `mvi_*`-методы `NodeFormEditorJsInterop`.
- [x] Режимы: `Roots` (полный путь, чип «path») и `Root`+`ShowRoot` (чип корня + относительный путь);
      фильтр кандидатов по корням (в относительном режиме префикс корня срезается).
- [x] `DebugNode`: форма на `MarsPathInput` (все корни, значения в DebugMode), `PropertyPathPayloadDefault`
      → `msg.Payload`, impl резолвит путь по корням без интерпретатора (`ReadByPath`: msg →
      `DynamicNodeMsgWrapper.GetValueByPath`, контексты → `TryGetValue`, VarNode → `GetVarNodeVarible`),
      legacy-`@` снимается.
- [x] `TemplateNodeForm`: `Property` → `MarsPathInput Root="msg"`; `TemplateNodeImpl` пишет глубокий путь
      через `SetValueByPath` (односегментные — как раньше, совместимость полная).
- [x] `VariableSetNodeForm`: `ValuePath` → `MarsPathInput` (все корни) вместо `InputText`.
- [x] `ValueSourceEditor` (Inject): msg-ветка на `MarsPathInput` (параметр `Candidates` удалён — компонент
      сам тянет подсказки из контейнера); `InjectNodeForm`: поле `Key` → `MarsPathInput Root="msg"`
      (запись по пути: `InjectNode.IsValidKey` разрешает dot-path, `InjectNodeImpl` пишет глубокие ключи
      через `SetValueByPath`). `FieldPathPicker.razor` удалён вместе с `.fpp-*`-стилями.
      **JsonNode исключён**: `JsonNodeImpl` не использует `Node.Property` (поле формы мертво) — чинить
      отдельно от ввода путей.
- [x] Тесты: Inject (dot-path валидация ×2, глубокая запись), Template (глубокая запись), Debug
      (legacy `@msg.Payload`, msg-ключ контекста, `GlobalContext.*`).
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (530 / 0 / 0); стенд — за пользователем.
      `MarsAppVersion` поднят до `0.8.3-alpha.16` (правлены `style.css` / `style.less`).

## Фаза E — пересборка DebugMode и хранения значений (решения 2026-09-22)

Задача-источник: пункт «Перелопатить DebugMode» (2026-09-21). Обсуждение развилок A–E с пользователем
2026-09-22: взяты A1+A2, B1, C (auto-off), E (суженный pull); **отклонены** A3 (захват на входе ноды —
семантика «что нода отдала дальше» сохранена) и D (история/кольцевой буфер — остаётся один последний
снимок на ключ).

Решения:

1. **Синхронная сборка, копия сообщения убрана.** Отложенная сборка в `SmartThrottleByKey` гонялась с
   мутациями payload: `e.Copy()` копирует `Context`, но payload — по ссылке, и снимок мог собраться
   уже из изменённых данных. Теперь `NodeDebugStore.Save` строит снимок сразу; троттл — leading-edge
   по timestamp на ключ (300 мс): сообщение внутри окна пропускается без сборки. Цена: в быстром
   потоке снимок обновляется не чаще раза в 300 мс на пару «нода+порт».
2. **Сервер отдаёт обе формы данных.** `NodeDebugSnapshotBuilder` за один проход производит обрезанное
   дерево (JSON для `JsonObjectViewer`) и плоский `Values: path → value` (индексы `[N]`, агрегат
   `[N items]`, invariant-культура для чисел). Клиентский `DebugSnapshotValues.cs` удалён — лимиты
   обрезки теперь только в Core; провайдер режет значение до 80 для отображения
   (`MsgValueRootProvider.MaxDisplayValueLength`).
3. **Auto-off режима.** `DebugModeState` хранит окно `EnabledUntil` (30 минут, `AutoOffAfter`),
   `Enabled` вычисляется лениво при чтении; вручную выключается как раньше. Не персистится.
4. **Транспорт: pull + version-bump сигнал.** `BroadcastHub.DebugSnapshotsChanged()` — эфир без данных,
   троттл 1 с в `NodeRuntime` (`SmartThrottleByKey`); клиент по сигналу перетягивает снимки. После
   реконнекта — тоже pull (`OnWsReconnected`). Данные по эфиру не теряются.
5. **Pull сужен до upstream-замыкания.** `DebugSnapshots(nodeIds)` — клиент считает замыкание
   выбранной и редактируемой ноды по проводам (`NodeEditor1.DebugSnapshotScope`). Pull переехал из
   `NodeEditContainer1` в `NodeEditor1.RefreshDebugSnapshots`: вызывается при открытии формы, смене
   выбора (debounce 300 мс — INPUT-панель наполняется без открытия формы) и по сигналу хаба.
   `HostValueHints.SetDebugSnapshots` теперь **мержит** (partial pull не должен затирать чужие ключи).
6. **Часы — серверные.** DTO `NodeDebugSnapshotsResponse` (`Core/Contracts/Nodes/NodeResponse.cs`):
   `ServerTimeUtc` + `DebugMode` + снимки. Stale-метка INPUT-панели считается через
   `IHostValueHints.GetSnapshotAge` (серверный возраст + локальный интервал с момента pull),
   `CapturedAt` — UTC, в UI отображается локальным. Тумблер DEBUG синхронизируется из ответа
   (`DebugModeChanged`), после auto-off не залипает.
7. **Исключения сборки логируются** (`ILogger` в `NodeDebugStore`), а не теряются в fire-and-forget.
   Ключ `nodeId|port` — один хелпер `NodeDebugSnapshot.Key` (стор и клиентский кэш).

- [x] Сервер: `NodeDebugStore` (синхронный Save → bool, leading-edge троттл, auto-off `DebugModeState`,
      логгер), `NodeDebugSnapshotBuilder` (+`Flatten`, `capturedAtUtc`), `NodeDebugSnapshot` (+`Values`,
      +`Key`), `NodeTaskJob.callbackNext` (без `Copy`, сигнал рантайму при записи), `NodeRuntime`
      (+`DebugSnapshotsChanged` с троттлом 1 с), `INodeRuntime`, `BroadcastHub`, `NodeController.DebugSnapshots(nodeIds)`
      (DTO с серверным временем; контроллер больше не ходит в `BaseNodes.Keys`).
- [x] Клиент: `INodeServiceClient`/`NodeServiceClient` (nodeIds → DTO), `IHostValueHints`/`HostValueHints`
      (мерж, `GetSnapshotAge`), `MsgValueRootProvider` (значения из `Values`, обрез 80),
      `DebugSnapshotValues.cs` удалён, `NodeEditor1.RefreshDebugSnapshots` + скоуп + debounce на выбор,
      `NodeEditContainer1` (pull убран), `NodeInputViewer` (серверный возраст, локальное время),
      `ClientHub.OnDebugSnapshotsChanged`, подписки в `NodeRedPage.razor.cs` и девстенд
      `NodeRedPageContent.razor.cs` (+ refresh на реконнекте).
- [x] Тесты: `NodeDebugStoreTests` переписаны под синхронность (мутация payload после Save не протекает,
      окно троттла через fake Clock, TTL, auto-off, manual off), `NodeDebugCaptureTests` без задержек
      (+плоский `Values` из реального job), `ValueFieldProviderTests` — снимки через
      `NodeDebugSnapshotBuilder.Build`, `DebugSnapshotValuesTests` удалены.
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (532 / 0 / 0). CSS/JS не правились —
      `MarsAppVersion` не поднимался.

Грабли фазы E:

- **Троттл leading-edge**: в непрерывном потоке снимок — это первое сообщение окна, а не последнее
  (старый `SmartThrottleByKey` давал trailing). После паузы > 300 мс следующее же сообщение обновляет снимок.
- **Мерж в `HostValueHints`**: снимки удалённых/переподключённых нод не очищаются — живут до переполнения
  словаря или перезагрузки страницы; срок годности виден по stale-метке.
- **`RefreshDebugSnapshots` при пустом скоупе** (ничего не выбрано) не тянет ничего — INPUT-панель пуста
  до первого выбора ноды.

## Фаза F — DebugNode хранит полный объект (решения 2026-09-22)

Задача-источник: обсуждение «что будет, если в Debug попадёт большая HTML-страница» — обрезанные снимки
фазы C/E не дают посмотреть полное значение. Решение пользователя: подсказки остаются маленькими;
тяжёлое хранение — явный opt-in на `DebugNode`, показ — в его же форме, модалка с Monaco.

Решения (ответы пользователя на дизайн 2026-09-22):

1. **Что хранить — зависит от `CompleteInputMessage`**: включён — весь msg (`AsFullDict`), выключен —
   только значение `PropertyPath` (та же резолюция, что для печати в консоль, включая legacy-`@`).
2. **Жёсткий лимит 2 МБ, только последний.** Превышение — режем строку с маркером
   `...[truncated: exceeded 2 MB limit]`, JSON становится битым — это осознанно: человек может сузить
   путь или обработать сообщение на входе. Модалка в этом случае показывает `log`-язык вместо `json`.
3. **Независимо от глобального DebugMode** — галочка на ноде уже явное согласие. Троттл тот же
   (leading-edge 300 мс на ноду). **Без TTL** — до перезаписи или рестарта; форма показывает время
   и stale-метку (старше минуты, от серверных часов).
4. **Сборка — прямая сериализация без обхода по полям** (уточнение пользователя 2026-09-22): объект
   сериализуется как есть (`WriteIndented`, `IgnoreCycles`) и просто режется по общему лимиту 2 МБ;
   при исключении сериализации — fallback в `ToString()`. Никакого `Truncate`-обхода с пресетом.
5. **Транспорт**: `DebugNodeFull(nodeId, includeJson)` — метаданные (время/размер/truncated) лёгкие,
   тело (до 2 МБ) тянется только при открытии модалки.
6. **Impl сам достаёт стор** — `RNS.ServiceProvider.GetService(typeof(INodeDebugStore))`,
   отдельного метода в `IRuntimeNodeScope` не добавляли (решение пользователя).
7. Подсказки/INPUT-панель не меняются — значения там остаются обрезанными.

- [x] `DebugNode.StoreFullObject`; `NodeDebugFullSnapshot` (Core); `NodeDebugSnapshotBuilder.BuildFull`
      (прямая сериализация + рез по 2 МБ); `INodeDebugStore.SaveFull/GetFull`; вторая полка в `NodeDebugStore`
      (общий `Throttled`-хелпер, без TTL, без проверки DebugMode); `DebugNodeImpl` (захват после
      `RNS.DebugMsg`, `ReadDisplayValue` вынесен и переиспользован консольной веткой).
- [x] Транспорт: `NodeController.DebugNodeFull` (маршрут `DebugNodeFull/{nodeId}` — без сегмента в
      шаблоне клиентский путь `/DebugNodeFull/<id>` давал 404) + `NodeDebugFullResponse` (`ServerTimeUtc`,
      `CapturedAt`, `Size`, `Truncated`, `Json?`) + `INodeServiceClient.DebugNodeFull`.
- [x] Форма: `DebugNodeForm` — чекбокс `StoreFullObject` (при включении сразу тянет метаданные),
      строка состояния (время + stale, размер, truncated-badge), кнопки `show object` / `refresh`;
      модалка `DebugNodeFullObjectDialog` (FormEditor/EditForms/Common) — Monaco `CodeEditor2`
      (редактируемый, правки не сохраняются; `readOnly` мешал F1/палитре — убран 2026-09-22),
      язык `log` при truncation, pull с `includeJson=true` при открытии.
- [x] Тесты: стор (`NodeDebugStoreFullTests`: независимость от режима, только последний, троттл,
      отсутствие TTL, неизвестная нода, обрезка/маркер 2 МБ) и импл (`DebugNodeTests`: complete — весь
      msg, path — только значение, выключено — ничего).
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (542 / 0 / 0). CSS/JS не правились —
      `MarsAppVersion` не поднимался.

Грабли фазы F:

- **Битый JSON при truncation** — договорённость: маркер в конце, модалка на `log`-языке, дерево не строим.
- **`StoreFullObject` живёт в flows.json** — в отличие от глобального DebugMode это персистимый флаг;
  нода с галочкой сериализует до 2 МБ на каждое сообщение (под троттлом) постоянно, а не только в debug-сессии.
- **Форма тянет метаданные один раз при первом рендере** (и по кнопке refresh) — live-обновление по
  сигналу `DebugSnapshotsChanged` не делали: сигнал дёргает pull снимков фазы C, полный объект — отдельная
  история; при необходимости подписать форму на bump `IHostValueHints.Version`.

## Фаза G — спеки и умные поля остальных нод Common/Network (2026-09-22)

Задача-источник: запрос пользователя «поработать над остальными нодами по типам подсказок и полям свойств,
начать с Common и Network». Решения пользователя: EmailSend расширяем до const/msg/expr, `Message` чиним;
MqttOut.Topic делаем msg-драйвен; HttpInNode — вариант A (object + Description, конкретика из DebugMode);
опечатку `MqttNodeMessagePaylad` исправляем («пусть ломаются»); баг `CallNodeForm.Timeout` чиним;
разворот `EndpointNode.JsonSchema` в подсказки — отложен.

Ключевая механика фазы: **мерж источников специй**. Атрибут статичен (не видит конфиг), интерфейс на модели
считается в браузере (видит конфиг, но не видит типы из Implements). Поэтому: config-зависимые ветки —
интерфейс на модели (VarType строкой, можно объявлять пути типов из чужих сборок вручную), сложные DTO —
атрибут на impl, а `MsgValueRootProvider.SpecsOf` теперь **объединяет** инстансные спеки с хостовыми
(дедуп по Path, инстанс побеждает) вместо «или-или». Рекурсию типов (Exception.InnerException) держат
существующие `DefaultMaxDepth=3` + `visited` экспандера.

- [x] F1: мерж specs в `MsgValueRootProvider.SpecsOf` (инстанс + хост); серверный мерж model/impl атрибутов
      в `NodeService.AddOutputValueSpecs` уже был. Тест `GetFields_InstanceSpecsAndHostSpecs_AreMerged`.
- [x] `CatchErrorNodeImpl`: `[NodeOutputValueSpec(typeof(Exception))]` (Payload — Exception, инжектится в
      `NodeService` при ошибке). Тест: разворот без рекурсии InnerException.
- [x] `HttpRequestNode`: интерфейс на модели (Payload = string/object по `ReturnResponse`) + атрибут на impl
      `[NodeOutputValueSpec(typeof(HttpRequestInfo), Name = nameof(HttpRequestInfo))]` (слот из `Set(requestInfo)`).
- [x] `EndpointNode`: интерфейс на модели (String → `Payload: string`; JsonSchema → object «JSON validated by schema»).
- [x] `HttpInFormSaveFilesNode`: интерфейс на модели (SaveInMediaFiles → пути `FileListItem` вручную строками,
      иначе `Payload: string[]`).
- [x] `HttpInNode`: интерфейс на модели — `Payload: object` + Description «string, JSON (JsonNode) or form-data —
      by request Content-Type» (ветка по content-type запроса, статически не определить; конкретика — из снимков DebugMode).
- [x] `HttpRequestNodeForm.Url` → `MarsValueInput` c маппингом `UrlKind` (`@`-префикс → Expression, как в
      `InjectNodeForm`); impl не менялся — `InputValueResolver` уже работал.
- [x] `EmailSendNode`: `ToEmailKind`/`SubjectKind`/`MessageKind` + resolver в impl; `Message` починен
      (override из Node.Message; payload не-dto → `Message = payload.ToString()`); форма на `MarsValueInput`,
      Message — `Multiline`.
- [x] `MarsValueInput`: параметр `Multiline` (+`Rows`) — textarea без слоя `.mvi-highlight` (текст рисуется
      сам, как в `MarsPathInput`), стили `.mvi-multiline` в `style.less`/`style.css`.
- [x] `MqttOutNode`: `TopicKind` + resolver в impl (пустой topic → `NodeExecuteException`), форма на `MarsValueInput`.
- [x] Переименование `MqttNodeMessagePaylad` → `MqttNodeMessagePayload` (импл, `MqttManager`, тесты);
      имя слота runtime-only, в flows.json не persistится.
- [x] Фикс `CallNodeForm.Timeout`: `.Milliseconds` → `(int)TotalMilliseconds` (дефолт 2s показывался как 0).
- [x] Тесты: ридер (HttpRequest ветки + слот impl, Endpoint, HttpIn, HttpInFormSaveFiles, CatchError/Exception),
      провайдер (мерж инстанс+хост).
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (549 / 0 / 0).
      `MarsAppVersion` поднят до `0.8.3-alpha.19` (правлены `style.css`/`style.less`).

Грабли фазы G:

- **Kind-поля EmailSend/MqttOut/HttpRequest маппятся через `@`-префикс**: UI создаёт только Const/Expression
  (Msg-kind отображается, но из поля не создаётся — `@msg.x` уходит в Expression; семантически эквивалентно,
  та же грабля что в `InjectNodeForm`).
- **Старые flows EmailSend**: `ToEmailKind` и др. по умолчанию Const — поведение прежнее, кроме `Message`:
  непустой `Node.Message` теперь реально применяется (раньше игнорировался) и payload не-dto идёт в `Message`.
- **HttpInFormSaveFiles спеки рукописные** — при изменении `FileListItem`/`FileSummary`
  (`Mars.Media.Abstractions/Dto/Files/`) список путей в модели надо обновлять вручную.

Отложено (фаза G): разворот `EndpointNode.JsonSchema` в точные пути подсказок без DebugMode; кандидаты из
собственных свойств ноды (ось «дефолт значения поля» для `HttpRequestNode.Url` — см. «Дописано в план»).

## Фаза H — спеки и умные поля остальных групп, партиями по 5–9 нод (старт 2026-09-22)

Продолжение фазы G на остальные группы (Functions, Sequences, Parsers, Storage, Events, Diagnostics,
TaskNodes, Validation, Connections). DevNodes пропущены (debug-эксперименты/мёртвые заглушки).
Решения пользователя по развилкам: JsonNode мёртвые поля — **оживить** (`Property` → MarsPathInput,
`FormatJsonString` → impl); `FileReadNode.FilePath` — **расширить** до msg-driven; `DevAdminConnection.Message` —
**добавить** `MessageKind`; баги JoinNode/DirRead — чинить (объяснить подробнее к моменту партии 2/3);
InlineFunction null-callback — обсудить позже.

### Партия 1 — Functions (сделана 2026-09-22)

- [x] `ExecNodeImpl`: `[NodeOutputValueSpec(typeof(string))]` (stdout).
- [x] `StringNode`: интерфейс на модели — тип Payload по `ReturnType` последней операции
      (`string` / `string[]`, иначе object); пустой список операций — ничего не объявляем (passthrough).
      Методы парсятся ленивым статическим кэшем `StringNodeOperationUtilsMethodParser` (browser-safe, Core).
- [x] `EvalNodeForm`: **баг-фикс** — `MarsValueInput` биндился в `Node.Input` напрямую без маппинга
      `@`↔`ValueKind`: введённый `@…` уходил в DynamicExpresso вместе с `@`. Теперь маппинг как в Inject
      (`@`-префикс → Expression со снятием префикса; без префикса → Const).
- [x] `SwitchNodeForm`: условия — plain FluentTextField → `MarsValueInput` с маппингом `Condition.ValueKind`
      (модель/impl уже резолвили, форма отставала). `$else` показывается как есть без `@` (impl сверяет
      `Value == "$else"` после снятия префикса — round-trip безопасен).
- [x] `DelayNode` (passthrough), `SwitchNode` (транзит — спека пустая намеренно), `FunctionNode` /
      `InlineFunctionNode` (выход динамический — статически необъявим, fallback object) — без изменений.
      `DelayNode.DelayMillis` оставлен const.
- [x] Тесты: Exec-атрибут, StringNode (ToUpper→string, Split→string[], Split+Join→string, пусто→ничего).
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (552 / 0 / 0). CSS/JS не правились —
      `MarsAppVersion` не поднимался.

### Партия 2 — Sequences + Parsers (сделана 2026-09-22)

- [x] **Дедуп специй по `(Path, OutputPort)`** вместо `Path` — иначе портовые ветки (два `Payload` на разных
      портах) терялись: `NodeOutputValueSpecReader.Read`/`ReadStatics`, `NodeService.AddOutputValueSpecs`,
      `MsgValueRootProvider.SpecsOf`.
- [x] `ForeachNode`: интерфейс на модели — порт 0 `Payload: int` (count), порт 1 `Payload: object` (элемент),
      слот `ForeachCycle` + `index/count/arr` **вручную** (у цикла public поля — экспандер ходит только свойства).
- [x] `QueueNodeImpl`: атрибуты — порт 0 `int` «total processed», порт 1 `object` «queued item».
- [x] `JoinNodeImpl`: атрибут `typeof(object[])` на все порты.
- [x] `SplitNodeImpl`: атрибут `typeof(object)` + Description (элемент строки/коллекции или `{PropertyName,Value}`).
- [x] `JsonNode` — **оживление мёртвых полей** (решение пользователя): impl уважает `Property`
      (маршрутизация как в `TemplateNodeImpl`: "Payload" / dot-path через `SetValueByPath` / слот через `Set`)
      и `FormatJsonString` (раньше formatted было зашито `true`); форма: `Property` → `MarsPathInput Root="msg"`.
      Спека — интерфейс на модели: цель = `Property`, ToJsonString → string, иначе object (DynamicJson).
      **Изменение поведения**: flows с непустым `Property` ≠ "Payload" теперь реально пишут в слот/путь;
      выход ToJsonString по умолчанию теперь компактный (чекбокс Format стал рабочим, default false).
- [x] `HtmlParseNode`: интерфейс на модели — Text/Html → `string[]` (+`Payload[]`), `ReturnEachObjectAsMessage`
      → по элементу; MapToObjects → `object[]` + пути `Payload[].<OutputField>` из конфига InputMappings
      (пустое имя → `field{N}` как в impl).
- [x] `JoinNodeForm` — **фикс**: fallback при неудачном парсинге таймспана писал `15 * 1000` **секунд**
      (≈4ч10м) в `AggregationTimeSeconds`/`InputAggregationTimeoutSeconds` — перепутаны миллисекунды с
      секундами; теперь неудачный парсинг значение не меняет (геттер перерисует текущее).
- [x] Тесты: Foreach (порты+слот), Queue (порты), Join/Split (атрибуты), JsonNode (Action/Property),
      HtmlParse (все три Output-ветки).
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (557 / 0 / 0). CSS/JS не правились —
      `MarsAppVersion` не поднимался.

### Партия 3 — Storage (сделана 2026-09-22)

- [x] `FileReadNode`: интерфейс на модели (`SingleString`/`MsgPerLine` → `Payload: string`,
      `SingleBuffer` → object «byte[]»); **добавлен `FilePathKind`** + `InputValueResolver` в impl
      (решение пользователя — симметрично `FileWriteNode`); форма — `MarsValueInput` с маппингом `@`↔Kind.
- [x] `FileServiceReadNode`: интерфейс на модели (те же три ветки `OutputMode`). `FilePath`/`StorageFileId`
      оставлены const (медиа-пикер, ids — не msg-значения).
- [x] `DirReadNodeImpl`: атрибут `typeof(string[])`; **фикс**: `Node.UseRootGitIgnore` не передавался в
      `FileListUtility.GetFiles` (параметр `useRootGitIgnore` существовал, default false) — флаг формы был мёртв.
- [x] `FileWriteNodeForm`: `FilePath` — plain FluentTextField → `MarsValueInput` с маппингом уже
      существующего `FilePathKind` (impl резолвил и раньше, UI kind не выставлял).
- [x] `FileWrite`/`FileServiceWrite` — passthrough, специй нет.
- [x] Тесты: FileRead (три ветки), FileServiceRead (buffer), DirRead (атрибут).
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (560 / 0 / 0). CSS/JS не правились —
      `MarsAppVersion` не поднимался.

### Партия 4 — Events/Diagnostics/TaskNodes/Validation/Connections (сделана 2026-09-22)

- [x] `EventListenerNodeImpl`: атрибут `typeof(ManagerEventPayload)` (Id/Created/Data/Topic; `Data` — object,
      не разворачивается).
- [x] `CounterNodeImpl`: атрибут `typeof(int)`.
- [x] `CheckUserNodeImpl`: атрибут `typeof(IRequestContext)`, `Name = nameof(IRequestContext)`, порт 0
      (`TryAdd(requestContext)` → слот по имени интерфейса; UserName/IsAuthenticated/Roles:string[] и т.д.).
- [x] `ActionCommandNodeImpl`: атрибут `typeof(object)` + Description «command args (string → string)»
      (payload инжектит `CommandNodesActionProvider` словарём — ключи неизвестны статически).
- [x] `ExecXActionNodeImpl`: атрибут `typeof(XActResult)` (Ok/Message/MessageIntent/Effects).
- [x] `DevAdminConnectionNode`: `MessageKind` + `InputValueResolver` в impl (семантика «пусто → msg.Payload»
      сохранена — применяется к уже резолвнутому значению); форма — multiline `MarsValueInput` (решение
      пользователя: добавлять Kind).
- [x] Sink'и без специй: `LoggerNode`, `TerminateAllJobsNode`, `KillTaskJobNode`, `DevAdminConnectionNode`;
      `CounterNode` формы не имеет — не создавали. DevNodes пропущены (debug/мёртвые).
- [x] Тесты: EventListener, Counter, CheckUser (слот+порт), ActionCommand, ExecXAction.
      Проверка: `dotnet build Mars.slnx` + `Mars.Nodes.Tests.exe` (565 / 0 / 0). CSS/JS не правились —
      `MarsAppVersion` не поднимался.

Заметки партии 4 (не правили, только наблюдение): `KillTaskJobNode` отправляет в wires[0] задачу с
`msg = null`; `InlineFunctionNodeImpl` при null-результате не зовёт callback (поток молча обрывается) —
обсуждение отложено (решение пользователя); `ExecNodeImpl` читает stderr и выбрасывает без логирования.

## Черновик гайда (влить в NodesValueSpecsGuide.md при закрытии инициативы)

### Архитектура: контракт выходов → подсказки в поле

Цепочка доставки подсказки до пикера (всё считается без обращений к серверу при наборе текста):

1. Нода **объявляет**, что кладёт в msg: атрибут на impl/модели или интерфейс на модели (см. рецепты ниже).
2. `OutputValueSpecExpander` (`Mars.Nodes.Core`) разворачивает CLR-тип атрибута в плоские пути
   (`Payload.user.name`, `[]` = элемент массива): глубина ≤ 3, visited-set против рекурсии типов
   (`Exception.InnerException` не зацикливается), `NoExpansionTypes` (object/Type/NodeMsg/JsonElement/
   JsonDocument/JsonNode) и словари/IEnumerable не разворачиваются — их ключи неизвестны статически.
3. Хост при `Load()` собирает **статику** (атрибуты моделей + impl'ов, `NodeService.CollectOutputValueSpecs`,
   мерж по TypeId) и отдаёт в `NodesDataResponse.OutputValueSpecs`; клиент кладёт их в `IHostValueHints`.
   **Динамику** (интерфейс) клиент считает сам — инстанс ноды в редакторе есть, конфиг он видит.
4. `MsgValueRootProvider` (`Mars.Nodes.Workspace/Services/ValueFields/`) при открытии пикера обходит провода
   вверх от редактируемой ноды: для каждой ноды **мержит** инстансные спеки с хостовыми (дедуп по
   `(Path, OutputPort)`, инстанс побеждает — он знает конфиг), фильтр по порту выхода, ближайшее объявление
   побеждает; `Fallback` (`Payload: object`) — только если Payload не объявил никто по цепочке.
   В DebugMode к путям добавляются значения из снимков.

### Рецепт: статическое объявление выхода (атрибут)

Когда: выход одинаков для всех экземпляров ноды — фиксированный тип payload или именованный слот.

```csharp
[NodeOutputValueSpec(typeof(HttpRequestInfo), Name = nameof(HttpRequestInfo))]  // слот из input.Set(obj)
[NodeOutputValueSpec(typeof(int), OutputPort = 0, Description = "total processed")] // конкретный порт
public class SomeNodeImpl : INodeImplement<SomeNode> { ... }
```

- `Name` по умолчанию `"Payload"`. Для слота **обязан** совпадать с рантайм-ключом: `NodeMsg.Set<T>(obj)`
  пишет под `typeof(T).Name` — бери `nameof(Тип)`.
- `OutputPort`: номер выхода или `OutputValueSpec.AllOutputPorts` (-1).
- Атрибуты читаются и с impl'а, и с модели (`ReadStatics` обоих → мерж), но место атрибута — **impl**:
  модели из `Mars.Nodes.Core` часто не видят DTO-типы (они в Implements/модулях), а impl видит всё.
- Тип разворачивается автоматически: public **свойства** (поля не участвуют!), вложенность до глубины 3.
- Проверка: тест `NodeOutputValueSpecReader.ReadStatics(typeof(XxxNodeImpl))` — см.
  `tests/Mars.Nodes.Tests/OutputValueSpecs/NodeOutputValueSpecReaderTests.cs`.

### Рецепт: config-зависимый выход (интерфейс на модели)

Когда: тип/путь выхода зависит от конфига экземпляра (`ReturnResponse`, `OutputMode`, `Action`, список
операций) или DTO живёт в сборке, недоступной из Core.

```csharp
public class SomeNode : Node, INodeOutputValueSpec
{
    public IEnumerable<OutputValueSpec> GetOutputValueSpec()
    {
        yield return Mode == Mode.Json
            ? new OutputValueSpec(nameof(NodeMsg.Payload), VarNode.ObjectTypeName, Description: "parsed JSON")
            : new OutputValueSpec(nameof(NodeMsg.Payload), "string");
    }
}
```

- `OutputValueSpec(Path, VarType, OutputPort = 0, Description = null)`; `VarType` — **строка** из словаря
  `VarNode` (`int`/`string`/`Guid`/`string[]`/…; `object` — только для специй, не валидный тип хранения).
  Можно писать пути вручную для типов из чужих сборок (`"Payload[].Name", "string"`) — но при изменении
  DTO список придётся обновлять руками.
- Считается в браузере — не ссылаться на серверные типы; только Core.
- Интерфейс **не отменяет** атрибуты impl'а: провайдер мержит оба источника (так у HttpRequestNode
  уживаются динамический Payload и статический слот `HttpRequestInfo`).
- Транзит-нода (msg не меняет) не объявляет **ничего** — пустой спек это «ничего не добавляет»,
  провайдер унаследует объявления вышестоящих. Проверка: тест `Read_*` на каждую config-ветку.

### Рецепт: поле значения MarsValueInput (const / @expression / msg)

Когда: в поле формы может прийти значение из msg или вычисленное выражение (URL, topic, email, условие,
путь к файлу). Ось одна: строка без префикса = const, `@…` = выражение (корни `msg`/`GlobalContext`/
`FlowContext`/`VarNode`/`env()`).

1. **Модель** — парное Kind-поле, дефолт Const (старые flows остаются валидными):
   ```csharp
   public string Url { get; set; } = "";
   public string UrlKind { get; set; } = InputValueKind.Const;
   ```
2. **Impl** — резолв через `InputValueResolver` (`Mars.Nodes.Expressions`); interpreter создавать только
   если kind не Const:
   ```csharp
   Interpreter? interpreter = null;
   if (Node.UrlKind is InputValueKind.Expression or InputValueKind.Msg)
       interpreter = InputValueResolver.CreateInterpreter(RNS, input);
   var url = (string)InputValueResolver.Resolve(Node.UrlKind, Node.Url, "string",
       interpreter, new ExpressionScope(RNS, input), Node, "Url")!;
   ```
3. **Форма** — `MarsValueInput` + маппинг `@`-префикса в Kind (эталон — `InjectNodeForm`/`HttpRequestNodeForm`):
   ```razor
   <MarsValueInput Class="mvi-fill" VarType="string" Value="@UrlValue" ValueChanged="SetUrlValue" For="() => Node.Url" />
   ```
   ```csharp
   string UrlValue => Node.UrlKind switch
   {
       InputValueKind.Expression => "@" + Node.Url,
       InputValueKind.Msg => "@msg." + Node.Url,
       _ => Node.Url,
   };
   void SetUrlValue(string value)
   {
       if (value.StartsWith('@')) { Node.UrlKind = InputValueKind.Expression; Node.Url = value[1..].Trim(); }
       else { Node.UrlKind = InputValueKind.Const; Node.Url = value; }
   }
   ```
   - `For="() => X"` **обязателен** — по имени члена модели компонент сам тянет подсказки из контейнера.
   - `Class="mvi-fill"` — растянуть по ячейке (внешние классы только через параметр `Class`, иначе затрут
     корневой класс компонента и все стили).
   - `Multiline Rows="N"` — textarea (без слоя подсветки выражения); для многострочных тел (email Message).
   - `VarType` — для const-валидации и подписи тега (`int`/`bool`/`DateTime` → проверка формата).
   - Kind `Msg` только отображается (из UI `@msg.x` сохранится как Expression — семантически то же).
   - Значения-«свободный текст» (константы с переводом строк) переживают режим expr без потерь:
     префикс снимается/возвращается симметрично.

### Рецепт: поле пути MarsPathInput (куда писать результат)

Когда: поле задаёт **путь**, а не значение (`Property` у Template/Json, `PropertyPath` у Debug,
`ValuePath` у VariableSet, `Key` у Inject).

```razor
<MarsPathInput Root="msg" @bind-Value=@Node.Property For="() => Node.Property" />   @* относительный путь *@
<MarsPathInput Roots="…" @bind-Value=… />                                            @* полные пути с корнем *@
```

- `Root="msg"` + `ShowRoot="false"` — чип корня слева, значение относительное; `Roots` — список разрешённых
  корней, значение с корнем. Свободный ввод несуществующего пути разрешён (цель записи может ещё не
  существовать), валидация только синтаксическая.
- **Impl** — маршрутизация записи по образцу `TemplateNodeImpl`/`JsonNodeImpl`:
  ```csharp
  if (Node.Property == "Payload") input.Payload = result;
  else if (Node.Property.Contains('.')) new DynamicNodeMsgWrapper(input).SetValueByPath(Node.Property, result);
  else input.Set(Node.Property, result);
  ```
  Ограничение: `SetValueByPath` — только рефлексия (не словари/JsonElement), промежуточные сегменты должны
  существовать. Dataflow-нодам не писать в контексты (msg-корень): запись в GlobalContext не видна ни
  снимкам DebugMode, ни провайдеру подсказок — это работа VariableSetNode.

### Инварианты и грабли

- Дедуп специй — **только по `(Path, OutputPort)`** (ридер, серверный мерж, провайдер): по одному Path
  портовые ветки теряются.
- Бюджет интеропа: подсказки — при открытии пикера и по refresh, фильтрация локальная; набор текста не
  должен порождать обращений к провайдеру.
- Экспандер видит только свойства; класс с public **полями** (ForeachCycle) объявлять вручную строками.
- enum/uint/byte/short дают `object` (нет в словаре `VarNode`) — для точных имён расширять `_namesDict`.
- Рукописные пути в моделях — при изменении DTO обновлять вручную (компенсируется тестом `Read_*`).
- CSS: `style.less` и `style.css` править **оба** (компиляция ручная, compilerconfig.json); при правке —
  bump `MarsAppVersion` в `Directory.Build.props` и `?v=` у ассета.
- Тесты: `ReadStatics_*` (атрибуты) и `Read_*` (интерфейс, каждая config-ветка) в
  `tests/Mars.Nodes.Tests/OutputValueSpecs/`, провайдер — `ValueFields/ValueFieldProviderTests.cs`.

## Грабли и риски

- **Фаза D, внешний класс — только через параметр `Class`**: у `MarsValueInput`/`MarsPathInput` есть
  `[Parameter] string? Class` — он биндится с атрибута `class`/`Class` (регистронезависимо) и добавляется
  к корневому классу. Историческая грабля (2026-09-21): до параметра `Class="…"` не подхватывался
  компонентом и сплатился отдельным атрибутом, который перезатирал `class="@RootClass"` — компонент терял
  все стили.
- **Фаза D, чтение контекстов по пути**: `DebugNodeImpl.ReadByPath` для `GlobalContext`/`FlowContext` читает
  только ключ первого уровня (`TryGetValue(rest)`) — вложенный путь `GlobalContext.var1.x` вернёт null;
  deep-read контекстов — вместе с задачей про массивы/индексы.
- **Фаза D, запись по пути — только рефлексия**: `DynamicNodeMsgWrapper.SetValueByPath`/`SetProperty`
  не работают со словарями и `JsonElement`/`DynamicJson` (те же ограничения у msg-ветки `VariableSetNodeImpl`);
  промежуточные сегменты должны существовать (null по дороге — тихий отказ записи).
- **Не называть папки в репо `Debug/`** — `.gitignore:23` (`[Dd]ebug/`) их молча игнорирует, файлы не попадут
  в коммит (поймано на `tests/Mars.Nodes.Tests/Debug/` → переименовано в `DebugMode/`).

- Форма работает с **копией** ноды (`Mars.Nodes.Workspace/NodeEditContainer1.razor.cs`: `_node = node.Copy(...)`):
  `Id` и `TypeId` есть, живых ссылок на рантайм нет — всё динамическое обязано приходить через сервис.
- `DynamicNodeMsgWrapper` поднимает свойства payload на верхний уровень и затеняет одноимённые ключи Context
  (грабля из первого плана) — при построении дерева `msg.*` это проявится.
- Debug-сообщения после реконнекта теряются (эфир без хранения): если UI показывает «последнее входящее»,
  нужен серверный кэш, одного эфира мало.
- Клиент не видит `Mars.Nodes.Expressions`, а тянуть DynamicExpresso (Expression Trees) в WASM нельзя —
  browser-safe часть (список корней и разбор путей) нужно вынести в отдельный маленький проект либо держать
  один источник и генерацию регекса из него.
- Значения в подсказках — это пользовательские данные в UI: обрезка обязательна (и по длине строки, и по
  глубине/числу элементов JSON).
- `MarsValueInputMocks.cs` держит только операции `ƒ`; `ValueFieldInfo` теперь в
  `Mars.Nodes.Front.Abstractions/Services/IValueFieldProvider.cs`, моковых полей нет — пустой `Candidates`
  даёт пустой пикер, а не фальшивые подсказки.
- Атрибутов выхода может быть несколько — провайдер не должен рассчитывать на единственный выход.
- Объём данных: сообщение может быть на мегабайты — лимиты нужны и на серверной стороне, и в DTO.
- `NodeOutput` в репо занято: это **порт/провод** (`Mars.Nodes.Core/NodeOutput.cs`, `Node.Outputs` + `Wires`),
  а не тип данных — отсюда корень `NodeValue*`.
- Ключи `msg` **плоские**: `NodeMsg.Context` — словарь, `NodeMsg.Set<T>` пишет под именем CLR-типа
  (`HttpRequestNodeImpl` → `Set(requestInfo)`), а `InjectNode.Validate` запрещает точку в `Key` (только
  идентификатор). Значит `msg.user.email` из моков — это форма **данных** внутри `Payload`, а не объявленный
  ключ; вложенность даёт debug-значение или разворот типа из spec.
- Тримминга сборок нет (`PublishTrimmed=false` в `Mars.Admin.csproj` и `Mars.WebApp.csproj`) — рефлексия по DTO
  в WASM сейчас безопасна; если включат, разворот типов придётся унести на хост — держать как запасной путь.
- Схемы: `EndpointJsonSchemaTool` лежит в `Mars.Nodes.Core.Implements` (server-only),
  `ModelJsonSchemaGenerator` — в `Mars.SemanticKernel.Abstractions`; из Core не переиспользуются, разворачиватель
  пишем свой.
- `MqttOutNodeImpl` в `Execute` **не зовёт callback** — это sink, а не «нода с типом на выходе», как она была
  записана в первом обсуждении.

## Отклонённые альтернативы

- **Инференс типов из примера JSON (как INPUT-панель n8n) как основной источник** — отклонён: источник истины
  это контракт ноды; наблюдённые данные появляются только в DebugMode и служат значениями, а не типами.
  (Это и был «пункт 4» предыдущего обсуждения — он снимается атрибутами/контрактами.)
- **Хранить последние входящие по умолчанию** — отклонено: память и приватность; только DebugMode, TTL 10 мин.
- **Читать значения `flow`/`global` на клиент в первой итерации** — отклонено: нужны только имена; значения —
  позже и только в debug-режиме.
- **Значения в подсказках без обрезки** — отклонено (см. решения 3 и 5).

## Открытые вопросы

- Где живёт флаг DebugMode (нода / редактор / `DebugNode`) и включает ли запись для всех нод или выборочно.
- Глубина разворота типа: сколько уровней по умолчанию, что показывать для `object`, открытых словарей
  и коллекций без известного типа элемента; циклы в DTO (взаимные ссылки) — проверять на реальных типах нод.
- Откуда клиент берёт список переменных `flow`/`global`: из графа (кто пишет ключи) или из дефолтов `VarNode`;
  и в каком виде приходит SignalR-обновление.
- **Проверить**: реализован ли flow-контекст полностью (`IRuntimeNodeScope.FlowContext`,
  `NodeRuntime.FlowContexts`, `NodeService.cs:418` передаёт `flowContext: null`) — от этого зависит, когда
  отдавать имена flow-переменных в `Load()`.
- Нужен ли отдельный проект контрактов провайдера или хватит `Mars.Nodes.Front.Abstractions`.

## Дописано в план (2026-09-17, в конец списка работ)

- **Перелопатить DebugMode и запись значений** — **сделано 2026-09-22, фаза E** (см. ниже): синхронная
  сборка снимка без `e.Copy()`, плоский вид `path → value` на сервере, auto-off режима, version-bump
  по SignalR при сохранении pull-транспорта, суженный pull (upstream-замыкание вместо всего графа).
  Следующий пункт (что хранит `DebugNode`) закрыт фазой F.
- **Отложено: что хранит `DebugNode`** — **сделано 2026-09-22, фаза F** (см. ниже): DebugNode — явный
  рекордер полного объекта (`StoreFullObject`, последний, жёсткий лимит 2 МБ), просмотр в модалке
  с Monaco из формы ноды. Дубля стора фазы C нет: отдельные полка и ключ (просто `nodeId`).
- **Цена захвата при включённом режиме** (обновлено 2026-09-22, фаза E): `e.Copy()` убран — снимок
  собирается синхронно в `Save` (обрезка ограничивает стоимость), вне окна троттла (300 мс на ключ)
  сообщение пропускается без сборки. Стор ограничен `число нод × портов`. При выключенном режиме —
  одна проверка флага.
- **Разворачивать `object`-поля в подсказках глубже.** Сейчас часть путей останавливается на `object`:
  свойства с типом `object`/интерфейс/абстрактный класс не разворачиваются (`OutputValueSpecExpander.NoExpansionTypes`),
  примитивы вне `VarNode._typesDict` (`uint`, `short`, `byte`, `char`) дают `object`, enum — тоже `object`.
  `Dictionary`/`JsonElement`/`DynamicJson` статически не развернуть принципиально (ключи неизвестны — придут
  только debug-значениями). Доделать: разворачивать `object`/интерфейсы по фактическим свойствам, расширить
  словарь имён, enum → `string`/`int`, и **учитывать атрибуты сериализации**: `[JsonIgnore]` /
  `JsonIgnoreCondition` не предлагать, `[JsonPropertyName]`/`[Display(Name)]` — для подписи.
  Грабля: `DynamicNodeMsgWrapper` читает значения обычной рефлексией (`GetProperties`), поэтому `[JsonIgnore]`-свойство
  у CLR-объекта в выражении всё равно доступно — надо решить, что важнее: совпадение с JSON-формой значения
  или с рантаймом выражения.

- **`HttpRequestNode.Url`**: в Node-RED URL берётся не из `payload`, а из свойства/поля ноды. Первичное
  предположение: наша нода тоже берёт `Url` из своего свойства, если в поле значения не указано иное.
  Обыграть в провайдере: для таких нод в кандидатах должны быть **собственные свойства ноды**
  (`Mars.Nodes.Core/Nodes/Network/HttpRequestNode.cs` — `Url`, `UrlKind`, `Method`, `Headers`), а не только
  `msg.*`; источник поля значения по умолчанию — свойство ноды.
  **Проверено 2026-09-17:** в выражениях доступны только корни `msg` / `GlobalContext` / `FlowContext` /
  `VarNode` (`InputValueResolver.RootPathRegex`, `CreateInterpreter`) плюс `env(...)` — корня для свойств ноды
  нет, значит «свойство ноды» не может быть путём-кандидатом. Это решение про *дефолт значения поля*
  (const-режим читает свойство ноды) и, возможно, про новую ось — не про список подсказок. `For` даёт имя
  поля (`ValueFieldContext.FieldName`) — хук для этого решения уже есть.
- **JSON-редактор для поля значения**: добавить в `MarsValueInput` режим редактирования JSON-значения.
  Прецедент в репо — `Mars.Nodes.FormEditor/EditForms/Network/EndpointNodeForm.razor` (поле `JsonSchema` через
  `MarsCodeEditor2`/`MarsEditors`, `editor.GetValue()`); поле значения должно ходить в те же компоненты.
- **`EvalNode.ValueKind` не синхронизирован с осью `@`**: в `EvalNodeForm.razor` поле биндится на `Node.Input`
  как есть, режим ноды (`ValueKind`, по умолчанию `Expression`) остаётся её собственным — если решим жить по
  конвенции «символ в начале строки задаёт интерпретацию», это надо согласовать для `EvalNode` отдельно
  (`src/Mars.Nodes/Mars.Nodes.Core/Nodes/Functions/EvalNode.cs`).

