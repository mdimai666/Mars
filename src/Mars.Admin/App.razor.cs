using System.Reflection;
using Mars.Admin.Pages.Public;
using Mars.Admin.Framework.AuthProviders;
using Mars.Admin.Framework.Hub;
using Mars.Admin.Contracts.Options;
using Mars.Plugin.Front;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Toolbelt.Blazor;
using Toolbelt.Blazor.HotKeys2;

namespace Mars.Admin;

public partial class App
{
    static RouteData trackRouteData = default!;
    public static Type PageType => trackRouteData?.PageType ?? typeof(App);
    [Inject] IAuthenticationService AuthenticationService { get; set; } = default!;
    [Inject] AuthenticationStateProvider authStateProvider { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IJSRuntime JSRuntime { get; set; } = default!;

    [Inject] HttpClientInterceptor Interceptor { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;
    [Inject] ILogger<App> _logger { get; set; } = default!;
    [Inject] ClientHub hub { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] DeveloperControlService controlService { get; set; } = default!;
    [Inject] HotKeys HotKeys { get; set; } = default!;

    public static bool IsDevelopment => Q.IsDevelopment;

    HotKeysContext appHotKeysContext = default!;
    FluentDesignSystemProvider? _fluentDesignSystemProvider;

    protected override async Task OnInitializedAsync()
    {
        _logger.LogTrace("App.OnInitializedAsync...");
        Interceptor.AfterSend += Interceptor_AfterSend!;
        Q.Root.On("GoBack", () => JSRuntime.InvokeVoidAsync("history.back"));
        Q.Root.On("App.SetupTheme", SetupThemeExternal);

        appHotKeysContext = HotKeys.CreateContext()
                                .Add(Code.F9, OpenPageSource, "open page source");

        var vm = await viewModelService.GetLocalInitialSiteDataViewModel();
        Q.UpdateInitialSiteData(vm);
        _logger.LogTrace("App.OnInitializedAsync - UpdateInitialSiteData updated.");
        SetupTheme();

        hub.OnShowNotifyMessage += Hub_OnShowNotifyMessage;
        _logger.LogTrace("App.OnInitializedAsync - finish.");
    }

    private void Interceptor_AfterSend(object sender, HttpClientInterceptorEventArgs e)
    {
        bool isPublicPage = PageType.Namespace.StartsWith("Mars.Admin.Pages.Public");

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
