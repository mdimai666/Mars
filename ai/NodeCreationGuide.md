# Создание нод — гайд для агента

> Читатель — агент, который добавляет или меняет ноду (Core, модуль или плагин).
> **Гайд — карта, не ТЗ.** Конвенции, решения и инварианты ниже можно брать как есть;
> сигнатуры, списки параметров компонентов и перечни «кто уже подключён» перед правкой
> сверяй с названными эталонами/кодом — код может быть новее гайда. Каждый рецепт указывает
> файл-эталон: открой его и повтори, вместо того чтобы кодить по памяти гайда.
> Проверено по коду: 2026-09-26 (в т.ч. экспериментом 2026-09-25: агент с одним этим гайдом
> создал ноду `UpperNode` полным комплектом — сборка 0 errors, 573 теста зелёных; сама
> экспериментальная нода удалена, её проверенный код сохранён в «Скелете» ниже).
> История и статус реворка полей/значений — `ai/NodesReworkPlan.md`, `ai/NodesReworkStage2Plan.md`
> (планы открыты; контракты выходов, DebugMode, `MarsValueInput`/`MarsPathInput` — там).
> Иконки — `ai/NodeIconsGuide.md`. Ноды в плагине — `ai/PluginCreationGuide.md`.
> Документация для людей — `docs/dev_docs/Nodes/` (`CreateFirstNode.md`, `NodeAnatomy.md`).

## Состав ноды (где что лежит)

Нода — это набор, а не один файл. Для Core-ноды:

- **Модель** — `src/Mars.Nodes/Mars.Nodes.Core/Nodes/<Категория>/XxxNode.cs` — класс-наследник
  `Node`; все публичные свойства сериализуются в flows.json.
- **Имплементация** — `src/Mars.Nodes/Mars.Nodes.Core.Implements/Nodes/<Категория>/XxxNodeImpl.cs` —
  `INodeImplement<XxxNode>`, рантайм-логика.
- **Форма** — `src/Mars.Nodes/Mars.Nodes.FormEditor/EditForms/<Категория>/XxxNodeForm.razor`.
- **Справка** — `src/Mars.Nodes/Mars.Nodes.FormEditor/wwwroot/docs/XxxNode/XxxNode.md` + `.ru.md`
  (папка в нижнем регистре — так в git; оба файла обязательны, проверяет `NodesDocTests`).
- **Тесты** — `tests/Mars.Nodes.Tests/Nodes/XxxNodeTests.cs`.
- Опционально: **пример** — `src/Mars.Nodes/Mars.Nodes.Core/Examples/Nodes/`, **контракт выходов** —
  атрибуты на impl (раздел ниже).

Категория ≈ папка и `[Display(GroupName)]` = группа в палитре (папки иногда расходятся с именами
групп: `Diagnostics` → `diagnostic`, `TaskNodes` → `task`, `Validation` → `validations`).
Существующие группы (актуальный список — grep `GroupName = ` по `Nodes/`): `common`,
`functions`, `network`, `storage`, `parser`, `sequence`, `events`, `connections`, `diagnostic`,
`task`, `validations`, `dev`, `database` (модуль Datasource). Без атрибута — группа `other`.
Имя в палитре — `Node.Label`: свойство `Name`, иначе имя класса минус последние 4 символа («Node»).

Модульные ноды живут в проектах модуля (образец — `src/Mars.Datasource/Mars.Datasource/Nodes/SqlNode.cs`
+ `SqlNodeImpl` в `Mars.Datasource.Host` + форма в `Mars.Datasource.Front`).

**Global usings**: в `Mars.Nodes.Core/Globals.cs`, `Mars.Nodes.Core.Implements/Globals.cs` и
`tests/Mars.Nodes.Tests/Globals.cs` подключены namespaces категорий нод (`...Nodes.Common`,
`...Nodes.Functions`, `Mars.Nodes.Contracts.Hubs` и др.) — поэтому эталоны компилируются без
using'ов. Нода в **новом** namespace без правки `Globals.cs` в эталонах не видна; правка
`Globals.cs` — изменение существующего файла, делать осознанно. То же для форм:
`Mars.Nodes.FormEditor/_Imports.razor` уже подключает Core-namespaces категорий.

