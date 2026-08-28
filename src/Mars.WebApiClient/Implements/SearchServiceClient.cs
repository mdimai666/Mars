using System.Net.Http;
using Flurl.Http;
using Mars.Cms.Contracts.Search;
using Mars.WebApiClient.Interfaces;

namespace Mars.WebApiClient.Implements;

internal class SearchServiceClient : BasicServiceClient, ISearchServiceClient
{
    public SearchServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Search";
    }

    public async Task<IReadOnlyCollection<SearchFoundElementResponse>> Query(string text, int maxCount = 20, CancellationToken cancellationToken = default)
        => await _client.Request($"{_basePath}{_controllerName}", "Query")
                        .AppendQueryParam(new { text, maxCount })
                        .GetJsonAsync<List<SearchFoundElementResponse>>(HttpCompletionOption.ResponseContentRead, cancellationToken);
}
