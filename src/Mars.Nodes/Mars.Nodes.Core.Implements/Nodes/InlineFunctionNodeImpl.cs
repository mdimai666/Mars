using System.Reflection;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Core.StringFunctions;
using Mars.Nodes.Host.Shared;

namespace Mars.Nodes.Core.Implements.Nodes;

public class InlineFunctionNodeImpl : INodeImplement<InlineFunctionNode>
{
    private readonly INodeImplementFactory _implementFactory;

    public InlineFunctionNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public InlineFunctionNodeImpl(InlineFunctionNode node, IRuntimeNodeScope rns, INodeImplementFactory implementFactory)
    {
        Node = node;
        RNS = rns;
        _implementFactory = implementFactory;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        return ExecuteInlineFunctionNode(Node, input, callback, parameters);
    }

    private async Task ExecuteInlineFunctionNode(InlineFunctionNode node, NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var def = _implementFactory.GetInlineFunctionNodeDefinition(node.FunctionId);

        // 1. Получаем параметры метода делегата
        MethodInfo methodInfo = def.Delegate.Method;
        ParameterInfo[] parametersInfo = methodInfo.GetParameters();

        // 2. Определяем наличие аргументов NodeMsg и ExecutionParameters
        bool hasNodeMsgParam = parametersInfo.Any(p => p.ParameterType == typeof(NodeMsg));
        bool hasExecutionParamsParam = parametersInfo.Any(p => p.ParameterType == typeof(ExecutionParameters));

        // 3. Формируем массив аргументов для вызова делегата
        var args = new List<object?>();
        var argumentIndex = 0;

        var paramsWithoutExecutionParams = parametersInfo.Where(p => p.ParameterType != typeof(ExecutionParameters)
                                                                    && p.ParameterType != typeof(ExecutionParameters))
                                                        .ToList();

        var nodeArgumentsList = node.Arguments.ToList();

        var ppt = paramsWithoutExecutionParams.Any() ? VariableSetNodeImpl.CreateInterpreter(RNS, input) : null;

        for (int i = 0; i < parametersInfo.Length; i++)
        {
            ParameterInfo? param = parametersInfo[i];
            if (param.ParameterType == typeof(NodeMsg))
            {
                args.Add(input);
            }
            else if (param.ParameterType == typeof(ExecutionParameters))
            {
                args.Add(parameters);
            }
            else
            {
                var nodePassArgument = nodeArgumentsList.ElementAtOrDefault(argumentIndex)
                                            ?? param.DefaultValue?.ToString()
                                            ?? throw new ArgumentNullException($"required argument '{param.Name}'({param.ParameterType.Name}) on index {argumentIndex} not pass.");
                object? calcValue;
                bool isLiteralValue = !nodePassArgument.StartsWith('@');
                if (isLiteralValue)
                    calcValue = StringValueParser.ParseByType(param.ParameterType, nodePassArgument);
                else
                    calcValue = ppt.Get.Eval(nodePassArgument[1..], param.ParameterType);
                args.Add(calcValue);

                argumentIndex++;
                //throw new InvalidOperationException($"Unsupported parameter type: {param.ParameterType}");
            }
        }

        object? result = null;

        try
        {
            // 4. Вызываем делегат (синхронно или асинхронно)
            object? invokeResult = def.Delegate.DynamicInvoke(args.ToArray());

            // 5. Обрабатываем результат в зависимости от типа возвращаемого значения
            if (invokeResult is Task task)
            {
                await task.ConfigureAwait(false);
                task.GetAwaiter().GetResult();

                // Получаем результат если это Task<T>
                if (task.GetType().IsGenericType)
                {
                    var taskProperty = task.GetType().GetProperty("Result");
                    result = taskProperty?.GetValue(task);
                }
            }
            else
            {
                result = invokeResult;
            }

            // 6. Если есть результат, записываем его в callback (опционально)
            if (result != null && callback != null)
            {
                input.Payload = result;

                callback(input);
            }
        }
        catch (TargetInvocationException ex)
        {
            // Обрабатываем исключение из вызванного метода
            throw ex.InnerException ?? ex;
        }
    }
}
