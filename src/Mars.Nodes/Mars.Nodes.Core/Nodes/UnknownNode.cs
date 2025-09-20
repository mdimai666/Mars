using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

/// <summary>
/// Special type for not found types
/// </summary>
[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/UnknownNode/UnknownNode{.lang}.md")]
public class UnknownNode : Node
{
    public string JsonBody { get; set; } = "";
    public string UnrecognizedType { get; set; } = "";
    public bool IsDefinedAsConfig { get; set; }

    public UnknownNode()
    {

    }

    public UnknownNode(NodeBasicImplement basic, string jsonBody)
    {
        JsonBody = jsonBody;
        Id = basic.Id;
        Container = basic.Container;
        UnrecognizedType = basic.Type;
        Name = basic.Name;
        Wires = basic.Wires;
        Disabled = basic.Disabled;
        Inputs = basic.Inputs;
        Outputs = basic.Outputs;

        IsDefinedAsConfig = basic.IsConfigNode;

        X = basic.X;
        Y = basic.Y;
        Z = basic.Z;
    }
}
