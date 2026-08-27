using Mars.Shared.Common;
using Mars.Shared.Contracts.Files;
using Mars.Shared.Contracts.Galleries;
using Microsoft.AspNetCore.Http;

namespace AppFront.Shared.Services.GallerySpace;

public class GalleryService : BasicClientService<GalleryDetailResponse>
{
    public GalleryService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _basePath = "/api/";
        _controllerName = "Gallery";
    }

    public Task<PagingResult<GalleryPhotoResponse>> PhotosListTable(Guid galleryId, BasicListQuery filter)
    {
        return _client.Post<PagingResult<GalleryPhotoResponse>>($"{_basePath}{_controllerName}/PhotosListTable/{galleryId}", filter)!;
    }

    public Task<UserActionResult<List<FileDetailResponse>>> PostPhotos(Guid galleryId, IFormFileCollection files)
    {
        return _client.Post<UserActionResult<List<FileDetailResponse>>>($"{_basePath}{_controllerName}/PostPhotos/{galleryId}/", files)!;
    }

    public Task<UserActionResult> DeletePhoto(Guid photoId)
    {
        return _client.DELETE<UserActionResult>($"{_basePath}{_controllerName}/DeletePhoto/{photoId}")!;
    }

}
