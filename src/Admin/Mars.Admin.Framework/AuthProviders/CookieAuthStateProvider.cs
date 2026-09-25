using System.Security.Claims;
using Mars.Admin.Framework.Services;
using Mars.Identity.Contracts.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;

namespace Mars.Admin.Framework.AuthProviders;

/// <summary>
/// Cookie-схема (A1): клиент не хранит токенов — браузер сам шлёт Identity-cookie,
/// а состояние сессии берётся из server-rendered данных хост-страницы
/// (<see cref="UserPrimaryInfo"/> в InitialSiteDataViewModel; для анонимного
/// запроса сервер отдаёт null). После логина/логаута страница перезагружается
/// полностью (forceLoad), поэтому состояние всегда свежее.
/// </summary>
public class CookieAuthStateProvider : AuthenticationStateProvider
{
    private readonly ViewModelService _viewModelService;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
    private bool _loggedOut;

    public CookieAuthStateProvider(ViewModelService viewModelService)
    {
        _viewModelService = viewModelService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_loggedOut)
            return new AuthenticationState(_anonymous);

        var vm = await _viewModelService.GetLocalInitialSiteDataViewModel();
        var info = vm.InitialUserPrimaryInfo;

        if (info is null || info.Id == Guid.Empty)
            return new AuthenticationState(_anonymous);

        if (Q.User.Id != info.Id)
            Q.UpdateUserByInitialVM(info, null);

        return new AuthenticationState(BuildPrincipal(info));
    }

    public Task MarkUserAsLoggedOut()
    {
        _loggedOut = true;
        Q.LogoutUser();
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        return Task.CompletedTask;
    }

    private static ClaimsPrincipal BuildPrincipal(UserPrimaryInfo info)
    {
        var identity = new ClaimsIdentity("cookie");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, info.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, info.Username));
        identity.AddClaim(new Claim(ClaimTypes.Email, info.Email ?? ""));
        identity.AddClaim(new Claim(ClaimTypes.GivenName, info.FirstName));
        identity.AddClaim(new Claim(ClaimTypes.Surname, info.LastName));
        foreach (var role in info.Roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return new ClaimsPrincipal(identity);
    }
}
