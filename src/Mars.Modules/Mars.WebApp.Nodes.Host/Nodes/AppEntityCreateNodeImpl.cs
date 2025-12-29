using Mars.Core.Exceptions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements;
using Mars.WebApp.Nodes.Host.Builders;
using Mars.WebApp.Nodes.Nodes;

namespace Mars.WebApp.Nodes.Host.Nodes;

public class AppEntityCreateNodeImpl : INodeImplement<AppEntityCreateNode>, INodeImplement
{
    private readonly IAppEntityFormBuilderFactory _formBuilderFactory;

    public AppEntityCreateNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public AppEntityCreateNodeImpl(AppEntityCreateNode node, IRED _RED, IAppEntityFormBuilderFactory formBuilderFactory)
    {
        Node = node;
        RED = _RED;
        _formBuilderFactory = formBuilderFactory;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (!Node.FormCommand.EntityUri.HasValue)
            throw new NodeExecuteException(Node, "EntityUri is not set.");

        var builder = _formBuilderFactory.GetBuilder(Node.FormCommand.EntityUri.Root!);

        if (builder is null)
            throw new NodeExecuteException(Node, $"Cannot find form builder for entity '{Node.FormCommand.EntityUri.Root}'.");

        try
        {
            var createdObject = await builder.Save(Node.FormCommand, input, parameters.CancellationToken);
            input.Payload = createdObject;
            callback(input);
        }
        catch (MarsValidationException ex)
        {
            input.Payload = ex;
            callback(input, 1);
            throw;
        }
    }
}
