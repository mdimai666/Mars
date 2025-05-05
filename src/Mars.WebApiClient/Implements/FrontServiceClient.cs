using System.Web;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Renders;
using Mars.Shared.Contracts.WebSite.Dto;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class FrontServiceClient : BasicServiceClient, IFrontServiceClient
{
    public FrontServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Front";
    }

    public Task<FMarsAppFrontTemplateMinimumResponse> FrontMinimal()
        => _client.Request($"{_basePath}{_controllerName}", "FrontMinimal")
                    .GetJsonAsync<FMarsAppFrontTemplateMinimumResponse>();
    public Task<FMarsAppFrontTemplateSummaryResponse> FrontFiles()
        => _client.Request($"{_basePath}{_controllerName}", "FrontFiles")
                    .GetJsonAsync<FMarsAppFrontTemplateSummaryResponse>();
    public Task<FrontSummaryInfoResponse> FrontSummaryInfo()
        => _client.Request($"{_basePath}{_controllerName}", "FrontSummaryInfo")
                    .GetJsonAsync<FrontSummaryInfoResponse>();
    public Task<FWebPartResponse?> GetPart(string fileRelPath)
        => _client.Request($"{_basePath}{_controllerName}", "GetPart")
                    .AppendQueryParam(new { fileRelPath })
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<FWebPartResponse?>();
}
