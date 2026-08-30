using Mars.Cms.Abstractions.Dto.Feedbacks;
using Mars.Contracts.Common;
using Mars.Core.Exceptions;

namespace Mars.Cms.Abstractions.Repositories;

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
    Task<int> DeleteMany(DeleteManyFeedbackQuery query, CancellationToken cancellationToken);
}
