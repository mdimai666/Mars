using System.Data;
using Mars.Cms.Abstractions.Dto.NavMenus;
using Mars.Cms.Abstractions.Repositories;
using Mars.Cms.Abstractions.Services;
using Mars.Contracts.Common;
using Mars.Core.Exceptions;
using Mars.Server.Abstractions.Managers;
using Mars.Server.Abstractions.Managers.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Cms.Host.Services;

public class NavMenuService : INavMenuService
{
    private readonly INavMenuRepository _navMenuRepository;
    private readonly IPostTypeRepository _postTypeRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly IEventManager _eventManager;

    private const string DevMenuKey = "NavMenuService::NavMenu.dev";
    private const string ActiveMenusMenuKey = "NavMenuService::NavMenu.activeMenus";
    private readonly TimeSpan _cacheTtl = TimeSpan.FromHours(24);

    public NavMenuService(IServiceScopeFactory scopeFactory, IMemoryCache memoryCache, IEventManager eventManager)
    {
        var scope = scopeFactory.CreateScope();
        _navMenuRepository = scope.ServiceProvider.GetRequiredService<INavMenuRepository>();
        _postTypeRepository = scope.ServiceProvider.GetRequiredService<IPostTypeRepository>();
        _memoryCache = memoryCache;
        _eventManager = eventManager;
        _eventManager.AddEventListener(_eventManager.Defaults.PostTypeAnyOperation(), payload =>
        {
            _memoryCache.Remove(DevMenuKey);
        });
    }

    public async Task<NavMenuSummary?> Get(Guid id, CancellationToken cancellationToken)
        => await _navMenuRepository.Get(id, cancellationToken) ?? (id == DevMenuFactory.DevMenuId ? DevMenu() : null);

    public async Task<NavMenuDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        if (id == DevMenuFactory.DevMenuId)
            return DevMenu();

