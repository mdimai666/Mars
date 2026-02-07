using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.PostCategoryTypes;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class PostCategoryTypeRepository : IPostCategoryTypeRepository
{
    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    IQueryable<PostCategoryTypeEntity> _listAllQuery => _marsDbContext.PostCategoryTypes.OrderByDescending(s => s.CreatedAt);

    public PostCategoryTypeRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<PostCategoryTypeSummary?> Get(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.PostCategoryTypes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToSummary();

    IQueryable<PostCategoryTypeEntity> InternalDetail => _marsDbContext.PostCategoryTypes.AsNoTracking()
                                                        .Include(s => s.MetaFields);

    public async Task<PostCategoryTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
                                        => (await InternalDetail
                                            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                            ?.ToDetail();
    public async Task<PostCategoryTypeSummary?> GetByName(string name, CancellationToken cancellationToken)
                                        => (await InternalDetail
                                            .FirstOrDefaultAsync(s => s.TypeName == name, cancellationToken))
                                            ?.ToSummary();
    public async Task<PostCategoryTypeDetail?> GetDetailByName(string name, CancellationToken cancellationToken)
                                        => (await InternalDetail
                                            .FirstOrDefaultAsync(s => s.TypeName == name, cancellationToken))
                                            ?.ToDetail();

    public Task<bool> TypeNameExist(string name, CancellationToken cancellationToken)
        => _marsDbContext.PostCategoryTypes.AsNoTracking().AnyAsync(s => s.TypeName == name, cancellationToken);

    public async Task<Guid> Create(CreatePostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = query.ToEntity();

        await _marsDbContext.PostCategoryTypes.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task Update(UpdatePostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.PostCategoryTypes.Include(s => s.MetaFields!)
                                                        .ThenInclude(s => s.Variants)
                                                    .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
                                                    ?? throw new NotFoundException();

        var modifiedAt = DateTimeOffset.Now;
        var oldTypeName = entity.TypeName;

        entity.Title = query.Title;
        entity.TypeName = query.TypeName;
        entity.Tags = query.Tags.ToList();
        MetaFieldsTools.ModifyMetaFields(_marsDbContext, entity.MetaFields!, query.MetaFields, modifiedAt);

        entity.ModifiedAt = modifiedAt;

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.PostCategoryTypes.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> DeleteMany(DeleteManyPostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return _marsDbContext.PostCategoryTypes.Where(s => query.Ids.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
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

    private IQueryable<PostCategoryTypeEntity> ListAllInternal(ListAllPostCategoryTypeQuery query)
    {
        var list = _listAllQuery.AsNoTracking();

        if (query.Ids is { Count: > 0 })
        {
            list = list.Where(u => query.Ids.Contains(u.Id));
        }

        return list;
    }

    public async Task<IReadOnlyCollection<PostCategoryTypeSummary>> ListAll(ListAllPostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return (await ListAllInternal(query).ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<IReadOnlyCollection<PostCategoryTypeDetail>> ListAllDetail(ListAllPostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return (await ListAllInternal(query).Include(s => s.MetaFields).ToListAsync(cancellationToken)).ToDetailList();
    }

    IQueryable<PostCategoryTypeEntity> ListFilterQuery(ListPostCategoryTypeQuery query) => _listAllQuery
                                    .AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.TypeName, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Title, $"%{query.Search}%")));

    public async Task<ListDataResult<PostCategoryTypeSummary>> List(ListPostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(PostCategoryTypeMapping.ToSummaryList);
    }

    public async Task<PagingResult<PostCategoryTypeSummary>> ListTable(ListPostCategoryTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(PostCategoryTypeMapping.ToSummaryList);

    }
}
