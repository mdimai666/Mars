using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core;

public class NodesLocator
{
    Dictionary<string, NodeDictItem> _dict = [];
    bool invalid = true;
    HashSet<Assembly> assemblies = [];
    object _lock = new { };

    public IReadOnlyDictionary<string, NodeDictItem> Dict { get { if (invalid) RefreshDict(); return _dict; } }

    private void RefreshDict(bool force = false)
    {
        if (!invalid && !force) return;

        lock (_lock)
        {
            _dict.Clear();

            foreach (var assembly in assemblies.ToList())
            {

                var types = GetEnumerableOfType<Node>(assembly);

                foreach (Type type in types)
                {
                    var item = new NodeDictItem
                    {
                        NodeType = type,
                        DisplayAttribute = type.GetCustomAttribute<DisplayAttribute>() ?? new DisplayAttribute(),
                        FunctionApiDocument = type.GetCustomAttribute<FunctionApiDocumentAttribute>()
                    };
                    _dict.Add(type.FullName!, item);
                }
            }
            invalid = false;
        }
    }

    public IReadOnlyCollection<Type> GetEnumerableOfType<T>(Assembly assembly, params object[] constructorArgs) where T : class
    {
        List<Type> objects =
        [
            .. assembly.GetTypes()
            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T))),
        ];
        //objects.Sort();
        return objects;
    }

    public void RegisterAssembly(Assembly assembly)
    {
        if (assemblies.Contains(assembly)) return;
        assemblies.Add(assembly);
    }

    public Type? GetTypeByFullName(string typeFullname)
    {
        return Dict.GetValueOrDefault(typeFullname)?.NodeType;
        //throw new NullReferenceException($"node with type {typeFullname} not found in NodesLocator");
    }

    public IReadOnlyCollection<Type> RegisteredNodes()
    {
        return Dict.Select(s => s.Value.NodeType).ToList();
    }

    public IReadOnlyCollection<NodeDictItem> RegisteredNodesEx()
    {
        return Dict.Select(s => s.Value).ToList();
    }

    public static JsonSerializerOptions CreateJsonSerializerOptions(NodesLocator nodesLocator, bool writeIndented = false)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            
            Converters = { new Converters.NodeJsonConverter(nodesLocator) }
        };
        return jsonSerializerOptions;
    }
}

public record NodeDictItem
{
    public required Type NodeType;
    public required DisplayAttribute DisplayAttribute;
    public required FunctionApiDocumentAttribute? FunctionApiDocument;
}
