using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.WebApp;
using Microsoft.Extensions.DependencyInjection;
using Mars.Nodes.FormEditor.EditForms;
using Mars.Nodes.Workspace.Services;

namespace Mars.Nodes.Workspace;

public static class MainNodeWorkspace
{
    public static IServiceCollection AddNodeWorkspace(this IServiceCollection services)
    {
        NodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);
        NodesLocator.RegisterAssembly(typeof(CssCompilerNode).Assembly);
        NodesLocator.RefreshDict();

        NodeFormsLocator.RegisterAssembly(typeof(InjectNodeForm).Assembly);
        NodeFormsLocator.RefreshDict();

        services.AddScoped<INodeServiceClient, NodeServiceClient>();

        return services;
    }

}
