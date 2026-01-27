using Flurl.Http;
using Mars.Shared.Contracts.XActions;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class ActServiceClient : BasicServiceClient, IActServiceClient
{
    public ActServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Act";
    }

    public Task<XActResult> Inject(string actionId, string[] args)
        => _client.Request($"{_basePath}{_controllerName}", "Inject", actionId)
                    .OnError(OnStatus404ThrowException)
                    .PostJsonAsync(args)
                    .ReceiveJson<XActResult>();

}
