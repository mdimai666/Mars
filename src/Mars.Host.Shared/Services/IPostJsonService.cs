using Mars.Host.Shared.Dto.PostJsons;
using Mars.Host.Shared.Dto.Posts;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Services;

public interface IPostJsonService
{
    Task<PostJsonDto?> GetDetail(Guid id, bool renderContent = true, CancellationToken cancellationToken = default);
    Task<PostJsonDto?> GetDetailBySlug(string slug, string type, bool renderContent = true, CancellationToken cancellationToken = default);
    Task<ListDataResult<PostJsonDto>> List(ListPostQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostJsonDto>> ListTable(ListPostQuery query, CancellationToken cancellationToken);
    Task<PostJsonDto> Create(CreatePostJsonQuery query, CancellationToken cancellationToken);
    Task<PostJsonDto> Update(UpdatePostJsonQuery query, CancellationToken cancellationToken);
}
