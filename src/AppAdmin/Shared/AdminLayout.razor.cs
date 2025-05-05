using AppFront.Shared.AuthProviders;
using AppFront.Shared.Features;
using AppFront.Shared.Models;
using Mars.Shared.ViewModels;
using Microsoft.AspNetCore.Components;
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

    private List<MenuItem> menu_items = new();

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

    protected override void OnInitialized()
    {
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

        this.HotKeysContext = this.HotKeys.CreateContext()
             .Add(ModCode.None, Code.F1, () => headerAdmin.FocusActionCenter(), "Focus Action center");
    }

    bool collapsed;

    void UpdateMenuItems()
    {
        var devMenu = Q.Site.NavMenus.First(s => s.Slug == "dev");
        menu_items = MyNavMenu.Convert(devMenu);

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
