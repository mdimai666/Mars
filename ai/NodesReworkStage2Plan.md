# План: этап 2 — контракты выходов нод и провайдер подсказок в поле значения

> **Статус: фазы A и B сделаны 2026-09-17 (`NodeOutputValueSpec` + провайдер подсказок), в работе — C.**
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

- [ ] Флаг DebugMode: где живёт (нода в графе, редактор, отдельная `DebugNode`) и включает ли запись для всех
      нод или выборочно — см. открытые вопросы.
- [ ] Запись последних входящих на сервере: место хранения (обёртка impl / `NodeService` / `NodeRuntime`),
      ключ `nodeId`, лимит по числу и размеру, TTL 10 минут (InMemory).
- [ ] Обрезка значений одной функцией: строка — по длине, JSON — по глубине и первым N элементам; тесты.
- [ ] Транспорт в форму: метод эндпоинта/хаба + клиентский кэш; подписка на `DebugMsg` как быстрый путь
      (в WASM-стенде `ClientHub` уже регистрируется).
- [ ] Пикер в debug-режиме: колонка значения с обрезкой, признак свежести/`stale`.
- [ ] UI-добор из этапа 3 соседнего плана: панель INPUT по последнему сообщению, автокомплит `msg.`/`flow.`/
      `global.`/`VarNode.` — теперь на реальном провайдере.

## Грабли и риски

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
- Транспорт debug-значений: расширять `NodeController` или хаб.
- Нужен ли отдельный проект контрактов провайдера или хватит `Mars.Nodes.Front.Abstractions`.

## Дописано в план (2026-09-17, в конец списка работ)

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

