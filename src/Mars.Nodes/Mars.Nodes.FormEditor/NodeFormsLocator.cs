using System.Reflection;
using Mars.Nodes.Core.Attributes;
using Microsoft.AspNetCore.Components;

namespace Mars.Nodes.Core;

public static class NodeFormsLocator
{
    static Dictionary<Type, Type> dict = new();

    static bool invalide = true;

    static HashSet<Assembly> assemblies = new();

    static object _lock = new { };

    public static void RefreshDict(bool force = false)
    {
        if (!invalide && !force) return;

        lock (_lock)
        {
            dict.Clear();

            foreach (var assembly in assemblies)
            {
                var _dict = GetNodeEditForms(assembly);

                foreach (var a in _dict)
                {
                    dict.Add(a.Key, a.Value);
                }
            }
        }
    }

    public static void RegisterAssembly(Assembly assembly)
    {
        if (assemblies.Contains(assembly)) return;
        assemblies.Add(assembly);
    }

    //public static Type GetTypeByFullName(string typeFullname)
    //{
    //    if (dict.ContainsKey(typeFullname))
    //    {
    //        return dict[typeFullname];
    //    }
    //    throw new NullReferenceException($"node with type {typeFullname} not found in NodesLocator");
    //}

    public static Type GetForNodeType(Type nodeType)
    {
        if (dict.ContainsKey(nodeType))
        {
            return dict[nodeType];
        }
        throw new NullReferenceException($"node with type {nodeType.FullName} not found in NodeFormsLocator");
    }

    public static Type? TryGetForNodeType(Type nodeType)
    {
        if (dict.ContainsKey(nodeType))
        {
            return dict[nodeType];
        }
        return null;
    }

    public static List<Type> RegisteredForms()
    {
        return dict.Select(s => s.Value).ToList();
    }

    /// <summary>
    /// find NodeEditFormForNodeAttribute in assembly
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>Key NodeType; Valye FormType</returns>
    public static Dictionary<Type, Type> GetNodeEditForms(Assembly assembly)
    {
        var type = typeof(ComponentBase);

        var types =
            //AppDomain.CurrentDomain.GetAssemblies()
            //.SelectMany(s => s.GetTypes())
            //Assembly.GetAssembly(typeof(Program)).GetTypes()
            //Assembly.GetAssembly(program).GetTypes()
            assembly.GetTypes()
            .Where(p =>
                type.IsAssignableFrom(p)
                && p.IsPublic
                && p.IsClass
                && !p.IsAbstract
            //&& p.Assembly ==
            );

        Dictionary<Type, Type> dict = new();

        foreach (var formType in types)
        {
            NodeEditFormForNodeAttribute? attribute = formType.GetCustomAttribute<NodeEditFormForNodeAttribute>();

            if (attribute is not null)
            {
                Type nodeType = attribute.ForNodeType;

                dict.Add(nodeType, formType);
            }

        }

        return dict;
    }
}
