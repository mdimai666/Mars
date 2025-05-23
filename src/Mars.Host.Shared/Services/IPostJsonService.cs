using Mars.Host.Shared.Dto.Posts;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Services;

public interface IPostJsonService
{
    Task<PostJsonDto?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<PostJsonDto?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken);
    Task<ListDataResult<PostJsonDto>> List(ListPostQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostJsonDto>> ListTable(ListPostQuery query, CancellationToken cancellationToken);

}
