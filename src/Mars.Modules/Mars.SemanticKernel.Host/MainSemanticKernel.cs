using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.SemanticKernel.Host.Nodes;
using Mars.SemanticKernel.Host.Service;
using Mars.SemanticKernel.Host.Shared.Interfaces;
using Mars.SemanticKernel.Shared.Nodes;
using Mars.SemanticKernel.Shared.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SemanticKernel.Host;

public static class MainSemanticKernel
{
    public static IServiceCollection AddMarsSemanticKernel(this IServiceCollection services)
    {
        NodesLocator.RegisterAssembly(typeof(AIRequestNode).Assembly);
        NodeImplementFabirc.RegisterAssembly(typeof(AIRequestNodeImpl).Assembly);

        services.AddSingleton<IMarsAIService, MarsAIService>();

        return services;
    }

    public static IApplicationBuilder UseMarsSemanticKernel(this IApplicationBuilder app)
    {
        var op = app.ApplicationServices.GetRequiredService<IOptionService>();

        op.RegisterOption<AIToolOption>();

        return app;
    }
}
