using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes.Common;

/// <summary>
/// Special type for not found types
/// </summary>
[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/UnknownNode/UnknownNode{.lang}.md")]
[Display(GroupName = "common")]
public class UnknownNode : Node
{
    public override string TypeId => "core.UnknownNode";

    public string JsonBody { get; set; } = "";
    public string UnrecognizedType { get; set; } = "";
    public bool IsDefinedAsConfig { get; set; }

    public UnknownNode()
    {

    }

    public UnknownNode(NodeBasicObj basic, string jsonBody)
    {
        JsonBody = jsonBody;
        Id = basic.Id;
        Container = basic.Container;
        UnrecognizedType = basic.TypeId;
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
