# Nodes — гайд подсистемы (карта)

> Читатель — агент, который меняет нод-движок. **Гайд — карта, не ТЗ**: пути и решения сверяй с кодом.
> Проверено по коду: 2026-09-26.
> Рецепт создания ноды (модель/impl/форма/тесты/спека) — [NodeCreationGuide.md](./NodeCreationGuide.md).
> Производительность выражений — [ExpressionEnginePlan.md](./ExpressionEnginePlan.md).
> История работы (планы схлопнуты в этот гайд): `git show 2d0f12dc:ai/NodesReworkPlan.md`,
> `git show 2d0f12dc:ai/NodesReworkStage2Plan.md`.

## Состав — проекты и точки входа

- `src/Mars.Nodes/Mars.Nodes.Core` — модели нод (`Nodes/<Группа>/XxxNode.cs`), `VarNode` (словарь типов
  значений), `ValuePath.cs` (единый источник корней/синтаксиса путей), контракты выходов
  (`INodeOutputValueSpec`, `NodeOutputValueSpecAttribute`, `OutputValueSpec`, `NodeOutputValueSpecReader`,
  `OutputValueSpecExpander`), `InputValueKind` (`Nodes/Common/`), примеры (`Examples/Nodes/`). Browser-safe.
- `src/Mars.Nodes/Mars.Nodes.Core.Implements` — impl'ы нод (`Nodes/<Группа>/XxxNodeImpl.cs`),
  `NodeImplementFactory`.
- `src/Mars.Nodes/Mars.Nodes.Expressions` — **единственный резолвер выражений полей**
  (`InputValueResolver`), `ExpressionScope` (корни `msg`/`GlobalContext`/`FlowContext`/`VarNode`/`env`),
  `ExpressionRunner(+Pool)`, `ExpressionSession` (вход — `RNS.Expressions(node)`),
  `DynamicNodeMsgWrapper` (msg как dynamic-объект).
- `src/Mars.Nodes/Mars.Nodes.Contracts` — wire-DTO: `Nodes/` (ответы `Load`/`NodesDataResponse`,
  `NodeDebugSnapshot(+Full)`), `Hubs/` (`DebugMessage`, `NodeStatus`, `NodeExecutionTrigger`), `NodeTaskJob/`.
- `src/Mars.Nodes/Mars.Nodes.Abstractions` — серверные абстракции: `IRuntimeNodeScope`, `INodeDebugStore`, хабы.
- `src/Mars.Nodes/Mars.Nodes.Host` — `Services/NodeService` (Load, статусы, спеки), `Services/NodeRuntime`
  (контексты, VarNodes), `NodeTaskJob` (исполнитель цепочки), `Scheduler/NodeSchedulerService`,
  `Services/NodeDebugStore` + `NodeDebugSnapshotBuilder`, `Controllers/NodeController`, CLI (`NodesCli`).
- `src/Mars.Nodes/Mars.Nodes.FormEditor` — формы нод (`EditForms/<Группа>/XxxNodeForm.razor`), компоненты
  полей (`EditForms/Components/`: `MarsValueInput` — значение, `MarsPathInput` — путь, `ValueFieldTree`),
  справка нод в `wwwroot/docs/XxxNode/` (en + ru).
- `src/Mars.Nodes/Mars.Nodes.Front.Abstractions` — клиентские контракты: `Services/IValueFieldProvider.cs`
  (провайдер подсказок, `ValueFieldInfo`, `ValueFieldContext`), `Services/IHostValueHints.cs`, редакторная
  геометрия и состояние (`Editor/`: `NodesDocument`, `Wire`/`WirePoints`, `WireDrawUtil`).
- `src/Mars.Nodes/Mars.Nodes.Workspace` — редактор: `NodeEditor1` (+`NodesDocument`), `NodeEditContainer1`
  (форма работает с **копией** ноды), провайдеры подсказок (`Services/ValueFields/`: композит
  `ValueFieldProvider`, `MsgValueRootProvider`, `ContextValueRootProviders`, `HostValueHints`), палитра
  (`Models/PaletteBuilder`), INPUT-панель (`EditorParts/NodeInputViewer.razor`).
- Ноды модулей/плагинов — тот же паттерн вне `src/Mars.Nodes` (эталон — `Mars.Docker.*`:
  Contracts/Host/Front; рецепт в NodeCreationGuide).

## Поток исполнения

