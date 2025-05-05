using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/NodeFormEditor/Docs/VariableSetNode/VariableSetNode{.lang}.md")]
public class VariableSetNode : Node
{
    [ValidateComplexType]
    public List<VariableSetExpression> Setters { get; set; } = [
        new VariableSetExpression { ValuePath = "msg.Payload", Expression = "1+1", Operation = VariableSetOperation.Set }
    ];

    public VariableSetNode()
    {
        haveInput = true;
        Color = "#ecb56a";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/NodeWorkspace/nodes/ext-48.png";
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
