using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.Search;
using Mars.Host.Shared.Services;
using static Mars.Host.Shared.Dto.Search.SearchFoundElement;

namespace Mars.Host.Services;

internal class PostsSearchProvider(
    IPostService _postService
    ) : ICentralSearchProvider
{
    public int Order => 20;

    public async Task<IReadOnlyCollection<SearchFoundElement>> SearchAsync(string query, int maxCount, CancellationToken cancellationToken)
    {
        var postQuery = new ListPostQuery
        {
            Take = maxCount,
            Sort = nameof(PostSummary.Title),
            Search = query,
            Type = null,
        };
        var posts = await _postService.List(postQuery, cancellationToken);

        return posts.Items
            .Select(x => CreateRecord(x.Title, PostUrl(x), x.Id, x.Type, x.Slug))
            .ToList();
    }

    static string PostUrl(PostSummary post) => $"/dev/EditPost/{post.Type}/{post.Id}";
}
