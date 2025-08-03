using EditorJsBlazored.Core;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Attributes;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using static Mars.Shared.Contracts.PostTypes.PostTypeConstants;

namespace Mars.Services;

[KeyredHandler(key: DefaultPostContentTypes.BlockEditor, Tags = ["post"])]
internal class BlockEditor1PostContentProcessor(IPostRepository postRepository, IMetaModelTypesLocator metaModelTypesLocator) : IPostContentProcessor
{

    public async Task<string?> RenderPostContent(Guid postId, CancellationToken cancellationToken)
    {
        var post = await postRepository.GetDetail(postId, cancellationToken) ?? throw new NotFoundException($"postId '{postId}' not found");
        var postType = metaModelTypesLocator.GetPostTypeByName(post.Type) ?? throw new NotFoundException($"postType '{post.Type}' not found"); ;

        return await RenderPostContent(postType, post.Content ?? "", cancellationToken);
    }

    public Task<string?> RenderPostContent(PostTypeDetail postType, string content, CancellationToken cancellationToken)
    {
        if (postType.PostContentSettings.PostContentType != DefaultPostContentTypes.BlockEditor)
            throw new NotSupportedException($"{nameof(BlockEditor1PostContentProcessor)} is support only '{DefaultPostContentTypes.BlockEditor}'. Retrive PostType '{postType.TypeName}'");

        if (string.IsNullOrEmpty(content)) return Task.FromResult<string?>(null);

        var editorContent = EditorJsContent.FromJsonAutoConvertToRawBlockOnException(content, out var _);

        return Task.FromResult(EditorTools.RenderToHtml(editorContent))!;
    }
}
