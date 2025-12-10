using Mars.Core.Exceptions;
using Mars.Core.Utils;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class PostTypeRepository : IPostTypeRepository, IDisposable
{
    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    IQueryable<PostTypeEntity> _listAllQuery => _marsDbContext.PostTypes.OrderByDescending(s => s.CreatedAt);

    public PostTypeRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<PostTypeSummary?> Get(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.PostTypes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToSummary();

    public async Task<PostTypeDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.PostTypes.AsNoTracking().Include(s => s.MetaFields).FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToDetail();

    public async Task<PostTypeSummary?> GetByName(string name, CancellationToken cancellationToken)
        => (await _marsDbContext.PostTypes.AsNoTracking().FirstOrDefaultAsync(s => s.TypeName == name, cancellationToken))?.ToSummary();

    public async Task<PostTypeDetail?> GetDetailByName(string name, CancellationToken cancellationToken)
        => (await _marsDbContext.PostTypes.AsNoTracking().Include(s => s.MetaFields).FirstOrDefaultAsync(s => s.TypeName == name, cancellationToken))?.ToDetail();

    public Task<bool> TypeNameExist(string name, CancellationToken cancellationToken)
        => _marsDbContext.PostTypes.AsNoTracking().AnyAsync(s => s.TypeName == name, cancellationToken);

    public async Task<Guid> Create(CreatePostTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = query.ToEntity();

        await _marsDbContext.PostTypes.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task Update(UpdatePostTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.PostTypes.Include(s => s.MetaFields!)
                                                        .ThenInclude(s => s.Variants)
                                                    .Include(s => s.PostStatusList)
                                                    .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
                                                    ?? throw new NotFoundException();
        var oldTypeName = entity.TypeName;
        entity.UpdateEntity(query);
        var modifiedAt = entity.ModifiedAt!.Value;
        
        ModifyStatusList(entity, query, modifiedAt);
        MetaFieldsTools.ModifyMetaFields(_marsDbContext, entity.MetaFields!, query.MetaFields, modifiedAt);

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    void ModifyStatusList(PostTypeEntity entity, UpdatePostTypeQuery query, DateTimeOffset modifiedAt)
    {
        var statusDiff = DiffList.FindDifferencesBy(entity.PostStatusList, query.PostStatusList.Select(s => s.ToEntity(modifiedAt)).ToList(), s => s.Id);
        if (statusDiff.HasChanges)
        {
            foreach (var item in statusDiff.ToRemove) entity.PostStatusList.Remove(item);
            foreach (var item in statusDiff.ToAdd)
            {
                item.CreatedAt = modifiedAt;
                item.ModifiedAt = null;
                entity.PostStatusList.Add(item);
            }
        }
        foreach (var item in entity.PostStatusList.Except(statusDiff.ToRemove).Except(statusDiff.ToAdd))
        {
            var q = query.PostStatusList.First(s => s.Id == item.Id);
            item.Slug = q.Slug;
            item.Title = q.Title;
            item.Tags = q.Tags.ToList();
            item.ModifiedAt = modifiedAt;

        }

        // отключено после перевода [jsonb] аттрибута в .ToJson()
        //_marsDbContext.Entry(entity).Property(e => e.PostStatusList).IsModified = true;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.PostTypes.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

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

    public async Task<IReadOnlyCollection<PostTypeSummary>> ListAll(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return (await _listAllQuery.AsNoTracking().ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<IReadOnlyCollection<PostTypeSummary>> ListAllActive(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return (await _listAllQuery.AsNoTracking().Where(s => !s.Disabled).ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<IReadOnlyCollection<PostTypeDetail>> ListAllDetail(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return (await _listAllQuery.AsNoTracking().Include(s => s.MetaFields).ToListAsync(cancellationToken)).ToDetailList();
    }

    IQueryable<PostTypeEntity> ListFilterQuery(ListPostTypeQuery query) => _listAllQuery
                                    .AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.TypeName, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Title, $"%{query.Search}%")));

    public async Task<ListDataResult<PostTypeSummary>> List(ListPostTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(PostTypeMapping.ToSummaryList);
    }

    public async Task<PagingResult<PostTypeSummary>> ListTable(ListPostTypeQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = ListFilterQuery(query);

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(PostTypeMapping.ToSummaryList);

    }
}
