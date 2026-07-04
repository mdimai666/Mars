using System.Reflection;

namespace Mars.Nodes.FormEditor;

public interface INodeFormsLocator
{
    public void RegisterAssembly(Assembly assembly);
    public Type? GetForNodeType(Type nodeType);

    public IEnumerable<Type> RegisteredForms();

    /// <summary>
    /// find NodeEditFormForNodeAttribute in assembly
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns>Key NodeType; Valye FormType</returns>
    public Dictionary<Type, Type> GetNodeEditForms(Assembly assembly);
    Type? GetNodeComponentExtender(Type nodeType);
}

public record NodeFormItem
{
    public required Type NodeType { get; init; } = default!;
    public required Type? FormType { get; init; } = default!;
    public required Type? NodeComponentExtender { get; init; } = default!;
    public required Type? NodeComponent { get; init; } = default!;
}