## Скелет новой Core-ноды (точные формы)

Модель (код из эксперимента 2026-09-25 — собран и протестирован; живой эталон пары
`Kind`+`Value` — `Nodes/Storage/FileWriteNode.cs`):

```csharp
[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/docs/UpperNode/UpperNode{.lang}.md")]
[Display(GroupName = "functions")]
public class UpperNode : Node
{
    public override string TypeId => "core.UpperNode";

    // InputValueKind — класс строковых констант, НЕ enum
    public string SuffixKind { get; set; } = InputValueKind.Const;

    [Display(Name = "Suffix")]
    public string Suffix { get; set; } = "";

    public UpperNode()
    {
        Inputs = [new()];
        Outputs = [new()];
        Color = "#b2b2b2";
        Icon = "_content/Mars.Nodes.Workspace/nodes/string.svg";
    }
}
```

Impl (обязательны: `RNS { get; set; }`, явная реализация нетипизированного свойства,
ленивый интерпретатор; живой эталон — `Nodes/Storage/FileWriteNodeImpl.cs`):

```csharp
[NodeOutputValueSpec(typeof(string), Description = "...")]
public class UpperNodeImpl : INodeImplement<UpperNode>
{
    public UpperNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public UpperNodeImpl(UpperNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        Interpreter? interpreter = null;   // создаём, только если есть expression/msg kind
        if (Node.SuffixKind is InputValueKind.Expression or InputValueKind.Msg)
            interpreter = InputValueResolver.CreateInterpreter(RNS, input);

        var suffix = (string)InputValueResolver.Resolve(Node.SuffixKind, Node.Suffix, "string",
            interpreter, new ExpressionScope(RNS, input), Node, "Suffix")!;

        input.Payload = (input.Payload?.ToString() ?? "").ToUpper() + suffix;
        callback(input);
        return Task.CompletedTask;
    }
}
```

Шапка формы — минимум `@using`'ов (полный набор копировать из эталона `FileWriteNodeForm.razor`):

```razor
@using Mars.Admin.Framework.Components              @* FormItem2, AutoInputLabel *@
@using Mars.Nodes.Core
@using Mars.Nodes.FormEditor
@using Mars.Nodes.FormEditor.EditForms.Components   @* MarsValueInput, MarsPathInput *@
@using Microsoft.FluentUI.AspNetCore.Components
@inherits NodeEditForm
@attribute [NodeEditFormForNode(typeof(UpperNode))]
```

## Модель (Node)

- **TypeId**: Core-ноды переопределяют `public override string TypeId => "core.XxxNode";`.
  Модули/плагины НЕ переопределяют (дефолт `GetType().FullName`). TypeId — ключ в flows.json:
  **не менять после релиза**; переименование класса/namespace модульной ноды ломает сохранённые flows.
  Дубликат TypeId — исключение при сборке словаря локатора (первое обращение к `Dict`,
  там `Dictionary.Add`).
- **Атрибуты**: `[Display(GroupName = "...")]`; `[FunctionApiDocument(url)]` — url справки с
  `{.lang}`-подстановкой. Core: `./_content/mdimai666.Mars.Nodes.FormEditor/docs/XxxNode/XxxNode{.lang}.md`
  (URL в нижнем регистре — совпадает с git-регистром папки; приведено 2026-09-26, раньше было
  `Docs/` и 404-ило бы на Linux); модуль: `./_content/<ФронтПакет>/docs/...` (образец — SqlNode);
  плагин: `/_plugin/<key>/...`.
  Справку рендерит `DocViewer` (`Mars.Admin.Framework/Components/DocViewer.razor.cs`) в
  `NodeEditContainer1`. Для Core-нод атрибут обязателен: без него падает
  `NodesDocTests.NodeTypes_AllRegistered_HaveFunctionApiDocumentAttribute` (исключение —
  только `InlineFunctionNode`).
