using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Mappings.MetaFields;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;

namespace Mars.Host.Shared.Mappings.Posts;

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
            Author = entity.Author.ToResponse(),
        };

    public static PostDetailResponse ToResponse(this PostDetail entity)
      => new()
      {
          Id = entity.Id,
          CreatedAt = entity.CreatedAt,
          Title = entity.Title,
          Type = entity.Type,
          Slug = entity.Slug,
          Content = entity.Content,
          Author = entity.Author.ToResponse(),
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
