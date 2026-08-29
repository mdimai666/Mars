using System.Text.Json;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Workspace.Locators;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core;

public static class NodesLocatorServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует локатор типов нод и связанные с ним настройки сериализации.
    /// Используется и серверным хостом, и WASM-приложениями.
    /// </summary>
    public static IServiceCollection AddNodesLocator(this IServiceCollection services)
    {
        var nodesLocator = new NodesLocator();
        services.AddSingleton<INodesLocator>(nodesLocator);
        services.AddKeyedSingleton<JsonSerializerOptions>(typeof(NodeJsonConverter), nodesLocator.CreateJsonSerializerOptions());

        return services;
    }
}
