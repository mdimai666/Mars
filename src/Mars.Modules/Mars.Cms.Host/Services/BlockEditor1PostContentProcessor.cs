using EditorJsBlazored.Core;
using Mars.Cms.Abstractions.Attributes;
using Mars.Core.Exceptions;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Contracts.MetaFields;

namespace Mars.Cms.Host.Services;

[KeyredHandler(key: MetaFieldEditorCatalog.BlockEditor, Tags = ["post"])]
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
        if (postType.ContentEditorKey() != MetaFieldEditorCatalog.BlockEditor)
            throw new NotSupportedException($"{nameof(BlockEditor1PostContentProcessor)} is support only '{MetaFieldEditorCatalog.BlockEditor}'. Retrieved PostType '{postType.TypeName}'");

        if (string.IsNullOrEmpty(content)) return Task.FromResult<string?>(null);

        var editorContent = EditorJsContent.FromJsonAutoConvertToBlocks(content, out var _);

        return Task.FromResult(EditorTools.RenderToHtml(editorContent))!;
    }
}
