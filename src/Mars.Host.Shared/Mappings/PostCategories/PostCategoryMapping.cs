using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.PostCategories;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostCategories;

namespace Mars.Host.Shared.Mappings.PostCategories;

public static class PostCategoryMapping
{
    public static PostCategorySummaryResponse ToResponse(this PostCategorySummary entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Slug = entity.Slug,

            PathIds = entity.PathIds,
            SlugPath = entity.SlugPath,
            LevelsCount = entity.LevelsCount
        };

    public static PostCategoryDetailResponse ToResponse(this PostCategoryDetail entity)
      => new()
      {
          Id = entity.Id,
          CreatedAt = entity.CreatedAt,
          Title = entity.Title,
          TypeName = entity.Type,
          Slug = entity.Slug,
          Tags = entity.Tags,

          PostType = entity.PostType,
          PathIds = entity.PathIds,
          SlugPath = entity.SlugPath,
          LevelsCount = entity.LevelsCount,

          Disabled = entity.Disabled,
          MetaValues = entity.MetaValues.ToResponse(),
      };

    public static PostCategoryListItemResponse ToListItemResponse(this PostCategorySummary entity)
      => new()
      {
          Id = entity.Id,
          Title = entity.Title,
          Slug = entity.Slug,
          CreatedAt = entity.CreatedAt,
          Tags = entity.Tags,

          PathIds = entity.PathIds,
          SlugPath = entity.SlugPath,
          LevelsCount = entity.LevelsCount
      };

    public static PostCategoryEditResponse ToResponse(this PostCategoryEditDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            Type = entity.Type,
            PostType = entity.PostType,
            Slug = entity.Slug,
            Tags = entity.Tags,

            ParentId = entity.ParentId,
            PathIds = entity.PathIds,
            Disabled = entity.Disabled,
            MetaValues = entity.MetaValues.ToDetailResponse(),

        };

    public static ListDataResult<PostCategoryListItemResponse> ToResponse(this ListDataResult<PostCategorySummary> list)
        => list.ToMap(ToListItemResponse);

    public static PagingResult<PostCategoryListItemResponse> ToResponse(this PagingResult<PostCategorySummary> list)
        => list.ToMap(ToListItemResponse);

    public static IReadOnlyCollection<PostCategorySummaryResponse> ToResponse(this IEnumerable<PostCategorySummary> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<PostCategoryListItemResponse> ToResponseItems(this IEnumerable<PostCategorySummary> list)
        => list.Select(ToListItemResponse).ToList();

    public static IReadOnlyCollection<PostCategoryDetailResponse> ToResponse(this IEnumerable<PostCategoryDetail> list)
        => list.Select(ToResponse).ToList();

}
