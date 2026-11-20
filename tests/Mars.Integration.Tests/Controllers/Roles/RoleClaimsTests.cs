using FluentAssertions;
using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Data.Entities;
using Mars.Identity.Contracts.ViewModels;
using Mars.Identity.Host.Controllers;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Mars.Integration.Tests.Controllers.Roles;

/// <seealso cref="RoleController.EditRolesViewModel"/>
/// <seealso cref="RoleController.SaveRoleClaims"/>
public class RoleClaimsTests : ApplicationTests
{
    const string _apiUrl = "/api/Role";

    public RoleClaimsTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    static Task<EditRolesViewModelDto> GetMatrix(IFlurlClient client)
        => client.Request($"{_apiUrl}/{nameof(RoleController.EditRolesViewModel)}")
                 .GetJsonAsync<EditRolesViewModelDto>();

    static Task<IFlurlResponse> SaveMatrix(IFlurlClient client, EditRolesViewModelDto dto)
        => client.Request($"{_apiUrl}/{nameof(RoleController.SaveRoleClaims)}")
                 .PutJsonAsync(dto);

    static RoleClaimsDto RoleEntry(EditRolesViewModelDto dto, string roleName)
        => dto.RoleClaims.Single(s => s.Role.Name == roleName);

    static IEnumerable<RoleCapElementCheckable> AllCaps(RoleClaimsDto role)
        => role.Groups.SelectMany(s => s.RoleCapElements);

