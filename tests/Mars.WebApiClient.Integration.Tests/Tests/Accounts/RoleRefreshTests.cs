using System.Net.Http.Json;
using FluentAssertions;
using Mars.Admin.Contracts.ViewModels;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Contracts.Auth;
using Mars.Identity.Contracts.ViewModels;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using static Mars.Test.Common.Constants.UserConstants;

namespace Mars.WebApiClient.Integration.Tests.Tests.Accounts;

/// <summary>
/// Смена ролей должна применяться к живой cookie-сессии на следующем же запросе
/// (ISecurityStampCache + CookiePrincipalValidator), без перелогина и без ожидания
/// ValidationInterval.
/// </summary>
public class RoleRefreshTests(ApplicationFixture appFixture) : BaseWebApiClientTests(appFixture)
{
    [IntegrationFact]
    public async Task RoleChange_AppliesToActiveCookieSession_Immediately()
    {
        var http = AppFixture.GetClientEx(isAnonymous: true);

        var loginResponse = await http.PostAsJsonAsync("/api/Account/Login",
            new AuthCredentialsRequest { Login = TestUserUsername, Password = TestUserPassword });
        var login = await loginResponse.Content.ReadFromJsonAsync<AuthResultResponse>();
        login!.IsAuthSuccessful.Should().BeTrue();

        var before = await GetPrimaryInfoAsync(http);
        before.Roles.Should().NotContain(r => string.Equals(r, "manager", StringComparison.OrdinalIgnoreCase));

        using var scope = AppFixture.ServiceProvider.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var result = await userRepo.UpdateUserRoles(TestUserId, ["admin", "manager"], default);
        result.Ok.Should().BeTrue();

        // та же cookie, без перелогина — claims в principal уже свежие
        var after = await GetPrimaryInfoAsync(http);
        after.Roles.Should().Contain(r => string.Equals(r, "manager", StringComparison.OrdinalIgnoreCase));
    }

    static async Task<UserPrimaryInfo> GetPrimaryInfoAsync(HttpClient http)
    {
        var vm = await http.GetFromJsonAsync<InitialSiteDataViewModel>("/vm/ViewModel/InitialSiteDataViewModel");
        vm!.UserPrimaryInfo.Should().NotBeNull();
        return vm.UserPrimaryInfo!;
    }
}
