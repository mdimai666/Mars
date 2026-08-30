# План: память, скиллы и ноды для ИИ-агента (Mars.AiChat)

Исходная задача: `ai/Prompts/CreateAiSkillsAndMemoryPrompt.md`. Статус: **черновик, не выполняем — думаем**.
План разбит на фазы; каждая фаза самодостаточна и коммитится отдельно. **Ноды — последними.**

## Контекст и решения

- Агент — harness-агент Microsoft Agent Framework 1.17.0 (`AsHarnessAgent` в
  `src/Mars.Modules/Mars.AiChat.Host/Services/AiChatAgentService.cs`), но harness-провайдеры сейчас
  отключены: `DisableFileMemory = true`, `DisableAgentSkillsProvider = true`.
- Референс организации — **Qwen Code CLI**: `~/.qwen/memories/` (файлы + индекс `MEMORY.md`,
  frontmatter name/description/type) и `~/.qwen/skills/<имя>/SKILL.md` (frontmatter name/description,
  ресурсы рядом). Harness использует тот же формат скиллов (SKILL.md + YAML frontmatter).
- Решения, согласованные с пользователем:
  - память — **общая на инстанс** (не по чатам и не по юзерам);
  - скиллы создают **и агент, и админ** (агенту даётся рабочая папка);
  - всё хранится в **`/data/ai/...`**;
  - постоянные автоматизации — через ноды (сохраняются), разовые запуски нод — **без сохранения**.

## Раскладка на диске

Корень данных Mars: keyed `IOptions<FileHostingInfo>("data")` → `<ContentRoot>/data`
(регистрация — `MainMarsHost.UseFileStorages`, `src/Mars.Host/MainMarsHost.cs`; в Docker это `/data`).

```
<data>/ai/            — «дом» агента (FileAccessStore)
<data>/ai/memory/     — память (FileMemoryStore)
<data>/ai/skills/     — кастомные скиллы (SKILL.md), пишет админ и/или агент
<assembly>/skills/    — bundled-скиллы, shipятся с модулем
```

---

## Фаза 1 — Память

Цель: агент помнит факты между чатами и перезапусками; формат и дух — как у Qwen Code.

1. `AiChatAgentService.RunChatAsync`:
   - убрать `DisableFileMemory = true`;
   - `FileMemoryStore = new FileSystemAgentFileStore(Path.Combine(aiRoot, "memory"))`.
   - Дефолтный working folder пустой → все сессии/чаты пишут в один корень = общая память.
   - Агент получает инструменты `file_memory_write/read/delete/ls/grep/replace/replace_lines`
     и автоиндекс `memories.md` (встроенное поведение `FileMemoryProvider`).
2. `AiChatPrompts.cs`: секция с правилами памяти (в духе Qwen Code): запоминать устойчивые факты
   о сайте/предпочтениях админа; не запоминать временное; перед использованием проверять актуальность.
3. Проверка: `dotnet build`; в чате «запомни, что …» → файл появился в `<data>/ai/memory/`;
   в **новом** чате агент факт помнит.

API (проверено по NuGet-докам `Microsoft.Agents.AI` 1.17.0): `FileSystemAgentFileStore(rootDirectory)` —
безопасный корень, relative-only пути, авто-создание корня; `HarnessAgentOptions.FileMemoryStore`.

---

## Фаза 2 — Скиллы + рабочая папка агента

Цель: процедурные знания — данными (SKILL.md), а не кодом; масштабирование на «много скиллов».

1. `AiChatAgentService`:
   - убрать `DisableAgentSkillsProvider = true`;
   - `AgentSkillsSource = new AggregatingAgentSkillsSource([
       new AgentFileSkillsSource(<data>/ai/skills, ...),
       new AgentFileSkillsSource(<AppContext.BaseDirectory>/skills, ...)])`.
   - `AgentFileSkillsSource` сам находит SKILL.md (до 2 уровней вложенности), валидирует frontmatter,
     подтягивает ресурсы/скрипты.
