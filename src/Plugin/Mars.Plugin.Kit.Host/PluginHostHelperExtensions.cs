using System.Reflection;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Plugin.Kit.Host;

public static class PluginHostHelperExtensions
{
    public static IServiceProvider AutoHostRegisterHelper(this IServiceProvider serviceProvider, Assembly[] assemblies)
    {
        var nodesLocator = serviceProvider.GetRequiredService<NodesLocator>();
        //var nodeFormsLocator = serviceProvider.GetRequiredService<NodeFormsLocator>();
        var nodeImplementFabirc = serviceProvider.GetRequiredService<NodeImplementFabirc>();
        //var optionsFormsLocator = serviceProvider.GetRequiredService<OptionsFormsLocator>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract);

            if (types.Any(t => typeof(Node).IsAssignableFrom(t)))
            {
                nodesLocator.RegisterAssembly(assembly);
            }

            if (types.Any(type => type.GetInterfaces()
                                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INodeImplement<>))))
            {
                nodeImplementFabirc.RegisterAssembly(assembly);
            }

            //ValidatorFabric
        }

        return serviceProvider;
    }
}
