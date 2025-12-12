using DynamicExpresso;
using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Core.Implements.Nodes;

public class SwitchNodeImpl : INodeImplement<SwitchNode>, INodeImplement
{

    public SwitchNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public SwitchNodeImpl(SwitchNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;
    }

#if !DynamicExpresso
    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var interpreter = new Interpreter();//https://github.com/dynamicexpresso/DynamicExpresso

        for (int i = 0; i < Node.Conditions.Count; i++)
        {
            var a = Node.Conditions[i];

            if (string.IsNullOrEmpty(a.Value)) continue;

            var result = interpreter.Eval<bool>(a.Value,
                input.AsFullDict()!.Select(kv => new Parameter(kv.Key, kv.Value)).ToArray()
            );

            if (result == true)
            {
                callback(input, i);
                if (Node.BreakAfterFirst)
                {
                    break;
                }
            }
        }

        return Task.CompletedTask;
    }
#else

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        for (int i = 0; i < Node.Conditions.Count; i++)
        {
            var a = Node.Conditions[i];

            if (string.IsNullOrEmpty(a.Value)) continue;

            try
            {
                //https://github.com/ncalc/ncalc
                var expr = new Expression(a.Value);

                var inputDict = input.AsFullDict();
                expr.Parameters = inputDict!;

                //expr.Parameters["Payload"] = input.Payload!;
                //expr.Parameters["msg"] = input;

                //PROBLEM: property access like msg.Property1.SubProperty2 do not work out of the box
                // EvaluateParameter does not work because '.' dot in the name is Parsing error
                expr.EvaluateParameter += (name, args) =>
                {
                    if (name == "Payload")
                    {
                        args.Result = input.Payload;
                        return;
                    }

                    //if (name.StartsWith("msg."))
                    {
                        object? current = inputDict;

                        var parts = name.Split('.');

                        foreach (var part in parts.Skip(1))
                        {
                            var prop = current!.GetType().GetProperty(part);
                            current = prop?.GetValue(current);
                        }

                        if (current == null)
                        {
                            args.Result = null;
                            return;
                        }

                        // Если convertible — типизированный результат
                        if (current is IConvertible)
                        {
                            var type = current.GetType();
                            args.Result = Convert.ChangeType(current, type);
                            return;
                        }

                        // иначе отдаём как есть (например class)
                        args.Result = current;
                        return;
                    }

                    //if (name == "msg")
                    //    args.Result = input;
                };

                var result = (bool)expr.Evaluate()!;

                if (result == true)
                {
                    callback(input, i);
                    if (Node.BreakAfterFirst)
                    {
                        break;
                    }
                }
            }
            catch (NCalc.Exceptions.NCalcParserException ex)
            {
                throw new NodeExecuteException(Node, ex.Message + $" Expression='{a.Value}'.", ex);
            }
        }

        return Task.CompletedTask;

    }
#endif
}
