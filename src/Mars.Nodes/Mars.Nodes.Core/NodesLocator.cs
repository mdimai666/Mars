using System.Reflection;

namespace Mars.Nodes.Core;

public static class NodesLocator
{
    public static Dictionary<string, Type> dict = new();

    static bool invalide = true;

    public static List<Assembly> assemblies = new();

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
                    dict.Add(type.FullName!, type);
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
        assemblies.Add(assembly);
    }

    public static Type? GetTypeByFullName(string typeFullname)
    {
        return dict.GetValueOrDefault(typeFullname);
        //throw new NullReferenceException($"node with type {typeFullname} not found in NodesLocator");
    }
    public static List<Type> RegisteredNodes()
    {
        return dict.Select(s => s.Value).ToList();
    }
}
