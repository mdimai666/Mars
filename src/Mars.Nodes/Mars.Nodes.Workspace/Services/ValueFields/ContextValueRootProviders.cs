using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Front.Abstractions.Services;

namespace Mars.Nodes.Workspace.Services.ValueFields;

/// <summary>Names of context variables written by the graph — VariableSetNode is the one that names them.</summary>
internal static class ContextVariableNames
{
    public static IEnumerable<(string Name, string? Source)> FromSetters(ValueFieldContext context, string root)
    {
        foreach (var node in context.Nodes.Values)
        {
            if (node is not VariableSetNode setNode) continue;

            foreach (var setter in setNode.Setters)
            {
                var segments = setter.ValuePath.Split('.');

                if (segments.Length < 2 || segments[0] != root) continue;
                yield return (segments[1], setNode.DisplayName);
            }
        }
    }
}

internal class FlowContextValueRootProvider : IValueRootProvider
{
    public const string RootName = "FlowContext";

    public int Order => 10;

    public IEnumerable<ValueFieldInfo> GetFields(ValueFieldContext context)
        => ContextVariableNames.FromSetters(context, RootName)
            .Select(name => new ValueFieldInfo($"{RootName}.{name.Name}", VarNode.ObjectTypeName, name.Source));
}

internal class GlobalContextValueRootProvider(IHostValueHints hostHints) : IValueRootProvider
{
    public const string RootName = "GlobalContext";

    public int Order => 20;

    public IEnumerable<ValueFieldInfo> GetFields(ValueFieldContext context)
    {
        var fromGraph = ContextVariableNames.FromSetters(context, RootName)
            .Select(name => new ValueFieldInfo($"{RootName}.{name.Name}", VarNode.ObjectTypeName, name.Source));

        var fromHost = hostHints.GlobalVariableNames
            .Select(name => new ValueFieldInfo($"{RootName}.{name}", VarNode.ObjectTypeName));

        return fromGraph.Concat(fromHost).DistinctBy(field => field.Path);
    }
}

internal class VarNodeValueRootProvider : IValueRootProvider
{
    public const string RootName = nameof(VarNode);

    public int Order => 30;

    public IEnumerable<ValueFieldInfo> GetFields(ValueFieldContext context)
    {
        foreach (var node in context.Nodes.Values)
        {
            if (node is not VarNode varNode || string.IsNullOrWhiteSpace(varNode.Name)) continue;

            yield return new ValueFieldInfo($"{RootName}.{varNode.Name}", varNode.VarType, varNode.DisplayName);
        }
    }
}
