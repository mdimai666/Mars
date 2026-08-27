using Mars.Core.Extensions;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Abstractions.Dto.Search;
using Mars.Cms.Abstractions.Services;
using Mars.Cms.Host.Services;
using static Mars.Cms.Abstractions.Dto.Search.SearchFoundElement;

namespace Mars.Cms.Host.Services;

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
