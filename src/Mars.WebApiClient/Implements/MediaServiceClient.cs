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

    public Task<FileDetailResponse> Upload(Stream stream, string fileName)
        => _client.Request($"{_basePath}{_controllerName}", "Upload")
                    .PostMultipartAsync(mp => mp
                        //.AddFile("file", GenerateStreamFromString(fileContent), fileName)
                        .AddFile("file", stream, fileName)
                    )
                    .ReceiveJson<FileDetailResponse>();

    //public Task Update(UpdateMediaRequest request)
    //    => _client.Request($"{_basePath}{_controllerName}")
    //                .PutJsonAsync(request);

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<ListDataResult<FileListItemResponse>> List(ListFileQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<FileListItemResponse>>();

    public Task<PagingResult<FileListItemResponse>> ListTable(TableFileQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/ListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<FileListItemResponse>>();

    public Task<UserActionResult> ExecuteAction(ExecuteActionRequest action)
        => _client.Request($"{_basePath}{_controllerName}", "ExecuteAction")
                    .PostJsonAsync(action)
                    .ReceiveJson<UserActionResult>();

}
