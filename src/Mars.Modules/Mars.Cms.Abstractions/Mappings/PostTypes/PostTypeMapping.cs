using Mars.Data.Extensions;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Mappings.MetaFields;
using Mars.Contracts.Common;
using Mars.Cms.Contracts.PostTypes;

namespace Mars.Cms.Abstractions.Mappings.PostTypes;

public static class PostTypeMapping
{
    public static PostTypeSummaryResponse ToResponse(this PostTypeSummary entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            EnabledFeatures = entity.EnabledFeatures,
            Visibility = entity.Visibility,
            ImageFieldKey = entity.ImageFieldKey,
            //MetaFields = entity.MetaFields.ToResponse(),
        };

    public static PostTypeDetailResponse ToResponse(this PostTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            Disabled = entity.Disabled,
            Visibility = entity.Visibility,
            EnabledFeatures = entity.EnabledFeatures,
            PostStatusList = entity.PostStatusList.ToResponse(),
            MetaFields = entity.MetaFields.ToDetailResponse(),
            ImageFieldKey = entity.ImageFieldKey,
        };

    public static PostTypeSummary ToSummary(this PostTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            EnabledFeatures = entity.EnabledFeatures,
            Disabled = entity.Disabled,
            Visibility = entity.Visibility,
            ImageFieldKey = entity.ImageFieldKey,
        };

    public static PostTypeSummaryResponse ToSummaryResponse(this PostTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            EnabledFeatures = entity.EnabledFeatures,
            Visibility = entity.Visibility,
            ImageFieldKey = entity.ImageFieldKey,
        };

    public static PostTypeListItemResponse ToItemResponse(this PostTypeSummary entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            TypeName = entity.TypeName,
            CreatedAt = entity.CreatedAt,
            EnabledFeatures = entity.EnabledFeatures,
            Tags = entity.Tags,
            Disabled = entity.Disabled,
            Visibility = entity.Visibility,
            ImageFieldKey = entity.ImageFieldKey,
        };

    public static PostStatusResponse ToResponse(this PostStatusDto entity)
        => new()
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Title = entity.Title,
            Color = entity.Color,
            Order = entity.Order,
        };

    public static ListDataResult<PostTypeListItemResponse> ToResponse(this ListDataResult<PostTypeSummary> postTypes)
        => postTypes.ToMap(ToItemResponse);

    public static PagingResult<PostTypeListItemResponse> ToResponse(this PagingResult<PostTypeSummary> postTypes)
        => postTypes.ToMap(ToItemResponse);

    public static IReadOnlyCollection<PostTypeSummaryResponse> ToResponse(this IReadOnlyCollection<PostTypeSummary> list)
        => list.Select(ToResponse).ToList();

    public static IReadOnlyCollection<PostStatusResponse> ToResponse(this IReadOnlyCollection<PostStatusDto> list)
        => list.Select(ToResponse).ToList();

    public static PostTypeAdminPanelItemResponse ToAdminPanelItemResponse(this PostTypeDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            TypeName = entity.TypeName,
            Tags = entity.Tags,
            EnabledFeatures = entity.EnabledFeatures,
            Visibility = entity.Visibility,
            ImageFieldKey = entity.ImageFieldKey,
            Presentation = entity.Presentation.ToResponse(),
        };
}
