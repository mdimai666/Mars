using System.Text.Json;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.FormEditor;
using Mars.Nodes.FormEditor.EditForms;
using Mars.Nodes.Front.Shared.Services;
using Mars.Nodes.Workspace.ActionManager;
using Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;
using Mars.Nodes.Workspace.EditorParts;
using Mars.Nodes.Workspace.Locators;
using Mars.Nodes.Workspace.Services;
using Microsoft.Extensions.DependencyInjection;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Toolbelt.Blazor.HotKeys2;

namespace Mars.Nodes.Workspace;

public static class MainNodeWorkspace
{
    public static IServiceCollection AddNodeWorkspace(this IServiceCollection services)
    {
        var nodesLocator = new NodesLocator();
        services.AddSingleton<INodesLocator>(nodesLocator);
        var jsonSerializerOptions = nodesLocator.CreateJsonSerializerOptions();
        services.AddKeyedSingleton<JsonSerializerOptions>(typeof(NodeJsonConverter), jsonSerializerOptions);

        services.AddSingleton<INodeFormsLocator, NodeFormsLocator>();
        services.AddSingleton<EditorActionLocator>();

        services.AddScoped<INodeServiceClient, NodeServiceClient>();

        if (!OperatingSystem.IsBrowser()) return services;

        if (!services.Any(d => d.ServiceType == typeof(HotKeys))) services.AddHotKeys2();

        services.AddScoped<NodeWorkspaceJsInterop>();

        return services;
    }

    public static IServiceProvider UseNodeWorkspace(this IServiceProvider services)
    {
        var nodesLocator = services.GetRequiredService<INodesLocator>();

        nodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);

        var nodeFormsLocator = services.GetRequiredService<INodeFormsLocator>();
        nodeFormsLocator.RegisterAssembly(typeof(InjectNodeForm).Assembly);

        if (!OperatingSystem.IsBrowser()) return services;

        var editorActionLocator = services.GetRequiredService<EditorActionLocator>();
        editorActionLocator.RegisterAssembly(typeof(DeleteSelectedNodesAndWiresAction).Assembly);

        return services;
    }

}
