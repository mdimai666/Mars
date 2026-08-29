using Mars.Admin.Framework.Components.MediaViews;
using Mars.Media.Contracts.Files;
using Mars.WebApiClient.Interfaces;

namespace Mars.Admin.Framework.Services;

public interface IAppMediaService
{
    Task<FileDetailResponse?> Get(Guid id);
    Task<PagingResult<FileListItemResponse>> ListTable(TableFileQueryRequest request);
    Task Delete(Guid id);
    Task<FileListItemResponse> OpenSelectMedia();
    Task<IReadOnlyCollection<FileListItemResponse>> OpenSelectMediaMany();

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

    public async Task<IReadOnlyCollection<FileListItemResponse>> OpenSelectMediaMany()
    {
        return await ModalMediaSelect.ShowModalForSelectMany();
    }
}
