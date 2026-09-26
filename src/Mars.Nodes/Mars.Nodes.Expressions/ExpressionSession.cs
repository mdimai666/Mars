using DynamicExpresso;
using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;

namespace Mars.Nodes.Expressions;

/// <summary>
/// Точка входа impl'ов в резолвинг полей на одно исполнение ноды: аренда runner'а из пула
/// (или ephemeral, если пул не зарегистрирован — тесты/моки), возврат в Dispose.
/// <para>
/// Как пользоваться: <c>using var expr = RNS.Expressions(Node);</c> и <c>expr.Resolve(...)</c>.
/// Если после резолвинга в Execute идёт долгий I/O (файлы, HTTP, MQTT, SMTP), аренду надо сузить
/// до самого резолвинга — локальной функцией с <c>using var</c> внутри:
/// <code>
/// var url = ResolveUrl();
/// ... долгий запрос ...
/// string ResolveUrl()
/// {
///     using var expr = RNS.Expressions(Node);
///     return (string)expr.Resolve(Node.UrlKind, Node.Url, "string", input, Node, "Url")!;
/// }
/// </code>
/// Почему: Execute одной ноды может идти параллельно (до 10 воркеров NodeTaskJob); пока runner
/// арендован, соседний воркер той же ноды создаёт новый runner с новым Interpreter (~28 КБ) —
/// пул разрастается до числа медленных операций вместо числа реальных резолвингов.
/// В коротких синхронных нодах, где резолвинг занимает всё тело (Inject, Switch, Eval),
/// достаточно <c>using var</c> в начале Execute.
/// </para>
/// </summary>
public sealed class ExpressionSession : IDisposable
{
    readonly ExpressionRunnerPool? _pool;
    readonly string _nodeId;
    readonly IRuntimeNodeScope _rns;
    readonly ExpressionRunner _runner;
    bool _disposed;

    internal ExpressionSession(IRuntimeNodeScope rns, Node node, ExpressionRunnerPool? pool)
    {
        _rns = rns;
        _nodeId = node.Id;
        _pool = pool;
        _runner = pool?.Rent(node.Id) ?? new ExpressionRunner();
    }

    public object? Resolve(string kind, string value, string varType, NodeMsg msg, Node node, string source)
        => _runner.Resolve(kind, value, varType, new ExpressionScope(_rns, msg), node, source);

    public Interpreter GetInterpreter(NodeMsg msg)
        => _runner.GetInterpreter(new ExpressionScope(_rns, msg));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pool?.Return(_nodeId, _runner);
    }
}