flows.json → `NodeService.Load()` (в ответе также `OutputValueSpecs` по TypeId, `GlobalVariableNames`,
`DebugMode`) → клиентский `NodesDocument`. Запуск: триггер (вход/расписание/CLI) → `NodeTaskJob` →
`INodeImplement.Execute(input, callback, parameters)` → `callback(msg)` → провода в следующие ноды;
в `callbackNext` исполнителя — захват снимка DebugMode. Контексты: `msg.Payload` + `msg.Context`
(плоские ключи, `Set<T>` пишет под именем CLR-типа), `GlobalContext`/`FlowContext` в `NodeRuntime`,
переменные — ноды `VarNode` из графа.

## Ось значений полей

- Хранение плоско: `XxxKind` (`const|msg|expression`, дефолт `const`) + `Xxx` строкой; kind `msg`
  компилируется в выражение `msg.<path>` — весь резолвинг одним путём через `InputValueResolver`.
- В impl: `using var expr = RNS.Expressions(Node); expr.Resolve(kind, value, varType, input, Node, "Field")`;
  перед долгим I/O аренду сужать до локальной функции (док в XML `ExpressionSession`).
- В форме: `MarsValueInput` (конвенция ввода `@` = expression, `For="() => Node.X"` — по нему компонент
  сам тянет подсказки), `MarsPathInput` — путь без `@` (куда писать результат). Рецепты — NodeCreationGuide.

## Контракты выходов и цепочка подсказок

1. Нода объявляет выход: атрибут `[NodeOutputValueSpec(typeof(T), Name, OutputPort, Description)]` на
   **impl** (статика) или интерфейс `INodeOutputValueSpec` на модели (config-зависимый, browser-safe).
   Транзит-нода не объявляет ничего. Пусто = «ничего не добавляет», не ошибка.
2. `OutputValueSpecExpander` разворачивает CLR-тип в плоские пути (только свойства, глубина ≤ 3,
   visited-set, скип-лист object/JsonElement/словари).
3. Хост собирает статику в `Load()` → клиент `IHostValueHints`; динамику клиент считает сам (инстанс есть).
4. `MsgValueRootProvider` обходит провода вверх от редактируемой ноды, мержит инстанс+хост, фильтр по порту,
   ближайшее объявление побеждает; `Fallback` (`Payload : object`) — только если Payload не объявлен никем.
5. Композит `ValueFieldProvider`: корни msg(0)/FlowContext(10)/GlobalContext(20)/VarNode(30), дедуп по Path.

**Инвариант интеропа:** подсказки тянутся при открытии попапа и по refresh, фильтрация локальная; набор
текста не порождает обращений ни к серверу, ни к провайдеру.

## DebugMode

- Глобальный флаг на сервере, не персистится, auto-off 30 мин (`INodeDebugMode`/`DebugModeState`);
  тумблер DEBUG в редакторе.
- Захват делает исполнитель в `NodeTaskJob.callbackNext` — всё, что нода отдала дальше; ключ `nodeId|port`
  (`NodeDebugSnapshot.Key`). `NodeDebugStore`: синхронная сборка снимка, leading-edge троттл 300 мс,
  TTL 10 мин; снимок — обрезанное дерево + плоский `Values: path → value` (`NodeDebugSnapshotBuilder`).
- Транспорт pull: `DebugSnapshots(nodeIds)` — upstream-замыкание выбранной/редактируемой ноды + сигнал
  `BroadcastHub.DebugSnapshotsChanged()` без данных; часы серверные (`ServerTimeUtc`).
- `DebugNode.StoreFullObject` — отдельная полка стора (персистимый флаг ноды): полный msg до 2 МБ,
  только последний, без TTL; просмотр — модалка с Monaco из формы ноды.
- Значения из снимков приходят в подсказки (`ValueFieldInfo.Value`, обрез 80) и в INPUT-панель.

## Грабли

- `DynamicNodeMsgWrapper` поднимает свойства payload наверх и **затеняет одноимённые ключи Context**;
  `NodeMsg.AsFullDict()` кладёт Payload поверх Context. Hot path выражений обязан соблюдать тот же приоритет.
- `.gitignore` содержит `[Dd]ebug/` — папки с именем `Debug/` молча не попадают в коммит.
- Дедуп специй — только по `(Path, OutputPort)`: по одному Path портовые ветки теряются.
- Запись по пути (`SetValueByPath`) — только рефлексия: словари/`JsonElement`/`DynamicJson` не поддерживаются,
  промежуточные сегменты должны существовать (null по дороге — тихий отказ).
