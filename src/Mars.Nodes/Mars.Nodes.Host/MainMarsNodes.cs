using System.Text;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Managers.Mqtt;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Host.CommandLine;
using Mars.Nodes.Host.Middlewares;
using Mars.Nodes.Host.NodeTasks;
using Mars.Nodes.Host.Scheduler;
using Mars.Nodes.Host.Services;
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
        services.AddSingleton<NodeImplementFactory>();

        services.AddSingleton<INodeService, NodeService>();
        services.AddSingleton<INodeTaskManager, NodeTaskManager>();
        services.AddSingleton<INodeSchedulerService, NodeSchedulerService>();
        services.AddSingleton<RED>();
        services.AddSingleton<INodesReader, NodesReader>();
        services.AddSingleton<MqttManager>();
        services.AddScoped<FunctionCodeSuggestService>();
        services.AddSingleton<CommandNodesActionProvider>();
        services.AddSingleton<NodeEditorToolServce>();
        //services.AddHostedService<FlowExecutionBackgroundService>();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        ICommandLineApi.Register<NodesCli>();

        services.AddOptions<Microsoft.AspNetCore.Mvc.JsonOptions>()
                .Configure<NodesLocator>((options, locator) =>
                {
                    options.JsonSerializerOptions.Converters.Add(new NodeJsonConverter(locator));
                    options.JsonSerializerOptions.Converters.Add(new InputValueJsonConverterFactory());
                });

        services.AddNodeWorkspace();

        return services;
    }

    public static IApplicationBuilder UseMarsNodes(this WebApplication app)
    {
        app.Services.UseNodeWorkspace();

        var nodeImplementFactory = app.Services.GetRequiredService<NodeImplementFactory>();
        nodeImplementFactory.RegisterAssembly(typeof(InjectNodeImpl).Assembly);

        var templatorFeaturesLocator = app.Services.GetRequiredService<ITemplatorFeaturesLocator>();
        templatorFeaturesLocator.Functions.Add(nameof(RegisterNodeTemplatorFunction.Node), RegisterNodeTemplatorFunction.Node!);

        var actionManager = app.Services.GetRequiredService<IActionManager>();
        var commandNodesActionProvider = app.Services.GetRequiredService<CommandNodesActionProvider>();
        actionManager.AddActionsProvider(commandNodesActionProvider);

        app.UseMiddleware<MarsNodesMiddleware>();

        return app;
    }

}
