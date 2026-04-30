using Mars.Nodes.Core.Nodes;

namespace Mars.Nodes.Host.Shared.ExceptionModule;

public class NodesErrorHandlerRegistry
{
    HashSet<string> _globalHandlers;
    Dictionary<string, string[]> _handlersByFlow;

    public NodesErrorHandlerRegistry(IEnumerable<CatchErrorNode> nodes)
    {
        _globalHandlers = nodes.Where(node => node.Scope == NodesErrorCatchScope.All)
                                  .Select(h => h.Id).ToHashSet();

        _handlersByFlow = nodes.Where(node => node.Scope == NodesErrorCatchScope.Flow)
                                  .GroupBy(h => h.Container)
                                  .ToDictionary(g => g.Key,
                                                g => g.Select(h => h.Id).ToArray());
    }

    public IReadOnlyCollection<string> GetHandlersFor(string flowId, string nodeId)
    {
        return _handlersByFlow.GetValueOrDefault(flowId, Array.Empty<string>())
                              .Concat(_globalHandlers)
                              .Distinct()
                              .ToList();
    }
}
