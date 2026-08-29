using Mars.Cms.Abstractions.Dto.PostCategoryTypes;
using Mars.Cms.Abstractions.Mappings.MetaFields;
using Mars.Cms.Contracts.PostCategoryTypes;
using Mars.Contracts.Common;
using Mars.Data.Extensions;

namespace Mars.Cms.Abstractions.Mappings.PostCategoryTypes;

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
