using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Dto.Search;
using Mars.Cms.Abstractions.Services;
using static Mars.Cms.Abstractions.Dto.Search.SearchFoundElement;

namespace Mars.Cms.Host.Services;

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
