using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.PostCategoryTypes;

namespace Mars.Host.Repositories.Mappings;

internal static class PostCategoryTypeMapping
{
    public static PostCategoryTypeSummary ToSummary(this PostCategoryTypeEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
        };

    public static PostCategoryTypeDetail ToDetail(this PostCategoryTypeEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            ModifiedAt = entity.ModifiedAt,
            MetaFields = entity.MetaFields!.ToDto()
        };

    public static IReadOnlyCollection<PostCategoryTypeSummary> ToSummaryList(this IEnumerable<PostCategoryTypeEntity> entities)
        => entities.Select(ToSummary).ToArray();

    public static IReadOnlyCollection<PostCategoryTypeDetail> ToDetailList(this IEnumerable<PostCategoryTypeEntity> entities)
        => entities.Select(ToDetail).ToArray();

    public static PostCategoryTypeEntity ToEntity(this CreatePostCategoryTypeQuery query)
        => new()
        {
            Id = query.Id ?? Guid.Empty,
            Title = query.Title,
            TypeName = query.TypeName,
            Tags = query.Tags.ToList(),

            MetaFields = query.MetaFields.ToEntity()
        };

}
