using Mars.Core.Utils;
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
            Categories = entity.Categories?.ToSummaryList(),
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
            Categories = entity.Categories!.ToSummaryList(),
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
            CategoryIds = entity.Categories!.Select(s => s.Id).ToList(),
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
    {
        var postEntity = new PostEntity()
        {
            Id = query.Id ?? Guid.Empty,
            Slug = query.Slug,
            Title = query.Title,
            PostTypeId = postTypeId,
            Tags = query.Tags.ToList(),
            Status = query.Status ?? "",
            UserId = query.UserId,
            Content = query.Content,

            Excerpt = query.Excerpt,
            LangCode = query.LangCode,
            MetaValues = query.MetaValues.ToEntity(),
        };
        postEntity.PostPostCategories = query.CategoryIds.Select(categoryId =>
            new PostPostCategoriesEntity
            {
                Post = postEntity,
                PostCategoryId = categoryId
            }).ToList();

        return postEntity;
    }

    public static PostEntity UpdateEntity(this PostEntity entity, UpdatePostQuery query)
    {
        entity.Title = query.Title;
        entity.Slug = query.Slug;
        entity.Tags = query.Tags.ToList();
        entity.Content = query.Content;
        entity.Excerpt = query.Excerpt;
        entity.Status = query.Status ?? "";
        entity.LangCode = query.LangCode;

        var existIds = entity.PostPostCategories!.Select(s => s.PostCategoryId).ToList();
        var categoryDiff = DiffList.FindDifferences(existIds, query.CategoryIds);
        if (categoryDiff.HasChanges)
        {
            foreach (var id in categoryDiff.ToRemove)
            {
                var item = entity.PostPostCategories!.First(s => s.PostCategoryId == id);
                entity.PostPostCategories.Remove(item);
            }
            foreach (var id in categoryDiff.ToAdd)
            {
                entity.PostPostCategories.Add(new()
                {
                    PostId = entity.Id,
                    PostCategoryId = id,
                });
            }
        }

        entity.ModifiedAt = DateTimeOffset.Now;
        return entity;
    }

    public static PostDetailWithType ToDetailWithType(this PostEntity entity)
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
            PostType = entity.PostType.ToDetail(),
            Categories = entity.Categories!.ToSummaryList(),
        };
}
