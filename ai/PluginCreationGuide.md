# Mars Plugin Creation Guide for Agent

## Plugin Structure (minimal)

```
XxxPlugin/
  src/
    XxxPlugin/              # Backend (SDK: Microsoft.NET.Sdk.Razor, net10.0)
    XxxPlugin.Front/        # Frontend (SDK: Microsoft.NET.Sdk.BlazorWebAssembly, net10.0)
    XxxPlugin.Shared/       # Shared DTOs/options (SDK: Microsoft.NET.Sdk, net10.0)
```

## Backend: Startup.cs

```csharp
[assembly: WebApplicationPlugin(typeof(MainXxxPlugin))]

namespace XxxPlugin;

public class MainXxxPlugin : WebApplicationPlugin
{
    public const string PluginPackageName = "author.XxxPlugin";

    public override void ConfigureWebApplicationBuilder(WebApplicationBuilder builder, PluginSettings settings)
    {
        builder.Services.AddSingleton<MyService>();
    }

    public override void ConfigureWebApplication(WebApplication app, PluginSettings settings)
    {
        // Register nodes from both assemblies
        app.Services.AutoHostRegisterHelper([GetType().Assembly, typeof(XxxPluginFront).Assembly]);
    }
}
```

## Custom Front Render Engine

A plugin can add its own front render engine by registering an `IWebRenderEngineFactory` in DI
(inside `ConfigureWebApplicationBuilder` — it runs before the container is built, so the factory
lands in the `IEnumerable<IWebRenderEngineFactory>` that `WebRenderEngineLocator` consumes).
Admins then pick the engine per front (`FrontsOption.EngineId`, editable at runtime — no restart).

```csharp
[Display(Name = "MyEngine", Description = "My custom front renderer")]
public class MyRenderEngineFactory : IWebRenderEngineFactory
{
    public string Id => "my-engine"; // value for FrontItem.EngineId

    public IWebRenderEngine Create(MarsAppFront appFront, IServiceProvider services)
    {
        var engine = ActivatorUtilities.CreateInstance<MyRenderEngine>(services, appFront);
        engine.Setup();
        // initialize template source (files from appFront.Configuration.Path), then:
        return engine;
    }
}

// in ConfigureWebApplicationBuilder:
builder.Services.AddSingleton<IWebRenderEngineFactory, MyRenderEngineFactory>();
```

`IWebRenderEngine` contract: `Setup()` (validation) and `RenderPage(RenderEngineRenderRequestContext, ...)`.
The pipeline resolves the front by URL, serves `<front>/wwwroot` statics itself, and calls the engine
only for page rendering. See `HandlebarsRenderEngineFactory`/`HandlebarsWebRenderEngine` as reference.

## Frontend: Startup.cs

```csharp
public class XxxPluginFront : IWebAssemblyPluginFront
{
    public void ConfigureServices(WebAssemblyHostBuilder builder) { }
    public void ConfigureApplication(WebAssemblyHost app)
    {
        app.Services.AutoFrontRegisterHelper([GetType().Assembly]);
    }
}
```

## Node Definition (in Front project)

```csharp
[Display(GroupName = "category")]
public class MyNode : Node
{
    public InputConfig<MyConfigNode> Config { get; set; }
    public string MyProperty { get; set; } = "";

    public MyNode()
    {
        Inputs = [new()];
        Outputs = [new()];
        Color = "#3fc9af";
        Icon = "/_plugin/XxxPlugin/icon.png";
    }
}

public class MyConfigNode : ConfigNode
{
    [Required]
    public string ApiKey { get; set; } = "";
}
```

## Node Implementation (in Backend project)

```csharp
public class MyNodeImpl : INodeImplement<MyNode>
{
    public MyNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public MyNodeImpl(MyNode node, IRuntimeNodeScope rns, MyService service)
    {
        Node = node;
        RNS = rns;
        Node.Config = RNS.GetConfig(node.Config); // ALWAYS resolve config
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        // Process input.Payload
        input.Payload = result;
        callback(input); // forward to next node
        // callback(input, 1); // for multi-output, specify index
        return Task.CompletedTask;
    }
}
```

## Node Edit Form (Razor, in Front project)

```razor
@inherits NodeEditForm
@attribute [NodeEditFormForNode(typeof(MyNode))]

<div class="form-group compact">
    <FormItem2 For="() => Node.Config">
        <InputConfigField @bind-Value=Node.Config TConfig="MyConfigNode" />
    </FormItem2>
    <FormItem2 For="() => Node.MyProperty">
        <FluentTextField @bind-Value=Node.MyProperty />
    </FormItem2>
</div>

@code {
    [CascadingParameter] Node? Value { get; set; }
    MyNode Node { get => (MyNode)Value!; set => Value = value; }
}
```

## Key NuGet Packages

**Backend:**
- `mdimai666.Mars.Plugin.Kit.Host`
- `mdimai666.Mars.Plugin.PluginPublishScript`

**Frontend:**
- `mdimai666.Mars.Plugin.Kit.Front`

## Key Patterns

1. **Config resolution:** Always call `Node.Config = RNS.GetConfig(node.Config)` in constructor
2. **Multi-output:** Use `callback(input, outputIndex)` to route to specific outputs
3. **Status/Debug:** Use `RNS.Status(new NodeStatus("text"))` and `RNS.DebugMsg(DebugMessage.NodeMessage(...))`
4. **Services:** Inject via constructor or `RNS.ServiceProvider.GetRequiredService<T>()`
5. **REST API:** Map endpoints in `ConfigureWebApplication` with `app.MapGet/MapPost/MapPut`

## Examples

- Simple plugin: https://github.com/mdimai666/YandexWeatherPlugin
- Custom nodes: https://github.com/mdimai666/Mars.TelegramPlugin
- Complex with host services: https://github.com/mdimai666/Mars.PlayAudioNodePlugin
- AI integration: https://github.com/mdimai666/Mars.SberDevApiPlugin
