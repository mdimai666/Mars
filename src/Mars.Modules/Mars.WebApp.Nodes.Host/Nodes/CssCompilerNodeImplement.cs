using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.WebApp.Nodes.Nodes;

namespace Mars.WebApp.Nodes.Host.Nodes;

public class CssCompilerNodeImplement : INodeImplement<CssCompilerNode>, INodeImplement
{
    public CssCompilerNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public CssCompilerNodeImplement(CssCompilerNode node, IRED _RED)
    {
        Node = node;
        RED = _RED;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (input.Payload is not string)
        {
            throw new ArgumentException("Payload must be string");
        }

        string css = dotless.Core.Less.Parse(input.Payload as string);

        input.Payload = css;

        callback(input);

        return Task.CompletedTask;
    }
}
