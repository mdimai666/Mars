using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Shared.Dto.PostTypes;

public static class PostTypeRequestExtensions
{
    public static CreatePostTypeQuery ToQuery(this CreatePostTypeRequest request)
        => new()
        {
            Title = request.Title,
            TypeName = request.TypeName,
            Id = request.Id,
            Tags = request.Tags,
            Disabled = request.Disabled,
            EnabledFeatures = request.EnabledFeatures,
            PostContentSettings = request.PostContentSettings.ToDto(),
            PostStatusList = request.PostStatusList.ToDto(),
            MetaFields = request.MetaFields.ToDto()
        };

    public static UpdatePostTypeQuery ToQuery(this UpdatePostTypeRequest request)
        => new()
        {
            Title = request.Title,
            TypeName = request.TypeName,
            Id = request.Id,
            Tags = request.Tags,
            Disabled = request.Disabled,
            EnabledFeatures = request.EnabledFeatures,
            PostContentSettings = request.PostContentSettings.ToDto(),
            PostStatusList = request.PostStatusList.ToDto(),
            MetaFields = request.MetaFields.ToDto()
        };

    public static ListPostTypeQuery ToQuery(this ListPostTypeQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListPostTypeQuery ToQuery(this TablePostTypeQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static PostContentSettingsDto ToDto(this CreatePostContentSettingsRequest request)
        => new()
        {
            PostContentType = request.PostContentType,
            CodeLang = request.CodeLang,
        };

    public static PostContentSettingsDto ToDto(this UpdatePostContentSettingsRequest request)
        => new()
        {
            PostContentType = request.PostContentType,
            CodeLang = request.CodeLang,
        };

    public static PostStatusDto ToDto(this CreatePostStatusRequest request)
        => new()
        {
            Id = request.Id,
            Slug = request.Slug,
            Tags = request.Tags,
            Title = request.Title
        };

    public static PostStatusDto ToDto(this UpdatePostStatusRequest request)
        => new()
        {
            Id = request.Id,
            Slug = request.Slug,
            Tags = request.Tags,
            Title = request.Title
        };

    public static IReadOnlyCollection<PostStatusDto> ToDto(this IReadOnlyCollection<CreatePostStatusRequest> entities)
        => entities.Select(ToDto).ToList();

    public static IReadOnlyCollection<PostStatusDto> ToDto(this IReadOnlyCollection<UpdatePostStatusRequest> entities)
        => entities.Select(ToDto).ToList();
}
