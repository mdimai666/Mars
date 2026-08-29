using System.Reflection;
using System.Text;
using Mars.CommandLine.Abstractions;
using Mars.HttpSmartAuthFlow;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Abstractions.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Implements.Managers.Mqtt;
using Mars.Nodes.Core.Implements.Nodes.Common;
using Mars.Nodes.Core.Implements.Nodes.InlineFunctions;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Host.CommandLine;
using Mars.Nodes.Host.Factories;
using Mars.Nodes.Host.Middlewares;
using Mars.Nodes.Host.NodeTasks;
using Mars.Nodes.Host.Scheduler;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Host.Templator;
using Mars.Server.Abstractions.Managers;
using Mars.SiteEngine.Abstractions.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host;

public static class MainMarsNodes
{
    public static IServiceCollection AddMarsNodes(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

        services.AddMemoryCache();

        services.AddSingleton<INodeImplementFactory, NodeImplementFactory>();

        services.AddSingleton<INodeService, NodeService>();
        services.AddSingleton<INodeTaskManager, NodeTaskManager>();
        services.AddSingleton<INodeSchedulerService, NodeSchedulerService>();
        services.AddSingleton<INodeRuntime, NodeRuntime>();
        services.AddSingleton<INodesReader, NodesReader>();
        services.AddSingleton<MqttManager>();
        services.AddScoped<FunctionCodeSuggestService>();
        services.AddSingleton<CommandNodesActionProvider>();
        //services.AddHostedService<FlowExecutionBackgroundService>();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        services.AddOptions<Microsoft.AspNetCore.Mvc.JsonOptions>()
                .Configure<INodesLocator>((options, locator) =>
                {
                    options.JsonSerializerOptions.Converters.Add(new NodeJsonConverter(locator));
                });

        // Серверная часть воркспейса: локатор типов нод. Фронтовые регистрации
        // (формы нод, EditorActionLocator, js-интероп) добавляет AddNodeWorkspace
        // из браузерного процесса.
        services.AddNodesLocator();

        //Dependies
        var authClientManager = new AuthClientManager();
        services.AddSingleton(authClientManager);

        return services;
    }

    public static IApplicationBuilder UseMarsNodes(this WebApplication app)
    {
        app.Services.GetRequiredService<INodesLocator>().RegisterAssembly(typeof(InjectNode).Assembly);

        var nodeImplementFactory = app.Services.GetRequiredService<INodeImplementFactory>();
        nodeImplementFactory.RegisterAssembly(typeof(InjectNodeImpl).Assembly);

        foreach (var def in InlineFunctionsUtilsMethodParser.ParseMethods(typeof(InlineFunctionsUtils)))
            nodeImplementFactory.RegisterInlineFunctionNode(def);

        app.Services.GetService<ITemplatorFeaturesLocator>()
            ?.Functions.Add(nameof(RegisterNodeTemplatorFunction.Node), RegisterNodeTemplatorFunction.Node!);

        var actionManager = app.Services.GetRequiredService<IActionManager>();
        var commandNodesActionProvider = app.Services.GetRequiredService<CommandNodesActionProvider>();
        actionManager.AddActionsProvider(commandNodesActionProvider);

        app.Services.GetService<ICommandLineApi>()?.Register<NodesCli>();

        app.UseMiddleware<MarsNodesMiddleware>();

        return app;
    }

}
