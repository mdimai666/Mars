using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Identity.Contracts.ApiKeys;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class ApiKeyServiceClient : BasicServiceClient, IApiKeyServiceClient
{
    public ApiKeyServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "ApiKey";
    }

    public async Task<IReadOnlyCollection<ApiKeySummaryResponse>> List()
        => await _client.Request($"{_basePath}{_controllerName}")
                    .GetJsonAsync<List<ApiKeySummaryResponse>>();

    public Task<UserActionResult<CreatedApiKeyResponse>> Create(CreateApiKeyRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<UserActionResult<CreatedApiKeyResponse>>();

    public Task<UserActionResult> Revoke(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .DeleteAsync()
                    .ReceiveJson<UserActionResult>();
}
