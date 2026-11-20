using AutoFixture;
using FluentAssertions;
using Mars.Cms.Host.Seeding;
using Mars.Data.Entities;
using Mars.Identity.Abstractions.Services;
using Mars.Identity.Contracts.ViewModels;
using Mars.Integration.Tests.Attributes;
using Mars.Integration.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Integration.Tests.Seeding;

/// <summary>
/// T4 «роли = наборы прав»: сид пресетов (5 ролей, точные наборы капов), идемпотентность,
/// неперекрытие кастомизаций и миграция Manager → Editor (rename / слияние пользователей).
/// Сид фикстуры уже выполнен в базовом конструкторе; повторный запуск — <see cref="SeedRoles.SeedFirstData"/>.
/// </summary>
public sealed class RoleSeedTests : ApplicationTests
{
    public RoleSeedTests(ApplicationFixture appFixture) : base(appFixture)
    {
    }

    IRoleCapsCache Cache => AppFixture.ServiceProvider.GetRequiredService<IRoleCapsCache>();

    // Ожидаемые пресеты по плану (ai/RolesPermissionsPlan.md, «Пресеты») — независимо от реализации сида.
    static readonly Dictionary<string, string[]> ExpectedPresets = new()
    {
        ["Admin"] =
        [
            RoleCaps.PostCap.Add, RoleCaps.PostCap.Update, RoleCaps.PostCap.UpdateAny,
            RoleCaps.PostCap.Delete, RoleCaps.PostCap.DeleteAny, RoleCaps.PostCap.Publish,
            RoleCaps.MediaCap.Add, RoleCaps.MediaCap.Update, RoleCaps.MediaCap.Delete,
            RoleCaps.PostCategoryCap.Manage, RoleCaps.PostTypeCap.Manage, RoleCaps.NavMenuCap.Manage,
            RoleCaps.UserCap.Manage, RoleCaps.UserCap.UpdateRole, RoleCaps.UserTypeCap.Manage,
            RoleCaps.RoleCap.Manage, RoleCaps.FeedbackCap.Manage, RoleCaps.SettingsCap.Manage,
            RoleCaps.NotificationsCap.Manage, RoleCaps.PluginCap.Manage, RoleCaps.FlowCap.Manage,
            RoleCaps.DatasourceCap.Manage, RoleCaps.SchedulerCap.Manage, RoleCaps.FrontCap.Manage,
            RoleCaps.AiChatCap.Use, RoleCaps.SystemCap.Manage,
        ],
        ["Editor"] =
        [
            RoleCaps.PostCap.Add, RoleCaps.PostCap.Update, RoleCaps.PostCap.UpdateAny,
            RoleCaps.PostCap.Delete, RoleCaps.PostCap.DeleteAny, RoleCaps.PostCap.Publish,
            RoleCaps.MediaCap.Add, RoleCaps.MediaCap.Update, RoleCaps.MediaCap.Delete,
            RoleCaps.PostCategoryCap.Manage, RoleCaps.FeedbackCap.Manage,
        ],
        ["Author"] = [RoleCaps.PostCap.Add, RoleCaps.PostCap.Update, RoleCaps.PostCap.Delete, RoleCaps.MediaCap.Add],
        ["Viewer"] = [],
        ["Developer"] =
        [
            RoleCaps.PluginCap.Manage, RoleCaps.FlowCap.Manage, RoleCaps.DatasourceCap.Manage,
            RoleCaps.SchedulerCap.Manage, RoleCaps.FrontCap.Manage, RoleCaps.AiChatCap.Use,
        ],
    };

    [IntegrationFact]
    public void Seed_FreshDatabase_FivePresetRoles_WithExactCaps()
    {
        //Arrange
        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();

        //Act
        var roles = ef.Roles.AsNoTracking().ToList();

        //Assert — ровно 5 пресет-ролей, Manager не сеется
        roles.Select(s => s.Name).Should().BeEquivalentTo(["Viewer", "Admin", "Editor", "Author", "Developer"]);
        roles.Should().OnlyContain(s => s.IsActive);

        foreach (var (name, expected) in ExpectedPresets)
        {
            var role = roles.Single(s => s.Name == name);
            var caps = ef.RoleClaims.AsNoTracking()
                .Where(s => s.RoleId == role.Id && s.ClaimType == RoleCaps.ClaimType)
                .Select(s => s.ClaimValue).ToList();
            caps.Should().BeEquivalentTo(expected, because: $"пресет роли {name}");
        }

        // размерности пресетов: Admin 26, Editor 11, Author 4, Developer 6, Viewer 0
        CapsOf("Admin").Should().HaveCount(26);
        CapsOf("Editor").Should().HaveCount(11);
        CapsOf("Author").Should().HaveCount(4);
        CapsOf("Developer").Should().HaveCount(6);
        CapsOf("Viewer").Should().BeEmpty();
    }

