using Mars.Nodes.Abstractions;
using Mars.Nodes.Core;

namespace Mars.Nodes.Expressions;

public static class RuntimeNodeScopeExtensions
{
    public static ExpressionSession Expressions(this IRuntimeNodeScope rns, Node node)
        => new(rns, node, rns.ServiceProvider.GetService(typeof(ExpressionRunnerPool)) as ExpressionRunnerPool);
}
