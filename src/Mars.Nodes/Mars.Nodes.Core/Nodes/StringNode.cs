using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/StringNode/StringNode{.lang}.md")]
public class StringNode : Node
{
    public StringNodeOperation[] Operations { get; set; } = [new() { Name = BaseOperation.ToUpper.ToString() }];

    public StringNode()
    {
        haveInput = true;
        Color = "#b2b2b2";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/function.svg";
    }
}

public class StringNodeOperation
{
    public string Name { get; set; } = default!;

    public string[] Args { get; set; } = [];

    public void NameChanged(string name)
    {
        if (Name.Equals(name)) return;
        this.Name = name;
        this._inputs = null;
        if (Enum.TryParse<BaseOperation>(name, out BaseOperation op))
        {
            int newArgsCount = op switch
            {
                BaseOperation.Replace => 2,
                //BaseOperation.Encode => 2,
                _ => 0
            };
            if (Args.Length < newArgsCount)
            {
                Args = [.. Args, .. Enumerable.Repeat("", newArgsCount - Args.Length)];
            }
        }
    }

    OperationInput[]? _inputs;

    public IEnumerable<OperationInput> Inputs()
    {
        if (_inputs is not null) return _inputs;

        if (Args.Length == 0)
        {
            _inputs = [];
        }
        else if (Name == nameof(BaseOperation.Replace))
        {
            _inputs = [
                new (){
                    Type = OperationInputType.Input,
                    Label = "old value",
                    Placeholder = "old value",
                    DefaultValue = ""
                },
                new(){
                    Type = OperationInputType.Input,
                    Label = "new value",
                    Placeholder = "new value",
                    DefaultValue = ""
                }
            ];
        }
        return _inputs ?? [];
    }

}



public enum BaseOperation
{
    ToUpper,
    ToLower,
    Trim,
    TrimStart,
    TrimEnd,
    Replace,
    Split,
    Join,
    Format,
    //Encode,
}

public class OperationInput
{
    public OperationInputType Type { get; set; }
    public string? Placeholder { get; set; }
    public string Label { get; set; } = default!;
    public string? DefaultValue { get; set; }
}

public enum OperationInputType
{
    Input
}
