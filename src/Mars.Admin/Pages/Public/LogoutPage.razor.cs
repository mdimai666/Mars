using Mars.Admin.Framework.AuthProviders;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Pages.Public;

public partial class LogoutPage
{
    [Inject] public IAuthenticationService AuthenticationService { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await AuthenticationService.Logout();
        NavigationManager.NavigateTo("/dev/Login");
    }
}
