using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Nodes.Core.Nodes.Common;

namespace Mars.Nodes.Core.Nodes.Functions;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/VariableSetNode/VariableSetNode{.lang}.md")]
[Display(GroupName = "functions")]
public class VariableSetNode : Node
{
    public override string TypeId => "core.VariableSetNode";

    [ValidateComplexType]
    public VariableSetExpression[] Setters { get; set; } = [
        new VariableSetExpression { ValuePath = "msg.Payload", Expression = "1+1", Operation = VariableSetOperation.Set }
    ];

    public VariableSetNode()
    {
        Inputs = [new()];
        Color = "#ecb56a";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/function-x.svg";
    }

}

public class VariableSetExpression : IValidatableObject
{
    public VariableSetOperation Operation { get; set; }
    public string Expression { get; set; } = "1";
    public string ValuePath { get; set; } = "msg.Payload";

    public string[] AllowedContextList = ["msg", "GlobalContext", "FlowContext", nameof(VarNode)];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var segments = ValuePath.Split(".");
        var valuePathRoot = segments[0];

        if (segments.Length < 2)
        {
            yield return new ValidationResult("ValuePath must contain Sub property like 'msg.Payload'", [nameof(ValuePath)]);
            yield break;
        }

        if (!AllowedContextList.Contains(valuePathRoot))
            yield return new ValidationResult($"Path Root '{valuePathRoot}' is not allowed. Must be one of [{string.Join(',', AllowedContextList)}]", [nameof(ValuePath)]);

    }

}

public enum VariableSetOperation
{
    Set,
    Delete,

    Append,
    Prepend,
    Shift,
    Unshift,
}
