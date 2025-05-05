using Mars.Nodes.Core.Nodes;
using DynamicExpresso;

namespace Mars.Nodes.Core.Implements.Nodes;

public class SwitchNodeImpl : INodeImplement<SwitchNode>, INodeImplement
{

    public SwitchNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public SwitchNodeImpl(SwitchNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {
        var interpreter = new Interpreter();//https://github.com/dynamicexpresso/DynamicExpresso

        try
        {
            for (int i = 0; i < Node.Conditions.Count; i++)
            {
                var a = Node.Conditions[i];

                if (string.IsNullOrEmpty(a.Value)) continue;

                var result = interpreter.Eval<bool>(a.Value,
                    new Parameter("Payload", input.Payload)
                );

                //input.Payload = result;

                if (result == true)
                {
                    callback(input, i);
                    if (Node.BreakAfterFirst)
                    {
                        break;
                    }
                }

            }
        }
        catch (Exception ex)
        {
            RED.Status(new NodeStatus { Text = "error" });
            RED.DebugMsg(ex);
        }

        return Task.CompletedTask;

    }
}
