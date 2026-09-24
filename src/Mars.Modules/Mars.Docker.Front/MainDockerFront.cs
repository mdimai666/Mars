using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Front.Nodes.Forms;
using Mars.Nodes.Core;
using Mars.Nodes.FormEditor;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Front;

public static class MainDockerFront
{
    public static IServiceCollection AddDockerFront(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceProvider UseDockerFront(this IServiceProvider services)
    {
        services.GetRequiredService<INodesLocator>().RegisterAssembly(typeof(DockerPullNode).Assembly);
        services.GetRequiredService<INodeFormsLocator>().RegisterAssembly(typeof(DockerPullNodeForm).Assembly);

        return services;
    }
}
