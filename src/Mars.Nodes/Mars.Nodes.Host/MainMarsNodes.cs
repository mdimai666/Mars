using System.Text;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.HttpSmartAuthFlow;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Implements.Managers.Mqtt;
using Mars.Nodes.Core.Implements.Nodes.Common;
using Mars.Nodes.Core.Implements.Nodes.InlineFunctions;
using Mars.Nodes.Host.CommandLine;
using Mars.Nodes.Host.Factories;
using Mars.Nodes.Host.Middlewares;
using Mars.Nodes.Host.NodeTasks;
using Mars.Nodes.Host.Scheduler;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.Services;
using Mars.Nodes.Host.Templator;
using Mars.Nodes.Workspace;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host;

public static class MainMarsNodes
{
    public static IServiceCollection AddMarsNodes(this IServiceCollection services)
    {
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

        services.AddNodeWorkspace();

        //Dependies
        var authClientManager = new AuthClientManager();
        services.AddSingleton(authClientManager);

        return services;
    }

    public static IApplicationBuilder UseMarsNodes(this WebApplication app)
    {
        app.Services.UseNodeWorkspace();

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