- **Конструктор** (без параметров — модель создаётся через `Activator.CreateInstance`):
  `Inputs = [new()]`, `Outputs = [new()]` (или `OutputCount = N`), `Color = "#..."`,
  `Icon = "_content/Mars.Nodes.Workspace/nodes/<name>.svg"`. Флаг `IsInjectable = true` —
  кнопка ручного запуска на ноде в редакторе (`NodeComponent.razor`). `HasTailButton` — легаси
  (рендер в `NodeComponent` закомментирован), новым нодам не ставить.
- **Свойства**: публичные get/set идут в JSON. `[Display(Name = "...")]` для подписи. Клиентское
  состояние — только с `[JsonIgnore]` (в базовом `Node` ещё живут поля-состояние без `[JsonIgnore]` —
  бэклог P3 структурного реворка, не усугублять).
- **Валидация**: DataAnnotations на свойствах (`[Required]` и т.п.); для коллекций полей —
  `[ValidateComplexType]` на свойстве + `IValidatableObject` на модели (эталон — `InjectNode`).
  Форма показывает ошибки через `ObjectGraphDataAnnotationsValidator` + `FluentValidationSummary`
  (`NodeFormEditor1.razor`).
- **Поля со значениями** хранятся плоско строками: `VarType` (ось типа) и `ValueKind` (ось источника)
  ортогональны. Типы — словарь `VarNode` (`int/long/float/double/decimal/bool/string/DateTime/Guid`
  + массивы + `timestamp`); источники — `InputValueKind` (`const`/`msg`/`expression`, Core
  `Nodes/Common/InputValueKind.cs`).

## Имплементация (INodeImplement&lt;TNode&gt;)

- Конструктор: `(TNode node, IRuntimeNodeScope rns, ...)` — первые два аргумента подставляет
  `NodeImplementFactory` (`ActivatorUtilities.CreateFactory`), остальные резолвятся из
  `rns.ServiceProvider`. **Один impl на тип ноды** (фабрика берёт первый при сканировании).
  `Node INodeImplement.Node => Node;` — явная реализация нетипизированного свойства.
- `Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)`:
  - вход — `input.Payload` + `input.Context` (`Get/Set/Add/TryAdd`, `Get<T>()` по имени типа,
    `AsFullDict()`);
  - выход — `callback(msg)` (порт 0) или `callback(msg, i)` (порт i, индекс в `Outputs`/`Wires`).
    Не вызвать callback = ветка заканчивается (sink). Вызвать несколько раз можно (Split/Foreach).
    Несколько условий → несколько портов — эталон `SwitchNodeImpl`;
  - статус под нодой — `RNS.Status(new NodeStatus("text"))`; отладочная панель —
    `RNS.DebugMsg(DebugMessage.NodeMessage(Node.Id, "..."))` (есть `NodeWarnMessage`,
    `NodeErrorMessage`, `NodeException`);
  - ошибки — кидать `NodeExecuteException(Node, "сообщение")` (`Mars.Nodes.Core/Exceptions`):
    рантайм показывает её на ноде и в debug-ленте. Не глотать исключения молча;
  - сервисы — `RNS.ServiceProvider`; config-ноды — `Node.Config = RNS.GetConfig(Node.Config)`
    **в конструкторе**; контексты — `RNS.GlobalContext`, `RNS.FlowContext`, `RNS.GetVarNodeVarible(name)`.
- Экземпляр impl живёт с момента `Deploy` до redeploy/удаления — **не держать состояние между
  сообщениями** в полях impl; состояние — в `msg.Context`, контекстах или VarNode.
- Ручное завершение (нода отдаёт ответ асинхронно позже, как `HttpResponse`): маркерный интерфейс
  `ISelfFinalizingNode` + `RNS.Done(parameters)` в точке завершения; `Done` без маркера кидает
  (`NodeTaskJob`). Образец — `DevMicroschemeNodeImpl`.
- Жизненный цикл: `INodeLifecycleOnAssigned.OnNodeAssigned` (при деплое), `INodeLifecycleOnDelete.OnNodeDelete`.

## Значения полей: const / msg / expression

