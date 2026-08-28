using Mars.Contracts.Common;

namespace Mars.Cms.Contracts.MetaFields;

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

    /// <summary>Превью-адрес картинки поста по указателю типа (пусто, если картинки нет)</summary>
    public string? ImageUrl { get; init; }
}

public record MetaValueRelationModelsListQueryRequest : BasicListQueryRequest
{
    public required string ModelName { get; init; }

}
