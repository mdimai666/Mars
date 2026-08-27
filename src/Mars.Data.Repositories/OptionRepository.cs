using System.Text.Json;
using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Options;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class OptionRepository : IOptionRepository
{
    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    IQueryable<OptionEntity> _listAllQuery => _marsDbContext.Options.OrderByDescending(s => s.CreatedAt);

    protected static readonly JsonSerializerOptions serializerOptions = new();

    public OptionRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<T?> Get<T>(Guid id, CancellationToken cancellationToken = default)
        where T : class
    {
        var exist = await _marsDbContext.Options.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (exist is null) return null;
        return JsonSerializer.Deserialize<T>(exist.Value, serializerOptions);
    }

    public async Task<T?> GetKey<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        var exist = await _marsDbContext.Options.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (exist is null) return null;
        return JsonSerializer.Deserialize<T>(exist.Value, serializerOptions);
    }

    public async Task<OptionDetail?> GetKeyRaw(string key, CancellationToken cancellationToken = default)
    {
        var exist = await _marsDbContext.Options.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        return exist?.ToDetail();
    }

    public async Task<Guid> Create<T>(CreateOptionQuery<T> query, CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = new OptionEntity
        {
            Id = query.Id ?? Guid.Empty,
            Key = query.Key,
            Type = query.Value.GetType().FullName!,

            Value = JsonSerializer.Serialize(query.Value, serializerOptions)
        };

        await _marsDbContext.Options.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task Update<T>(UpdateOptionQuery<T> query, CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.Options.FirstOrDefaultAsync(s => s.Key == query.Key, cancellationToken);

        if (entity is null) throw new NotFoundException();

        entity.Key = query.Key;
        entity.Value = JsonSerializer.Serialize(query.Value, serializerOptions);
        entity.Type = query.Value.GetType().FullName!;

        if (entity.Value is null) throw new ArgumentNullException(nameof(query.Value), "cannot serialize value into json");

        _marsDbContext.Entry(entity).CurrentValues.SetValues(entity);
        entity.ModifiedAt = DateTimeOffset.Now;

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.Options.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.Options.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

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

    public async Task<IReadOnlyCollection<OptionSummary>> ListAll(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return (await _listAllQuery.AsNoTracking().ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<ListDataResult<OptionSummary>> List(ListOptionQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Key, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Type, $"%{query.Search}%")));

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(OptionMapping.ToSummaryList);

    }

    public async Task<PagingResult<OptionSummary>> ListTable(ListOptionQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Key, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Type, $"%{query.Search}%")));

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(OptionMapping.ToSummaryList);

    }

    public Task<int> DeleteMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return _marsDbContext.Options.Where(s => ids.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
    }
}
