using Mars.Cms.Abstractions.Dto.MetaFields;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Server.Abstractions.Extensions;
using Mars.Contracts.PostTypes;

namespace Mars.Cms.Abstractions.Dto.PostTypes;

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
            Visibility = request.Visibility,
            ImageFieldKey = request.ImageFieldKey,
            EnabledFeatures = request.EnabledFeatures,
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
            Visibility = request.Visibility,
            ImageFieldKey = request.ImageFieldKey,
            EnabledFeatures = request.EnabledFeatures,
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
            IncludeComponent = request.IncludeComponent,
        };

    public static ListPostTypeQuery ToQuery(this TablePostTypeQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
            IncludeComponent = request.IncludeComponent,
        };

    public static PostStatusDto ToDto(this CreatePostStatusRequest request)
        => new()
        {
            Id = request.Id,
            Slug = request.Slug,
            Title = request.Title,
            Color = request.Color,
            Order = request.Order,
        };

    public static PostStatusDto ToDto(this UpdatePostStatusRequest request)
        => new()
        {
            Id = request.Id,
            Slug = request.Slug,
            Title = request.Title,
            Color = request.Color,
            Order = request.Order,
        };

    public static IReadOnlyCollection<PostStatusDto> ToDto(this IReadOnlyCollection<CreatePostStatusRequest> entities)
        => entities.Select(ToDto).ToList();

    public static IReadOnlyCollection<PostStatusDto> ToDto(this IReadOnlyCollection<UpdatePostStatusRequest> entities)
        => entities.Select(ToDto).ToList();
}
