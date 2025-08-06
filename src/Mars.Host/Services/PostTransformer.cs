using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Services;

namespace Mars.Host.Services;

internal class PostTransformer : IPostTransformer
{
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IPostContentProcessorsLocator _postContentProcessorsLocator;

    public PostTransformer(IMetaModelTypesLocator metaModelTypesLocator,
                            IPostContentProcessorsLocator postContentProcessorsLocator)
    {
        _metaModelTypesLocator = metaModelTypesLocator;
        _postContentProcessorsLocator = postContentProcessorsLocator;
    }

    public async Task<PostDetail> Transform(PostDetail post, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(post.Content)) return post;
        var postType = _metaModelTypesLocator.GetPostTypeByName(post.Type) ?? throw new NotFoundException($"PostType '{post.Type}' not found");
        var postContentProcessor = _postContentProcessorsLocator.GetProvider(postType.PostContentSettings.PostContentType);
        if (postContentProcessor is null) return post;
        var content = await postContentProcessor.RenderPostContent(postType, post.Content, cancellationToken);

        return post with
        {
            Content = content,
        };
    }
}
