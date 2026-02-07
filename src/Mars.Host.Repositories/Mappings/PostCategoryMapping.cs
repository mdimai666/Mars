using Mars.Core.Extensions;
using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.PostCategories;

namespace Mars.Host.Repositories.Mappings;

internal static class PostCategoryMapping
{
    public static PostCategorySummary ToSummary(this PostCategoryEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            Type = entity.PostCategoryType!.TypeName,
            Tags = entity.Tags,
            Slug = entity.Slug,

            PostType = entity.PostType!.TypeName,
            Path = entity.Path,
            SlugPath = entity.SlugPath,
            PathIds = entity.PathIds,
            LevelsCount = entity.LevelsCount,
        };

    public static PostCategoryDetail ToDetail(this PostCategoryEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Type = entity.PostCategoryType!.TypeName,
            Slug = entity.Slug,
            Tags = entity.Tags,
            ModifiedAt = entity.ModifiedAt,

            PostType = entity.PostType!.TypeName,
            Path = entity.Path,
            SlugPath = entity.SlugPath,
            PathIds = entity.PathIds,
            LevelsCount = entity.LevelsCount,

            Disabled = entity.Disabled,
            MetaValues = entity.MetaValues!.ToDto(),
        };

    public static PostCategoryEditDetail ToEditDetail(this PostCategoryEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Type = entity.PostCategoryType!.TypeName,
            PostType = entity.PostType!.TypeName,
            Slug = entity.Slug,
            Tags = entity.Tags,
            ModifiedAt = entity.ModifiedAt,

            ParentId = entity.ParentId,
            PathIds = entity.PathIds,
            Disabled = entity.Disabled,

            MetaValues = entity.MetaValues!.ToDetailDto(),
        };

    public static IReadOnlyCollection<PostCategorySummary> ToSummaryList(this IEnumerable<PostCategoryEntity> entities)
        => entities.Select(ToSummary).ToArray();

    public static IReadOnlyCollection<PostCategoryDetail> ToDetailList(this IEnumerable<PostCategoryEntity> entities)
        => entities.Select(ToDetail).ToArray();

    public static PostCategoryEntity ToEntity(this CreatePostCategoryQuery query, Guid[] pathIds, string slugPath)
        => new()
        {
            Id = query.Id ?? Guid.Empty,
            Slug = query.Slug,
            Title = query.Title,
            Tags = query.Tags.ToList(),
            PostCategoryTypeId = query.PostCategoryTypeId,
            PostTypeId = query.PostTypeId,

            ParentId = query.ParentId,
            Path = '/' + pathIds.Select(s => s.ToString()).JoinStr("/"),
            SlugPath = slugPath,
            RootId = pathIds[0],
            PathIds = pathIds,
            LevelsCount = pathIds.Length,
            Disabled = query.Disabled,

            MetaValues = query.MetaValues.ToEntity(),
        };

    public static PostCategoryEntity UpdateEntity(this PostCategoryEntity entity, UpdatePostCategoryQuery query, Guid[] pathIds, string slugPath)
    {
        entity.Id = query.Id;
        entity.Slug = query.Slug;
        entity.Title = query.Title;
        entity.Tags = query.Tags.ToList();
        entity.PostCategoryTypeId = query.PostCategoryTypeId;
        entity.PostTypeId = query.PostTypeId;

        entity.ParentId = query.ParentId;
        entity.Path = '/' + pathIds.Select(s => s.ToString()).JoinStr("/");
        entity.SlugPath = slugPath;
        entity.RootId = pathIds[0];
        entity.PathIds = pathIds;
        entity.LevelsCount = pathIds.Length;
        entity.Disabled = query.Disabled;

        entity.ModifiedAt = DateTimeOffset.Now;
        return entity;
    }
}
