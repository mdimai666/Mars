using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Mars.Core.Attributes;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Functions;

namespace Mars.Nodes.Host.Shared;

public interface INodeImplementFactory
{
    IReadOnlyDictionary<Type, NodeImplementItem> Dict { get; }
    INodeImplement Create(INodeBasic node, IRuntimeNodeScope rns);
    void RegisterAssembly(Assembly assembly);
    void RegisterInlineFunctionNode(InlineFunctionNodeDefinition definition);
    InlineFunctionNode CreateInlineFunctionNode(InlineFunctionNodeDefinition def, string[] args);
    InlineFunctionNodeDefinition? GetInlineFunctionNodeDefinition(string typeId);
    InlineFunctionNodeDefinition[] InlineFunctionNodeList { get; }

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
