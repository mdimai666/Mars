using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Shared;
using Microsoft.Extensions.Caching.Hybrid;

namespace Mars.Nodes.Core.Implements.Nodes;

public class CounterNodeImpl : INodeImplement<CounterNode>, INodeLifecycleOnAssigned, INodeLifecycleOnDelete
{
    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _entryOptions = new()
    {
        LocalCacheExpiration = TimeSpan.FromHours(12),
        Expiration = TimeSpan.FromDays(1),
    };
    private readonly Debouncer _debouncer = new(500);

    public int Count { get; set; }

    public CounterNode Node { get; }
    public IRuntimeNodeScope RNS { get; set; }
    Node INodeImplement.Node => Node;

    public CounterNodeImpl(CounterNode node, IRuntimeNodeScope rns, HybridCache hybridCache)
    {
        Node = node;
        RNS = rns;
        _cache = hybridCache;
    }

    public Task Execute(NodeMsg input, ExecuteAction callback, ExecutionParameters parameters)
    {
        Count += parameters.InputPort == 0 ? +1 : -1;
        input.Payload = Count;

        //RNS.DebugMsg(DebugMessage.NodeMessage(Node.Id, $"input port = {parameters.InputPort}"));
        Node.status = $"count = {Count}";
        RNS.Status(new NodeStatus(Node.status));

        _debouncer.Debouce(() =>
        {
            _cache.SetAsync<int>(KeyCount, Count, _entryOptions, ["nodes"]);
        });
        callback(input);
        return Task.CompletedTask;
    }

    private string KeyCount => nameof(CounterNode) + Node.Id + ":Count";

    public async Task OnNodeAssigned(CancellationToken cancellationToken)
    {
        Count = await _cache.GetOrCreateAsync<int>(KeyCount,
            ct => ValueTask.FromResult(0),
            _entryOptions,
            tags: ["nodes"]);
        Node.status = $"count = {Count}";
    }

    public Task OnNodeDelete()
    {
        _cache.RemoveAsync(KeyCount);
        return Task.CompletedTask;
    }
}