        return await _navMenuRepository.GetDetail(id, cancellationToken);
    }

    public Task<ListDataResult<NavMenuSummary>> List(ListNavMenuQuery query, CancellationToken cancellationToken)
        => _navMenuRepository.List(query, cancellationToken);

    public Task<PagingResult<NavMenuSummary>> ListTable(ListNavMenuQuery query, CancellationToken cancellationToken)
        => _navMenuRepository.ListTable(query, cancellationToken);

    /// <summary>
    /// Список меню для админки: несохранённые системные меню подмешиваются виртуальной записью.
    /// </summary>
    public async Task<ListDataResult<NavMenuSummary>> ListForAdmin(ListNavMenuQuery query, CancellationToken cancellationToken)
    {
        var result = await _navMenuRepository.List(query, cancellationToken);

        if (query.Skip > 0) return result;
        if (await _navMenuRepository.Get(DevMenuFactory.DevMenuId, cancellationToken) is not null) return result;
        if (!MatchesSearch(DevMenuFactory.DevMenuTitle, query.Search) && !MatchesSearch(DevMenuFactory.DevMenuSlug, query.Search)) return result;

        var devMenu = new NavMenuSummary
        {
            Id = DevMenuFactory.DevMenuId,
            CreatedAt = DateTimeOffset.Now,
            Title = DevMenuFactory.DevMenuTitle,
            Slug = DevMenuFactory.DevMenuSlug,
            Disabled = false,
            Tags = [DevMenuFactory.SystemTag],
        };

        var items = new List<NavMenuSummary> { devMenu };
        items.AddRange(result.Items);
        if (items.Count > query.Take) items = [.. items.Take(query.Take)];

        return new ListDataResult<NavMenuSummary>(items, result.HasMoreData, (result.TotalCount ?? 0) + 1);
    }

    static bool MatchesSearch(string value, string? search)
        => string.IsNullOrWhiteSpace(search) || value.Contains(search, StringComparison.OrdinalIgnoreCase);

    public async Task<Guid> Create(CreateNavMenuQuery query, CancellationToken cancellationToken)
    {
        var id = await _navMenuRepository.Create(query, cancellationToken);
        var created = await Get(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.NavMenuAdd(), created!);//TODO: сделать явный тип.
        _eventManager.TriggerEvent(payload);
        ClearActiveMenusCache();

        return id;
    }

    public async Task Update(UpdateNavMenuQuery query, CancellationToken cancellationToken)
    {
        if (query.Id == DevMenuFactory.DevMenuId)
            query = EnforceSystemMenuInvariants(query);

        var exists = await _navMenuRepository.Get(query.Id, cancellationToken) is not null;

        if (exists)
        {
            await _navMenuRepository.Update(query, cancellationToken);
        }
        else if (query.Id == DevMenuFactory.DevMenuId)
        {
            // первое редактирование dev menu — сохраняем копию в БД
            await _navMenuRepository.Create(new CreateNavMenuQuery
            {
                Id = query.Id,
                Title = query.Title,
                Slug = query.Slug,
                Disabled = query.Disabled,
                Tags = query.Tags,
                MenuItems = query.MenuItems,
                Class = query.Class,
                Style = query.Style,
                Roles = query.Roles,
                RolesInverse = query.RolesInverse,
            }, cancellationToken);
        }
        else
        {
            throw new NotFoundException();
        }

        var updated = (await Get(query.Id, cancellationToken))!;

        var payload = new ManagerEventPayload(_eventManager.Defaults.NavMenuUpdate(), updated);
        _eventManager.TriggerEvent(payload);
        ClearActiveMenusCache();
    }

    /// <summary>
    /// Системное меню нельзя лишить тега system или увести с дефолтного slug
    /// (админ-панель ищет его по slug "dev").
    /// </summary>
    static UpdateNavMenuQuery EnforceSystemMenuInvariants(UpdateNavMenuQuery query)
    {
        var tags = query.Tags.Contains(DevMenuFactory.SystemTag)
            ? query.Tags
            : [.. query.Tags, DevMenuFactory.SystemTag];

        return query with { Slug = DevMenuFactory.DevMenuSlug, Tags = tags };
    }

    public async Task<NavMenuSummary> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (id == DevMenuFactory.DevMenuId)
            throw new UserActionException("Системное меню нельзя удалить. Используйте «Сбросить к дефолту».");

        var navMenu = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _navMenuRepository.Delete(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.NavMenuDelete(), navMenu);
        _eventManager.TriggerEvent(payload);
        ClearActiveMenusCache();

        return navMenu;
    }

    public async Task<IReadOnlyCollection<NavMenuSummary>> DeleteMany(DeleteManyNavMenuQuery query, CancellationToken cancellationToken)
    {
        if (query.Ids.Contains(DevMenuFactory.DevMenuId))
            throw new UserActionException("Системное меню нельзя удалить. Используйте «Сбросить к дефолту».");

        var navMenus = await _navMenuRepository.ListAll(new() { Ids = query.Ids }, cancellationToken);

        await _navMenuRepository.DeleteMany(query, cancellationToken);

        foreach (var navMenu in navMenus)
        {
            var payload = new ManagerEventPayload(_eventManager.Defaults.NavMenuDelete(), navMenu);
            _eventManager.TriggerEvent(payload);
        }
        ClearActiveMenusCache();

        return navMenus;
    }

    /// <summary>
    /// Сброс системного меню к дефолту: удаляет сохранённую в БД копию,
    /// после чего меню снова отдаётся генерируемым кодом состоянием.
    /// </summary>
    public async Task Reset(Guid id, CancellationToken cancellationToken)
    {
        if (id != DevMenuFactory.DevMenuId)
            throw new UserActionException("Сбросить к дефолту можно только системное меню.");

        var navMenu = await _navMenuRepository.Get(id, cancellationToken);
        if (navMenu is null) return; // не сохранено — сбрасывать нечего

        await _navMenuRepository.Delete(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.NavMenuDelete(), navMenu);
        _eventManager.TriggerEvent(payload);
        ClearActiveMenusCache();
    }

    public Task<NavMenuExport> Export(Guid id)
    {
        throw new NotImplementedException();
        //return await Get(id);
    }

    public Task<UserActionResult> Import(Guid id, NavMenuImport navMenu)
    {
        throw new NotImplementedException();
        //var exist = await Get(id);

        //if (exist == null)
        //{
        //    return new UserActionResult
        //    {
        //        Message = "not found"
        //    };
        //}

        //navMenu.Id = id;

        //var upd = await Update(id, navMenu);

        //return new UserActionResult
        //{
        //    Ok = true,
        //    Message = "success import"
        //};
    }

    /// <summary>
    /// Dev menu: сохранённая в БД копия, смерженная с дефолтным (генерируемым кодом) состоянием.
    /// Пока копии нет в БД — отдаётся дефолт.
    /// </summary>
    public NavMenuDetail DevMenu()
    {
        return _memoryCache.GetOrCreate(DevMenuKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheTtl;

            var postTypes = _postTypeRepository.ListAllActive(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            var defaultMenu = DevMenuFactory.Build(postTypes);

            var dbMenu = _navMenuRepository.GetDetail(DevMenuFactory.DevMenuId, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

            return dbMenu is null ? defaultMenu : DevMenuFactory.Merge(dbMenu, defaultMenu);
        })!;
    }

    public IReadOnlyCollection<NavMenuDetail> GetAppInitialDataMenus(bool includeDevMenu = false)
    {
        var activeMenus = _memoryCache.GetOrCreate(ActiveMenusMenuKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
            return _navMenuRepository.ListAllActiveDetail(new(), CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        })!;

        // системные меню (тег system) не отдаются публичному фронту
        var menus = activeMenus.Where(s => !s.Tags.Contains(DevMenuFactory.SystemTag)).ToList();

        if (!includeDevMenu) return menus;

        var devMenu = DevMenu();
        if (devMenu.Disabled) return menus;

        devMenu = devMenu with { MenuItems = devMenu.MenuItems.Where(s => !s.Disabled).ToList() };
        return [.. menus, devMenu];
    }

    public void ClearActiveMenusCache()
    {
        _memoryCache.Remove(ActiveMenusMenuKey);
        _memoryCache.Remove(DevMenuKey);
    }
}
