using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.Feedbacks;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Feedbacks;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class FeedbackRepository : IFeedbackRepository, IDisposable
{
    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    IQueryable<FeedbackEntity> _listAllQuery => _marsDbContext.Feedbacks.OrderByDescending(s => s.CreatedAt);

    public FeedbackRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<FeedbackSummary?> Get(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.Feedbacks.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToSummary();

    public async Task<FeedbackDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.Feedbacks.AsNoTracking().Include(s => s.AuthorizedUser).FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToDetail();

    public async Task<Guid> Create(CreateFeedbackQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = new FeedbackEntity
        {
            Title = query.Title,
            FeedbackType = TypeParse(query.Type),
            Content = query.Content,
            Email = query.Email,
            Phone = query.Phone,
            FilledUsername = query.FilledUsername,
            AuthorizedUserId = query.AuthorizedUserId,
        };

        await _marsDbContext.Feedbacks.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    EFeedbackType TypeParse(string type) => Enum.Parse<EFeedbackType>(type);

    public async Task Update(UpdateFeedbackQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.Feedbacks.FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Entry(entity).CurrentValues.SetValues(new
        {
            FeedbackType = TypeParse(query.Type),
        });
        entity.ModifiedAt = DateTimeOffset.Now;

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.Feedbacks.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Throws if this class has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        ObjectDisposedThrowHelper.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Dispose the store
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }

    private IQueryable<FeedbackEntity> ListAllInternal(ListAllFeedbackQuery query)
    {
        return _listAllQuery.AsNoTracking()
                            .Where(s => (query.Ids == null || query.Ids.Contains(s.Id)));
    }

    public async Task<IReadOnlyCollection<FeedbackSummary>> ListAll(ListAllFeedbackQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var list = ListAllInternal(query);

        return (await list.ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<IReadOnlyCollection<FeedbackDetail>> ListAllDetail(ListAllFeedbackQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var list = ListAllInternal(query).Include(s => s.AuthorizedUser);

        return (await list.ToListAsync(cancellationToken)).ToDetailList();
    }

    public async Task<ListDataResult<FeedbackSummary>> List(ListFeedbackQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Email!, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Title, $"%{query.Search}%")));

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(FeedbackMapping.ToSummaryList);
    }

    public async Task<PagingResult<FeedbackSummary>> ListTable(ListFeedbackQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Email!, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Title, $"%{query.Search}%")));

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(FeedbackMapping.ToSummaryList);
    }

    public Task<int> DeleteMany(DeleteManyFeedbackQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return _marsDbContext.Feedbacks.Where(s => query.Ids.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
    }
}
