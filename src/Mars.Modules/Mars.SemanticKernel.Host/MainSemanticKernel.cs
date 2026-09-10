using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;
using Mars.Options.Abstractions.Services;
using Mars.SemanticKernel.Abstractions.Interfaces;
using Mars.SemanticKernel.Contracts.Nodes;
using Mars.SemanticKernel.Contracts.Options;
using Mars.SemanticKernel.Host.Nodes;
using Mars.SemanticKernel.Host.Services;
using Mars.Server.Abstractions.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SemanticKernel.Host;

public static class MainSemanticKernel
{
    public static IServiceCollection AddMarsSemanticKernel(this IServiceCollection services)
    {
        KernelFactory.RegisterKernelFactory(services);

        services.AddTransient<GeneralAiRequestHandler>();
        services.AddTransient<NodesAiRequestHandler>();
        services.AddSingleton<IMarsAIService, MarsAIService>();
        services.AddSingleton<IAIToolService, AIToolService>();

        return services;
    }

    public static IApplicationBuilder UseMarsSemanticKernel(this IApplicationBuilder app)
    {
        var op = app.ApplicationServices.GetRequiredService<IOptionService>();
        op.RegisterOption<AIToolOption>();

        app.ApplicationServices.GetRequiredService<INodesLocator>().RegisterAssembly(typeof(AIRequestNode).Assembly);
        app.ApplicationServices.GetRequiredService<INodeImplementFactory>().RegisterAssembly(typeof(AIRequestNodeImpl).Assembly);

        return app;
    }
}
