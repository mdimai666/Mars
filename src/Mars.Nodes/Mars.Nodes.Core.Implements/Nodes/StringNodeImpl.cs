using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.StringFunctions;

namespace Mars.Nodes.Core.Implements.Nodes;

public class StringNodeImpl : INodeImplement<StringNode>, INodeImplement
{
    public StringNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    IReadOnlyDictionary<string, StringMethod> _functions;

    public StringNodeImpl(StringNode node, IRED red)
    {
        Node = node;
        RED = red;
        _functions = StringNodeOperationUtilsMethodParser.ParseMethods(typeof(StringNodeOperationUtils)).ToDictionary(s => s.Name);
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        object? inputValue = input.Payload switch
        {
            null => null,
            string str => str,
            string[] arrayString => arrayString,
            IEnumerable<string> enumString => enumString.ToArray(),
            _ => input.Payload.ToString()
        };

        var operations = Node.Operations.Select(op => new StringOperation { Method = op.Method, ParameterValues = ConvertStringParamsToTypedParams(op, _functions[op.Method]) }).ToList();

        var result = ExecuteOperations(inputValue!, operations, _functions);

        input.Payload = result;
        callback(input);

        return Task.CompletedTask;
    }

    public static object[] ConvertStringParamsToTypedParams(StringNodeOperation nodeOperation, StringMethod function)
    {
        return ConvertStringParamsToTypedParams(nodeOperation.ParameterValues, function);
    }

    public static object[] ConvertStringParamsToTypedParams(string[] nodeOperationParameters, StringMethod function)
    {
        return function.Parameters.Skip(1).Select((f, i) =>
        {
            var stringValue = nodeOperationParameters.ElementAtOrDefault(i);
            return StringValueParser.TryParseByTypeOrDefault(f.Type, stringValue!);
        }).ToArray();
    }

    /// <summary>
    /// Calc operations results
    /// </summary>
    /// <param name="inputValue"></param>
    /// <param name="stringOperations"></param>
    /// <param name="availableMethods"></param>
    /// <returns>string or string[]</returns>
    public static object? ExecuteOperations(object? inputValue, IReadOnlyCollection<StringOperation> stringOperations, IReadOnlyDictionary<string, StringMethod> availableMethods)
    {
        object? value = inputValue;

        foreach (var op in stringOperations)
        {
            var method = availableMethods[op.Method];

            var parameters = new List<object?>(method.Parameters.Length) { value };

            // Добавление остальных параметров
            foreach (var param in method.Parameters.Skip(1))
            {
                var argument = op.ParameterValues.ElementAtOrDefault(parameters.Count - 1);
                parameters.Add(argument ?? GetDefaultValue(param.Type)!);
            }

            var result = method.MethodInfo.Invoke(null, parameters.ToArray());

            value = result;
        }

        return value;
    }

    public static object? GetDefaultValue(Type type)
    {
        //if (type == typeof(string)) return "";
        //if (type == typeof(bool)) return false;
        //if (type == typeof(int)) return 0;
        //if (type == typeof(StringSplitOptions)) return StringSplitOptions.None;
        //if (type.IsArray && type.GetElementType() == typeof(string)) return Array.Empty<string>();
        //if (type.IsArray) return Array.CreateInstance(type.GetElementType()!, 0);
        //return null;
        return StringValueParser.GetDefaultValue(type);
    }

}
