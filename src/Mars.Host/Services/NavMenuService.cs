using System.Data;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.NavMenus;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Managers.Extensions;
using Mars.Host.Shared.Repositories;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Resources;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Services;

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

    public Task<NavMenuSummary?> Get(Guid id, CancellationToken cancellationToken)
        => _navMenuRepository.Get(id, cancellationToken);

    public Task<NavMenuDetail?> GetDetail(Guid id, CancellationToken cancellationToken)
        => _navMenuRepository.GetDetail(id, cancellationToken);

    public Task<ListDataResult<NavMenuSummary>> List(ListNavMenuQuery query, CancellationToken cancellationToken)
        => _navMenuRepository.List(query, cancellationToken);

    public Task<PagingResult<NavMenuSummary>> ListTable(ListNavMenuQuery query, CancellationToken cancellationToken)
        => _navMenuRepository.ListTable(query, cancellationToken);

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
        await _navMenuRepository.Update(query, cancellationToken);
        var updated = (await Get(query.Id, cancellationToken))!;

        var payload = new ManagerEventPayload(_eventManager.Defaults.NavMenuUpdate(), updated);
        _eventManager.TriggerEvent(payload);
        ClearActiveMenusCache();
    }

    public async Task<NavMenuSummary> Delete(Guid id, CancellationToken cancellationToken)
    {
        var navMenu = await Get(id, cancellationToken) ?? throw new NotFoundException();

        await _navMenuRepository.Delete(id, cancellationToken);

        var payload = new ManagerEventPayload(_eventManager.Defaults.NavMenuDelete(), navMenu);
        _eventManager.TriggerEvent(payload);
        ClearActiveMenusCache();

        return navMenu;
    }

    public async Task<IReadOnlyCollection<NavMenuSummary>> DeleteMany(DeleteManyNavMenuQuery query, CancellationToken cancellationToken)
    {
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

    public NavMenuDetail DevMenu()
    {
        return _memoryCache.GetOrCreate(DevMenuKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
            var postTypes = _postTypeRepository.ListAllActive(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            var devMenu = CreateDevMenu(postTypes);
            return devMenu;
        })!;
    }

    NavMenuDetail CreateDevMenu(IEnumerable<PostTypeSummary> postTypes)
    {
        var d = "/dev/";

        var dev_menu = new Guid("9596ffe0-f688-452c-885e-e72f1123e50d");
        var razdels = new Guid("9f34a009-e39e-4c7c-80bf-be6efa4dc8da");
        var manage = new Guid("a7bc610c-412d-4292-9e0c-dd126725e285");

        List<string> adminRoles = ["Admin"];

        var menu = new NavMenuDetail()
        {
            Roles = [],
            Slug = "dev",
            Title = "Dev admin menu",
            MenuItems = ((MenuItemInternal[])[
                new(){ Title = "Главная" , Url = d, },
                new(){ Title = AppRes.Media , Url = d+"Media"  },
                new(){ IsDivider = true },
                new(){ Title = "Записи", Url = d+"Post/post", },
                ..postTypes.Where(s=>s.TypeName!="post").OrderBy(s=>s.Title).Select(postType=>new MenuItemInternal(){
                    Title = postType.Title,
                    Url = d+$"Post/{postType.TypeName}",
                }),
                new(){ IsDivider = true },
                new(){ Title = "Типы", Url = d+"PostType", Roles=adminRoles },
                new(){ Title = "Меню", Url = d+"NavMenu", Roles=adminRoles },
                new(){ Title = "Разделы", Url = "#razdels", Id=razdels, Roles=adminRoles },
                    new(){ Title = "Письма", Url = d+"FeedbackList", ParentId=razdels },
                    //new(){ Title = "Geo", Url = d+"geo/GeoRegion", ParentId=razdels },
                new(){ Title = "Управление", Url = d+"Manage", Id=manage, Roles = adminRoles },
                    //new() { Title = "Анкета", Url = d+"Manage/AnketaManage", ParentId=manage },
                    new() { Title = AppRes.Users, Url = d+"Users", ParentId=manage},
                    new() { Title = AppRes.UserTypes, Url = d+"UserType", ParentId=manage},
                    //new() { Title = "Контакты", Url = d+"ContactsManagement", ParentId=manage},
    #if DEBUG
		            //new() { Title = "Роли", Url = d+"RoleManagement", ParentId=manage},
	#endif
                    //new() { Title = "Комментарии", Url = d+"Comments", ParentId=manage},
                new(){ IsDivider = true },
                new (){ Title = AppRes.Plugins, Url = d+"Plugins", Roles=adminRoles },
                new (){ Title = "Настройки", Url = d+"Settings", Roles=adminRoles },

            ]).Select(NewMenuItem).ToList(),

            CreatedAt = DateTime.Now,
            ModifiedAt = null,
            Disabled = false,
            Id = dev_menu,
            RolesInverse = false,
            Class = "",
            Style = "",
            Tags = []
        };

        return menu;
    }

    class MenuItemInternal
    {
        public Guid Id = Guid.NewGuid();
        public string? Title;
        public string? Url;
        public bool IsDivider = false;
        public bool IsHeader = false;
        public IReadOnlyCollection<string>? Roles;
        public Guid ParentId;

    }

    NavMenuItemDto NewMenuItem(MenuItemInternal item)
        => new()
        {
            Title = item.Title ?? "",
            Url = item.Url ?? "",

            Id = item.Id,
            Class = "",
            Style = "",
            Disabled = false,
            Icon = "",
            IsDivider = item.IsDivider,
            IsHeader = item.IsHeader,
            OpenInNewTab = false,
            ParentId = item.ParentId,
            Roles = item.Roles ?? [],
            RolesInverse = false
        };

    public IReadOnlyCollection<NavMenuDetail> GetAppInitialDataMenus(bool includeDevMenu = false)
    {
        var activeMenus = _memoryCache.GetOrCreate(ActiveMenusMenuKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheTtl;
            return _navMenuRepository.ListAllActiveDetail(new(), CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        })!;

        return includeDevMenu ? [.. activeMenus, DevMenu()] : activeMenus;
    }

    public void ClearActiveMenusCache()
    {
        _memoryCache.Remove(ActiveMenusMenuKey);
    }
}
