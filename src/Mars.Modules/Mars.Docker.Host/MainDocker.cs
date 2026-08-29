using Docker.DotNet;
using Mars.Docker.Host.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Docker.Host;

public static class MainDocker
{
    public static IServiceCollection AddMarsDocker(this IServiceCollection services)
    {
        // https://github.com/dotnet/Docker.DotNet/
        DockerClient client = new DockerClientConfiguration().CreateClient();

        services.AddSingleton(client);
        services.AddSingleton<IDockerService, DockerService>();

        return services;
    }
}
