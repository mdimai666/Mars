<!-- Title: Создание первой ноды -->
<!-- Order: 1 -->

# Создание первой ноды на C#

В этом руководстве показано, как создать свою ноду для визуального редактора Mars
на примере плагина. Устройство ноды (сообщения, порты, поля значений, контракты выходов)
описано в справочнике [Устройство ноды](NodeAnatomy.md).

Для начала [создайте плагин](../Plugins/PluginGettingStart.md) — шаблон
[MyMarsPlugin](https://github.com/mdimai666/MyMarsPlugin) уже содержит нужные ссылки
на пакеты и структуру проектов:

```
MyNewPlugin/
    src/
        MyNewPlugin/           # Backend — модель ноды, реализация, регистрация
        MyNewPlugin.Shared/    # Общие DTO
        MyNewPlugin.Front/     # Frontend (Blazor WASM) — форма редактирования
```

## 1. Создайте класс-наследник Node

Модель ноды — это класс, унаследованный от `Node`. Все его публичные свойства
сохраняются в JSON схемы, поэтому держите в модели только настройки ноды.

```csharp
using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Nodes.Core;

namespace MyNewPlugin.Nodes;

[FunctionApiDocument("./_plugin/MyNewPlugin/docs/MyFirstNode/MyFirstNode{.lang}.md")]
[Display(GroupName = "custom")]
public class MyFirstNode : Node
{
    [Required]
    public string CustomProperty { get; set; } = "Default Value";

    public MyFirstNode()
    {
        Inputs = [new()];         // один входной порт
        Outputs = [new()];        // один выходной порт
        Color = "#ffcc00";        // цвет ноды в редакторе
        Icon = "/_plugin/MyNewPlugin/nodes/img/icon.png";
    }
}
```

Пояснения:

- `[Display(GroupName = "custom")]` — группа в палитре редактора. Имя в палитре —
  свойство `Name`, а если оно пустое — имя класса без суффикса `Node` (здесь «MyFirst»).
- `[FunctionApiDocument(...)]` — адрес markdown-справки, которая откроется в форме ноды.
  `{.lang}` автоматически заменяется на `.ru` для русской локали (см. шаг 6).
- Конструктор обязательно без параметров — редактор создаёт ноду через рефлексию.

## 2. Реализуйте логику выполнения

Логика ноды — класс, реализующий `INodeImplement<MyFirstNode>`. Он получает входящее
сообщение `NodeMsg`, обрабатывает его и передаёт дальше через `callback`.

```csharp
using Mars.Nodes.Abstractions;
using Mars.Nodes.Contracts.Hubs;
using Mars.Nodes.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyNewPlugin.Nodes;

namespace MyNewPlugin.Implements;

public class MyFirstNodeImpl : INodeImplement<MyFirstNode>
{
    public MyFirstNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    readonly ILogger<MyFirstNodeImpl> _logger;

    public MyFirstNodeImpl(MyFirstNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
        _logger = RNS.ServiceProvider.GetRequiredService<ILogger<MyFirstNodeImpl>>();
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        // Вход: input.Payload и input.Context (msg.<ключ>)
        var message = $"{Node.CustomProperty}: {input.Payload}";

        // Выход: изменяем payload и передаём сообщение следующему узлу
        input.Payload = message;
        callback(input);

        // Текст-статус под нодой в редакторе
        RNS.Status(new NodeStatus(DateTime.Now.ToString("HH:mm:ss")));

        // Сообщение в отладочную панель редактора
        RNS.DebugMsg(DebugMessage.NodeMessage(Node.Id, message));

        _logger.LogInformation("MyFirstNode: {Message}", message);
        return Task.CompletedTask;
    }
}
```

Пояснения:

- Конструктор всегда принимает `(node, rns)`; дополнительные сервисы можно добавить
  параметрами — они разрешатся из DI автоматически, либо достаньте их вручную через
  `RNS.ServiceProvider`.
- `callback(input)` отправляет сообщение в порт 0. Для нод с несколькими выходами
  используйте `callback(input, index)`; если `callback` не вызвать — ветка схемы
  на этой ноде закончится.
- Ошибки кидайте как `NodeExecuteException(Node, "текст")` — редактор покажет их на ноде.

## 3. Зарегистрируйте ноду

Регистрация — это добавление сборок в локаторы. В шаблоне плагина вызовы уже есть;
если создаёте проект вручную, добавьте в backend (`Startup.cs`):

```csharp
public override void ConfigureWebApplication(WebApplication app, PluginSettings settings)
{
    // модели (Node) и реализации (INodeImplement<>) — из обеих сборок
    app.Services.AutoHostRegisterHelper([GetType().Assembly, typeof(MyNewPluginFront).Assembly]);
}
```

и во фронтенде:

```csharp
public void ConfigureApplication(WebAssemblyHost app)
{
    // модели (Node) и формы (NodeEditForm)
    app.Services.AutoFrontRegisterHelper([GetType().Assembly]);
}
```

Хелперы сами найдут в сборках классы `Node`, `INodeImplement<>` и `NodeEditForm`
и зарегистрируют их.

## 4. Создайте форму редактирования

Форма — Razor-компонент во фронтенд-проекте. Она открывается двойным кликом по ноде.

```razor
@using Mars.Admin.Framework.Components
@using Mars.Nodes.Core
@using Mars.Nodes.FormEditor
@using Microsoft.FluentUI.AspNetCore.Components
@using MyNewPlugin.Nodes
@inherits NodeEditForm
@attribute [NodeEditFormForNode(typeof(MyFirstNode))]

<div class="form-group compact" style="--fluent-input-label-basis:150px">
    <div class="vstack gap-2">
        <FormItem2 For="() => Node.CustomProperty">
            <FluentTextField @bind-Value=@Node.CustomProperty />
        </FormItem2>
    </div>
</div>

@code {
    [CascadingParameter] Node? Value { get; set; }
    MyFirstNode Node { get => (MyFirstNode)Value!; set => Value = value; }
}
```

- `FormItem2` сам берёт подпись из `[Display(Name = ...)]` и показывает ошибки
  валидации DataAnnotations (например, `[Required]`).
- Если форму не создать, редактор покажет предупреждение «NodeEditFormType not implement»;
  имя ноды и флаг Disabled останутся редактируемыми.
- Готовые компоненты полей (ввод значений с выражениями, выбор пути, cron-маска и др.)
  лежат в `Mars.Nodes.FormEditor/EditForms/Components` — см. [Устройство ноды](NodeAnatomy.md).

## 5. Проверьте ноду в редакторе

После сборки и установки плагина нода появится в палитре редактора
(в админке: Nodes, или dev-стенд `http://localhost:5004/dev/nodered`).

Для проверки соберите схему `Inject → MyFirstNode → Debug`:

1. перетащите ноды из палитры и соедините их проводами;
2. нажмите кнопку запуска на Inject (или включите тумблер **DEBUG** в панели редактора,
   чтобы видеть значения сообщений);
3. в панели отладки появится сообщение из `RNS.DebugMsg`, а под нодой — статус;
4. двойной клик по ноде откроет форму из шага 4.

## 6. Добавьте справку ноды

Справка показывается в форме ноды (вкладка помощи) и берётся по URL из
`[FunctionApiDocument]`. Создайте в `wwwroot` фронтенд-проекта два файла:

```
wwwroot/docs/MyFirstNode/MyFirstNode.md       # английская версия
wwwroot/docs/MyFirstNode/MyFirstNode.ru.md    # русская версия
```

В справке опишите назначение ноды, её поля, формат входящего/выходного сообщения
и пример. Образец — справка ноды Inject в репозитории
(`src/Mars.Nodes/Mars.Nodes.FormEditor/wwwroot/Docs/InjectNode/InjectNode.ru.md`).

---

**Что дальше:**

- сообщения `Payload`/`Context`, порты, выражения `@`, контракты выходов и DebugMode —
  в справочнике [Устройство ноды](NodeAnatomy.md);
- публикация плагина (zip/NuGet) — в [PluginSdk](../Plugins/PluginSdk.md).
