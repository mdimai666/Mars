using System.Text;
using Mars.Host.Shared.CommandLine;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Middlewares;
using Mars.Nodes.Core.Implements.Managers.Mqtt;
using Mars.Nodes.Host.CommandLine;
using Mars.Nodes.Host.NodeTasks;
using Mars.Nodes.Host.Scheduler;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Host.Templator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host;

public static class MainMarsNodes
{
    public static IServiceCollection AddMarsNodes(this IServiceCollection services)
    {
        services.AddSingleton<INodeService, NodeService>();
        services.AddSingleton<INodeTaskManager, NodeTaskManager>();
        services.AddSingleton<INodeSchedulerService, NodeSchedulerService>();
        services.AddSingleton<RED>();
        services.AddSingleton<INodesReader, NodesReader>();
        services.AddSingleton<MqttManager>();
        services.AddScoped<FunctionCodeSuggestService>();
        services.AddSingleton<CommandNodesActionProvider>();
        //services.AddHostedService<FlowExecutionBackgroundService>();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        ICommandLineApi.Register<NodesCli>();

        return services;
    }

    public static IApplicationBuilder UseMarsNodes(this WebApplication app)
    {

        var locator = app.Services.GetRequiredService<ITemplatorFeaturesLocator>();
        locator.Functions.Add(nameof(RegisterNodeTemplatorFunction.Node), RegisterNodeTemplatorFunction.Node!);

        var actionManager = app.Services.GetRequiredService<IActionManager>();
        var commandNodesActionProvider = app.Services.GetRequiredService<CommandNodesActionProvider>();
        actionManager.AddActionsProvider(commandNodesActionProvider);

        app.UseMiddleware<MarsNodesMiddleware>();

        return app;
    }

}
