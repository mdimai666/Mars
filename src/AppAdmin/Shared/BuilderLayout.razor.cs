using AppFront.Shared.AuthProviders;
using AppFront.Shared.Features;
using Mars.Shared.Contracts.Systems;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AppAdmin.Shared;

using MenuItem = AppFront.Shared.Models.MenuItem;

public partial class BuilderLayout
{
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService vms { get; set; } = default!;
    [Inject] IMarsWebApiClient client { get; set; } = default!;

    private List<MenuItem> menu_items = new();

    public SystemMinStatResponse hostAppStat = SystemMinStatResponse.Empty();

    protected override void OnAfterRender(bool firstRender)
    {
        JSRuntime.InvokeVoidAsync("d_onPageLoad");
    }

    protected override void OnInitialized()
    {
        Q.Root.On(typeof(UserFromClaims), EmitTypeMode.All, d =>
        {
            StateHasChanged();
        });

        Console.WriteLine("BuilderLayout.OnInitialized");
        AfterLoad();

    }

    async void AfterLoad()
    {
        if (Q.User.IsAuth)
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
