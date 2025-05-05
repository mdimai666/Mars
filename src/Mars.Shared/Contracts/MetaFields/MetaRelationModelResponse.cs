using Mars.Shared.Common;

namespace Mars.Shared.Contracts.MetaFields;

public record MetaRelationModelResponse
{
    public required string Title { get; init; }
    public required string TitlePlural { get; init; }
    public required string Key { get; init; }

    public required IReadOnlyCollection<RelationModelSubTypeResponse> SubTypes { get; init; }
}

public record RelationModelSubTypeResponse
{
    public required string Title { get; init; }
    public required string TitlePlural { get; init; }
    public required string Key { get; init; }
}

public record MetaValueRelationModelSummaryResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public record MetaValueRelationModelsListQueryRequest : BasicListQueryRequest
{
    public required string ModelName { get; init; }

}
