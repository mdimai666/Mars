using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Core.Attributes;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Core.Implements;

public class NodeImplementFactory
{
    Dictionary<Type, NodeImplementItem> _dict = [];
    public IReadOnlyDictionary<Type, NodeImplementItem> Dict { get { if (invalid) RefreshDict(); return _dict; } }

    private bool invalid;
    private HashSet<Assembly> assemblies = [];

    private readonly object _lock = new { };
    private readonly ConcurrentDictionary<Type, ObjectFactory> _factoryCache = new();

    private void RefreshDict(bool force = false)
    {
        if (!invalid && !force) return;
        lock (_lock)
        {
            _dict.Clear();

            foreach (var assembly in assemblies)
            {

                var types = GetAllTypesImplementingOfInterface(typeof(INodeImplement<>), assembly).ToList();

                int count = types.Count();

                foreach (var type in types)
                {
                    _dict.Add(type.Key, type.Value);
                }
            }

            invalid = false;
        }
    }

    private Dictionary<Type, NodeImplementItem> GetAllTypesImplementingOfInterface(Type interfaceType, Assembly assembly)
    {
        if (!interfaceType.IsInterface)
            throw new ArgumentException($"{interfaceType} is not an interface");

        return assembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.GetInterfaces()
                    .Any(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == interfaceType.GetGenericTypeDefinition()
                    )
            )
            .Select(t =>
            {
                var implInterface = t.GetInterfaces()
                    .First(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == interfaceType.GetGenericTypeDefinition());

                var genericArg = implInterface.GetGenericArguments().Single();

                return new NodeImplementItem
                {
                    NodeBaseType = genericArg,
                    NodeImplementType = t
                };
            })
            .GroupBy(x => x.NodeBaseType)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public INodeImplement Create(INodeBasic node, IRED _RED)
    {
        Type instantiateType;

        if (node is ConfigNode) instantiateType = typeof(ConfigNodeImpl);
        else instantiateType = Dict[node.GetType()].NodeImplementType;

        var factory = _factoryCache.GetOrAdd(instantiateType, type =>
                        ActivatorUtilities.CreateFactory(type, [node.GetType(), typeof(IRED)]));

        var instance = (INodeImplement)factory(_RED.ServiceProvider, [node, _RED]);

        return instance;
    }

    public void RegisterAssembly(Assembly assembly)
    {
        if (assemblies.Contains(assembly)) return;
        assemblies.Add(assembly);
        invalid = true;
    }

    //========== INLINE FUNCTIONS
    ConcurrentDictionary<string, InlineNodeDictItem> _inlineNodesDict = [];

    //public ConcurrentDictionary<string, InlineNodeDictItem> InlineNodesDict => _inlineNodesDict;

    public void RegisterInlineFunctionNode(InlineFunctionNodeDefinition definition)
    {
        _inlineNodesDict.TryAdd(definition.TypeId, new()
        {
            NodeDefinition = definition,
            DisplayAttribute = definition.Delegate.GetType().GetCustomAttribute<DisplayAttribute>() ?? new(),
            FunctionApiDocument = definition.Delegate.GetType().GetCustomAttribute<FunctionApiDocumentAttribute>() ?? new("")
        });
    }

    public InlineFunctionNode CreateInlineFunctionNode(InlineFunctionNodeDefinition def, string[] args)
    {
        return new()
        {
            Name = def.Name,
            Inputs = def.Inputs.ToList(),
            Outputs = def.Outputs.ToList(),
            Color = def.Color ?? InlineFunctionNode.DefaultColor,
            Icon = def.Icon ?? InlineFunctionNode.DefaultIcon,

            FunctionId = def.TypeId,
            Arguments = args
        };
    }

    public InlineFunctionNodeDefinition? GetInlineFunctionNodeDefinition(string typeId)
        => _inlineNodesDict.GetValueOrDefault(typeId)?.NodeDefinition;

    public InlineFunctionNodeDefinition[] InlineFunctionNodeList => _inlineNodesDict.Values.Select(x => x.NodeDefinition).ToArray();
}

public record NodeImplementItem
{
    public required Type NodeBaseType;
    public required Type NodeImplementType;
}

public record InlineNodeDictItem
{
    public required InlineFunctionNodeDefinition NodeDefinition;
    public required DisplayAttribute DisplayAttribute;
    public required FunctionApiDocumentAttribute? FunctionApiDocument;
}
