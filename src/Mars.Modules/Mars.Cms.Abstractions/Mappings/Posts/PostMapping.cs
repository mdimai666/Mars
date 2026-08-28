using Mars.Data.Extensions;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Mappings.MetaFields;
using Mars.Cms.Abstractions.Mappings.PostCategories;
using Mars.Contracts.Common;
using Mars.Cms.Contracts.Posts;

namespace Mars.Cms.Abstractions.Mappings.Posts;

public static class PostMapping
{
    public static PostSummaryResponse ToResponse(this PostSummary entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Title = entity.Title,
            Type = entity.Type,
            Slug = entity.Slug,
            Tags = entity.Tags,
            Author = entity.Author.ToResponse(),
            Categories = entity.Categories?.ToResponse(),
        };

    public static PostDetailResponse ToResponse(this PostDetail entity)
      => new()
      {
          Id = entity.Id,
          CreatedAt = entity.CreatedAt,
          Title = entity.Title,
          Type = entity.Type,
          Slug = entity.Slug,
          Tags = entity.Tags,
          Content = entity.Content,
          Author = entity.Author.ToResponse(),
          Categories = entity.Categories?.ToResponse(),
          MetaValues = entity.MetaValues.ToListResponse(),
      };

    public static PostListItemResponse ToListItemResponse(this PostSummary entity)
      => new()
      {
          Id = entity.Id,
          Title = entity.Title,
          Type = entity.Type,
          Slug = entity.Slug,
          CreatedAt = entity.CreatedAt,
          Tags = entity.Tags,
          Author = entity.Author.ToResponse(),
          Status = entity.Status,
          Categories = entity.Categories?.ToResponse(),
      };

    public static PostEditResponse ToResponse(this PostEditDetail entity)
        => new()
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            Title = entity.Title,
            Type = entity.Type,
            Slug = entity.Slug,
            Tags = entity.Tags,
            Author = entity.Author.ToResponse(),
            Status = entity.Status,
            Content = entity.Content,
            Excerpt = entity.Excerpt,
            LangCode = entity.LangCode,
            CategoryIds = entity.CategoryIds,
            MetaValues = entity.MetaValues.ToDetailResponse(),
        };

    public static ListDataResult<PostListItemResponse> ToResponse(this ListDataResult<PostSummary> postTypes)
        => postTypes.ToMap(ToListItemResponse);

    public static PagingResult<PostListItemResponse> ToResponse(this PagingResult<PostSummary> postTypes)
        => postTypes.ToMap(ToListItemResponse);

    public static PostAuthorResponse ToResponse(this PostAuthor entity)
        => new()
        {
            Id = entity.Id,
            UserName = entity.UserName,
            DisplayName = entity.DisplayName,
        };

}