- **Резолвер один** — `InputValueResolver` (`src/Mars.Nodes/Mars.Nodes.Expressions/InputValueResolver.cs`).
  Не создавать своих интерпретаторов (исторические `XInterpreter`/`@`-конвенция удалены).
  - `CreateInterpreter(RNS, input)` — DynamicExpresso (`Default | LateBindObject`, reference на
    `Enumerable`), корни: `msg` (`DynamicNodeMsgWrapper`), `GlobalContext`, `FlowContext`, `VarNode`,
    `env("KEY")`.
  - `Resolve(kind, value, varType, interpreter, new ExpressionScope(RNS, input), Node, "имя поля")`:
    `const` — парсинг по `varType` (JSON для чисел/массивов, спец-логика `timestamp`: пусто = сейчас);
    `msg` — компилируется в `msg.<path>`; `expression` — как есть; результат конвертируется в
    `varType`; ошибки — `NodeExecuteException` с именем поля и значением.
  - Интерпретатор создавать лениво, только если есть kind `expression`/`msg`
    (образец — `SwitchNodeImpl`: `interpreter ??= ...`).
- Синтаксис путей — `ValuePath` (`Mars.Nodes.Core/ValuePath.cs`): сегменты-идентификаторы через
  точку, индексы `[N]`, корни `msg`/`GlobalContext`/`FlowContext`/`VarNode`. Единый источник и для
  регекса резолвера, и для редакторов ввода.
- **Форма**: компоненты `MarsValueInput` (поле **значения**, конвенция ввода `@` = expression,
  kind `msg` отображается как `@msg.<path>`) и `MarsPathInput` (поле **пути**, без `@`) —
  `Mars.Nodes.FormEditor/EditForms/Components/`. Оба сами тянут подсказки из каскадного
  `NodeEditContainer1.GetValueFields`, если передать `For="() => Node.Prop"` (обязателен);
  в DebugMode в подсказках видны значения. Хоткеи `MarsValueInput`: Ctrl+Space (автокомплит),
  Alt+Down/Alt+Up (дерево полей / операции), F4 (дата-пикер для DateTime).
  Параметры `MarsValueInput`: `VarType` (const-валидация формата и подпись тега),
  `Multiline Rows="N"` (textarea без подсветки выражения — для тел писем и т.п.),
  `Class` — **единственный** способ добавить внешний css-класс (`Class="mvi-fill"` — растянуть
  по ячейке); атрибут `class` снаружи перезатирает корневой класс компонента и ломает стили.
  Kind `Msg` из UI не сохраняется: `@msg.x` запишется как `Expression` (семантически то же).
- Модель хранит kind плоско (`XxxKind` + `Xxx`, дефолт kind — `Const`, чтобы старые flows
  оставались валидными), форма конвертирует в `@`-строку и обратно — эталон
  `FileWriteNodeForm.razor` (`FilePathValue`/`SetFilePathValue`). Уже подключены
  (актуальный список — grep `MarsValueInput` по `EditForms/`): Inject, Switch, Eval,
  FileWrite, FileRead, HttpRequest, MqttOut, EmailSend, VariableSet, DevAdminConnection.
  Новое поле со значением — повторять этот паттерн.
- `MarsPathInput` — режимы: `Root="msg"` + `ShowRoot="false"` (чип корня, значение относительное)
  либо `Roots` (список разрешённых, значение с корнем). Свободный ввод несуществующего пути
  разрешён (цель записи может ещё не существовать), валидация только синтаксическая.
  Запись по пути в impl — маршрутизация по образцу `TemplateNodeImpl`: `"Payload"` →
  `input.Payload`, путь с точкой → `DynamicNodeMsgWrapper(input).SetValueByPath(path, value)`,
  односегментный → `input.Set(key, value)`. `SetValueByPath` — только рефлексия (не словари/
  JsonElement), промежуточные сегменты должны существовать. Dataflow-нодам не писать в
  контексты (GlobalContext/FlowContext) — запись туда не видна ни снимкам DebugMode, ни
  провайдеру подсказок; это работа `VariableSetNode`.

## Контракты выходов (автокомплит подсказок)

