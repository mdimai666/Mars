using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.NavMenus;
using Mars.Host.Shared.Dto.Posts;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Dto.Search;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Shared.Contracts.XActions;
using static Mars.Host.Shared.Dto.Search.SearchFoundElement;

namespace Mars.Host.Services;

internal class CentralSearchService(
    IRequestContext _requestContext,
    IPostService _postService,
    IPostTypeService _postTypeService,
    INavMenuService _navMenuService,
    IActionManager _actionManager
    ) : ICentralSearchService
{

    public async Task<IReadOnlyCollection<SearchFoundElement>> ActionBarSearch(string query, int maxCount, CancellationToken cancellationToken)
    {
        bool isAdmin = _requestContext.IsAuthenticated && _requestContext.Roles.Contains("Admin");

        var (actions, postTypes, posts, navs) = await Aggregator(query, maxCount, cancellationToken);

        var items = (IReadOnlyCollection<SearchFoundElement>)[
            ..actions.Select(CreateAction),
            ..postTypes.Select(x=>CreateRecord(x.Title, PostTypeUrl(x), x.Id, x.TypeName, x.Tags.JoinStr(" "))),
            ..posts.Select(x=>CreateRecord(x.Title, PostUrl(x), x.Id, x.Type, x.Slug)),
            ..navs.Select(CreatePage)
            ];

        return items.OrderByDescending(s => s.Relevant).Take(maxCount).ToList();
    }

    async Task<(
        IReadOnlyCollection<XActionCommand> actions,
        IReadOnlyCollection<PostTypeSummary> postTypes,
        IReadOnlyCollection<PostSummary> posts,
        IReadOnlyCollection<NavMenuItemDto> navs
        )> Aggregator(string query, int maxCount, CancellationToken cancellationToken)
    {
        var actions = _actionManager.XActions.Values
                                    .Where(s => s.Label.Contains(query, StringComparison.OrdinalIgnoreCase) || s.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
                                    .Take(maxCount)
                                    .ToList();

        var postTypeQuery = new ListPostTypeQuery
        {
            Take = maxCount,
            Sort = nameof(PostTypeSummary.Title),
            Search = query
        };
        var postTypes = await _postTypeService.List(postTypeQuery, cancellationToken);

        var postQuery = new ListPostQuery
        {
            Take = maxCount,
            Sort = nameof(PostSummary.Title),
            Search = query,
            Type = null,
        };
        var posts = await _postService.List(postQuery, cancellationToken);

        var navs = _navMenuService.DevMenu().MenuItems.Where(s => !s.IsDivider && !s.IsHeader && !s.Disabled && !string.IsNullOrEmpty(s.Url))
                                                        .Where(s => (s.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                                                                || (s.Url.Contains(query, StringComparison.OrdinalIgnoreCase)))
                                                        .Take(maxCount)
                                                        .ToList();

        return (actions, postTypes.Items, posts.Items, navs);
    }

    string PostTypeUrl(PostTypeSummary postType) => $"/dev/EditPostType/{postType.Id}";
    string PostUrl(PostSummary post) => $"/dev/EditPost/{post.Type}/{post.Id}";

}
