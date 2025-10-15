using System.Reflection;
using AppAdmin.Pages.Public;
using AppFront.Shared.AuthProviders;
using AppFront.Shared.Hub;
using Mars.Options.Models;
using Mars.Plugin.Front;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Toolbelt.Blazor;
using Toolbelt.Blazor.HotKeys2;

namespace AppAdmin;

public partial class App
{
    static RouteData trackRouteData = default!;
    public static Type PageType => trackRouteData?.PageType ?? typeof(App);

    [CascadingParameter]
    protected Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject] AuthenticationService AuthenticationService { get; set; } = default!;
    [Inject] AuthenticationStateProvider authStateProvider { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IJSRuntime JSRuntime { get; set; } = default!;

    [Inject] HttpClientInterceptor Interceptor { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;
    [Inject] ILogger<App> _logger { get; set; } = default!;
    [Inject] ClientHub hub { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] DeveloperControlService controlService { get; set; } = default!;
    [Inject] HotKeys HotKeys { get; set; } = default!;
    HotKeysContext appHotKeysContext = default!;
    FluentDesignSystemProvider? _fluentDesignSystemProvider;

    protected override async Task OnInitializedAsync()
    {
        Interceptor.AfterSend += Interceptor_AfterSend!;
        Q.Root.On("GoBack", () => JSRuntime.InvokeVoidAsync("history.back"));
        Q.Root.On("App.SetupTheme", SetupThemeExternal);

        appHotKeysContext = HotKeys.CreateContext()
                                .Add(Code.F9, OpenPageSource, "open page source");

        await viewModelService.GetLocalInitialSiteDataViewModel();
        //if (NavigationManager.BaseUri.Contains(":5185") == false)
        //{
        //    _ = AuthenticationService.GetProfile();
        //}
        SetupTheme();

        hub.OnShowNotifyMessage += Hub_OnShowNotifyMessage;
    }

    private void Interceptor_AfterSend(object sender, HttpClientInterceptorEventArgs e)
    {
        bool isPublicPage = PageType.Namespace.StartsWith("AppAdmin.Pages.Public");

        //!NavigationManager.Path().Equals("Login", StringComparison.OrdinalIgnoreCase)

        if (e.Response is not null && e.Response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !isPublicPage && PageType != typeof(LoginPage))
        {
            _logger.LogWarning("App::Unauthorized");
            Task.Run(async () =>
            {
                await AuthenticationService.Logout();
                NavigationManager.NavigateTo("/dev/Login");
            });
        }

    }

    private void Hub_OnShowNotifyMessage(string message, Mars.Core.Models.MessageIntent messageIntent)
    {
        if (!Q.User.IsAuth) return;
        messageService.Show(message, messageIntent);
    }

    void OpenPageSource()
    {
        controlService.OpenPageSource(App.PageType);
    }

    StylerStyle styler = new();

    void SetupTheme()
    {
        var devAdminStyle = Q.Site.GetOption<DevAdminStyleOption>();
        styler = devAdminStyle.StylerStyle;
    }

    void SetupThemeExternal()
    {
        SetupTheme();
        StateHasChanged();
    }
    IEnumerable<Assembly> AdditionalAssemblies => WebAssemblyPluginFrontExtensions.PluginLoadAssemblies;

}
