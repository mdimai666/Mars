using AppFront.Shared;
using AppFront.Shared.AuthProviders;
using AppFront.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorTemplateExample;

public partial class App
{
    static RouteData trackRouteData = default!;
    public static Type PageType => trackRouteData?.PageType ?? typeof(App);
    [Inject] IAuthenticationService AuthenticationService { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;

    [Parameter]
    public bool IsPrerenderProcess { get; set; }

    protected override async Task OnInitializedAsync()
    {
        //var vm = await viewModelService.GetLocalInitialSiteDataViewModel();
        //Q.UpdateInitialSiteData(vm);
    }
}
