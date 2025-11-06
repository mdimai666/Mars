using System.Reflection;
using Mars.Nodes.Core.Attributes;
using Microsoft.AspNetCore.Components;

namespace Mars.Nodes.Core;

public class NodeFormsLocator
{
    Dictionary<Type, Type> _dict = [];
    IDictionary<Type, Type> Dict { get { if (invalid) RefreshDict(); return _dict; } }
    bool invalid = true;
    HashSet<Assembly> assemblies = [];

    object _lock = new { };

    private void RefreshDict(bool force = false)
    {
        if (!invalid && !force) return;

        lock (_lock)
        {
            _dict.Clear();

            foreach (var assembly in assemblies)
            {
                var types = GetNodeEditForms(assembly);

                foreach (var a in types)
                {
                    _dict.Add(a.Key, a.Value);
                }
            }
            invalid = false;
        }
    }

    public void RegisterAssembly(Assembly assembly)
    {
        if (assemblies.Contains(assembly)) return;
        assemblies.Add(assembly);
        invalid = true;
    }

    public Type GetForNodeType(Type nodeType)
    {
        if (Dict.ContainsKey(nodeType))
        {
            return Dict[nodeType];
        }
        throw new NullReferenceException($"node with type {nodeType.FullName} not found in NodeFormsLocator");
    }

    public Type? TryGetForNodeType(Type nodeType)
    {
        if (Dict.ContainsKey(nodeType))
        {
            return Dict[nodeType];
        }
        return null;
    }

    public List<Type> RegisteredForms()
    {
        return Dict.Select(s => s.Value).ToList();
    }

    /// <summary>
    /// find NodeEditFormForNodeAttribute in assembly
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>Key NodeType; Valye FormType</returns>
    public Dictionary<Type, Type> GetNodeEditForms(Assembly assembly)
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

        Dictionary<Type, Type> dict = [];

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
