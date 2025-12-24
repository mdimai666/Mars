using Mars.Nodes.Core;
using Mars.Nodes.FormEditor;
using Mars.WebApp.Nodes.Front.Forms;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Front;

public static class MainMarsWebAppNodesFront
{
    public static IServiceCollection AddMarsWebAppNodesFront(this IServiceCollection services)
        => services;

    public static IServiceProvider UseMarsWebAppNodesFront(this IServiceProvider services)
    {
        var nodesLocator = services.GetRequiredService<NodesLocator>();
        nodesLocator.RegisterAssembly(typeof(ExcelNode).Assembly);

        var nodeFormsLocator = services.GetRequiredService<NodeFormsLocator>();
        nodeFormsLocator.RegisterAssembly(typeof(ExcelNodeForm).Assembly);

        return services;
    }
}
