using System.Net;
using Flurl.Http;
using Mars.Shared.Contracts.XActions;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class ActServiceClient : BasicServiceClient, IActServiceClient
{
    public ActServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Act";

        //TODO: implement ActService.Inject
    }

    public async Task<XActResult> Inject(string actionId, string[] args)
    {
        var res = await _client.Request($"{_basePath}{_controllerName}", "Inject", actionId)
                    .AllowAnyHttpStatus()
                    .PostJsonAsync(args);

        if (res.StatusCode == (int)HttpStatusCode.NotFound)
        {
            var result = await res.GetJsonAsync<XActResult>();
            return result;
        }
        else if (res.StatusCode == (int)HttpStatusCode.OK)
        {
            var result = await res.GetJsonAsync<XActResult>();
            return result;
        }

        HandleResponseGeneralErrors(res);
        throw new NotImplementedException();
    }

}
