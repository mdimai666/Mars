using Mars.Core.Extensions;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Dto.Search;
using Mars.Host.Shared.Services;
using static Mars.Host.Shared.Dto.Search.SearchFoundElement;

namespace Mars.Host.Services;

internal class PostTypesSearchProvider(
    IPostTypeService _postTypeService
    ) : ICentralSearchProvider
{
    public int Order => 10;

    public async Task<IReadOnlyCollection<SearchFoundElement>> SearchAsync(string query, int maxCount, CancellationToken cancellationToken)
    {
        var postTypeQuery = new ListPostTypeQuery
        {
            Take = maxCount,
            Sort = nameof(PostTypeSummary.Title),
            Search = query
        };
        var postTypes = await _postTypeService.List(postTypeQuery, cancellationToken);

        return postTypes.Items
            .Select(x => CreateRecord(x.Title, PostTypeUrl(x), x.Id, x.TypeName, x.Tags.JoinStr(" ")))
            .ToList();
    }

    static string PostTypeUrl(PostTypeSummary postType) => $"/dev/EditPostType/{postType.Id}";
}
