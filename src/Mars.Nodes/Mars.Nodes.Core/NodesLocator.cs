using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Mars.Nodes.Core;

public static class NodesLocator
{
    public static Dictionary<string, NodeDictItem> dict = new();

    static bool invalide = true;

    public static HashSet<Assembly> assemblies = new();

    static object _lock = new { };

    public static void RefreshDict(bool force = false)
    {
        if (!invalide && !force) return;

        lock (_lock)
        {
            dict.Clear();

            foreach (var assembly in assemblies)
            {

                var types = GetEnumerableOfType<Node>(assembly);

                foreach (Type type in types)
                {
                    var item = new NodeDictItem
                    {
                        NodeType = type,
                        DisplayAttribute = type.GetCustomAttribute<DisplayAttribute>() ?? new DisplayAttribute()
                    };
                    dict.Add(type.FullName!, item);
                }
            }
        }
    }

    public static IEnumerable<Type> GetEnumerableOfType<T>(Assembly assembly, params object[] constructorArgs) where T : class
    {
        List<Type> objects = new List<Type>();
        foreach (Type type in
            assembly.GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))))
        {
            //objects.Add((T)Activator.CreateInstance(type, constructorArgs));
            //return typeof(T);
            objects.Add(type);
        }
        //objects.Sort();
        return objects;
    }

    public static void RegisterAssembly(Assembly assembly)
    {
        if (assemblies.Contains(assembly)) return;
        assemblies.Add(assembly);
    }

    public static Type? GetTypeByFullName(string typeFullname)
    {
        return dict.GetValueOrDefault(typeFullname)?.NodeType;
        //throw new NullReferenceException($"node with type {typeFullname} not found in NodesLocator");
    }

    public static List<Type> RegisteredNodes()
    {
        return dict.Select(s => s.Value.NodeType).ToList();
    }

    public static List<NodeDictItem> RegisteredNodesEx()
    {
        return dict.Select(s => s.Value).ToList();
    }

}

public record NodeDictItem
{
    public required Type NodeType;
    public required DisplayAttribute DisplayAttribute;
}
