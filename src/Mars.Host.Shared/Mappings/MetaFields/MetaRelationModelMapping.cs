using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.MetaFields;
using Mars.Shared.Common;
using Mars.Shared.Contracts.MetaFields;

namespace Mars.Host.Shared.Mappings.MetaFields;

public static class MetaRelationModelMapping
{
    public static MetaRelationModelResponse ToResponse(this MetaRelationModel entity)
        => new()
        {
            Key = entity.Key,
            Title = entity.Title,
            TitlePlural = entity.TitlePlural,
            SubTypes = entity.SubTypes.ToResponse()
        };

    public static RelationModelSubTypeResponse ToResponse(this RelationModelSubType entity)
        => new()
        {
            Key = entity.Key,
            TitlePlural = entity.TitlePlural,
            Title = entity.Title,
        };

    public static IReadOnlyCollection<MetaRelationModelResponse> ToResponse(this IReadOnlyCollection<MetaRelationModel> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<RelationModelSubTypeResponse> ToResponse(this IReadOnlyCollection<RelationModelSubType> list)
        => list.Select(ToResponse).ToList();


    public static MetaValueRelationModelSummaryResponse ToResponse(this MetaValueRelationModelSummary entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
        };

    public static ListDataResult<MetaValueRelationModelSummaryResponse> ToResponse(this ListDataResult<MetaValueRelationModelSummary> items)
        => items.ToMap(ToResponse);

    public static PagingResult<MetaValueRelationModelSummaryResponse> ToResponse(this PagingResult<MetaValueRelationModelSummary> items)
        => items.ToMap(ToResponse);

}
