using FluentAssertions;
using Mars.Identity.Abstractions.Dto.Profile;
using Mars.Identity.Abstractions.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers;

/// <summary>
/// T4 «роли = наборы прав»: новые локальные пользователи при регистрации получают роль Author
/// (продуктовое решение «обычные юзеры создают свои посты»). SSO-пользователи остаются Viewer.
/// RegisterUser вызывается напрямую через DI: HTTP-эндпоинт гейтится опцией AllowUsersSelfRegister
/// (по умолчанию false), которая к назначению роли отношения не имеет.
/// </summary>
public sealed class RegistrationDefaultRoleTests : ApplicationTests
{
    public RegistrationDefaultRoleTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    [IntegrationFact]
    public async Task RegisterUser_NewLocalUser_GetsAuthorRole()
    {
        //Arrange
        using var scope = AppFixture.ServiceProvider.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountsService>();
        var email = $"reg{Guid.NewGuid().ToString("N")[..8]}@mail.localhost";
        var query = new UserForRegistrationQuery
        {
            Email = email,
            Password = "Password123@",
            FirstName = "Reg",
            LastName = "User",
        };

        //Act
        var result = await accounts.RegisterUser(query, CancellationToken.None);

        //Assert
        result.IsSuccessfulRegistration.Should().BeTrue();

        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        var user = ef.Users.AsNoTracking().Include(s => s.Roles).Single(s => s.Email == email);
        user.Roles!.Select(s => s.Name).Should().BeEquivalentTo(["Author"]);
    }
}
