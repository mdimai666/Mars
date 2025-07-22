using AppFront.Main.OptionEditForms;
using Mars.Nodes.Core;
using Mars.SemanticKernel.Front.Nodes.Forms;
using Mars.SemanticKernel.Front.OptionForms;
using Mars.SemanticKernel.Shared.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SemanticKernel.Front;

public static class MainSemanticKernelFront
{
    public static void AddSemanticKernelFront(this IServiceCollection services)
    {
        NodesLocator.RegisterAssembly(typeof(AIRequestNode).Assembly);
        NodeFormsLocator.RegisterAssembly(typeof(AIRequestNodeForm).Assembly);
        OptionsFormsLocator.RegisterAssembly(typeof(AIToolOptionEditForm).Assembly);
    }

}
