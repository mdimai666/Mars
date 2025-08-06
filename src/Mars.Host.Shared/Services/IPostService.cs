using Mars.Host.Shared.Dto.Posts;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Posts;

namespace Mars.Host.Shared.Services;

public interface IPostService
{
    Task<PostSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<PostDetail?> GetDetail(Guid id, bool renderContent = true, CancellationToken cancellationToken = default);
    Task<PostDetail?> GetDetailBySlug(string slug, string type, bool renderContent = true, CancellationToken cancellationToken = default);
    Task<ListDataResult<PostSummary>> List(ListPostQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostSummary>> ListTable(ListPostQuery query, CancellationToken cancellationToken);
    Task<PostDetail> Create(CreatePostQuery query, CancellationToken cancellationToken);
    Task<PostEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken);
    Task<PostEditViewModel> GetEditModelBlank(string type, CancellationToken cancellationToken);
    Task<PostDetail> Update(UpdatePostQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken);
    CreatePostQuery EnrichQuery(CreatePostRequest request);
    UpdatePostQuery EnrichQuery(UpdatePostRequest request);

}
