using AppFront.Main.OptionEditForms;
using Mars.Nodes.Core;
using Mars.Nodes.FormEditor;
using Mars.SemanticKernel.Front.Nodes.Forms;
using Mars.SemanticKernel.Front.OptionForms;
using Mars.SemanticKernel.Shared.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SemanticKernel.Front;

public static class MainSemanticKernelFront
{
    public static IServiceCollection AddSemanticKernelFront(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceProvider UseSemanticKernelFront(this IServiceProvider services)
    {
        services.GetRequiredService<INodesLocator>().RegisterAssembly(typeof(AIRequestNode).Assembly);
        services.GetRequiredService<INodeFormsLocator>().RegisterAssembly(typeof(AIRequestNodeForm).Assembly);
        services.GetRequiredService<IOptionsFormsLocator>().RegisterAssembly(typeof(AIToolOptionEditForm).Assembly);

        return services;
    }

}
