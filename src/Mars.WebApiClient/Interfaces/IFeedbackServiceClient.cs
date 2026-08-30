using Mars.Cms.Contracts.Feedbacks;
using Mars.Contracts.Common;
using Mars.Core.Exceptions;

namespace Mars.WebApiClient.Interfaces;

public interface IFeedbackServiceClient
{
    Task<FeedbackDetailResponse?> Get(Guid id);

    /// <summary>
    /// Создает
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="MarsValidationException"></exception>
    /// <exception cref="UserActionException"></exception>
    Task<Guid> Create(CreateFeedbackRequest request);
    Task<FeedbackDetailResponse> Update(UpdateFeedbackRequest request);
    Task<ListDataResult<FeedbackSummaryResponse>> List(ListFeedbackQueryRequest filter);
    Task<PagingResult<FeedbackSummaryResponse>> ListTable(TableFeedbackQueryRequest filter);
    Task Delete(Guid id);
    Task DeleteMany(Guid[] ids);
}
