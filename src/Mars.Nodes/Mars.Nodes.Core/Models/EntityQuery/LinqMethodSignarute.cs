using Mars.Core.Extensions;

namespace Mars.Nodes.Core.Models.EntityQuery;

public record LinqMethodSignarute
{
    public string Name { get; init; }
    public LinqMethodParameter[] Parameters { get; init; }

    public MethodHelperInfo Helper { get; init; }

    public LinqMethodSignarute(string name, LinqMethodParameter[] parameters, MethodHelperInfo helper)
    {
        Name = name;
        Parameters = parameters;
        Helper = helper;
    }

    public string ToMethodIntelliView() => $"{Name}({Parameters.Select(s => s.Name).JoinStr(", ")})";
}

public record LinqMethodParameter
{
    public string Name { get; init; }
    public LinqMethodParameterEvalType EvalType { get; init; }

    public string[] Variants { get; init; }

    public LinqMethodParameter(string name, LinqMethodParameterEvalType evalType, string[]? variants = null)
    {
        Name = name;
        EvalType = evalType;
        Variants = variants ?? [];
    }

    public LinqMethodParameter() : this("expr", LinqMethodParameterEvalType.Expression)
    {
    }

    public LinqMethodParameter(string name) : this(name, LinqMethodParameterEvalType.Expression)
    {
    }
}

public enum LinqMethodParameterEvalType
{
    Literally,
    Expression
}

public sealed record MethodHelperInfo(string Shortcut, string Example, string Description);
