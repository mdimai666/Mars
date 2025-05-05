using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Feedbacks;
using Mars.Shared.Common;

namespace Mars.Host.Shared.Repositories;

public interface IFeedbackRepository : IDisposable
{
    Task<FeedbackSummary?> Get(Guid id, CancellationToken cancellationToken);
    Task<FeedbackDetail?> GetDetail(Guid id, CancellationToken cancellationToken);
    Task<Guid> Create(CreateFeedbackQuery query, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException"/>
    Task Update(UpdateFeedbackQuery query, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException"/>
    Task Delete(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FeedbackSummary>> ListAll(ListAllFeedbackQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<FeedbackDetail>> ListAllDetail(ListAllFeedbackQuery query, CancellationToken cancellationToken);
    Task<ListDataResult<FeedbackSummary>> List(ListFeedbackQuery query, CancellationToken cancellationToken);
    Task<PagingResult<FeedbackSummary>> ListTable(ListFeedbackQuery query, CancellationToken cancellationToken);
}
