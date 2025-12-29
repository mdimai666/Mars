using Mars.Core.Interfaces;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Exceptions;
using Mars.Nodes.Core.Implements;
using Mars.WebApp.Nodes.Nodes;

namespace Mars.WebApp.Nodes.Host.Nodes;

public class AppEntityDeleteNodeImpl : INodeImplement<AppEntityDeleteNode>, INodeImplement
{
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;

    public AppEntityDeleteNode Node { get; }
    public IRED RED { get; set; }
    Node INodeImplement<Node>.Node => Node;

    public AppEntityDeleteNodeImpl(AppEntityDeleteNode node, IRED _RED, IMetaModelTypesLocator metaModelTypesLocator)
    {
        Node = node;
        RED = _RED;
        _metaModelTypesLocator = metaModelTypesLocator;
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
            var handler = _metaModelTypesLocator.GetMetaRelationModelProvider(requestInfo.EntityUri.Root!);

            if (handler == null)
                throw new NodeExecuteException(Node, $"AppEntityDeleteNodeImpl: Not found provider for '{requestInfo.EntityUri.Root}'");

            var deletedCount = await handler.DeleteMany(ids, parameters.CancellationToken);

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
