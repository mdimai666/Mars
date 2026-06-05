using System.ComponentModel.DataAnnotations;

namespace Mars.Nodes.Core.Nodes;

[Display(GroupName = "functions")]
public class InlineFunctionNode : Node
{
    public const string DefaultColor = "#dcdc9b";
    public const string DefaultIcon = "_content/Mars.Nodes.Workspace/nodes/function.svg";

    public string FunctionId { get; set; } = "";
    public string[] Arguments { get; set; } = [];

    public InlineFunctionNode()
    {
        Color = DefaultColor;
        Icon = DefaultIcon;
    }

    public static InlineFunctionNode CreateInlineFunctionNode(InlineFunctionNodeSchema def)
    {
        return new()
        {
            Name = def.Name,
            Inputs = def.Inputs.ToList(),
            Outputs = def.Outputs.ToList(),
            Color = def.Color ?? DefaultColor,
            Icon = def.Icon ?? DefaultIcon,

            FunctionId = def.TypeId,
            Arguments = def.Parameters.Select(p => p.DefaultValue ?? string.Empty).ToArray()
        };
    }
}

public class InlineFunctionNodeDefinition
{
    public required string TypeId { get; init; }
    public required string Name { get; init; }
    public required string? Color { get; init; }
    public required string? Icon { get; init; }

    public required string GroupName { get; init; } = "function";

    public required NodeInput[] Inputs { get; init; }
    public required NodeOutput[] Outputs { get; init; }

    public required Delegate Delegate { get; init; }

}

public record InlineFunctionNodeSchema
{
    public required string TypeId { get; init; }
    public required string Name { get; init; }
    public required string? Color { get; init; }
    public required string? Icon { get; init; }

    public required string GroupName { get; init; }

    public required NodeInput[] Inputs { get; init; }
    public required NodeOutput[] Outputs { get; init; }

    public required IFNS_Parameter[] Parameters { get; init; }
}

public record IFNS_Parameter
{
    public required string Name { get; init; }
    public required TypeCode Type { get; init; }
    public required string TypeName { get; init; }
    public required string? DefaultValue { get; init; }
    public required bool IsRequired { get; init; }
    public required string? Description { get; init; }

}

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class MethodInlineFunctionNodeDefineAttribute : Attribute
{
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public string? NodeTypeId { get; init; }
}
