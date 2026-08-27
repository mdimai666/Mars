using Mars.Host.Shared.Dto.PostTypes;

namespace Mars.Host.Shared.Services;

/// <summary>
/// locator is <see cref="IPostContentProcessorsLocator"/>
/// </summary>
public interface IPostContentProcessor
{
    public Task<string?> RenderPostContent(Guid postId, CancellationToken cancellationToken);
    public Task<string?> RenderPostContent(PostTypeDetail postType, string content, CancellationToken cancellationToken);
}
