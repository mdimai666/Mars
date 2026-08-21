using Mars.Shared.Common;
using Mars.Shared.Contracts.Files;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;
using Flurl.Http.Content;

namespace Mars.WebApiClient.Implements;

internal class MediaServiceClient : BasicServiceClient, IMediaServiceClient
{
    public MediaServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Media";
    }

    public Task<FileDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<FileDetailResponse?>();

    public Task<FileDetailResponse> Upload(Stream stream, string fileName, Guid? folderId = null)
    {
        var request = _client.Request($"{_basePath}{_controllerName}", "Upload");
        if (folderId is not null)
        {
            request = request.AppendQueryParam("folderId", folderId.Value.ToString());
        }

        return request.PostMultipartAsync(mp => mp
                        //.AddFile("file", GenerateStreamFromString(fileContent), fileName)
                        .AddFile("file", stream, fileName)
                    )
                    .ReceiveJson<FileDetailResponse>();
    }

    //public Task Update(UpdateMediaRequest request)
    //    => _client.Request($"{_basePath}{_controllerName}")
    //                .PutJsonAsync(request);

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task DeleteMany(Guid[] ids)
        => _client.Request($"{_basePath}{_controllerName}/DeleteMany")
                    .AppendQueryParam(new { ids })
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<ListDataResult<FileListItemResponse>> List(ListFileQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/offset")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<FileListItemResponse>>();

    public Task<PagingResult<FileListItemResponse>> ListTable(TableFileQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/list/page")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<FileListItemResponse>>();

    public Task<UserActionResult> ExecuteAction(ExecuteActionRequest action)
        => _client.Request($"{_basePath}{_controllerName}", "ExecuteAction")
                    .PostJsonAsync(action)
                    .ReceiveJson<UserActionResult>();

    public Task<List<FolderResponse>> ListFolders(Guid? parentId)
    {
        var request = _client.Request($"{_basePath}{_controllerName}", "folders");
        if (parentId is not null)
        {
            request = request.AppendQueryParam("parentId", parentId.Value.ToString());
        }

        return request.GetJsonAsync<List<FolderResponse>>();
    }

    public Task<List<FolderResponse>> FolderBreadcrumbs(Guid folderId)
        => _client.Request($"{_basePath}{_controllerName}", "folders", folderId, "breadcrumbs")
                    .GetJsonAsync<List<FolderResponse>>();

    public Task<FolderResponse> CreateFolder(CreateFolderRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "folders")
                    .PostJsonAsync(request)
                    .ReceiveJson<FolderResponse>();

    public Task<FolderResponse> RenameFolder(Guid id, RenameFolderRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "folders", id, "rename")
                    .PutJsonAsync(request)
                    .ReceiveJson<FolderResponse>();

    public Task DeleteFolder(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", "folders", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<UserActionResult> MoveFiles(MoveFilesRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "move-files")
                    .PostJsonAsync(request)
                    .ReceiveJson<UserActionResult>();
}
