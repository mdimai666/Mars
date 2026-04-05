using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Nodes.Core.StringFunctions;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/StringNode/StringNode{.lang}.md")]
[Display(GroupName = "function")]
public class StringNode : Node
{
    public StringNodeOperation[] Operations { get; set; } = [new() { Method = nameof(StringNodeOperationUtils.ToUpper) }];

    public StringNode()
    {
        Inputs = [new()];
        Color = "#b2b2b2";
        Outputs = [new NodeOutput()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/string.svg";
    }
}

public record StringNodeOperation
{
    public string Method { get; init; } = nameof(StringNodeOperationUtils.ToUpper);
    public string[] ParameterValues { get; init; } = [];
}

public record StringNodeMethodInfo
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string GroupName { get; init; }
    public required StringNodeMethodParameterInfo[] Parameters { get; init; }
    public required string? Description { get; init; }
}

public class StringNodeMethodParameterInfo
{
    public required string Name { get; init; }
    public required TypeCode Type { get; init; }
    public required string DefaultValue { get; init; } = "";
    public required bool IsRequired { get; init; }
    public required string Description { get; init; }
    public required bool IsArray { get; init; }
    public required TypeCode? ArrayElementType { get; init; }
}

public static class StringNodeOperationExtensions
{
    public static StringNodeMethodParameterInfo ToDto(this MethodParameter entity)
        => new()
        {
            Name = entity.Name,
            Type = StringValueParser.TypeToTypeCode(entity.Type),
            DefaultValue = entity.DefaultValue?.ToString() ?? string.Empty,
            IsRequired = entity.IsRequired,
            Description = entity.Description,
            IsArray = entity.IsArray,
            ArrayElementType = entity.ArrayElementType == null ? null : StringValueParser.TypeToTypeCode(entity.ArrayElementType)
        };

    public static StringNodeMethodInfo ToDto(this StringMethod entity)
        => new()
        {
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            GroupName = entity.GroupName,
            Parameters = entity.Parameters.Select(p => p.ToDto()).ToArray(),
            Description = entity.Description,
        };

    public static IReadOnlyDictionary<string, StringNodeMethodInfo> ToDtoDictionary(this IEnumerable<StringMethod> list)
        => list.ToDictionary(s => s.Name, s => s.ToDto());

}
