using Mars.Shared.Common;
using Mars.Shared.Contracts.Feedbacks;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class FeedbackServiceClient : BasicServiceClient, IFeedbackServiceClient
{
    public FeedbackServiceClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Feedback";
    }

    public Task<FeedbackDetailResponse?> Get(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<FeedbackDetailResponse?>();

    public Task<Guid> Create(CreateFeedbackRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PostJsonAsync(request)
                    .ReceiveJson<Guid>();

    public Task<FeedbackDetailResponse> Update(UpdateFeedbackRequest request)
        => _client.Request($"{_basePath}{_controllerName}")
                    .PutJsonAsync(request)
                    .ReceiveJson<FeedbackDetailResponse>();

    public Task Delete(Guid id)
        => _client.Request($"{_basePath}{_controllerName}", id)
                    .OnError(OnStatus404ThrowException)
                    .DeleteAsync();

    public Task<ListDataResult<FeedbackSummaryResponse>> List(ListFeedbackQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<FeedbackSummaryResponse>>();

    public Task<PagingResult<FeedbackSummaryResponse>> ListTable(TableFeedbackQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/ListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<FeedbackSummaryResponse>>();

}
