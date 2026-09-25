using System.Collections.Concurrent;

namespace Mars.Nodes.Expressions;

/// <summary>
/// Ленивый пул <see cref="ExpressionRunner"/> по node.Id: один runner арендуется одним воркером,
/// несколько одновременных NodeTaskJob дают несколько runner'ов на ноду (мешок растёт до
/// фактического параллелизма и дальше переиспользуется). Runner не привязан к RNS —
/// сброс пула на редеплой flow не нужен.
/// </summary>
public class ExpressionRunnerPool
{
    readonly ConcurrentDictionary<string, ConcurrentBag<ExpressionRunner>> _runners = new();

    public ExpressionRunner Rent(string nodeId)
        => _runners.GetOrAdd(nodeId, static _ => new()).TryTake(out var runner) ? runner : new ExpressionRunner();

    public void Return(string nodeId, ExpressionRunner runner)
        => _runners.GetOrAdd(nodeId, static _ => new()).Add(runner);
}
