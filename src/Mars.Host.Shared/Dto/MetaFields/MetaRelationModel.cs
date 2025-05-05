using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Dto.MetaFields;

/// <summary>
/// <see cref="MetaRelationModelResponse"/>
/// </summary>
public record MetaRelationModel
{
    public required string Title { get; init; }
    public required string TitlePlural { get; init; }
    public required string Key { get; init; }

    public required IReadOnlyCollection<RelationModelSubType> SubTypes { get; init; }
}

/// <summary>
/// <see cref="RelationModelSubTypeResponse"/>
/// </summary>
public record RelationModelSubType
{
    
    public required string Title { get; init; }
    public required string TitlePlural { get; init; }
    public required string Key { get; init; }
}

/// <summary>
/// <see cref="MetaValueRelationModelSummaryResponse"/>
/// </summary>
public record MetaValueRelationModelSummary
{
    
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}


/// <summary>
/// <see cref="MetaValueRelationModelsListQueryRequest"/>
/// </summary>
public record MetaValueRelationModelsListQuery : BasicListQuery
{
    public required string ModelName { get; init; }
}
