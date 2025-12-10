using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Posts;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Repositories;

public interface IPostRepository : IDisposable
{
    Task<PostSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<PostDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<PostDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken);
    Task<PostEditDetail?> GetPostEditDetail(Guid id, CancellationToken cancellationToken);

    Task<Guid> Create(CreatePostQuery query, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException"/>
    Task Update(UpdatePostQuery query, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException"/>
    Task Delete(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PostSummary>> ListAll(ListAllPostQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PostDetail>> ListAllDetail(ListAllPostQuery query, CancellationToken cancellationToken);
    //Task<IReadOnlyCollection<PostSummary>> ListAllActive(CancellationToken cancellationToken);
    Task<ListDataResult<PostSummary>> List(ListPostQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostSummary>> ListTable(ListPostQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<PostDetail>> ListDetail(ListPostQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostDetail>> ListTableDetail(ListPostQuery query, CancellationToken cancellationToken);
    Task<PostDetailWithType?> PostDetailWithType(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistAsync(string typeName, string slug, CancellationToken cancellationToken);
}
