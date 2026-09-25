using Docker.DotNet;
using Mars.Docker.Contracts.Nodes;
using Mars.Docker.Host.Nodes;
using Mars.Docker.Host.Options;
using Mars.Docker.Host.Services;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Mars.Docker.Host;

public static class MainDocker
{
    public static IServiceCollection AddMarsDocker(this IServiceCollection services, IConfiguration configuration)
    {
        // https://github.com/dotnet/Docker.DotNet/
        services.Configure<DockerOptions>(configuration.GetSection(DockerOptions.SectionName));
        services.AddMemoryCache();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DockerOptions>>().Value;
            var clientConfiguration = string.IsNullOrWhiteSpace(options.Endpoint)
                ? new DockerClientConfiguration()
                : new DockerClientConfiguration(new Uri(options.Endpoint));
            return clientConfiguration.CreateClient();
        });
        services.AddSingleton<IDockerService, DockerService>();

        return services;
    }

    public static IApplicationBuilder UseMarsDocker(this IApplicationBuilder app)
    {
        app.ApplicationServices.GetRequiredService<INodesLocator>().RegisterAssembly(typeof(DockerPullNode).Assembly);
        app.ApplicationServices.GetRequiredService<INodeImplementFactory>().RegisterAssembly(typeof(DockerPullNodeImpl).Assembly);

        return app;
    }
}
