using System.Reflection;
using Mars.Nodes.Core.Attributes;
using Mars.Nodes.FormEditor;
using Mars.Nodes.Front.Shared.Components.NodeViews;

namespace Mars.Nodes.Workspace.Locators;

internal class NodeFormsLocator : INodeFormsLocator
{
    Dictionary<Type, NodeFormItem> _dict = [];
    IDictionary<Type, NodeFormItem> Dict { get { if (invalid) RefreshDict(); return _dict; } }
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
                var formTypes = GetNodeEditForms(assembly);
                var nodeComponentExtends = GetNodeComponentExtends(assembly);

                foreach (var nodeType in formTypes.Keys.Concat(nodeComponentExtends.Keys).ToHashSet())
                {
                    var x = new NodeFormItem
                    {
                        NodeType = nodeType,
                        FormType = formTypes.GetValueOrDefault(nodeType),
                        NodeComponent = null,
                        NodeComponentExtender = nodeComponentExtends.GetValueOrDefault(nodeType)
                    };
                    _dict.Add(nodeType, x);
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

    public Type? GetForNodeType(Type nodeType)
    {
        return Dict.GetValueOrDefault(nodeType).FormType;
    }

    public IEnumerable<Type> RegisteredForms()
    {
        return Dict.Values.Where(s => s.FormType is not null).Select(s => s.FormType)!;
    }

    public Type? GetNodeComponentExtender(Type nodeType)
    {
        return Dict.GetValueOrDefault(nodeType)?.NodeComponentExtender;
    }

    #region Extractors
    /// <summary>
    /// find NodeEditFormForNodeAttribute in assembly
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>Key NodeType; Valye FormType</returns>
    public Dictionary<Type, Type> GetNodeEditForms(Assembly assembly)
    {
        var type = typeof(NodeEditForm);

        var types = assembly.GetTypes()
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
            var attributes = formType.GetCustomAttributes<NodeEditFormForNodeAttribute>();
            foreach (var attribute in attributes)
            {

                if (attribute is not null)
                {
                    Type nodeType = attribute.ForNodeType;

                    dict.Add(nodeType, formType);
                }
            }
        }

        return dict;
    }

    public Dictionary<Type, Type> GetNodeComponentExtends(Assembly assembly)
    {
        var type = typeof(NodeComponentExtendBase);

        var types = assembly.GetTypes()
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
            var attributes = formType.GetCustomAttributes<NodeEditFormForNodeAttribute>();
            foreach (var attribute in attributes)
            {

                if (attribute is not null)
                {
                    Type nodeType = attribute.ForNodeType;

                    dict.Add(nodeType, formType);
                }
            }
        }

        return dict;
    }
    #endregion

}
