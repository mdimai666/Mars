using System.Reflection;
using AppFront.Main.OptionEditForms;
using Mars.Nodes.Core;
using Mars.Nodes.FormEditor;
using Mars.Shared.Options.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Plugin.Front;

public static class PluginFrontHelperExtensions
{
    public static IServiceProvider AutoFrontRegisterHelper(this IServiceProvider serviceProvider, Assembly[] assemblies)
    {
        var nodesLocator = serviceProvider.GetRequiredService<NodesLocator>();
        var nodeFormsLocator = serviceProvider.GetRequiredService<NodeFormsLocator>();
        var optionsFormsLocator = serviceProvider.GetRequiredService<OptionsFormsLocator>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract);

            if (types.Any(t => typeof(Node).IsAssignableFrom(t)))
            {
                nodesLocator.RegisterAssembly(assembly);
            }

            if (types.Any(t => typeof(NodeEditForm).IsAssignableFrom(t)))
            {
                nodeFormsLocator.RegisterAssembly(assembly);
            }

            if (types.Any(t => typeof(ComponentBase).IsAssignableFrom(t) && t.GetCustomAttribute<OptionEditFormForOptionAttribute>() != null))
            {
                optionsFormsLocator.RegisterAssembly(assembly);
            }
        }

        return serviceProvider;
    }
}
