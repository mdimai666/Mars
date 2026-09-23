using Mars.CodeCompletion.Host.Abstractions;
using Mars.Cms.Abstractions.Services;
using Mars.Identity.Abstractions.Dto.Users;
using Mars.Nodes.Contracts.Nodes;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Functions;
using Mars.Nodes.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Services;

/// <summary>
/// Контекст FunctionNode для серверного IntelliSense: зеркалит ScriptOptions из
/// <see cref="FunctionNodeImpl"/>, чтобы подсказки видели то же, что рантайм скрипта.
/// </summary>
public class FunctionNodeContextProvider(IServiceCollection services) : ICodeContextProvider
{
    public string ContextId => NodeCompletionContexts.FunctionNode;

    public Type? HostObjectType => typeof(FunctionNodeImpl.ScriptExecuteContext);

    public IReadOnlyList<string> Imports { get; } =
    [
        "System",
        "System.Collections.Generic",
        "System.Linq",
        "System.Text",
        "System.Threading.Tasks",
        "System.Threading",
        "Mars.Nodes.Core",
        typeof(Node).Namespace!,
        "Microsoft.Extensions.DependencyInjection",
    ];

    public IReadOnlyCollection<MetadataReference> GetMetadataReferences()
    {
        var assemblies = new[]
        {
            typeof(DynamicNodeMsgWrapper).Assembly,
            typeof(UserDetail).Assembly,
            typeof(IPostService).Assembly,
            typeof(EntityFrameworkQueryableExtensions).Assembly,
            typeof(ServiceProviderServiceExtensions).Assembly,
            typeof(Node).Assembly,
        };

        return assemblies
            .Concat(services.Select(s => s.ServiceType.Assembly))
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Distinct()
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToList();
    }
}
