using System.Collections.Concurrent;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Helpers;

public static class NodeTypeCache
{
    private static readonly ConcurrentDictionary<Type, bool> _visualCache = new();
    private static readonly ConcurrentDictionary<Type, bool> _containerlessCache = new();
    private static readonly ConcurrentDictionary<Type, bool> _configNodeCache = new();

    public static bool IsVisualNode(Type nodeType)
    {
        return _visualCache.GetOrAdd(nodeType, type =>
            !Node.NonVisualNodes.Any(t => t == type || t.IsAssignableFrom(type)));
    }

    public static bool IsContainerlessNode(Type nodeType)
    {
        return _containerlessCache.GetOrAdd(nodeType, type =>
            Node.ContainerlessNodes.Any(t => t == type || t.IsAssignableFrom(type)));
    }

    public static bool IsConfigNode(Type nodeType)
    {
        return _configNodeCache.GetOrAdd(nodeType, type =>
            typeof(ConfigNode).IsAssignableFrom(type));
    }
}
