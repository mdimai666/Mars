using AutoFixture;
using FluentAssertions;
using Flurl.Http;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.Users;
using Mars.Identity.Contracts.ViewModels;
using Mars.Identity.Host.Controllers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Integration.Tests.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Controllers.Users;

/// <summary>
/// T3 «роли = наборы прав»: назначение/смена ролей требует отдельного права User.UpdateRole
/// поверх User.Manage — и на эндпоинте UpdateUserRoles, и в сервисном Update (эскалация).
/// </summary>
/// <seealso cref="UserController"/>
public sealed class UserRoleCapsTests : ApplicationTests
{
    const string _apiUrl = "/api/User";

    public UserRoleCapsTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    IRoleCapsCache Cache => AppFixture.ServiceProvider.GetRequiredService<IRoleCapsCache>();

    [IntegrationFact]
    public async Task UpdateUserRoles_WithUserManageOnly_403()
    {
        //Arrange
        _ = nameof(UserController.UpdateUserRoles);
        var actor = CreateUserWithCaps([RoleCaps.UserCap.Manage]);
        var target = CreateUserWithRole("Viewer");
        var client = AppFixture.GetClientForUser(actor.Id);

        //Act
        var result = await client.Request(_apiUrl, "UpdateUserRoles", target.Id)
                                 .AllowAnyHttpStatus().PutJsonAsync(new[] { "Admin" });

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Users.Include(s => s.Roles).First(s => s.Id == target.Id)
            .Roles!.Select(s => s.Name).Should().BeEquivalentTo(["Viewer"]);
    }

    [IntegrationFact]
    public async Task UpdateUserRoles_WithUpdateRoleCap_Succeeds()
    {
        //Arrange
        var actor = CreateUserWithCaps([RoleCaps.UserCap.Manage, RoleCaps.UserCap.UpdateRole]);
        var target = CreateUserWithRole("Viewer");
        var client = AppFixture.GetClientForUser(actor.Id);

        //Act
        var result = await client.Request(_apiUrl, "UpdateUserRoles", target.Id)
                                 .PutJsonAsync(new[] { "Editor" }).CatchUserActionError();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Users.Include(s => s.Roles).First(s => s.Id == target.Id)
            .Roles!.Select(s => s.Name).Should().BeEquivalentTo(["Editor"]);
    }

    [IntegrationFact]
    public async Task UpdateUser_ChangingRoles_WithoutUpdateRoleCap_403()
    {
        //Arrange — кап User.Manage проходит атрибут, смену ролей режет сервисная проверка
        _ = nameof(UserController.Update);
        var actor = CreateUserWithCaps([RoleCaps.UserCap.Manage]);
        var target = CreateUserWithRole("Viewer");
        var client = AppFixture.GetClientForUser(actor.Id);

        var request = _fixture.Create<UpdateUserRequest>() with
        {
            Id = target.Id,
            Type = UserTypeEntity.DefaultTypeName,
            MetaValues = [],
            Roles = ["Admin"],
        };

        //Act
        var result = await client.Request(_apiUrl).AllowAnyHttpStatus().PutJsonAsync(request);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        ef.Users.Include(s => s.Roles).First(s => s.Id == target.Id)
            .Roles!.Select(s => s.Name).Should().BeEquivalentTo(["Viewer"]);
    }

    [IntegrationFact]
    public async Task UpdateUser_SameRoles_WithoutUpdateRoleCap_Succeeds()
    {
        //Arrange — роли не меняются (паттерн UpdateProfile): User.Manage достаточно
        var actor = CreateUserWithCaps([RoleCaps.UserCap.Manage]);
        var target = CreateUserWithRole("Viewer");
        var client = AppFixture.GetClientForUser(actor.Id);

        var request = _fixture.Create<UpdateUserRequest>() with
        {
            Id = target.Id,
            Type = UserTypeEntity.DefaultTypeName,
            MetaValues = [],
            Roles = ["Viewer"],
        };

        //Act
        var result = await client.Request(_apiUrl).PutJsonAsync(request).CatchUserActionError().ReceiveJson<UserDetailResponse>();

        //Assert
        result.Id.Should().Be(target.Id);
        result.Roles.Should().BeEquivalentTo(["Viewer"]);
    }

    UserEntity CreateUserWithCaps(string[] caps)
    {
        Cache.Invalidate();
        var ef = AppFixture.MarsDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var role = new RoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "UserCapsRole-" + suffix,
            NormalizedName = ("UserCapsRole-" + suffix).ToUpper(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsActive = true,
        };
        ef.Roles.Add(role);
        ef.RoleClaims.AddRange(caps.Select(c => new RoleClaimEntity
        {
            RoleId = role.Id,
            ClaimType = RoleCaps.ClaimType,
            ClaimValue = c,
        }));
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        Cache.Invalidate();

        return CreateUserWithRole(role.Name!);
    }

    UserEntity CreateUserWithRole(string roleName)
    {
        var ef = AppFixture.MarsDbContext();
        var userType = ef.UserTypes.First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        var role = ef.Roles.AsNoTracking().First(s => s.Name == roleName);
        var createdUser = _fixture.Create<UserEntity>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        createdUser.UserName = "rolecaps-" + suffix;
        createdUser.NormalizedUserName = createdUser.UserName.ToUpper();
        createdUser.Email = "rolecaps-" + suffix + "@mail.localhost";
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
