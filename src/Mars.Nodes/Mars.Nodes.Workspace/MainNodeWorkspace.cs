using System.Text.Json;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.FormEditor.EditForms;
using Mars.Nodes.Front.Shared.Services;
using Mars.Nodes.Workspace.ActionManager;
using Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;
using Mars.Nodes.Workspace.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Workspace;

public static class MainNodeWorkspace
{
    public static IServiceCollection AddNodeWorkspace(this IServiceCollection services)
    {
        var nodesLocator = new NodesLocator();
        services.AddSingleton<NodesLocator>(nodesLocator);

        var jsonSerializerOptions = NodesLocator.CreateJsonSerializerOptions(nodesLocator);
        services.AddKeyedSingleton<JsonSerializerOptions>(typeof(NodeJsonConverter), jsonSerializerOptions);

        services.AddSingleton<NodeFormsLocator>();
        services.AddSingleton<EditorActionLocator>();

        services.AddScoped<INodeServiceClient, NodeServiceClient>();
        services.AddScoped<INodeEditorToolServiceClient, NodeEditorToolServiceClient>();

        return services;
    }

    public static IServiceProvider UseNodeWorkspace(this IServiceProvider services)
    {
        var nodesLocator = services.GetRequiredService<NodesLocator>();

        nodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);

        var nodeFormsLocator = services.GetRequiredService<NodeFormsLocator>();
        nodeFormsLocator.RegisterAssembly(typeof(InjectNodeForm).Assembly);

        var editorActionLocator = services.GetRequiredService<EditorActionLocator>();
        editorActionLocator.RegisterAssembly(typeof(DeleteSelectedNodesAndWiresAction).Assembly);

        return services;
    }

}
