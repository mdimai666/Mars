using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.EditorApi.Interfaces;
using Mars.Nodes.FormEditor.EditForms;
using Mars.Nodes.WebApp;
using Mars.Nodes.Workspace.Services;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<INodeEditorApi>(sp=> NodeEditor1.Instance!); // хак, для EditorActionManager - ActivatorUtilities.GetServiceOrCreateInstance

        return services;
    }

}
