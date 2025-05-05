using Mars.Host.Shared.Dto.MetaFields;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Extensions;
using Mars.Shared.Contracts.Posts;

namespace Mars.Host.Shared.Dto.Posts;

public static class PostRequestExtensions
{
    public static CreatePostQuery ToQuery(this CreatePostRequest request, Guid userId, IDictionary<Guid, MetaFieldDto> metaFields)
        => new()
        {
            Id = request.Id,
            Title = request.Title,
            Type = request.Type,
            Slug = request.Slug,
            Tags = request.Tags,
            Content = request.Content,
            Status = request.Status,
            UserId = userId,
            MetaValues = request.MetaValues.ToQuery(metaFields),
            Excerpt = request.Excerpt,
            LangCode = request.LangCode,
        };

    public static UpdatePostQuery ToQuery(this UpdatePostRequest request, Guid userId, IDictionary<Guid, MetaFieldDto> metaFields)
        => new()
        {
            Id = request.Id,
            Title = request.Title,
            Type = request.Type,
            Slug = request.Slug,
            Tags = request.Tags,
            Content = request.Content,
            Status = request.Status,
            UserId = userId,
            MetaValues = request.MetaValues.ToQuery(metaFields),
            Excerpt = request.Excerpt,
            LangCode = request.LangCode,
        };

    public static ListPostQuery ToQuery(this ListPostQueryRequest request, string? postTypeName)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,

            Type = postTypeName,
        };

    public static ListPostQuery ToQuery(this TablePostQueryRequest request, string? postTypeName)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,

            Type = postTypeName,
        };

}
