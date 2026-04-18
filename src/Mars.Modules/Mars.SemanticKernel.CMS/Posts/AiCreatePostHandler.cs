using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Core.Features;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Services;

namespace Mars.SemanticKernel.CMS.Posts;

internal class AiCreatePostHandler(IPostService postService, IMetaModelTypesLocator metaModelTypesLocator) : IAiCreatePostHandler
{
    public Task<PostDetail> Handle(AiCreatePostQuery query, CancellationToken cancellationToken)
    {
        var postType = metaModelTypesLocator.GetPostTypeByName(query.Type)
                        ?? throw MarsValidationException.FromSingleError(nameof(query.Type), $"post type '{query.Type}' not exist");

        var blankPost = postService.GetPostBlank(postType);

        var createPostQuery = new CreatePostQuery
        {
            Title = query.Title,
            Content = query.Content,
            Type = "post",
            Slug = TextTool.TranslateToPostSlug(query.Title),
            Status = blankPost.Status,
            LangCode = blankPost.LangCode,
            CategoryIds = blankPost.CategoryIds,
            MetaValues = [],
            Excerpt = blankPost.Content?.TextEllipsis(100),
            Tags = ["ai-generated", .. query.Tags, .. blankPost.Tags],
            UserId = blankPost.Author.Id
        };

        return postService.Create(createPostQuery, cancellationToken);
    }
}

public record AiCreatePostQuery
{
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string Type { get; init; } = "post";
    public string[] Tags { get; init; } = [];
}

public interface IAiCreatePostHandler
{
    Task<PostDetail> Handle(AiCreatePostQuery query, CancellationToken cancellationToken);
}
