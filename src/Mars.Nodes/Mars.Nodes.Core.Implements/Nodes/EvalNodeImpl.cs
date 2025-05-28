using DynamicExpresso;
using Mars.Nodes.Core.Nodes;

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

    public Task Execute(NodeMsg input, ExecuteAction callback)
    {

        var interpreter = new Interpreter();//https://github.com/dynamicexpresso/DynamicExpresso

        var result = interpreter.Eval(Node.Input, new Parameter("msg", input));

        input.Payload = result;

        callback(input);

        return Task.CompletedTask;
    }
}