    [IntegrationFact]
    public async Task EditRolesViewModel_Request_Unauthorized()
    {
        //Arrange
        var client = AppFixture.GetClient(true);

        //Act
        var result = await client.Request($"{_apiUrl}/{nameof(RoleController.EditRolesViewModel)}")
                                 .AllowAnyHttpStatus().GetAsync();

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task SaveRoleClaims_Request_Unauthorized()
    {
        //Arrange
        var client = AppFixture.GetClient(true);
        var dto = new EditRolesViewModelDto();

        //Act
        var result = await client.Request($"{_apiUrl}/{nameof(RoleController.SaveRoleClaims)}")
                                 .AllowAnyHttpStatus().PutJsonAsync(dto);

        //Assert
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [IntegrationFact]
    public async Task EditRolesViewModel_AllRolesAndGroups_CheckedReflectsClaims()
    {
        //Arrange
        var client = AppFixture.GetClient();
        var ef = AppFixture.MarsDbContext();
        var adminRole = ef.Roles.AsNoTracking().First(s => s.Name == "Admin");

        // T4 сеет пресеты капов; проверка «матрица отражает ровно клеймы» — начинаем с чистого листа
        var presetCaps = ef.RoleClaims.Where(s => s.ClaimType == RoleCaps.ClaimType).ToList();
        ef.RoleClaims.RemoveRange(presetCaps);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        ef.RoleClaims.AddRange(
            new RoleClaimEntity { RoleId = adminRole.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.PostCap.Add },
            new RoleClaimEntity { RoleId = adminRole.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.PostCap.Publish },
            // чужой ClaimType не должен попадать в матрицу
            new RoleClaimEntity { RoleId = adminRole.Id, ClaimType = "some_other_type", ClaimValue = RoleCaps.PostCap.Delete });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        //Act
        var dto = await GetMatrix(client);

        //Assert
        var dbRoles = ef.Roles.AsNoTracking().ToList();
        dto.RoleClaims.Should().HaveCount(dbRoles.Count);
        dto.RoleClaims.Select(s => s.Role.Id).Should().BeEquivalentTo(dbRoles.Select(s => s.Id));

        foreach (var role in dto.RoleClaims)
        {
            role.Role.Title.Should().NotBeNullOrWhiteSpace();
            role.Groups.Should().HaveCount(18);
            AllCaps(role).Should().HaveCount(26);
            role.Groups.Select(s => s.Title).Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t));
            AllCaps(role).Select(s => s.Title).Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t));
        }

        var allCapIds = AllCaps(dto.RoleClaims.First()).Select(s => s.Id).ToList();
        allCapIds.Should().OnlyHaveUniqueItems();
        allCapIds.Should().Contain(RoleCaps.PostCap.UpdateAny);
        // NavMenuCap.Manage больше не дублирует значение PostType.Manage
        allCapIds.Should().Contain("NavMenu.Manage");
        allCapIds.Should().ContainSingle(s => s == "PostType.Manage");

        var admin = RoleEntry(dto, "Admin");
        AllCaps(admin).Where(s => s.Checked).Select(s => s.Id)
            .Should().BeEquivalentTo([RoleCaps.PostCap.Add, RoleCaps.PostCap.Publish]);

        foreach (var roleName in new[] { "Viewer", "Editor", "Developer" })
        {
            AllCaps(RoleEntry(dto, roleName)).Should().OnlyContain(s => !s.Checked);
        }
    }

    [IntegrationFact]
    public async Task SaveRoleClaims_Roundtrip_SetThenUnset()
    {
        //Arrange
        var client = AppFixture.GetClient();
        var ef = AppFixture.MarsDbContext();
        var editorRole = ef.Roles.AsNoTracking().First(s => s.Name == "Editor");

        var dto = await GetMatrix(client);
        var editor = RoleEntry(dto, "Editor");
        foreach (var cap in AllCaps(editor))
        {
            cap.Checked = cap.Id is RoleCaps.PostCap.Add or RoleCaps.PostCap.UpdateAny or RoleCaps.MediaCap.Add;
        }

        //Act — выставил права
        var saveRes = await SaveMatrix(client, dto);
        var saveResult = await saveRes.GetJsonAsync<UserActionResult>();

        //Assert
        saveRes.StatusCode.Should().Be(StatusCodes.Status200OK);
        saveResult.Ok.Should().BeTrue();

        ef.ChangeTracker.Clear();
        var dbCaps = ef.RoleClaims.AsNoTracking()
            .Where(s => s.RoleId == editorRole.Id && s.ClaimType == RoleCaps.ClaimType)
            .Select(s => s.ClaimValue).ToList();
        dbCaps.Should().BeEquivalentTo([RoleCaps.PostCap.Add, RoleCaps.PostCap.UpdateAny, RoleCaps.MediaCap.Add]);

        var matrixAfterSet = await GetMatrix(client);
        AllCaps(RoleEntry(matrixAfterSet, "Editor")).Where(s => s.Checked).Select(s => s.Id)
            .Should().BeEquivalentTo([RoleCaps.PostCap.Add, RoleCaps.PostCap.UpdateAny, RoleCaps.MediaCap.Add]);

        //Act — снял одно право (остальные не трогал)
        var dto2 = await GetMatrix(client);
        foreach (var cap in AllCaps(RoleEntry(dto2, "Editor")))
        {
            if (cap.Id == RoleCaps.PostCap.UpdateAny) cap.Checked = false;
        }
        await SaveMatrix(client, dto2);

        //Assert
        ef.ChangeTracker.Clear();
        var dbCapsAfterUnset = ef.RoleClaims.AsNoTracking()
            .Where(s => s.RoleId == editorRole.Id && s.ClaimType == RoleCaps.ClaimType)
            .Select(s => s.ClaimValue).ToList();
        dbCapsAfterUnset.Should().BeEquivalentTo([RoleCaps.PostCap.Add, RoleCaps.MediaCap.Add]);

        var matrixAfterUnset = await GetMatrix(client);
        AllCaps(RoleEntry(matrixAfterUnset, "Editor")).Where(s => s.Checked).Select(s => s.Id)
            .Should().BeEquivalentTo([RoleCaps.PostCap.Add, RoleCaps.MediaCap.Add]);
    }

    [IntegrationFact]
    public async Task SaveRoleClaims_UnknownCapIds_Ignored()
    {
        //Arrange
        var client = AppFixture.GetClient();
        var ef = AppFixture.MarsDbContext();
        var editorRole = ef.Roles.AsNoTracking().First(s => s.Name == "Editor");

        var dto = new EditRolesViewModelDto
        {
            RoleClaims =
            [
                new RoleClaimsDto
                {
                    Role = new RoleShortDto { Id = editorRole.Id, Name = editorRole.Name! },
                    Groups =
                    [
                        new RoleCapGroupCheckable
                        {
                            Title = "fake",
                            RoleCapElements =
                            [
                                new RoleCapElementCheckable { Id = "Hacker.Manage", Title = "fake", Checked = true },
                                new RoleCapElementCheckable { Id = RoleCaps.PostCap.Add, Title = "add", Checked = true },
                                new RoleCapElementCheckable { Id = RoleCaps.PostCap.Delete, Title = "del", Checked = false },
                            ],
                        },
                    ],
                },
            ],
        };

        //Act
        var res = await SaveMatrix(client, dto);
        var result = await res.GetJsonAsync<UserActionResult>();

        //Assert
        result.Ok.Should().BeTrue();
        ef.ChangeTracker.Clear();
        var dbCaps = ef.RoleClaims.AsNoTracking()
            .Where(s => s.RoleId == editorRole.Id && s.ClaimType == RoleCaps.ClaimType)
            .Select(s => s.ClaimValue).ToList();
        dbCaps.Should().BeEquivalentTo([RoleCaps.PostCap.Add]);
    }

    [IntegrationFact]
    public async Task SaveRoleClaims_UnknownRoleId_Ignored()
    {
        //Arrange
        var client = AppFixture.GetClient();
        var ef = AppFixture.MarsDbContext();
        var unknownRoleId = Guid.NewGuid();

        var dto = new EditRolesViewModelDto
        {
            RoleClaims =
            [
                new RoleClaimsDto
                {
                    Role = new RoleShortDto { Id = unknownRoleId, Name = "Ghost" },
                    Groups =
                    [
                        new RoleCapGroupCheckable
                        {
                            Title = "g",
                            RoleCapElements = [new RoleCapElementCheckable { Id = RoleCaps.PostCap.Add, Checked = true }],
                        },
                    ],
                },
            ],
        };

        //Act
        var res = await SaveMatrix(client, dto);
        var result = await res.GetJsonAsync<UserActionResult>();

        //Assert
        result.Ok.Should().BeTrue();
        ef.ChangeTracker.Clear();
        ef.RoleClaims.AsNoTracking().Any(s => s.RoleId == unknownRoleId).Should().BeFalse();
    }

    [IntegrationFact]
    public async Task SaveRoleClaims_ForeignClaimType_Untouched()
    {
        //Arrange
        var client = AppFixture.GetClient();
        var ef = AppFixture.MarsDbContext();
        var adminRole = ef.Roles.AsNoTracking().First(s => s.Name == "Admin");

        ef.RoleClaims.Add(new RoleClaimEntity
        {
            RoleId = adminRole.Id,
            ClaimType = "foreign_type",
            ClaimValue = "foreign_value",
        });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var dto = await GetMatrix(client);
        foreach (var cap in AllCaps(RoleEntry(dto, "Admin")))
        {
            cap.Checked = cap.Id == RoleCaps.SettingsCap.Manage;
        }

        //Act
        await SaveMatrix(client, dto);

        //Assert
        ef.ChangeTracker.Clear();
        var foreign = ef.RoleClaims.AsNoTracking()
            .SingleOrDefault(s => s.RoleId == adminRole.Id && s.ClaimType == "foreign_type");
        foreign.Should().NotBeNull();
        foreign!.ClaimValue.Should().Be("foreign_value");

        var caps = ef.RoleClaims.AsNoTracking()
            .Where(s => s.RoleId == adminRole.Id && s.ClaimType == RoleCaps.ClaimType)
            .Select(s => s.ClaimValue).ToList();
        caps.Should().BeEquivalentTo([RoleCaps.SettingsCap.Manage]);
    }
}