- `DebugNodeImpl.ReadByPath` для контекстов читает только ключ первого уровня (`GlobalContext.var1.x` → null).
- Троттл снимков leading-edge: в непрерывном потоке снимок — первое сообщение окна, не последнее.
- `HostValueHints.SetDebugSnapshots` мержит и не очищает снимки удалённых нод (видны по stale-метке).
- C#-runtime-байндер не резолвит LINQ-extension'ы на dynamic-приёмнике — лечится `BindRootPaths`
  (подмена корневых путей статически типизированными параметрами); не изобретать обход заново.
- Экспандер специй видит только свойства: классы с public полями (`ForeachCycle`) объявлять путями вручную.
- enum/uint/byte/short дают `object` (нет в словаре `VarNode`);Dictionary/JsonElement не разворачиваются —
  их ключи придут только debug-значениями.
- CSS FormEditor/Workspace: править **оба** `style.less` и `style.css` (компиляция ручная), при правке —
  bump `MarsAppVersion` в `Directory.Build.props`.

## Инварианты

- Единственный резолвер выражений полей — `InputValueResolver` (`Mars.Nodes.Expressions`); Roslyn
  `CSharpScript` — только для полного C# в `FunctionNode`.
- Kind-поля по умолчанию `const` — старые flows остаются валидными; миграций не пишем.
- Форма редактирует копию ноды (`NodeEditContainer1`: `node.Copy(...)`) — всё динамическое приходит
  через сервисы, живых ссылок на рантайм у формы нет.
- Dataflow-ноды не пишут в контексты (только msg-корень): запись в GlobalContext/FlowContext не видна ни
  снимкам DebugMode, ни провайдеру подсказок — это работа `VariableSetNode`.
- `NodeOutput` в репо — порт/провод, не тип данных; типы «что нода отдаёт» — корень `NodeOutputValueSpec*`.

## Отклонено (не пересматривать без причины)

- Обратная совместимость/миграции при смене формата полей — решение пользователя: не нужны.
- «Все поля Inject в один объект Payload» (вариант B) — ломает ноды, ждущие в Payload скаляр/строку/поток.
- `@` как маркер внутри строки — отдельное Kind-поле; `@` живёт только как конвенция ввода в UI.
- Инференс типов из наблюдённого JSON как источник истины — источник истины контракт ноды; данные
  DebugMode дают значения, не типы.
- Хранить последние входящие сообщения по умолчанию — память/приватность; только DebugMode + TTL.
- Generic `InputSource<T>` для хранения — плоско строками (`Kind` + `Value`).
- Monaco для expression-полей сразу — строка с попапом; Monaco приходит через модуль CodeCompletion.

## Что ещё не сделано (бэклог)

- Разворот `object`/интерфейсов/enum в подсказках глубже + учёт атрибутов сериализации
  (`[JsonIgnore]` не предлагать, `[JsonPropertyName]`/`[Display]` для подписи); решить, что важнее —
  JSON-форма значения или рантайм выражений (`[JsonIgnore]`-свойство в выражении доступно).
- Разворот `EndpointNode.JsonSchema` в точные пути подсказок; JSON-редактор для значения `MarsValueInput`
  (прецедент — `EndpointNodeForm` c `MarsCodeEditor2`).
- Ось «дефолт значения поля из свойства ноды» (`HttpRequestNode.Url`): хук есть —
  `ValueFieldContext.FieldName`; корни выражений свойств ноды не имеют.
- Deep-read контекстов по пути; массивы/индексы `[N]` в сеттерах записи.
- `InlineFunctionNodeImpl` при null-результате не зовёт callback (поток молча обрывается) — обсудить;
  `@expr` в `InlineFunctionNode.Arguments` — старый механизм, на Kind-ось не переехал.
- Проверить реализован ли flow-контекст полностью (`NodeService` передаёт `flowContext: null`) — от этого
  зависит отдача имён flow-переменных в `Load()`; SignalR-обновление списков переменных.
- Inject `timestamp`: только unix-millis (ISO-8601?); массивы/объекты в const — сырой JSON-текст;
  предпросмотр собранного JSON в форме (как OUTPUT n8n); нерабочие ключи локализации `InjectNodeForm`.
