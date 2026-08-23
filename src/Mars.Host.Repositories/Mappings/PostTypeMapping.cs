using Mars.Host.Data.Entities;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Shared.Contracts.PostTypes;

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
            EnabledFeatures = entity.EnabledFeatures,
            Disabled = entity.Disabled,
            Visibility = (PostTypeVisibility)entity.Visibility,
            ImageFieldKey = entity.ImageFieldKey,
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
            Visibility = (PostTypeVisibility)entity.Visibility,
            ImageFieldKey = entity.ImageFieldKey,
            PostStatusList = entity.Statuses!.OrderBy(s => s.Order).ToDto(),
            MetaFields = entity.MetaFields!.ToDto(),

            Presentation = entity.Presentation.ToDto(),
        };

    public static PostStatusDto ToDto(this PostStatusEntity entity)
        => new()
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Title = entity.Title,
            Color = entity.Color,
            Order = entity.Order,
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
            Visibility = (EPostTypeVisibility)query.Visibility,
            ImageFieldKey = query.ImageFieldKey,
            EnabledFeatures = query.EnabledFeatures.ToList(),
            Statuses = query.PostStatusList.Select(s => ToEntity(s, null)).ToList(),
            MetaFields = query.MetaFields.ToEntity()
        };

    public static PostStatusEntity ToEntity(this PostStatusDto query, DateTimeOffset? modifiedAt)
        => new()
        {
            Id = query.Id,
            Title = query.Title,
            Slug = query.Slug,
            Color = query.Color,
            Order = query.Order,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = modifiedAt,
        };

    public static PostTypeEntity UpdateEntity(this PostTypeEntity entity, UpdatePostTypeQuery query)
    {
        entity.Title = query.Title;
        entity.TypeName = query.TypeName;
        entity.Tags = query.Tags.ToList();
        entity.EnabledFeatures = query.EnabledFeatures.ToList();
        entity.Disabled = query.Disabled;
        entity.Visibility = (EPostTypeVisibility)query.Visibility;
        entity.ImageFieldKey = query.ImageFieldKey;

        entity.ModifiedAt = DateTimeOffset.Now;
        return entity;
    }

    public static PostTypePresentation ToDto(this PostTypePresentationEntity? entity)
        => entity is null
            ? PostTypePresentation.Default()
            : new()
            {
                ListViewTemplate = entity.ListViewTemplateSourceUri,
                Grid = PostTypeGridSettingsJson.Parse(entity.GridSettings),
            };

}
