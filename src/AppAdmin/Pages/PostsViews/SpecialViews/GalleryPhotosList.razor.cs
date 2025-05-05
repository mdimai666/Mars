using AntDesign;
using AppFront.Shared.Interfaces;
using AppFront.Shared.Services.GallerySpace;
using AppShared.Dto.Gallery;
using AppShared.Models;
using Microsoft.AspNetCore.Components;

namespace AppAdmin.Pages.PostsViews.SpecialViews;

public partial class GalleryPhotosList
{
    [Inject] GalleryService galleryService { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] public ConfirmService _confirmService { get; set; } = default!;


    [Parameter] public Guid GalleryId { get; set; }
    [Parameter] public EventCallback<TotalResponse<GalleryPhoto>> OnLoad { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public bool DisableUpload { get; set; }

    [Parameter]
    public RenderFragment<GalleryPhoto>? ItemActionBottom { get; set; } = null;

    GalleryFluentFileUploader1 galleryFluentFileUploader1 = default!;

    int _currentPage = 1;

    public int CurrentPage => _currentPage;
    public int PageSize { get; set; } = 40;


    TotalResponse<GalleryPhoto>? photos = new();
    bool photosIsLoad = false;

    public async void Refresh()
    {
        //photos = await postService.ListTableJson<PhotoPost>("photo",
        //    new QueryFilterBasic() { Take = photos_size, Skip = (photos_page - 1) * photos_size });

        photos = await galleryService.PhotosListTable(GalleryId, new QueryFilter(CurrentPage, PageSize));

        StateHasChanged();

        _ = OnLoad.InvokeAsync(photos);
    }

    void OnAfterUpload(List<FileEntity> files)
    {
        Refresh();
    }

    string? ImageThumbUrl(GalleryPhoto post)
    {
        return post.Image
            ?.Meta.Thumbnails["md"]?.FileUrlFull
            ?? post.Image?.FileUrl;
    }

    async Task<bool> DeletePhoto(Guid photoId)
    {
        var res = await galleryService.DeletePhoto(photoId);

        if (res.Ok)
        {
            _ = messageService.Success(res.Message);
        }
        else
        {
            _ = messageService.Error(res.Message);
        }

        return res.Ok;
    }

    async void ItemRemoveClick(GalleryPhoto photo)
    {
        var confirm = await _confirmService.Show("Потдтвердите действие", "Уверены что хотите удалить?", ConfirmButtons.OKCancel, ConfirmIcon.Warning);

        if (confirm == ConfirmResult.OK)
        {
            var result = await DeletePhoto(photo.Id);

            if (result)
            {
                Refresh();
            }

            StateHasChanged();
        }
    }

    public void PaginatorChange(PaginationEventArgs e)
    {
        _currentPage = e.Page;
        PageSize = e.PageSize;
        Refresh();
    }

}
