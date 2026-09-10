using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Cms.Abstractions.Dto.PostTypes;
using Mars.Cms.Contracts.Posts;
using Mars.Contracts.Common;

namespace Mars.Cms.Abstractions.Services;

public interface IPostService
{
    Task<PostSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<PostDetail?> GetDetail(Guid id, bool renderContent = true, CancellationToken cancellationToken = default);
    Task<PostDetail?> GetDetailBySlug(string slug, string type, bool renderContent = true, CancellationToken cancellationToken = default);
    Task<ListDataResult<PostSummary>> List(ListPostQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostSummary>> ListTable(ListPostQuery query, CancellationToken cancellationToken);
    Task<PostDetail> Create(CreatePostQuery query, CancellationToken cancellationToken);
    Task<PostDetail> GetOrCreateSingleAsync(string typeName, CancellationToken cancellationToken);
    Task<PostEditViewModel> GetEditModel(Guid id, CancellationToken cancellationToken);
    Task<PostEditViewModel> GetEditModelBlank(string type, CancellationToken cancellationToken);
    PostEditDetail GetPostBlank(PostTypeDetail postType);
    Task<PostDetail> Update(UpdatePostQuery query, CancellationToken cancellationToken);
    Task<PostSummary> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostSummary>> DeleteMany(DeleteManyPostQuery query, CancellationToken cancellationToken);
    CreatePostQuery EnrichQuery(CreatePostRequest request);
    UpdatePostQuery EnrichQuery(UpdatePostRequest request);
}
