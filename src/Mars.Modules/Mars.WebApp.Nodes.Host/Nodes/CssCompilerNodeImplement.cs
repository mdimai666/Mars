using Mars.Nodes.Core;
using Mars.Nodes.Host.Shared;
using Mars.WebApp.Nodes.Nodes;

namespace Mars.WebApp.Nodes.Host.Nodes;

public class CssCompilerNodeImplement : INodeImplement<CssCompilerNode>
{
    public CssCompilerNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public CssCompilerNodeImplement(CssCompilerNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
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
