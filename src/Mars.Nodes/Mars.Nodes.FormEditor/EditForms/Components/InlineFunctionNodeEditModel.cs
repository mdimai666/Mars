using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.FormEditor.EditForms.Components;

internal class InlineFunctionNodeEditModel
{
    IReadOnlyDictionary<string, InlineFunctionNodeSchema> _functionSchemas;

    public string FunctionId { get; private set; }
    public bool IsUnknownFunction { get; private set; }

    public OperationInputEditModel[] ParameterValues { get; private set; } = [];

    public InlineFunctionNodeEditModel(InlineFunctionNode node, IReadOnlyDictionary<string, InlineFunctionNodeSchema> functionSchemas)
    {
        _functionSchemas = functionSchemas;
        FunctionId = node.FunctionId;
        IsUnknownFunction = !_functionSchemas.TryGetValue(FunctionId, out var func);
        if (!IsUnknownFunction)
            MethodChanged(func!, node.Arguments);
    }

    public void MethodChanged(InlineFunctionNodeSchema methodInfo, string[] arguments)
    {
        FunctionId = methodInfo.TypeId;
        ParameterValues = CalcParameterValues(methodInfo, arguments);
    }

    public static OperationInputEditModel[] CalcParameterValues(InlineFunctionNodeSchema methodInfo, string[] arguments)
    {
        return methodInfo.Parameters.Select((p, i) =>
                    new OperationInputEditModel(p.Name,
                                                arguments.ElementAtOrDefault(i) ?? p.DefaultValue ?? string.Empty,
                                                type: p.Type,
                                                placeholder: p.DefaultValue,
                                                description: p.Description,
                                                isRequired: p.IsRequired))
                .ToArray();
    }

    public string[] ToArgumentsRequest()
    {
        return ParameterValues.Select(s => s.Value).ToArray();
    }
}
