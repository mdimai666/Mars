using Mars.Core.Interfaces;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements;
using Mars.WebApp.Nodes.Host.Builders;
using Mars.WebApp.Nodes.Nodes;

namespace Mars.WebApp.Nodes.Host.Nodes;

public class AppEntityDeleteNodeImpl : INodeImplement<AppEntityDeleteNode>, INodeImplement
{
    private readonly IAppEntityFormBuilderFactory _formBuilderFactory;

    public AppEntityDeleteNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public AppEntityDeleteNodeImpl(AppEntityDeleteNode node, IRED _RED, IAppEntityFormBuilderFactory appEntityFormBuilderFactory)
    {
        Node = node;
        RED = _RED;
        _formBuilderFactory = appEntityFormBuilderFactory;
    }

    public async Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        var requestInfo = input.Get<AppEntityReadRequestInfo>();

        if (requestInfo is null)
            throw new NodeExecuteException(Node, "AppEntityDeleteNodeImpl: Missing AppEntityReadRequestInfo in input context. Plause use AppEntityReadNode before");

        Guid[] ids;

        if (input.Payload is IHasId idEntity) ids = [idEntity.Id];
        else if (input.Payload is IEnumerable<IHasId> idEntityList) ids = idEntityList.Select(s => s.Id).ToArray();
        else throw new NodeExecuteException(Node, $"AppEntityDeleteNodeImpl: Unsupported payload type {input.Payload?.GetType().FullName}");

        if (ids.Any())
        {
            var builder = _formBuilderFactory.GetBuilder(requestInfo.EntityUri.Root!);

            if (builder is null)
                throw new NodeExecuteException(Node, $"Cannot find form builder for entity '{requestInfo.EntityUri.Root}'.");

            var deletedCount = await builder.DeleteMany(ids, parameters.CancellationToken);

            input.Payload = deletedCount;
        }
        else
        {
            input.Payload = 0;
        }

        callback(input);

        var input2 = input.Copy(ids);
        callback(input2, 1);
    }
}