- Статический выход — атрибут **на impl**: `[NodeOutputValueSpec(typeof(T))]` (можно несколько;
  `Name` — слот сообщения, дефолт `Payload`; `OutputPort`, `OutputValueSpec.AllOutputPorts = -1`;
  `Description`). Образцы — `MqttInNodeImpl`, `QueueNodeImpl`, `HttpRequestNodeImpl` (слот
  `requestInfo` через `Name`). `Name` слота обязан совпадать с рантайм-ключом:
  `NodeMsg.Set<T>(obj)` пишет под `typeof(T).Name` — использовать `nameof(T)`.
  Атрибуты читаются и с impl, и с модели (мерж), но место атрибута — impl: модели из
  `Mars.Nodes.Core` часто не видят DTO-типы.
- Тип разворачивается в плоские пути `OutputValueSpecExpander`: только public **свойства**
  (поля не участвуют — класс с полями вроде `ForeachCycle` объявлять спек руками строками),
  вложенность ≤ 3, visited-set против рекурсии типов, `[]` = элемент массива;
  `Dictionary`/`IEnumerable`/`JsonElement`/`JsonNode`/`NodeMsg` не разворачиваются
  (`NoExpansionTypes`); enum/uint/byte/short дают `object` (нет в словаре `VarNode`).
- Выход, зависящий от настроек экземпляра ноды, — интерфейс **на модели**:
  `INodeOutputValueSpec.GetOutputValueSpec()` (эталон — `InjectNode`: спек из `Fields`).
  Считается в браузере — только Core-типы, без серверных ссылок. Интерфейс **не отменяет**
  атрибуты impl — провайдер мержит оба источника (так у HttpRequestNode уживаются
  динамический Payload и статический слот).
- Пусто = «нода ничего не добавляет в msg» (транзит) — провайдер пойдёт по проводам выше;
  fallback `Payload : object` показывается, только если `Payload` не объявил никто по цепочке.
- Хост собирает статику в `NodesDataResponse.OutputValueSpecs` по TypeId
  (`NodeService.CollectOutputValueSpecs`), клиентский композит — `ValueFieldProvider`,
  `MsgValueRootProvider` (`Mars.Nodes.Workspace/Services/ValueFields/`), контракты провайдера —
  `Mars.Nodes.Front.Abstractions/Services/IValueFieldProvider.cs`. Дедуп специй — по
  `(Path, OutputPort)`. Бюджет интеропа (инвариант): подсказки считаются при открытии пикера
  и по refresh, фильтрация локальная — набор текста не должен порождать обращений к провайдеру.
- Снимки DebugMode захватывает исполнитель (`NodeTaskJob.callbackNext`, ключ «нода+порт») —
  от кода ноды ничего не требуется.
- Тесты: `tests/Mars.Nodes.Tests/OutputValueSpecs/NodeOutputValueSpecReaderTests.cs`
  (`ReadStatics_*` на атрибуты, `Read_*` на каждую config-ветку интерфейса),
  `ValueFields/ValueFieldProviderTests.cs` на провайдер.

## Форма (Razor)

- `@inherits NodeEditForm` + `@attribute [NodeEditFormForNode(typeof(XxxNode))]`. Нода приходит
  каскадом: `[CascadingParameter] Node Value`, в `@code` — типизированная обёртка
  `XxxNode Node { get => (XxxNode)Value; set => Value = value; }`.
- Без зарегистрированной формы редактор показывает плашку «NodeEditFormType not implement»
  (имя и Disabled остаются редактируемыми) — для визуальных нод форма обязательна.
- Контролы: FluentUI + `FormItem2 For="() => Node.Prop"` (label из `[Display]`, ошибки валидации) +
  `AutoInputLabel`. Списки полей — `FluentSortableList` + `ArrayUtil.MoveItem` +
  `FluentValidationMessage` на элемент; **`FormItem2` внутри списка не работает** (нужно выражение
  `For` на конкретный элемент).
- Ctrl+S в форме сохраняет ноду (обработчик в `NodeFormEditor1.razor`).
- Стилизация полей — HUD-конвенция `MarsValueInput` (`--mvi-*` токены, стили в
  `Mars.Nodes.FormEditor/wwwroot/css/style.less`; less компилирует пользователь).

