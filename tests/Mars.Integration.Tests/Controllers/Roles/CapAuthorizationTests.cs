using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.ViewModels;
using Mars.Identity.Host.Controllers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Roles;

/// <summary>
/// T3 «роли = наборы прав»: CapAttribute → CapPolicyProvider → CapAuthorizationHandler.
/// Гейтимый эндпоинт — GET /api/Role/list/offset (кап Role.Manage).
/// Кэш — singleton фикстуры, поэтому права ролей меняются через SaveRoleClaims (инвалидирует)
/// либо с явным Invalidate после правки БД.
/// </summary>
/// <seealso cref="RoleController"/>
public sealed class CapAuthorizationTests : ApplicationTests
{
    const string _apiUrl = "/api/Role";

    public CapAuthorizationTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    IRoleCapsCache Cache => AppFixture.ServiceProvider.GetRequiredService<IRoleCapsCache>();

    [IntegrationFact]
    public async Task CapEndpoint_UserWithGrantedCap_200()
    {
        //Arrange
        var role = CreateRole();
        GrantCaps(role, RoleCaps.RoleCap.Manage);
        var user = CreateUserWithRole(role);
        var client = AppFixture.GetClientForUser(user.Id);

        //Act
        var result = await client.Request(_apiUrl, "list", "offset").AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [IntegrationFact]
    public async Task CapEndpoint_UserWithoutCap_403()
    {
        //Arrange
        var role = CreateRole();
        var user = CreateUserWithRole(role);
        var client = AppFixture.GetClientForUser(user.Id);

        //Act
        var result = await client.Request(_apiUrl, "list", "offset").AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [IntegrationFact]
    public async Task CapEndpoint_Anonymous_401()
    {
        //Arrange
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request(_apiUrl, "list", "offset").AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task CapEndpoint_Admin_Succeeds_WithoutClaims()
    {
        //Arrange — T4 сеет Admin пресет всех прав; снимаем клеймы, чтобы проверить bypass по роли, а не по правам
        var ef = AppFixture.MarsDbContext();
        var adminRole = ef.Roles.AsNoTracking().First(s => s.Name == "Admin");
        var adminCaps = ef.RoleClaims.Where(s => s.RoleId == adminRole.Id && s.ClaimType == RoleCaps.ClaimType).ToList();
        ef.RoleClaims.RemoveRange(adminCaps);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        Cache.Invalidate();
        ef.RoleClaims.AsNoTracking()
            .Any(s => s.RoleId == adminRole.Id && s.ClaimType == RoleCaps.ClaimType)
            .Should().BeFalse();
        var client = AppFixture.GetClient(); // TestUser — Admin

        //Act
        var result = await client.Request(_apiUrl, "list", "offset").AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [IntegrationFact]
    public async Task CapEndpoint_SaveRoleClaims_FreshWithoutRelogin()
    {
        //Arrange
        var role = CreateRole();
        var user = CreateUserWithRole(role);
        var userClient = AppFixture.GetClientForUser(user.Id);
        var adminClient = AppFixture.GetClient();

        var before = await userClient.Request(_apiUrl, "list", "offset").AllowAnyHttpStatus().GetAsync();
        before.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        //Act — админ выдаёт право через SaveRoleClaims; пользователь не перелогинивается
        await SaveCaps(adminClient, role, RoleCaps.RoleCap.Manage);
        var afterGrant = await userClient.Request(_apiUrl, "list", "offset").AllowAnyHttpStatus().GetAsync();

        //Assert
        afterGrant.StatusCode.Should().Be(StatusCodes.Status200OK);

        //Act — снятие права тоже действует сразу
        await SaveCaps(adminClient, role);
        var afterRevoke = await userClient.Request(_apiUrl, "list", "offset").AllowAnyHttpStatus().GetAsync();

        //Assert
        afterRevoke.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    static async Task SaveCaps(IFlurlClient adminClient, RoleEntity role, params string[] caps)
    {
        var dto = await adminClient.Request($"{_apiUrl}/{nameof(RoleController.EditRolesViewModel)}")
                                     .GetJsonAsync<EditRolesViewModelDto>();
        var entry = dto.RoleClaims.Single(s => s.Role.Id == role.Id);
        foreach (var cap in entry.Groups.SelectMany(s => s.RoleCapElements))
        {
            cap.Checked = caps.Contains(cap.Id);
        }
        await adminClient.Request($"{_apiUrl}/{nameof(RoleController.SaveRoleClaims)}").PutJsonAsync(dto);
    }

    RoleEntity CreateRole()
    {
        Cache.Invalidate();
        var ef = AppFixture.MarsDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var role = new RoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "CapRole-" + suffix,
            NormalizedName = ("CapRole-" + suffix).ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsActive = true,
        };
        ef.Roles.Add(role);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        return role;
    }

    void GrantCaps(RoleEntity role, params string[] caps)
    {
        var ef = AppFixture.MarsDbContext();
        ef.RoleClaims.AddRange(caps.Select(c => new RoleClaimEntity
        {
            RoleId = role.Id,
            ClaimType = RoleCaps.ClaimType,
            ClaimValue = c,
        }));
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        Cache.Invalidate();
    }

    UserEntity CreateUserWithRole(RoleEntity role)
    {
        var ef = AppFixture.MarsDbContext();
        var userType = ef.UserTypes.First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        var createdUser = _fixture.Create<UserEntity>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        createdUser.UserName = "capuser-" + suffix;
        createdUser.NormalizedUserName = createdUser.UserName.ToUpper();
        createdUser.Email = "capuser-" + suffix + "@mail.localhost";
        createdUser.NormalizedEmail = createdUser.Email.ToUpper();
        createdUser.MetaValues = [];
        createdUser.UserTypeId = userType.Id;
        ef.Users.Add(createdUser);
        ef.UserRoles.Add(new UserRoleEntity { UserId = createdUser.Id, RoleId = role.Id });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        return createdUser;
    }
}
