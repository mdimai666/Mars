using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core;

public interface INodesLocator
{
    IReadOnlyDictionary<string, NodeDictItem> Dict { get; }
    IReadOnlyCollection<Assembly> Assemblies { get; }
    void RegisterAssembly(Assembly assembly);
    Type? GetTypeByTypeId(string nodeTypeId);

    IReadOnlyCollection<Type> RegisteredNodes();
    JsonSerializerOptions CreateJsonSerializerOptions(bool writeIndented = false);
    IReadOnlyCollection<NodeExampleInfo> CreateExamplesList();

}

public record NodeDictItem
{
    public required Type NodeType;
    public required DisplayAttribute DisplayAttribute;
    public required FunctionApiDocumentAttribute? FunctionApiDocument;
    public required Node DefaultInstance;
}

public record NodeExampleInfo
{
    public required string Name;
    public required string Description;
    public required Type NodeType;
    public required INodeExample<Node> ExampleHandlerInstance;
}
