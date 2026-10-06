using Mars.Admin.Contracts.ViewModels;
using Mars.Admin.Framework.AuthProviders;
using Mars.Admin.Framework.Models;
using Mars.Admin.Shared.ActionCenter;
using Mars.Cms.Contracts.NavMenus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Toolbelt.Blazor.HotKeys2;
using MenuItem = Mars.Admin.Framework.Models.MenuItem;

namespace Mars.Admin.Shared.Cloudy;

public partial class CloudyLayout : LayoutComponentBase, IAsyncDisposable
{
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] HotKeys HotKeys { get; set; } = default!;
    [Inject] ActionCenterService ActionCenterService { get; set; } = default!;
    [CascadingParameter] public Task<AuthenticationState> AuthState { get; set; } = default!;

    HotKeysContext HotKeysContext = default!;

    internal List<MenuItem> menu_items = [];

    protected override void OnAfterRender(bool firstRender)
    {
        JSRuntime.InvokeVoidAsync("d_onPageLoad");
    }

    protected override void OnInitialized()
    {
        Q.Root.On(typeof(UserFromClaims), EmitTypeMode.All, d => StateHasChanged());
        Q.Root.On(typeof(InitialSiteDataViewModel), EmitTypeMode.All, d =>
        {
            UpdateMenuItems();
            StateHasChanged();
        });
        Q.Root.On(typeof(AppInitialViewModel), EmitTypeMode.All, d =>
        {
            UpdateMenuItems();
            StateHasChanged();
        });

        UpdateMenuItems();

        HotKeysContext = HotKeys.CreateContext()
             .Add(ModCode.None, Code.F1, () => ActionCenterService.Toggle(), "Open Action center")
             .Add(ModCode.Ctrl, Code.K, () => ActionCenterService.Toggle(), "Open Action center");
    }

    void UpdateMenuItems()
    {
        var devMenu = Q.Site?.NavMenus?.FirstOrDefault(s => s.Slug == "dev");
        if (devMenu is null) return;

        devMenu = devMenu with { MenuItems = devMenu.MenuItems.Where(MenuRolesCheck).ToList() };

        if (devMenu.MenuItems.LastOrDefault()?.IsDivider ?? false)
        {
            devMenu = devMenu with { MenuItems = devMenu.MenuItems.Take(devMenu.MenuItems.Count - 1).ToList() };
        }
        menu_items = MenuItem.Convert(devMenu);
    }

    bool MenuRolesCheck(NavMenuItemResponse item)
    {
        var userRoles = Q.User.Roles.Append("Viewer").ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (item.Roles == null || !item.Roles.Any())
            return true;

        bool hasIntersection = item.Roles.Intersect(userRoles, StringComparer.OrdinalIgnoreCase).Any();
        return item.RolesInverse ? !hasIntersection : hasIntersection;
    }

    public async ValueTask DisposeAsync()
    {
        if (HotKeysContext is not null)
            await HotKeysContext.DisposeAsync();
    }
}
