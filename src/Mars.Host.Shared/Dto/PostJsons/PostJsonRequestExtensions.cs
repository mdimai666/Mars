using Mars.Shared.Contracts.PostJsons;

namespace Mars.Host.Shared.Dto.PostJsons;

public static class PostJsonRequestExtensions
{
    public static CreatePostJsonQuery ToQuery(this CreatePostJsonRequest request, Guid userId)
        => new()
        {
            Id = request.Id,
            Title = request.Title,
            Type = request.Type,
            Slug = request.Slug ?? $"{request.Type}-{Guid.NewGuid()}",
            Tags = request.Tags,
            Content = request.Content,
            Status = request.Status,
            UserId = userId,
            Meta = request.Meta,
            Excerpt = request.Excerpt,
            LangCode = request.LangCode ?? string.Empty,
            CategoryIds = request.CategoryIds ?? [],
        };

    public static UpdatePostJsonQuery ToQuery(this UpdatePostJsonRequest request, Guid userId)
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
            Meta = request.Meta,
            Excerpt = request.Excerpt,
            LangCode = request.LangCode ?? string.Empty,
            CategoryIds = request.CategoryIds ?? []
        };
}
