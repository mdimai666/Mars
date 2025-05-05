using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.PostTypes;
using Mars.Host.Shared.Dto.Posts;
using Mars.Shared.Contracts.PostTypes;

namespace Mars.Host.Repositories.Mappings;

internal static class PostMapping
{
    public static PostSummary ToSummary(this PostEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            Type = entity.PostType!.TypeName,
            Tags = entity.Tags,
            Slug = entity.Slug,
            Author = entity.User!.ToPostAuthor(),
            Status = GetStatus(entity),
        };

    public static PostDetail ToDetail(this PostEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Type = entity.PostType!.TypeName,
            Slug = entity.Slug,
            Tags = entity.Tags,
            ModifiedAt = entity.ModifiedAt,
            Content = entity.Content,
            Author = entity.User!.ToPostAuthor(),
            MetaValues = entity.MetaValues!.ToDto(),
            Status = GetStatus(entity),
        };

    public static PostEditDetail ToEditDetail(this PostEntity entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Type = entity.PostType!.TypeName,
            Slug = entity.Slug,
            Tags = entity.Tags,
            ModifiedAt = entity.ModifiedAt,
            Content = entity.Content,
            Author = entity.User!.ToPostAuthor(),
            Status = entity.Status,
            Excerpt = entity.Excerpt,
            LangCode = entity.LangCode,
            MetaValues = entity.MetaValues!.ToDetailDto(),
        };

    public static KeyValuePair<string, string>? GetStatus(PostEntity entity)
    {
        if (entity.PostType.EnabledFeatures.Contains(PostTypeConstants.Features.Status))
        {
            var status = entity.PostType.PostStatusList.FirstOrDefault(s => s.Slug == entity.Status);
            if (status == null) return null;
            return new(status.Slug, status.Title);
        }
        return null;
    }

    public static IReadOnlyCollection<PostSummary> ToSummaryList(this IEnumerable<PostEntity> entities)
        => entities.Select(ToSummary).ToArray();

    public static IReadOnlyCollection<PostDetail> ToDetailList(this IEnumerable<PostEntity> entities)
        => entities.Select(ToDetail).ToArray();

    public static PostAuthor ToPostAuthor(this UserEntity entity)
        => new()
        {
            Id = entity.Id,
            UserName = entity.UserName,
            DisplayName = string.Join(' ', entity.LastName, entity.FirstName),
        };

    public static PostStatusDto ToDto(this PostStatusEntity entity)
        => new()
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Title = entity.Title,
            Tags = entity.Tags,
        };

    public static PostEntity ToEntity(this CreatePostQuery query, Guid postTypeId)
        => new()
        {
            Id = query.Id ?? Guid.Empty,
            Slug = query.Slug,
            Title = query.Title,
            //Type = query.Type,
            PostTypeId = postTypeId,
            Tags = query.Tags.ToList(),
            Status = query.Status ?? "",
            UserId = query.UserId,
            Content = query.Content,

            Excerpt = query.Excerpt,
            LangCode = query.LangCode,
            MetaValues = query.MetaValues.ToEntity(),
            
        };

}
