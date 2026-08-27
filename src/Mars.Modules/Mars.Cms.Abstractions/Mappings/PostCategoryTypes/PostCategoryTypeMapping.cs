using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategoryTypes;

namespace Mars.Host.Shared.Mappings.PostCategoryTypes;

public static class PostCategoryTypeMapping
{
    public static PostCategoryTypeSummaryResponse ToResponse(this PostCategoryTypeSummary entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
        };

    public static PostCategoryTypeDetailResponse ToResponse(this PostCategoryTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            MetaFields = entity.MetaFields.ToDetailResponse(),
        };

    public static PostCategoryTypeSummary ToSummary(this PostCategoryTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
        };

    public static PostCategoryTypeSummaryResponse ToSummaryResponse(this PostCategoryTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
        };

    public static PostCategoryTypeListItemResponse ToItemResponse(this PostCategoryTypeSummary entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            TypeName = entity.TypeName,
            CreatedAt = entity.CreatedAt,
            Tags = entity.Tags,
        };

    public static ListDataResult<PostCategoryTypeListItemResponse> ToResponse(this ListDataResult<PostCategoryTypeSummary> postTypes)
        => postTypes.ToMap(ToItemResponse);

    public static PagingResult<PostCategoryTypeListItemResponse> ToResponse(this PagingResult<PostCategoryTypeSummary> postTypes)
        => postTypes.ToMap(ToItemResponse);

    public static IReadOnlyCollection<PostCategoryTypeSummaryResponse> ToResponse(this IReadOnlyCollection<PostCategoryTypeSummary> list)
        => list.Select(ToResponse).ToList();

}
