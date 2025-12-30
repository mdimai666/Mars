using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.UserTypes;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class UserTypeRepository : IUserTypeRepository, IDisposable
{
    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    IQueryable<UserTypeEntity> _listAllQuery => _marsDbContext.UserTypes.OrderByDescending(s => s.CreatedAt);

    public UserTypeRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<UserTypeSummary?> Get(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.UserTypes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToSummary();

    IQueryable<UserTypeEntity> InternalDetail => _marsDbContext.UserTypes.AsNoTracking()
                                                        .Include(s => s.MetaFields);

    public async Task<UserTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
                                        => (await InternalDetail
                                            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken))
                                            ?.ToDetail();
    public async Task<UserTypeSummary?> GetByName(string name, CancellationToken cancellationToken)
                                        => (await InternalDetail
                                            .FirstOrDefaultAsync(s => s.TypeName == name, cancellationToken))
                                            ?.ToSummary();
    public async Task<UserTypeDetail?> GetDetailByName(string name, CancellationToken cancellationToken)
                                        => (await InternalDetail
                                            .FirstOrDefaultAsync(s => s.TypeName == name, cancellationToken))
                                            ?.ToDetail();

    public Task<bool> TypeNameExist(string name, CancellationToken cancellationToken)
        => _marsDbContext.UserTypes.AsNoTracking().AnyAsync(s => s.TypeName == name, cancellationToken);

    public async Task<Guid> Create(CreateUserTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = query.ToEntity();

        await _marsDbContext.UserTypes.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task Update(UpdateUserTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.UserTypes.Include(s => s.MetaFields!)
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

        var entity = await _marsDbContext.UserTypes.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> DeleteMany(DeleteManyUserTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return _marsDbContext.UserTypes.Where(s => query.Ids.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
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

    private IQueryable<UserTypeEntity> ListAllInternal(ListAllUserTypeQuery query)
    {
        var list = _listAllQuery.AsNoTracking();

        if (query.Ids is { Count: > 0 })
        {
            list = list.Where(u => query.Ids.Contains(u.Id));
        }

        return list;
    }

    public async Task<IReadOnlyCollection<UserTypeSummary>> ListAll(ListAllUserTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return (await ListAllInternal(query).ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<IReadOnlyCollection<UserTypeDetail>> ListAllDetail(ListAllUserTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return (await ListAllInternal(query).Include(s => s.MetaFields).ToListAsync(cancellationToken)).ToDetailList();
    }

    IQueryable<UserTypeEntity> ListFilterQuery(ListUserTypeQuery query) => _listAllQuery
                                    .AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.TypeName, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Title, $"%{query.Search}%")));

    public async Task<ListDataResult<UserTypeSummary>> List(ListUserTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(UserTypeMapping.ToSummaryList);
    }

    public async Task<PagingResult<UserTypeSummary>> ListTable(ListUserTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(UserTypeMapping.ToSummaryList);

    }
}
