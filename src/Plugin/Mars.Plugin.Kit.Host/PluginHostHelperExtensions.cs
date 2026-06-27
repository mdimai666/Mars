using System.Reflection;
using Mars.Nodes.Core;
using Mars.Nodes.Host.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Plugin.Kit.Host;

public static class PluginHostHelperExtensions
{
    public static IServiceProvider AutoHostRegisterHelper(this IServiceProvider serviceProvider, Assembly[] assemblies)
    {
        var nodesLocator = serviceProvider.GetRequiredService<INodesLocator>();
        //var nodeFormsLocator = serviceProvider.GetRequiredService<INodeFormsLocator>();
        var nodeImplementFactory = serviceProvider.GetRequiredService<INodeImplementFactory>();
        //var optionsFormsLocator = serviceProvider.GetRequiredService<IOptionsFormsLocator>();

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
                nodeImplementFactory.RegisterAssembly(assembly);
            }

            //ValidatorFactory
        }

        return serviceProvider;
    }
}
