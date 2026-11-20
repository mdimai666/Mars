using FluentAssertions;
using Mars.Contracts.Resources;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Dto.Roles;
using Mars.Identity.Abstractions.Interfaces;
using Mars.Identity.Abstractions.Repositories;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.ViewModels;
using Mars.Identity.Host.Services;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Mars.Server.Abstractions.Managers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// Кэш прав ролей: снимок, инвалидация при SaveRoleClaims и CRUD ролей, HasCap.
/// Кэш — singleton фикстуры, поэтому каждый тест начинает с Invalidate
/// (смежные тесты коллекции могли оставить снимок от своей БД).
/// </summary>
public class RoleCapsCacheTests : ApplicationTests
{
    public RoleCapsCacheTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    IRoleCapsCache Cache => AppFixture.ServiceProvider.GetRequiredService<IRoleCapsCache>();

    RoleService CreateService(IServiceScope scope, HashSet<string>? currentUserRoles)
    {
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.Roles.Returns(currentUserRoles);

        return new RoleService(
            scope.ServiceProvider.GetRequiredService<IRoleRepository>(),
            scope.ServiceProvider.GetRequiredService<IStringLocalizer<AppRes>>(),
            scope.ServiceProvider.GetRequiredService<IEventManager>(),
            requestContext,
            Cache);
    }

    static EditRolesViewModelDto GrantDto(Guid roleId, string roleName, params string[] caps)
        => new()
        {
            RoleClaims =
            [
                new RoleClaimsDto
                {
                    Role = new RoleShortDto { Id = roleId, Name = roleName },
                    Groups =
                    [
                        new RoleCapGroupCheckable
                        {
                            Title = "g",
                            RoleCapElements = caps.Select(c => new RoleCapElementCheckable { Id = c, Checked = true }).ToArray(),
                        },
                    ],
                },
            ],
        };

    [IntegrationFact]
    public async Task GetRoleCaps_SnapshotCached_UntilInvalidate()
    {
        //Arrange
        Cache.Invalidate();
        var ef = AppFixture.MarsDbContext();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "CacheSnapRole", NormalizedName = "CACHESNAPROLE", IsActive = true };
        ef.Roles.Add(role);
        ef.RoleClaims.Add(new RoleClaimEntity { RoleId = role.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.PostCap.Add });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        //Act
        var snapshot = await Cache.GetRoleCaps();

        //Assert
        snapshot["CacheSnapRole"].Should().BeEquivalentTo([RoleCaps.PostCap.Add]);

        //Act — правка БД мимо сервиса не видна, пока снимок жив
        ef.RoleClaims.Add(new RoleClaimEntity { RoleId = role.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.PostCap.Publish });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var cached = await Cache.GetRoleCaps();
        cached["CacheSnapRole"].Should().BeEquivalentTo([RoleCaps.PostCap.Add]);

        //Act — инвалидация перечитывает БД
        Cache.Invalidate();
        var fresh = await Cache.GetRoleCaps();

        //Assert
        fresh["CacheSnapRole"].Should().BeEquivalentTo([RoleCaps.PostCap.Add, RoleCaps.PostCap.Publish]);
    }

    [IntegrationFact]
    public async Task SaveRoleClaims_InvalidatesCache()
    {
        //Arrange
        Cache.Invalidate();
        var ef = AppFixture.MarsDbContext();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "CacheSaveRole", NormalizedName = "CACHESAVEROLE", IsActive = true };
        ef.Roles.Add(role);
        ef.RoleClaims.Add(new RoleClaimEntity { RoleId = role.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.PostCap.Add });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var before = await Cache.GetRoleCaps();
        before["CacheSaveRole"].Should().BeEquivalentTo([RoleCaps.PostCap.Add]);

        using var scope = AppFixture.ServiceProvider.CreateScope();
        var service = CreateService(scope, null);

        //Act
        var result = await service.SaveRoleClaims(
            GrantDto(role.Id, role.Name, RoleCaps.PostCap.Add, RoleCaps.MediaCap.Add), default);

        //Assert
        result.Ok.Should().BeTrue();
        var after = await Cache.GetRoleCaps();
        after["CacheSaveRole"].Should().BeEquivalentTo([RoleCaps.PostCap.Add, RoleCaps.MediaCap.Add]);
    }

    [IntegrationFact]
    public async Task RoleCrud_InvalidatesCache_RenameChangesKey()
    {
        //Arrange
        Cache.Invalidate();
        using var scope = AppFixture.ServiceProvider.CreateScope();
        var service = CreateService(scope, null);
        var ef = AppFixture.MarsDbContext();

        var created = await service.Create(new CreateRoleQuery { Id = Guid.NewGuid(), Name = "CacheCrudRole" }, default);
        var snapshot = await Cache.GetRoleCaps();
        snapshot.Should().ContainKey("CacheCrudRole");
        snapshot["CacheCrudRole"].Should().BeEmpty();

        // права мимо сервиса — снимок их не видит до инвалидации
        ef.RoleClaims.Add(new RoleClaimEntity { RoleId = created.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.PostCap.Add });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        //Act — rename инвалидирует кэш, ключ переезжает на новое имя
        await service.Update(new UpdateRoleQuery { Id = created.Id, Name = "CacheCrudRoleRenamed" }, default);
        var afterRename = await Cache.GetRoleCaps();

        //Assert
        afterRename.Should().NotContainKey("CacheCrudRole");
        afterRename["CacheCrudRoleRenamed"].Should().BeEquivalentTo([RoleCaps.PostCap.Add]);

        //Act — delete инвалидирует кэш
        await service.Delete(created.Id, default);
        var afterDelete = await Cache.GetRoleCaps();

        //Assert
        afterDelete.Should().NotContainKey("CacheCrudRoleRenamed");
    }

    [IntegrationFact]
    public async Task HasCap_ChecksCurrentUserRoles()
    {
        //Arrange
        Cache.Invalidate();
        var ef = AppFixture.MarsDbContext();
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "HasCapRole", NormalizedName = "HASCAPROLE", IsActive = true };
        ef.Roles.Add(role);
        ef.RoleClaims.Add(new RoleClaimEntity { RoleId = role.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.PostCap.Add });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        using var scope = AppFixture.ServiceProvider.CreateScope();

        //Act + Assert
        var withRole = CreateService(scope, ["HasCapRole"]);
        (await withRole.HasCap(RoleCaps.PostCap.Add)).Should().BeTrue();
        (await withRole.HasCap(RoleCaps.PostCap.Publish)).Should().BeFalse();

        var withoutRole = CreateService(scope, ["NoSuchRole"]);
        (await withoutRole.HasCap(RoleCaps.PostCap.Add)).Should().BeFalse();

        var anonymous = CreateService(scope, null);
        (await anonymous.HasCap(RoleCaps.PostCap.Add)).Should().BeFalse();
    }
}
