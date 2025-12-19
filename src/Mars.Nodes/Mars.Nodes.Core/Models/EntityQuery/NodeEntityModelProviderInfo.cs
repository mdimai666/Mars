namespace Mars.Nodes.Core.Models.EntityQuery;

public record NodeEntityModelProviderInfo
{
    public required string EntityName { get; init; }
    public required string Title { get; init; }
    public required string Group { get; init; }
    public required IReadOnlyCollection<LinqMethodSignarute> Methods { get; init; }
}

public record NodeEntityQueryBuilderDictionary
{
    public required IReadOnlyDictionary<string, NodeEntityModelProviderInfo> Providers { get; init; }
}
