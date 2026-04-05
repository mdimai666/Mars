using System.Reflection;

namespace Mars.Nodes.Core.StringFunctions;

public record StringMethod
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string GroupName { get; init; }
    public required MethodInfo MethodInfo { get; init; }
    public required MethodParameter[] Parameters { get; init; }
    public required string? Description { get; init; }
    public required Type ReturnType { get; init; }
}

public record MethodParameter
{
    public required string Name { get; init; }
    public required Type Type { get; init; }
    public required object? DefaultValue { get; init; }
    public required bool IsRequired { get; init; }
    public required string Description { get; init; }
    public required bool IsArray { get; init; }
    public required Type? ArrayElementType { get; init; }
}

public record StringOperation
{
    public string Method { get; init; } = nameof(StringNodeOperationUtils.ToUpper);
    public object[] ParameterValues { get; init; } = [];
}
