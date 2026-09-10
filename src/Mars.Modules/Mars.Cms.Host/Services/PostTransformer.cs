using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Services;
using Mars.Core.Exceptions;

namespace Mars.Cms.Host.Services;

internal class PostTransformer : IPostTransformer
{
    private readonly IMetaModelTypesLocator _metaModelTypesLocator;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPostContentProcessorsLocator _postContentProcessorsLocator;

    public PostTransformer(IMetaModelTypesLocator metaModelTypesLocator,
                            IServiceProvider serviceProvider,
                            IPostContentProcessorsLocator postContentProcessorsLocator)
    {
        _metaModelTypesLocator = metaModelTypesLocator;
        _serviceProvider = serviceProvider;
        _postContentProcessorsLocator = postContentProcessorsLocator;
    }

    public async Task<PostDetail> Transform(PostDetail post, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(post.Content)) return post;
        var postType = _metaModelTypesLocator.GetPostTypeByName(post.Type) ?? throw new NotFoundException($"PostType '{post.Type}' not found");

        var editorKey = postType.ContentEditorKey();
        if (string.IsNullOrEmpty(editorKey)) return post; // обычный текст — рендерить нечем

        var postContentProcessor = _postContentProcessorsLocator.GetProvider(editorKey, _serviceProvider);
        if (postContentProcessor is null) return post;
        var content = await postContentProcessor.RenderPostContent(postType, post.Content, cancellationToken);

        return post with
        {
            Content = content,
        };
    }
}
