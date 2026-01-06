using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Shared.Common;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Mappings.PostTypes;

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
            EnabledFeatures = entity.EnabledFeatures,
            PostContentSettings = entity.PostContentSettings.ToResponse(),
            PostStatusList = entity.PostStatusList.ToResponse(),
            MetaFields = entity.MetaFields.ToDetailResponse(),
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
            //EnabledFeatures = entity.EnabledFeatures,
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
        };

    public static PostStatusResponse ToResponse(this PostStatusDto entity)
        => new()
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Title = entity.Title,
            Tags = entity.Tags,
        };

    public static PostContentSettingsResponse ToResponse(this PostContentSettingsDto entity)
        => new()
        {
            CodeLang = entity.CodeLang,
            PostContentType = entity.PostContentType,
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
            Presentation = entity.Presentation.ToResponse(),
        };
}
