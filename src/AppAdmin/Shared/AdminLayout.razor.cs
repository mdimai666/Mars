using AppFront.Shared.AuthProviders;
using AppFront.Shared.Features;
using AppFront.Shared.Models;
using Mars.Shared.Contracts.NavMenus;
using Mars.Shared.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Toolbelt.Blazor.HotKeys2;

namespace AppAdmin.Shared;

using MenuItem = AppFront.Shared.Models.MenuItem;

public partial class AdminLayout : LayoutComponentBase, IAsyncDisposable
{
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService vms { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [CascadingParameter] public Task<AuthenticationState> AuthState { get; set; } = default!;

    private List<MenuItem> menu_items = [];

    bool _menuDrawerVisible = !true;
    public bool MenuDrawerVisible
    {
        get => _menuDrawerVisible;
        set
        {
            _menuDrawerVisible = value;
            OpenMobileMenu();
        }
    }

    [Inject] HotKeys HotKeys { get; set; } = default!;
    HotKeysContext HotKeysContext = default!;

    HeaderAdmin1 headerAdmin = default!;

    protected override void OnAfterRender(bool firstRender)
    {
        JSRuntime.InvokeVoidAsync("d_onPageLoad");
    }

    private void NavigationManager_LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        MenuDrawerOnClose();
    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState;

        navigationManager.LocationChanged += NavigationManager_LocationChanged;

        //if (Q.Profile.Username == "anonymous")
        //{
        //    //Navigation.NavigateTo($"Login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}");
        //}

        //Q.Root.On(typeof(Profile), EmitTypeMode.All, d =>
        //{
        //    StateHasChanged();
        //});
        Q.Root.On(typeof(UserFromClaims), EmitTypeMode.All, d =>
        {
            StateHasChanged();
        });
        Q.Root.On(typeof(InitialSiteDataViewModel), EmitTypeMode.All, d =>
        {
            UpdateMenuItems();
            StateHasChanged();
        });

        UpdateMenuItems();

        //Load();

        HotKeysContext = HotKeys.CreateContext()
             .Add(ModCode.None, Code.F1, () => headerAdmin.FocusActionCenter(), "Focus Action center");
    }

    bool collapsed;

    void UpdateMenuItems()
    {
        var devMenu = Q.Site.NavMenus.First(s => s.Slug == "dev");
        devMenu = devMenu with { MenuItems = devMenu.MenuItems.Where(MenuRolesCheck).ToList() };

        //remove last divider
        if (devMenu.MenuItems.LastOrDefault()?.IsDivider ?? false)
        {
            devMenu = devMenu with { MenuItems = devMenu.MenuItems.Take(devMenu.MenuItems.Count - 1).ToList() };
        }
        menu_items = MyNavMenu.Convert(devMenu);

    }

    bool MenuRolesCheck(NavMenuItemResponse item)
    {
        // роли пользователя (добавляем "Viewer" по умолчанию)
        var userRoles = Q.User.Roles.Append("Viewer").ToHashSet(StringComparer.OrdinalIgnoreCase);
        //Console.WriteLine("userRoles=" + userRoles.JoinStr(","));

        // если у меню нет указанных ролей — доступно всем (включая Viewer)
        if (item.Roles == null || !item.Roles.Any())
            return true;

        // пересечение ролей пользователя и меню
        bool hasIntersection = item.Roles.Intersect(userRoles, StringComparer.OrdinalIgnoreCase).Any();

        return item.RolesInverse ? !hasIntersection : hasIntersection;
    }

    void onCollapse(bool collapsed)
    {
        //Console.WriteLine(collapsed);
        this.collapsed = collapsed;
        //_ = localStorage.SetItemAsync("main-layout-sidebar", collapsed);
    }

    //async void Load()
    //{
    //    try
    //    {
    //        //collapsed = await localStorage.GetItemAsync<bool>("main-layout-sidebar");
    //        //Console.WriteLine("collapsed = " + collapsed);
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine("ex collapsed: " + ex.Message);
    //    }
    //    StateHasChanged();
    //}

    public async ValueTask DisposeAsync()
    {
        navigationManager.LocationChanged -= NavigationManager_LocationChanged;
        if (HotKeysContext is not null)
            await HotKeysContext.DisposeAsync();
    }

    void MenuDrawerOnClose()
    {
        MenuDrawerVisible = false;
    }

    private IDialogReference? _dialog;

    async void OpenMobileMenu()
    {
        if (!_menuDrawerVisible) return;

        DialogParameters<List<MenuItem>> parameters = new()
        {
            Content = menu_items,
            //Title = $"Hello {simplePerson.Firstname}",
            Alignment = HorizontalAlignment.Left,
            Modal = true,
            //ShowDismiss = false,
            //PrimaryAction = "Maybe",
            //SecondaryAction = "Cancel",

            Width = "80vw",
        };
        _dialog = await _dialogService.ShowPanelAsync<MobileMenu>(menu_items, parameters);
        DialogResult result = await _dialog.Result;
        //HandlePanel(result);
    }

}
