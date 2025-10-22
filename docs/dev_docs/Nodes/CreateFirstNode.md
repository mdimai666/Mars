# Создание первой ноды на C#

В этом руководстве показано, как создать свою первую пользовательскую ноду для платформы Mars.

Для начала [создайте плагин](https://github.com/mdimai666/MyMarsPlugin)

## 1. Создайте класс-наследник Node

Создайте новый файл, например, `MyFirstNode.cs`. В проекте фронта

Пример содержимого:
```
using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace MyMarsPlugin.Nodes;

[FunctionApiDocument("./_plugin/MyMarsPlugin/nodes/docs/MyFirstNode/MyFirstNode{.lang}.md")]
[Display(GroupName = "custom")]
public class MyFirstNode : Node
{
    [Required]
    public string CustomProperty { get; set; } = "Default Value";

    public MyFirstNode()
    {
        HaveInput = true;
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Color = "#ffcc00";
        Icon = "/_plugin/MyMarsPlugin/nodes/img/icon.png";
    }
}
```
## 2. Реализуйте логику выполнения

Создайте реализацию интерфейса `INodeImplement<MyFirstNode>`. Это который исполняет логику ноды. В основном проекте плагина.

```
using Mars.Nodes.Core.Nodes;

namespace MyMarsPlugin.Nodes.Implements;

public class MyFirstNodeImpl : INodeImplement<MyFirstNode>, INodeImplement
{
    private readonly ILogger<MyFirstNodeImpl> _logger;
    public MyFirstNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public MyFirstNodeImpl(MyFirstNode node, IRED red)
    {
        Node = node;
        RED = red;
        _logger = RED.ServiceProvider.GetRequiredService<ILogger<MyFirstNodeImpl>>();
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        // Ваша логика
        var message = $"CustomProperty: {Node.CustomProperty}"
        input.Payload = message;
        callback(input);

        // в задать статус, который отображается в админтке.
        RED.Status(new NodeStatus(Random.Shared.Next(1, 10).ToString()));
        // создает отладочное сообщение в админке.
        RED.DebugMsg(DebugMessage.NodeMessage(Node.Id, message));

        _logger.LogInformation("Это работает!");
        return Task.CompletedTask;
    }
}
```

## 3. Зарегистрируйте реализацию

В основном проекте плагина

```
//Startup.cs
NodesLocator.RegisterAssembly(typeof(MyFirstNode).Assembly);
NodeFormsLocator.RegisterAssembly(typeof(MyFirstNodeForm).Assembly);
NodeImplementFabirc.RegisterAssembly(typeof(MyFirstNodeImpl).Assembly);
```

и в проекте фронта
```
NodesLocator.RegisterAssembly(typeof(MyFirstNode).Assembly);
NodeFormsLocator.RegisterAssembly(typeof(MyFirstNodeForm).Assembly);
```

## 4. Создание формы редактирования

```
// MyFirstNodeForm.razor
@using AppFront.Shared.Components
@using Mars.Nodes.Core
@using Mars.Nodes.Core.Attributes
@using Mars.Nodes.FormEditor
@using Mars.Nodes.FormEditor.EditForms.Components
@using Microsoft.FluentUI.AspNetCore.Components
@using MyMarsPlugin.Nodes
@inherits NodeEditForm
@attribute [NodeEditFormForNode(typeof(MyFirstNode))]

<div class="form-group compact" style="--fluent-input-label-basis:150px">
    <div class="vstack gap-2">

        <FormItem2 For="() => Node.CustomProperty">
            <FluentTextField @bind-Value=@Node.CustomProperty Class="" />
        </FormItem2>

    </div>
</div>

@code {
    [CascadingParameter] Node? Value { get; set; }
    MyFirstNode Node { get => (MyFirstNode)Value!; set => Value = value; }

}
```

## 5. Используйте ноду в редакторе

После сборки проекта ваша нода появится в палитре редактора и будет доступна для использования в визуальных схемах. `http://localhost:5004/dev/nodered`

Для тестирования создайте связь `InjectNode -> MyFirstNode -> DebugNode`.

Для радактирования `.CustomProperty` дважды кликните по ноде

---

**Примечание:**
- Для документации по вашей ноде создайте файл в соответствующей папке docs.
