using Mars.Cms.Abstractions.Dto.Posts;

namespace Mars.Cms.Abstractions.Services;

public interface IPostTransformer
{
    /// <summary>
    /// Transform post content. e.t.c to html
    /// </summary>
    /// <param name="post">postDetail</param>
    /// <param name="cancellationToken">cancellation Token</param>
    /// <returns>transformed post</returns>
    Task<PostDetail> Transform(PostDetail post, CancellationToken cancellationToken);
}
