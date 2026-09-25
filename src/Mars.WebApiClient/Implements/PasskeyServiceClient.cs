using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Identity.Contracts.Passkeys;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class PasskeyServiceClient : BasicServiceClient, IPasskeyServiceClient
{
    public PasskeyServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Passkey";
    }

    public async Task<IReadOnlyCollection<PasskeySummaryResponse>> List()
        => await _client.Request($"{_basePath}{_controllerName}")
                    .GetJsonAsync<List<PasskeySummaryResponse>>();

    public Task<UserActionResult> Rename(RenamePasskeyRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "rename")
                    .PostJsonAsync(request)
                    .ReceiveJson<UserActionResult>();

    public Task<UserActionResult> Delete(string credentialId)
        => _client.Request($"{_basePath}{_controllerName}", credentialId)
                    .DeleteAsync()
                    .ReceiveJson<UserActionResult>();
}