    [IntegrationFact]
    public void Seed_RepeatedRun_Idempotent_NoDuplicates()
    {
        //Arrange
        var ef = AppFixture.MarsDbContext();
        ef.ChangeTracker.Clear();
        var beforeCount = ef.RoleClaims.AsNoTracking().Count(s => s.ClaimType == RoleCaps.ClaimType);

        //Act — повторные старты ничего не меняют
        SeedRoles.SeedFirstData(ef);
        SeedRoles.SeedFirstData(ef);
        ef.ChangeTracker.Clear();
        Cache.Invalidate();

        //Assert
        ef.Roles.AsNoTracking().Count().Should().Be(5);
        var after = ef.RoleClaims.AsNoTracking().Where(s => s.ClaimType == RoleCaps.ClaimType).ToList();
        after.Should().HaveCount(beforeCount);
        after.GroupBy(s => (s.RoleId, s.ClaimValue)).Should().OnlyContain(g => g.Count() == 1);
    }

    [IntegrationFact]
    public void Seed_CustomRoleWithClaims_NotOverwritten()
    {
        //Arrange — кастомная роль со своим набором прав
        var ef = AppFixture.MarsDbContext();
        var custom = new RoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "CustomSeedRole",
            NormalizedName = "CUSTOMSEEDROLE",
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsActive = true,
        };
        ef.Roles.Add(custom);
        ef.RoleClaims.AddRange(
            new RoleClaimEntity { RoleId = custom.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.PostCap.Add },
            new RoleClaimEntity { RoleId = custom.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.FeedbackCap.Manage });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        //Act
        SeedRoles.SeedFirstData(ef);
        ef.ChangeTracker.Clear();
        Cache.Invalidate();

