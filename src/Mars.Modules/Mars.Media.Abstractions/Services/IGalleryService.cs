using Mars.Media.Abstractions.Dto.Galleries;
using Mars.Contracts.Common;

namespace Mars.Media.Abstractions.Services;

public interface IGalleryService
{
    Task<GallerySummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<GalleryDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<GalleryDetail?> GetDetailBySlug(string slug, string type, CancellationToken cancellationToken);
    Task<ListDataResult<GallerySummary>> List(ListGalleryQuery query, CancellationToken cancellationToken);
    Task<PagingResult<GallerySummary>> ListTable(ListGalleryQuery query, CancellationToken cancellationToken);
    Task<GalleryDetail> Create(CreateGalleryQuery query, CancellationToken cancellationToken);
    //Task<GalleryEditModel> GetEditModel(Guid id);
    Task<GalleryDetail> Update(UpdateGalleryQuery query, CancellationToken cancellationToken);
    Task<UserActionResult> Delete(Guid id, CancellationToken cancellationToken);
}