## Справка ноды (wwwroot/docs)

- Два файла на ноду: `XxxNode.md` (en) и `XxxNode.ru.md` — `NodesDocTests.DocFiles_...ExistOnDisk`
  проверяет наличие обоих для каждой Core-ноды с `[FunctionApiDocument]`.
- **Регистр — всегда нижний `docs/`**: и папка на диске/в git, и URL в атрибуте (приведено
  2026-09-26: раньше Core-URL писали с заглавной `Docs/`, что на Linux-деплое дало бы 404 —
  отдача статики case-sensitive). Windows разницу не видит, поэтому сверять с `git ls-files`.
- Содержание (эталон — `docs/InjectNode/InjectNode.md`): назначение; поля (таблица/список);
  типы значений и kind'ы (`const`/`msg`/`expression`, корни выражений); JSON-пример ноды;
  CLI-команды, если есть; заметки о совместимости.

## Примеры (INodeExample)

- Класс `INodeExample<TNode>` в `Mars.Nodes.Core/Examples/Nodes/` — сканируется при регистрации
  той же сборки (`NodesLocator.CreateExamplesList`). Точная сигнатура:
  `IReadOnlyCollection<Node> Handle(IEditorState editorState)` + `Name` + `Description`;
  строить через `NodesWorkflowBuilder` (`Mars.Nodes.Core/Utils/NodesWorkflowBuilder.cs`):
  `AddNext(params Node[])`, `Build()` → `Node[]`.
  Простейший эталон — `StringNodeUpperCaseExample`, с несколькими полями —
  `InjectNodeMultipleFieldsExample1`.
- Примеры видны в редакторе (список примеров палитры) и удобны как готовые flows для тестов.

## Тесты

- Файл `tests/Mars.Nodes.Tests/Nodes/XxxNodeTests.cs`, база `NodeServiceUnitTestBase`
  (`tests/Mars.Nodes.Tests/Services/NodeServiceUnitTestBase.cs`):
  - `ExecuteNode(node, msg?)` — деплой с FlowNode и исполнение impl, возвращает `NodeMsg`;
  - `ExecuteNodeEx(node, msg?, flowNode:, varNode:)` — плюс номер выходного порта
    (`NodeExecutionResult`); через `varNode`/`flowNode` готовятся Var-ноды и контексты;
  - `RunUsingTaskManager(builder)` — полная цепочка через TaskManager с `TestCallBackNode` в конце;
  - сервисы — NSubstitute-моки в базе; DI-зависимости impl подменять через `_serviceProvider`.
- Подготовка данных в тестах: глобальный контекст — `Runtime.GlobalContext.SetValue("var", ...)`,
  контекст сообщения — `input.Set("key", ...)`. Рабочий эталон тестов с контекстами и
  резолвером — `VariableSetNodeTests.cs`.
- Конвенция всех нод-тестов: строка `_ = nameof(XxxNodeImpl.Execute);` — маркерная ссылка на
  тестируемый impl (копировать из эталона, не удалять «как мусор»).
- Секции `//Arrange` / `//Act` / `//Assert` обязательны (указание пользователя 2026-09-14).
- Запуск: `dotnet build tests/Mars.Nodes.Tests`, затем
  `tests\Mars.Nodes.Tests\bin\Debug\net10.0\Mars.Nodes.Tests.exe`
  (**не** `dotnet test` — MTP-драйвер сломан SDK 10.0.400, см. QWEN.md).
  Точечно один класс: `Mars.Nodes.Tests.exe -class Mars.Nodes.Tests.Nodes.XxxNodeTests`
  (MTP-синтаксис; `--filter` не существует).

## Регистрация

- **Core**: ничего делать не нужно — `MainNodes.UseMarsNodes` (Host) регистрирует сборки Core и
  Core.Implements, `MainNodeWorkspace` (Front) — Core и FormEditor.