        //Assert — кастомный набор сохранён, пресет не подмешан
        var caps = ef.RoleClaims.AsNoTracking()
            .Where(s => s.RoleId == custom.Id && s.ClaimType == RoleCaps.ClaimType)
            .Select(s => s.ClaimValue).ToList();
        caps.Should().BeEquivalentTo([RoleCaps.PostCap.Add, RoleCaps.FeedbackCap.Manage]);
    }

    [IntegrationFact]
    public void Seed_CustomizedPresetRole_KeepsCustomization()
    {
        //Arrange — кастомизируем Author: снимаем пресет, ставим свой набор
        var ef = AppFixture.MarsDbContext();
        var author = ef.Roles.AsNoTracking().First(s => s.Name == "Author");
        var authorCaps = ef.RoleClaims.Where(s => s.RoleId == author.Id && s.ClaimType == RoleCaps.ClaimType).ToList();
        ef.RoleClaims.RemoveRange(authorCaps);
        ef.RoleClaims.Add(new RoleClaimEntity { RoleId = author.Id, ClaimType = RoleCaps.ClaimType, ClaimValue = RoleCaps.SettingsCap.Manage });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        //Act
        SeedRoles.SeedFirstData(ef);
        ef.ChangeTracker.Clear();
        Cache.Invalidate();

        //Assert — пресет не восстановлен поверх кастомизации
        var caps = ef.RoleClaims.AsNoTracking()
            .Where(s => s.RoleId == author.Id && s.ClaimType == RoleCaps.ClaimType)
            .Select(s => s.ClaimValue).ToList();
        caps.Should().BeEquivalentTo([RoleCaps.SettingsCap.Manage]);
    }

    [IntegrationFact]
    public void Migrate_ManagerWithoutEditor_RenamesToEditor()
    {
        //Arrange — убираем Editor из сида, чтобы проверить ветку rename
        var ef = AppFixture.MarsDbContext();
        var editor = ef.Roles.First(s => s.Name == "Editor");
        ef.RoleClaims.RemoveRange(ef.RoleClaims.Where(s => s.RoleId == editor.Id));
        ef.UserRoles.RemoveRange(ef.UserRoles.Where(s => s.RoleId == editor.Id));
        ef.Roles.Remove(editor);
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        var manager = NewRole("Manager");
        ef.Roles.Add(manager);
        var user = CreateUser();
        ef.UserRoles.Add(new UserRoleEntity { UserId = user.Id, RoleId = manager.Id });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();
        var managerId = manager.Id;

        //Act
        SeedRoles.SeedFirstData(ef);
        ef.ChangeTracker.Clear();
        Cache.Invalidate();

        //Assert — Manager переименован в Editor (тот же Id), пользователь при нём, права — пресет Editor
        ef.Roles.AsNoTracking().Should().NotContain(s => s.Name == "Manager");
        var renamed = ef.Roles.AsNoTracking().Single(s => s.Name == "Editor");
        renamed.Id.Should().Be(managerId);
        renamed.NormalizedName.Should().Be("EDITOR");
        ef.UserRoles.AsNoTracking().Should().Contain(s => s.UserId == user.Id && s.RoleId == managerId);
        CapsOfRole(managerId).Should().BeEquivalentTo(ExpectedPresets["Editor"]);
    }

    [IntegrationFact]
    public void Migrate_ManagerAndEditor_MergesUsersAndDeletesManager()
    {
        //Arrange — Editor уже есть (сид), создаём Manager с пользователем
        var ef = AppFixture.MarsDbContext();
        var editor = ef.Roles.AsNoTracking().First(s => s.Name == "Editor");
        var editorCapsBefore = CapsOfRole(editor.Id);

        var manager = NewRole("Manager");
        ef.Roles.Add(manager);
        var user = CreateUser();
        ef.UserRoles.Add(new UserRoleEntity { UserId = user.Id, RoleId = manager.Id });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        //Act
        SeedRoles.SeedFirstData(ef);
        ef.ChangeTracker.Clear();
        Cache.Invalidate();

        //Assert — Manager удалён, пользователь перенесён в Editor, права Editor не перетёрты
        ef.Roles.AsNoTracking().Should().NotContain(s => s.Name == "Manager");
        ef.UserRoles.AsNoTracking().Should().Contain(s => s.UserId == user.Id && s.RoleId == editor.Id);
        ef.UserRoles.AsNoTracking().Should().NotContain(s => s.UserId == user.Id && s.RoleId == manager.Id);
        CapsOfRole(editor.Id).Should().BeEquivalentTo(editorCapsBefore);

        //Act — повторный запуск идемпотентен
        SeedRoles.SeedFirstData(ef);
        ef.ChangeTracker.Clear();

        //Assert
        ef.Roles.AsNoTracking().Should().NotContain(s => s.Name == "Manager");
        ef.UserRoles.AsNoTracking().Count(s => s.UserId == user.Id && s.RoleId == editor.Id).Should().Be(1);
    }

    [IntegrationFact]
    public void Migrate_UserInBothManagerAndEditor_NoDuplicateMembership()
    {
        //Arrange — пользователь уже и в Manager, и в Editor
        var ef = AppFixture.MarsDbContext();
        var editor = ef.Roles.AsNoTracking().First(s => s.Name == "Editor");
        var manager = NewRole("Manager");
        ef.Roles.Add(manager);
        var user = CreateUser();
        ef.UserRoles.Add(new UserRoleEntity { UserId = user.Id, RoleId = manager.Id });
        ef.UserRoles.Add(new UserRoleEntity { UserId = user.Id, RoleId = editor.Id });
        ef.SaveChanges();
        ef.ChangeTracker.Clear();

        //Act
        SeedRoles.SeedFirstData(ef);
        ef.ChangeTracker.Clear();
        Cache.Invalidate();

        //Assert — ровно одно членство в Editor, без дубля и без осиротевшей связи с Manager
        ef.UserRoles.AsNoTracking().Count(s => s.UserId == user.Id && s.RoleId == editor.Id).Should().Be(1);
        ef.UserRoles.AsNoTracking().Should().NotContain(s => s.RoleId == manager.Id);
    }

    List<string> CapsOf(string roleName)
    {
        var ef = AppFixture.MarsDbContext();
        var role = ef.Roles.AsNoTracking().First(s => s.Name == roleName);
        return CapsOfRole(role.Id);
    }

    List<string> CapsOfRole(Guid roleId)
    {
        var ef = AppFixture.MarsDbContext();
        return ef.RoleClaims.AsNoTracking()
            .Where(s => s.RoleId == roleId && s.ClaimType == RoleCaps.ClaimType)
            .Select(s => s.ClaimValue!).ToList();
    }

    static RoleEntity NewRole(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        NormalizedName = name.ToUpper(),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
        IsActive = true,
    };

    UserEntity CreateUser()
    {
        var ef = AppFixture.MarsDbContext();
        var userType = ef.UserTypes.First(s => s.TypeName == UserTypeEntity.DefaultTypeName);
        var user = _fixture.Create<UserEntity>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        user.UserName = "seeduser-" + suffix;
        user.NormalizedUserName = user.UserName.ToUpper();
        user.Email = "seeduser-" + suffix + "@mail.localhost";
        user.NormalizedEmail = user.Email.ToUpper();
        user.MetaValues = [];
        user.UserTypeId = userType.Id;
        ef.Users.Add(user);
        ef.SaveChanges();
        return user;
    }
}
