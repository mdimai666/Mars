using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.PostTypes;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;

namespace Mars.Host.Repositories.Mappings;

internal static class PostTypeMapping
{
    public static PostTypeSummary ToSummary(this PostTypeEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            EnabledFeatures = entity.EnabledFeatures
        };

    public static PostTypeDetail ToDetail(this PostTypeEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            ModifiedAt = entity.ModifiedAt,
            EnabledFeatures = entity.EnabledFeatures,
            Disabled = entity.Disabled,
            PostContentSettings = entity.PostContentType.ToDto(),
            PostStatusList = entity.PostStatusList.ToDto(),
            MetaFields = entity.MetaFields!.ToDto()

        };

    public static PostContentSettingsDto ToDto(this PostContentSettings entity)
        => new()
        {
            CodeLang = entity.CodeLang,
            PostContentType = entity.PostContentType,
        };

    public static PostStatusDto ToDto(this PostStatusEntity entity)
        => new()
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Tags = entity.Tags,
            Title = entity.Title
        };

    public static IReadOnlyCollection<PostTypeSummary> ToSummaryList(this IEnumerable<PostTypeEntity> entities)
        => entities.Select(ToSummary).ToArray();

    public static IReadOnlyCollection<PostTypeDetail> ToDetailList(this IEnumerable<PostTypeEntity> entities)
        => entities.Select(ToDetail).ToArray();

    public static IReadOnlyCollection<PostStatusDto> ToDto(this IEnumerable<PostStatusEntity> entities)
        => entities.Select(ToDto).ToArray();

    public static PostTypeEntity ToEntity(this CreatePostTypeQuery query)
        => new()
        {
            Id = query.Id ?? Guid.Empty,
            Title = query.Title,
            TypeName = query.TypeName,
            Tags = query.Tags.ToList(),

            Disabled = query.Disabled,
            EnabledFeatures = query.EnabledFeatures.ToList(),
            PostContentType = new()
            {
                PostContentType = query.PostContentSettings.PostContentType,
                CodeLang = query.PostContentSettings.CodeLang,
            },
            PostStatusList = query.PostStatusList.Select(s => ToEntity(s, null)).ToList(),
            MetaFields = query.MetaFields.ToEntity()
        };

    public static PostStatusEntity ToEntity(this PostStatusDto query, DateTimeOffset? modifiedAt)
        => new()
        {
            Id = query.Id,
            Title = query.Title,
            Slug = query.Slug,
            Tags = query.Tags.ToList(),
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = modifiedAt,
        };
}