- **Модуль**: в `Main<Module>.cs` (Host) — `INodesLocator.RegisterAssembly` +
  `INodeImplementFactory.RegisterAssembly`; в `Main<Module>Front.cs` — `INodesLocator` +
  `INodeFormsLocator`. Образцы: `Mars.Datasource.Host/MainDatasource.cs`,
  `Mars.Modules/Mars.WebApp.Nodes.Host/MainWebAppNodes.cs`, `Mars.SemanticKernel.Host/MainSemanticKernel.cs`.
- **Плагин**: `AutoHostRegisterHelper([сборки])` / `AutoFrontRegisterHelper([сборки])` сканируют
  типы сами (`Mars.Plugin.Kit.Host/PluginHostHelperExtensions.cs`, `Mars.Plugin.Front/PluginFrontHelperExtensions.cs`).
  Подробности — `ai/PluginCreationGuide.md`.

## Проверка результата

- `dotnet build Mars.slnx` — критерий **0 errors** (в дереве есть предсуществующие NU1903-варнинги
  про уязвимую бету `OpenTelemetry.Resources.Host` — не чинить их и не пугаться) +
  `Mars.Nodes.Tests.exe`.
- Правки css/js под `_content` — поднять `MarsAppVersion` в `Directory.Build.props`
  (cache-busting `?v=`; без bump браузер держит старое).
- Визуальную проверку в редакторе (`/dev/nodered`) делает пользователь — браузер без команды не открывать.

## Грабли

- **DynamicExpresso + dynamic**: C#-runtime-байндер не резолвит LINQ extension-методы на
  dynamic-приёмнике (`msg.Payload.Count()` → RuntimeBinderException). Лечится
  `InputValueResolver.BindRootPaths` (подмена корневых путей параметрами со статическим типом) —
  не изобретать обход заново.
- **Регекс путей**: .NET-regex с lookahead «сегмент не метод» (`(?!\s*\()`) на бэктреке обрезает
  сегмент; нужна полная граница `(?![A-Za-z0-9_(])` (уже в `RootPathRegex`).
- `DynamicNodeMsgWrapper` поднимает свойства payload наверх, и они **затеняют** Context-ключи с тем
  же именем; `AsFullDict()` кладёт `Payload` поверх копии Context.
- `NodeMsg.Copy()` копирует Context, но Payload — по ссылке: мутация payload после `callback` видна
  downstream-нодам.
- Новый тип в `VarNode._typesDict` без правки `GetTypeDefault`/`ResolveDefault` —
  `NotImplementedException` в валидации/дефолтах.
- Модель и impl живут долго (клиентский кэш / серверный Deploy): публичное свойство модели =
  поле flows.json; случайное state-свойство уедет в JSON навсегда.
- Старые flows с неизвестными JSON-свойствами — свойство молча игнорируется при десериализации
  (дефолт из инициализатора). Миграций не пишем (решение пользователя 2026-09-14).
- Чтение контекстов по пути (`DebugNodeImpl.ReadByPath`) берёт только ключ первого уровня
  (`GlobalContext.var1.x` вернёт null); deep-read контекстов — открытая задача.
- CSS форм: `Mars.Nodes.FormEditor/wwwroot/css/style.less` и `style.css` править **оба**
  (компиляция less ручная, compilerconfig.json).

## Инварианты

- Нода = модель + impl + форма + справка (en/ru) + тесты; примеры и контракт выходов — по необходимости.
- TypeId после релиза не меняется.
- Оси «тип значения» (`VarType`) и «источник значения» (`ValueKind`) ортогональны, хранение плоское строковое.
- Единственный резолвер выражений полей — `InputValueResolver` (проект `Mars.Nodes.Expressions`);
  движок — DynamicExpresso (Roslyn `CSharpScript` — только внутри FunctionNode).
- Семантика полей: `Key == "Payload"` (без учёта регистра) → `msg.Payload`, остальные → `msg.Set(key, ...)`
  (Context), доступны как `msg.<key>`.
- Захват DebugMode и провода живут в исполнителе (`NodeTaskJob`), не в нодах.
- Обратная совместимость JSON нод по умолчанию не обеспечивается; исключения — только явно
  согласованные (прецедент: `DebugNodeImpl` снимает legacy-`@` с пути).
