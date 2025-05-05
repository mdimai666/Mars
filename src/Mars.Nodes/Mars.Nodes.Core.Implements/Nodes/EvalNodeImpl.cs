using Mars.Nodes.Core.Nodes;
using DynamicExpresso;

namespace Mars.Nodes.Core.Implements.Nodes;

public class EvalNodeImpl : INodeImplement<EvalNode>, INodeImplement
{
    public EvalNodeImpl(EvalNode node, IRED RED)
    {
        this.Node = node;
        this.RED = RED;
    }

    public EvalNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public Task Execute(NodeMsg input, ExecuteAction callback, Action<Exception> Error)
    {

        var interpreter = new Interpreter();//https://github.com/dynamicexpresso/DynamicExpresso

        try
        {

            var result = interpreter.Eval(Node.Input, new Parameter("msg", input));

            input.Payload = result;

            callback(input);
        }
        catch (Exception ex)
        {
            RED.Status(new NodeStatus("error"));
            RED.DebugMsg(ex);
        }

        return Task.CompletedTask;
    }
}
