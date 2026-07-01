using Mars.Nodes.Core;
using Mars.Nodes.FormEditor;
using Mars.WebApp.Nodes.Front.Forms;
using Mars.WebApp.Nodes.Front.Services;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Front;

public static class MainMarsWebAppNodesFront
{
    public static IServiceCollection AddMarsWebAppNodesFront(this IServiceCollection services)
        => services.AddScoped<INodeEditorToolServiceClient, NodeEditorToolServiceClient>();

    public static IServiceProvider UseMarsWebAppNodesFront(this IServiceProvider services)
    {
        var nodesLocator = services.GetRequiredService<INodesLocator>();
        nodesLocator.RegisterAssembly(typeof(ExcelNode).Assembly);

        var nodeFormsLocator = services.GetRequiredService<INodeFormsLocator>();
        nodeFormsLocator.RegisterAssembly(typeof(ExcelNodeForm).Assembly);

        return services;
    }
}
