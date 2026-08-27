using Flurl.Http;
using Mars.Contracts.XActions;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class ActServiceClient : BasicServiceClient, IActServiceClient
{
    public ActServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Act";
    }

    public Task<XActResult> Inject(string actionId, IReadOnlyDictionary<string, string>? args = null)
        => _client.Request($"{_basePath}{_controllerName}", "Inject")
                    .OnError(OnStatus404ThrowException)
                    .PostJsonAsync(new XActionCommandCall
                    {
                        Id = actionId,
                        Args = args ?? new Dictionary<string, string>(),
                    })
                    .ReceiveJson<XActResult>();

    public async Task<IReadOnlyDictionary<string, XActionCommand>> List()
        => await _client.Request($"{_basePath}{_controllerName}", "list")
                        .OnError(OnStatus404ThrowException)
                        .GetJsonAsync<Dictionary<string, XActionCommand>>();

    public async Task<IReadOnlyCollection<XActionOption>> Options(string sourceKey)
        => await _client.Request($"{_basePath}{_controllerName}", "options", sourceKey)
                        .OnError(OnStatus404ThrowException)
                        .GetJsonAsync<List<XActionOption>>();

}
