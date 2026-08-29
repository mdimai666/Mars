using Mars.Admin.Framework.AuthProviders;
using Mars.Admin.Framework.Features;
using Mars.Server.Contracts.Systems;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

using MenuItem = Mars.Admin.Framework.Models.MenuItem;

namespace Mars.Admin.Shared;

public partial class BuilderLayout
{
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService vms { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [CascadingParameter] public Task<AuthenticationState> AuthState { get; set; } = default!;

    private List<MenuItem> menu_items = [];

    public SystemMinStatResponse hostAppStat = SystemMinStatResponse.Empty();

    protected override void OnAfterRender(bool firstRender)
    {
        JSRuntime.InvokeVoidAsync("d_onPageLoad");
    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState;

        Q.Root.On(typeof(UserFromClaims), EmitTypeMode.All, d =>
        {
            StateHasChanged();
        });

        Console.WriteLine("BuilderLayout.OnInitialized");
        AfterLoad();

    }

    async void AfterLoad()
    {
        if (Q.User.IsAuth && Q.User.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            hostAppStat = await client.System.SystemMinStat();
            StateHasChanged();
        }
    }

    public void _StateHasChanged()
    {
        StateHasChanged();
    }

}
