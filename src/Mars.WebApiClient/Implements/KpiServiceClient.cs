using Flurl.Http;
using Mars.Contracts.Common;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class KpiServiceClient : BasicServiceClient, IKpiServiceClient
{
    public KpiServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Kpi";
    }

    public async Task<IReadOnlyDictionary<string, KpiResult>> Get(IEnumerable<string> keys)
        => await _client.Request($"{_basePath}{_controllerName}")
                        .SetQueryParam("keys", string.Join(',', keys))
                        .GetJsonAsync<Dictionary<string, KpiResult>>();
}
