using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.Roles;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class RoleRepository : IRoleRepository
{
    private readonly MarsDbContext _marsDbContext;
    private readonly ILookupNormalizer _lookupNormalizer;
    private bool _disposed;

    IQueryable<RoleEntity> _listAllQuery => _marsDbContext.Roles.OrderByDescending(s => s.Name);

    public RoleRepository(MarsDbContext marsDbContext, ILookupNormalizer lookupNormalizer)
    {
        _marsDbContext = marsDbContext;
        _lookupNormalizer = lookupNormalizer;
    }

    public async Task<RoleDetail?> Get(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.Roles.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToDetail();

    public async Task<Guid> Create(CreateRoleQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = new RoleEntity
        {
            Id = query.Id ?? Guid.Empty,
            Name = query.Name,
            IsActive = true,
        };

        await _marsDbContext.Roles.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task Update(UpdateRoleQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.Roles.FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Entry(entity).CurrentValues.SetValues(new
        {
            query.Name,
        });
        entity.ModifiedAt = DateTimeOffset.Now;

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.Roles.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

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

    public async Task<IReadOnlyCollection<RoleSummary>> ListAll(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return (await _listAllQuery.AsNoTracking().ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<ListDataResult<RoleSummary>> List(ListRoleQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Name, $"%{query.Search}%")));

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(RoleMapping.ToSummaryList);

    }

    public async Task<PagingResult<RoleSummary>> ListTable(ListRoleQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Name, $"%{query.Search}%")));

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(RoleMapping.ToSummaryList);

    }

    public async Task<IReadOnlyCollection<RoleClaimSummary>> ListAllClaims(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return (await _marsDbContext.RoleClaims.AsNoTracking().ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<bool> RolesExsists(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var normalized = roleNames.Select(s => _lookupNormalizer.NormalizeName(s)).ToArray();

        var existCount = await _marsDbContext.Roles.AsNoTracking()
                                            .Select(s => s.NormalizedName)
                                            .CountAsync(s => normalized.Contains(s), cancellationToken);
        return existCount == roleNames.Count;
    }

    public async Task<bool> RolesExsists(IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var existCount = await _marsDbContext.Roles.AsNoTracking()
                                            .Select(s => s.Id)
                                            .CountAsync(s => roleIds.Contains(s), cancellationToken);
        return existCount == roleIds.Count;
    }

}
