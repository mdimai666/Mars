using Mars.Cms.Contracts.PostJsons;
using Mars.Cms.Contracts.Posts;
using Mars.Contracts.Common;

namespace Mars.WebApiClient.Interfaces;

public interface IPostJsonServiceClient
{
    Task<PostJsonResponse?> Get(Guid id, bool renderContent = true);
    Task<PostJsonResponse?> GetBySlug(string slug, string type, bool renderContent = true);
    Task<ListDataResult<PostJsonResponse>> List(ListPostQueryRequest filter, string type);
    Task<PagingResult<PostJsonResponse>> ListTable(TablePostQueryRequest filter, string type);
    Task<PostJsonResponse> Create(CreatePostJsonRequest request);
    Task<PostJsonResponse> Update(UpdatePostJsonRequest request);
}
