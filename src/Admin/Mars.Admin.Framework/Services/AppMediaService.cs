using Mars.Media.Contracts.Files;
using Mars.WebApiClient.Interfaces;

namespace Mars.Admin.Framework.Services;

public interface IAppMediaService
{
    Task<FileDetailResponse?> Get(Guid id);
    Task<FileListItemResponse> OpenSelectMedia();
    Task<IReadOnlyCollection<FileListItemResponse>> OpenSelectMediaMany();
}

public class AppMediaService : IAppMediaService
{
    private readonly IMarsWebApiClient _client;

    // Своего диалога выбора у Framework нет — хендлеры регистрирует хост-приложение
    // (Mars.Admin: MediaSelectRegistrar в App.razor, показ через IDialogService).
    public static Func<Task<FileListItemResponse?>>? SelectFileHandler { get; set; }
    public static Func<Task<IReadOnlyCollection<FileListItemResponse>>>? SelectFilesHandler { get; set; }

    public AppMediaService(IMarsWebApiClient client)
    {
        _client = client;
    }

    public Task<FileDetailResponse?> Get(Guid id)
    {
        return _client.Media.Get(id);
    }

    public async Task<FileListItemResponse> OpenSelectMedia()
    {
        return SelectFileHandler is null ? null! : (await SelectFileHandler())!;
    }

    public async Task<IReadOnlyCollection<FileListItemResponse>> OpenSelectMediaMany()
    {
        return SelectFilesHandler is null ? [] : await SelectFilesHandler();
    }
}
