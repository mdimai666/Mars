using Mars.Host.Shared.Dto.Feedbacks;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Services;

public interface IFeedbackService
{
    Task<FeedbackSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<FeedbackDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<ListDataResult<FeedbackSummary>> List(ListFeedbackQuery query, CancellationToken cancellationToken);
    Task<PagingResult<FeedbackSummary>> ListTable(ListFeedbackQuery query, CancellationToken cancellationToken);
    Task<FeedbackDetail> Create(CreateFeedbackQuery query, CancellationToken cancellationToken);
    Task<FeedbackDetail> Update(UpdateFeedbackQuery query, CancellationToken cancellationToken);
    Task<FeedbackSummary> Delete(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FeedbackSummary>> DeleteMany(DeleteManyFeedbackQuery query, CancellationToken cancellationToken);
    Task ExcelFeedbackList(MemoryStream stream, CancellationToken cancellationToken);
}
