using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;

namespace Mars.WebApiClient.Interfaces;

public interface IPostJsonServiceClient
{
    Task<PostJsonResponse?> Get(Guid id);
    Task<PostJsonResponse?> GetBySlug(string slug, string type);
    Task<ListDataResult<PostJsonResponse>> List(ListPostQueryRequest filter, string type);
    Task<PagingResult<PostJsonResponse>> ListTable(TablePostQueryRequest filter, string type);

}
