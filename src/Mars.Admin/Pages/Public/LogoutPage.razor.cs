using AppFront.Shared.AuthProviders;
using AppFront.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace AppAdmin.Pages.Public;

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
