using Mars.Cms.Abstractions.Dto.PostJsons;
using Mars.Cms.Abstractions.Dto.Posts;
using Mars.Contracts.Common;

namespace Mars.Cms.Abstractions.Services;

public interface IPostJsonService
{
    Task<PostJsonDto?> GetDetail(Guid id, bool renderContent = true, CancellationToken cancellationToken = default);
    Task<PostJsonDto?> GetDetailBySlug(string slug, string type, bool renderContent = true, CancellationToken cancellationToken = default);
    Task<ListDataResult<PostJsonDto>> List(ListPostQuery query, CancellationToken cancellationToken);
    Task<PagingResult<PostJsonDto>> ListTable(ListPostQuery query, CancellationToken cancellationToken);
    Task<PostJsonDto> Create(CreatePostJsonQuery query, CancellationToken cancellationToken);
    Task<PostJsonDto> Update(UpdatePostJsonQuery query, CancellationToken cancellationToken);
}
