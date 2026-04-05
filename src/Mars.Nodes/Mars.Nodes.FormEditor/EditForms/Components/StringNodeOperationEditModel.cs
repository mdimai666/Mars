using Mars.Core.Extensions;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.Resources;
using Mars.Nodes.Core.StringFunctions;

namespace Mars.Nodes.FormEditor.EditForms.Components;

internal class StringNodeEditModel
{
    IReadOnlyDictionary<string, StringNodeMethodInfo> _functions;

    public IEnumerable<IGrouping<string, StringNodeMethodInfo>> AvailFunctions { get; }

    public StringNodeOperationEditModel[] Operations { get; set; } = [];

    public StringNodeEditModel(StringNode node)
    {
        _functions = StringNodeOperationUtilsMethodParser.ParseMethods(typeof(StringNodeOperationUtils)).ToDtoDictionary();
        Operations = ItemsToModel(node);

        AvailFunctions = _functions.Values.GroupBy(s => NodeRes.ResourceManager.GetString("StringOperationGroupName_" + s.GroupName)!);
    }

    StringNodeOperationEditModel CreateBlank()
        => new(_functions[nameof(StringNodeOperationUtils.ToUpper)]);

    StringNodeOperationEditModel[] ItemsToModel(StringNode node)
    {
        var ops = node.Operations.Select(op => StringNodeOperationEditModel.ToModel(op, _functions)).ToArray();
        if (ops.None()) ops = [CreateBlank()];
        return ops;
    }

    public StringNodeOperation[] ItemsToRequest()
        => Operations.Select(x => x.ToRequest()).ToArray();

    public void AddNewOp(string methodName)
        => Operations = [.. Operations, CreateOp(methodName)];

    public void RemoveOp(StringNodeOperationEditModel op)
        => Operations = Operations.Where(x => x != op).ToArray();

    public void Clear()
        => Operations = [];

    private StringNodeOperationEditModel CreateOp(string methodName)
    {
        var op = _functions[methodName];
        return new(op);
    }
}

public class StringNodeOperationEditModel
{
    public string Method { get; private set; } = nameof(StringNodeOperationUtils.ToUpper);
    public OperationInputEditModel[] ParameterValues { get; private set; } = [];
    public string DisplayName { get; private set; }
    public string HelpText { get; private set; }

    public StringNodeOperationEditModel(StringNodeMethodInfo methodInfo)
    {
        Method = methodInfo.Name;
        ParameterValues = CalcParameterValues(methodInfo);
        DisplayName = methodInfo.DisplayName ?? methodInfo.Name ?? string.Empty;
        HelpText = methodInfo.Description ?? string.Empty;
    }

    public StringNodeOperationEditModel(StringNodeMethodInfo methodInfo, StringNodeOperation operation)
    {
        Method = methodInfo.Name;
        ParameterValues = CalcParameterValues(methodInfo, operation);
        DisplayName = methodInfo.DisplayName ?? methodInfo.Name ?? string.Empty;
        HelpText = methodInfo.Description ?? string.Empty;
    }

    public static StringNodeOperationEditModel ToModel(StringNodeOperation operation, IReadOnlyDictionary<string, StringNodeMethodInfo> dict)
        => new(dict[operation.Method], operation);

    public StringNodeOperation ToRequest()
        => new()
        {
            Method = Method,
            ParameterValues = ParameterValues.Select(s => s.Value).ToArray()
        };

    public void MethodChanged(StringNodeMethodInfo methodInfo)
    {
        Method = methodInfo.Name;
        ParameterValues = CalcParameterValues(methodInfo, ParameterValues);
        DisplayName = methodInfo.DisplayName;
        HelpText = methodInfo.Description ?? string.Empty;
    }

    public static OperationInputEditModel[] CalcParameterValues(StringNodeMethodInfo methodInfo)
        => CalcParameterValues(methodInfo, []);

    public static OperationInputEditModel[] CalcParameterValues(StringNodeMethodInfo methodInfo, OperationInputEditModel[] existParameterValues)
    {
        return methodInfo.Parameters.Skip(1).Select((p, i) =>
                    new OperationInputEditModel(p.Name,
                                                existParameterValues.ElementAtOrDefault(i)?.Value ?? p.DefaultValue,
                                                type: p.Type,
                                                placeholder: p.Description))
                .ToArray();
    }

    public static OperationInputEditModel[] CalcParameterValues(StringNodeMethodInfo methodInfo, StringNodeOperation operation)
    {
        return methodInfo.Parameters.Skip(1).Select((p, i) =>
                    new OperationInputEditModel(p.Name,
                                                //operation.ParameterValues.ElementAtOrDefault(i).Value ?? p.DefaultValue,
                                                operation.ParameterValues.ElementAtOrDefault(i) ?? p.DefaultValue,
                                                type: p.Type,
                                                placeholder: p.Description))
                .ToArray();
    }
}

public class OperationInputEditModel
{
    private Guid Id = Guid.NewGuid();
    public string Name { get; init; }
    public string Value { get; set; }
    public TypeCode Type { get; init; }
    public string? Placeholder { get; init; }

    public bool ValueBoolSetter { get => bool.TryParse(Value, out var b) ? b : false; set => Value = value.ToString(); }

    public int ValueIntSetter { get => int.TryParse(Value, out var i) ? i : 0; set => Value = value.ToString(); }

    public OperationInputEditModel(string name, string value, TypeCode type, string? placeholder = null)
    {
        Name = name;
        Value = value;
        Type = type;
        Placeholder = placeholder;
    }
}
