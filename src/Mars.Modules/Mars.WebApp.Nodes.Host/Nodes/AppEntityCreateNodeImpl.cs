using Mars.Core.Exceptions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Host.Shared;
using Mars.WebApp.Nodes.Host.Builders;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.WebApp.Nodes.Host.Nodes;

public class AppEntityCreateNodeImpl : INodeImplement<AppEntityCreateNode>
{
    public AppEntityCreateNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public AppEntityCreateNodeImpl(AppEntityCreateNode node, IRuntimeNodeScope rns)
    {
        Node = node;
        RNS = rns;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        if (!Node.FormCommand.EntityUri.HasValue)
            throw new NodeExecuteException(Node, "EntityUri is not set.");

        var formBuilderFactory = RNS.ServiceProvider.GetRequiredService<IAppEntityFormBuilderFactory>();
        var builder = formBuilderFactory.GetBuilder(Node.FormCommand.EntityUri.Root!);

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
