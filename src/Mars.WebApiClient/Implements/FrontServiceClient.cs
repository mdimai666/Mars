using Flurl.Http;
using Mars.Contracts.Common;
using Mars.SiteEngine.Contracts.WebSite.Dto;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class FrontServiceClient : BasicServiceClient, IFrontServiceClient
{
    public FrontServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Front";
    }

    public Task<IReadOnlyCollection<FFrontEngineResponse>> Engines()
        => _client.Request($"{_basePath}{_controllerName}", "Engines")
                    .GetJsonAsync<IReadOnlyCollection<FFrontEngineResponse>>();

    public Task<IReadOnlyCollection<string>> FrontTemplates()
        => _client.Request($"{_basePath}{_controllerName}", "FrontTemplates")
                    .GetJsonAsync<IReadOnlyCollection<string>>();

    public Task<FFrontTreeNodeResponse> FrontTree(string slug)
        => _client.Request($"{_basePath}{_controllerName}", "FrontTree")
                    .AppendQueryParam(new { slug })
                    .GetJsonAsync<FFrontTreeNodeResponse>();

    public Task<IReadOnlyCollection<FFrontPageResponse>> FrontPages(string slug)
        => _client.Request($"{_basePath}{_controllerName}", "FrontPages")
                    .AppendQueryParam(new { slug })
                    .GetJsonAsync<IReadOnlyCollection<FFrontPageResponse>>();

    public Task<FFrontFileContentResponse> ReadFrontFile(string slug, string relPath)
        => _client.Request($"{_basePath}{_controllerName}", "ReadFrontFile")
                    .AppendQueryParam(new { slug, relPath })
                    .GetJsonAsync<FFrontFileContentResponse>();

    public Task<UserActionResult> CreateFront(FCreateFrontRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "CreateFront")
                    .PostJsonAsync(request)
                    .ReceiveJson<UserActionResult>();

    public Task<UserActionResult> DeleteFront(string slug, bool deleteFolder)
        => _client.Request($"{_basePath}{_controllerName}", "DeleteFront")
                    .AppendQueryParam(new { slug, deleteFolder })
                    .DeleteAsync()
                    .ReceiveJson<UserActionResult>();

    public Task<UserActionResult> SaveFrontFile(string slug, string relPath, string content)
        => _client.Request($"{_basePath}{_controllerName}", "SaveFrontFile")
                    .AppendQueryParam(new { slug, relPath })
                    .PostJsonAsync(content)
                    .ReceiveJson<UserActionResult>();

    public Task<UserActionResult> CreateFrontFile(string slug, string relPath, bool isFolder)
        => _client.Request($"{_basePath}{_controllerName}", "CreateFrontFile")
                    .AppendQueryParam(new { slug, relPath, isFolder })
                    .PostAsync()
                    .ReceiveJson<UserActionResult>();

    public Task<UserActionResult> RenameFrontFile(string slug, string relPath, string newRelPath)
        => _client.Request($"{_basePath}{_controllerName}", "RenameFrontFile")
                    .AppendQueryParam(new { slug, relPath, newRelPath })
                    .PostAsync()
                    .ReceiveJson<UserActionResult>();

    public Task<UserActionResult> DeleteFrontFile(string slug, string relPath)
        => _client.Request($"{_basePath}{_controllerName}", "DeleteFrontFile")
                    .AppendQueryParam(new { slug, relPath })
                    .DeleteAsync()
                    .ReceiveJson<UserActionResult>();
}