2. Bundled-скилл для затравки: `Mars.AiChat.Host/skills/mars-nodes/SKILL.md`
   (формат JSON нод, примеры: HTTP-эндпоинт, cron, C#-функция; правила «разовое не сохраняем»)
   + `<Content Include="skills/**" CopyToOutputDirectory="PreserveNewest"/>` в csproj.
   (Скилл про ноды пригодится в Фазе 3; можно закоммитить вместе с ней.)
3. Рабочая папка: `FileAccessStore = new FileSystemAgentFileStore(aiRoot)` +
   `FileAccessProviderOptions { DisableReadOnlyToolApproval = true, DisableWriteToolApproval = true }`
   (UI для approve нет) — агент получает `file_access_*` и может сам создавать скиллы и артефакты
   в своём «доме», админ может положить SKILL.md руками.
4. Промпт: секция «следуй инструкциям подходящего скилла» (список скиллов harness подставляет сам).
5. Проверка: положить тестовый SKILL.md в `<data>/ai/skills` → агент его видит и применяет;
   попросить агента создать скилл → пишет файл сам.

---

## Фаза 3 — Ноды (последними)

### Проверенные факты о Mars.Nodes

- Один общий граф нод в `<data>/nodes/flows.json` (`NodeService`, `src/Mars.Nodes/Mars.Nodes.Host/Services/NodeService.cs`).
- Публичный интерфейс — `INodeService` (`src/Mars.Host.Shared/Services/INodeService.cs`):
  `BaseNodes`, `TryReadFlowFile`, `Deploy(все ноды)` (AssignNodes + SaveToFile + OnDeploy),
  `InjectAsync`, `CallNode`, `GetNodesData`.
- `Deploy` сразу оживляет всё: HTTP-роуты (`MarsNodesMiddleware` + `CompiledHttpRouteMatcher`,
  пересборка в `AssignNodes`), cron (`NodeSchedulerService` по `OnDeploy`), MQTT (`OnAssignNodes`),
  события (`IEventManager`). Отдельных create/update/delete потока нет — только деплой полного графа.
- Модель: `Node` (Id, TypeId, Container=Id FlowNode, `Wires: List<List<NodeWire>>` по выходным портам,
  Inputs/Outputs); полиморфный JSON — `INodesLocator.CreateJsonSerializerOptions()` (`NodeJsonConverter`).
- Каталог типов: `INodesLocator.Dict` (typeId → DisplayAttribute, DefaultInstance) +
  `NodesData.InlineFunctionNodeSchemas`.
- Билдер цепочек `NodesWorkflowBuilder` — **публичный**, `src/Mars.Nodes/Mars.Nodes.Core/Utils/NodesWorkflowBuilder.cs`
  (`Create().AddNext(node).AddNext([n], catchAllWires: true).BuildWithFlowNode()`).
- `NodeRuntime`/`NodeTaskManager` — **internal** (InternalsVisibleTo только тестам/бенчам) →
  песочница для разовых запусков реализуется **внутри Mars.Nodes.Host**.
- Паттерны one-shot (из `tests/Mars.Nodes.Tests/Services/NodeServiceUnitTestBase.cs`):
  временные `NodeRuntime`+`NodeTaskManager`; прямой `impl.Execute(input, capture, new ExecutionParameters(...))`;
  прогон через `NodeTaskManager.CreateJob` + callback-нода в конце (catch-all wires).
  Запрос-ответ с ожиданием результата — паттерн `NodeService.CallNode`
  (`CallNode` + `CallNodeCallbackAction` + TaskCompletionSource + таймаут).
- C#-выполнение: `FunctionNode` (`core.FunctionNode`, поле `Code`) — Roslyn scripting; в скрипте
  доступны `msg`, `RNS.GetService<T>()`, `Flow.Context`, `Send(...)`, `Debug(...)`.

### Работы

1. **`INodeSandbox`** — новый интерфейс в `src/Mars.Host.Shared/Services/` (рядом с `INodeService.cs`):
   `RunOnceAsync(nodes, injectNodeId, payload, ct)` и `RunCSharpOnceAsync(code, payload, ct)`
   → результат (ok, output, error). Реализация `NodeSandbox` в `Mars.Nodes.Host/Services/`:
   временный `NodeRuntime(new BroadcastHub(hubContext), factory, sp)` + `NodeTaskManager`,
   захват вывода — через авто-добавленный `CallNode` (паттерн `NodeService.CallNode`).
   Не трогает singleton-`NodeService`, ничего не сохраняет. Регистрация в `MainMarsNodes.AddMarsNodes`.
2. **`MarsNodesTools`** — `src/Mars.Modules/Mars.AiChat.Host/Tools/` (scoped, как `MarsSqlTools`):
   - `list_node_types` — `INodesLocator.Dict` компактно (typeId, имя, группа, порты) + inline-функции;
   - `get_flows` — дамп `INodeService.BaseNodes` (id/typeId/name/container/wires);
   - `add_automation(nodesJson)` — deserialize через locator-опции, авто-FlowNode-контейнер,
     слияние с текущим графом, `Deploy` (сохраняется и оживает);
   - `remove_nodes(nodeIds)` — фильтр графа + `Deploy`;
   - `inject_node(nodeId, payload)` — разовый триггер уже задеплоенной ноды (`InjectAsync`/`CreateJob`);
   - `run_once(nodesJson, injectNodeId, payload)` / `run_csharp_once(code, payload)` — `INodeSandbox`.
3. **Флаг** `EnableNodesAccess` (default true) в `AiChatOption` по образцу `EnableSqlAccess`;
   DI — `MainAiChat.AddMarsAiChat` (`AddScoped<MarsNodesTools>()`).
   AiChat.Host новых reference скорее всего не требует (интерфейсы доступны транзитивно через
   Mars.Host.Shared) — проверить сборкой.
4. **Bundled-скилл `mars-nodes`** (из Фазы 2) + секция промпта `NodesInstructions`:
   постоянное — через `add_automation` и с подтверждением `ask_user`; разовое — `run_once`/
   `run_csharp_once`/`inject_node`; перед правкой читать `get_flows`; после — проверять.
5. **Тест** `NodeSandbox` по образцу `NodeServiceUnitTestBase` (`RunCSharpOnce("return 1+2;")` → 3).
6. Проверка вручную: «сделай эндпоинт /api/hello» → виден в редакторе нод и отвечает по HTTP;
   `run_once` **не** появляется в `flows.json` и в редакторе.

---

## Сквозное

- После каждой фазы обновлять `ai/AiChatGuide.md` (память/скиллы/песочница, раскладка `/data/ai`).
- js/css фронта не меняются — bump `MarsAppVersion` не нужен.
- Каждая фаза — отдельный коммит; фазы не зависят друг от друга по коду (кроме bundled-скилла mars-nodes,
  который физически добавляется в Фазе 2, а промптом доиспользуется в Фазе 3).
