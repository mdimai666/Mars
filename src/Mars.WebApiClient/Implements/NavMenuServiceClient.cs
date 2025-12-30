using Mars.Shared.Common;
using Mars.Shared.Contracts.NavMenus;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class NavMenuServiceClient : BasicServiceClient, INavMenuServiceClient
{
    public NavMenuServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "NavMenu";
    }

    public Task<NavMenuDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<NavMenuDetailResponse?>();

    public Task<Guid> Create(CreateNavMenuRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<Guid>();

    public Task Update(UpdateNavMenuRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request);

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task DeleteMany(Guid[] ids)
        => _client.Request($"{_basePath}{_controllerName}/DeleteMany")
                    .AppendQueryParam(new { ids })
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<ListDataResult<NavMenuSummaryResponse>> List(ListNavMenuQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<NavMenuSummaryResponse>>();

    public Task<PagingResult<NavMenuSummaryResponse>> ListTable(TableNavMenuQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/ListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<NavMenuSummaryResponse>>();

    public Task<UserActionResult> Import(Guid id, string json)
    {
        throw new NotImplementedException();
    }

}
