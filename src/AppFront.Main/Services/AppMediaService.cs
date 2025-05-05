using AppFront.Shared.Components.MediaViews;
using Mars.Shared.Contracts.Files;
using Mars.WebApiClient.Interfaces;

namespace AppFront.Shared.Services;

public interface IAppMediaService
{
    Task<FileDetailResponse?> Get(Guid id);
    Task<PagingResult<FileListItemResponse>> ListTable(TableFileQueryRequest request);
    Task Delete(Guid id);
    Task<FileListItemResponse> OpenSelectMedia();
    Task<UserActionResult> ExecuteAction(ExecuteActionRequest action);

}

public class AppMediaService : IAppMediaService
{
    private readonly IMarsWebApiClient _client;
    public static ModalMediaSelect ModalMediaSelect { get; set; } = default!;

    public AppMediaService(IMarsWebApiClient client)
    {
        _client = client;
    }

    public Task<FileDetailResponse?> Get(Guid id)
    {
        return _client.Media.Get(id);
    }

    public Task<PagingResult<FileListItemResponse>> ListTable(TableFileQueryRequest request)
    {
        return _client.Media.ListTable(request);
    }

    public Task Delete(Guid id)
    {
        return _client.Media.Delete(id);
    }

    public async Task<FileListItemResponse> OpenSelectMedia()
    {
        //var re = await modalService.CreateModalAsync<FileEntity>(new ModalOptions
        //{
        //    Footer=null,
        //    Title = "Выбор файла",
        //    Width = "70%",
        //    Content = new MediaFilesList()
        //});


        //await re.OpenAsync();

        return (await ModalMediaSelect.ShowModalForSelect())!;

    }

    public Task<UserActionResult> ExecuteAction(ExecuteActionRequest action)
    {
        return _client.Media.ExecuteAction(action);
    }
}
