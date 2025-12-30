using Mars.Core.Exceptions;
using Mars.Host.Data.Contexts;
using Mars.Host.Data.Entities;
using Mars.Host.Data.OwnedTypes.NavMenus;
using Mars.Host.Repositories.Mappings;
using Mars.Host.Shared.Dto.Common;
using Mars.Host.Shared.Dto.NavMenus;
using Mars.Host.Shared.Repositories;
using Mars.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Mars.Host.Repositories;

internal class NavMenuRepository : INavMenuRepository
{
    private readonly MarsDbContext _marsDbContext;
    private bool _disposed;

    IQueryable<NavMenuEntity> _listAllQuery => _marsDbContext.NavMenus.OrderByDescending(s => s.CreatedAt);

    public NavMenuRepository(MarsDbContext marsDbContext)
    {
        _marsDbContext = marsDbContext;
    }

    public async Task<NavMenuSummary?> Get(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.NavMenus.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToSummary();

    public async Task<NavMenuDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => (await _marsDbContext.NavMenus.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken))?.ToDetail();

    public async Task<Guid> Create(CreateNavMenuQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = new NavMenuEntity
        {
            Id = query.Id ?? Guid.Empty,
            Title = query.Title,
            Slug = query.Slug,
            Disabled = query.Disabled,
            Tags = query.Tags.ToList(),
            MenuItems = MapToItems(query.MenuItems),
            Class = query.Class,
            Style = query.Style,
            Roles = query.Roles.ToList(),
            RolesInverse = query.RolesInverse,
        };

        await _marsDbContext.NavMenus.AddAsync(entity, cancellationToken);
        await _marsDbContext.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task Update(UpdateNavMenuQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var entity = await _marsDbContext.NavMenus.FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Entry(entity).CurrentValues.SetValues(new
        {
            Title = query.Title,
            Slug = query.Slug,
            Disabled = query.Disabled,
            Tags = query.Tags.ToList(),
            MenuItems = MapToItems(query.MenuItems),
            Class = query.Class,
            Style = query.Style,
            Roles = query.Roles.ToList(),
            RolesInverse = query.RolesInverse,
        });
        entity.ModifiedAt = DateTimeOffset.Now;

        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    NavMenuItem MapItem(NavMenuItemDto item)
        => new NavMenuItem
        {
            Id = item.Id,
            ParentId = item.ParentId,
            Title = item.Title,
            Url = item.Url,
            Icon = item.Icon,
            Roles = item.Roles.ToList(),
            RolesInverse = item.RolesInverse,
            Class = item.Class,
            Style = item.Style,
            OpenInNewTab = item.OpenInNewTab,
            Disabled = item.Disabled,
            IsHeader = item.IsHeader,
            IsDivider = item.IsDivider,
        };

    List<NavMenuItem> MapToItems(IReadOnlyCollection<NavMenuItemDto> items)
        => items.Select(MapItem).ToList();

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var entity = await _marsDbContext.NavMenus.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null) throw new NotFoundException();

        _marsDbContext.Remove(entity);
        await _marsDbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> DeleteMany(DeleteManyNavMenuQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        return _marsDbContext.NavMenus.Where(s => query.Ids.Contains(s.Id)).ExecuteDeleteAsync(cancellationToken);
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

    private IQueryable<NavMenuEntity> ListAllInternal(ListAllNavMenuQuery query)
    {
        return _listAllQuery.AsNoTracking()
                            .Where(s => (query.Ids == null || query.Ids.Contains(s.Id)));
    }

    public async Task<IReadOnlyCollection<NavMenuSummary>> ListAll(ListAllNavMenuQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var list = ListAllInternal(query);

        return (await list.ToListAsync(cancellationToken)).ToSummaryList();
    }

    public async Task<IReadOnlyCollection<NavMenuDetail>> ListAllActiveDetail(ListAllNavMenuQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var list = ListAllInternal(query);

        var menus = await list.AsNoTracking().Where(s => !s.Disabled).ToListAsync(cancellationToken);

        menus.ForEach(menu => menu.MenuItems = menu.MenuItems.Where(s => !s.Disabled).ToList());

        return menus.ToDetailList();
    }

    public async Task<ListDataResult<NavMenuSummary>> List(ListNavMenuQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Slug, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Title, $"%{query.Search}%")));

        var list = await queryable.ToListDataResult(query, cancellationToken);

        return list.ToMap(NavMenuMapping.ToSummaryList);

    }

    public async Task<PagingResult<NavMenuSummary>> ListTable(ListNavMenuQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var queryable = _listAllQuery.AsNoTracking().Where(s => query.Search == null
                                    || (EF.Functions.ILike(s.Slug, $"%{query.Search}%")
                                        || EF.Functions.ILike(s.Title, $"%{query.Search}%")));

        var list = await queryable.ToPagingResult(query, cancellationToken);

        return list.ToMap(NavMenuMapping.ToSummaryList);

    }
    
}
