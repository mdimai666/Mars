using Mars.Data.Contexts;
using Mars.Data.Entities;
using Mars.Identity.Contracts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Mars.Cms.Host.Seeding;

/// <summary>
/// Сид ролей «роли = наборы прав» (ai/RolesPermissionsPlan.md, «Пресеты», «Сид и миграция (T4)»).
/// Идемпотентно: каждая пресет-роль создаётся при отсутствии; капы пресета сеются только ролям
/// без mars_cap-claims (кастомизации не перетираются); роль Manager мигрирует в Editor.
/// Пишет напрямую в БД до старта приложения — кэш прав ещё пуст, инвалидация не нужна.
/// </summary>
public static class SeedRoles
{
    static readonly string[] PresetRoleNames = ["Viewer", "Admin", "Editor", "Author", "Developer"];

    /// <summary>Пресеты капов по ролям (Viewer — без прав). Значения — константы каталога RoleCaps.</summary>
    static readonly IReadOnlyDictionary<string, string[]> PresetCaps = new Dictionary<string, string[]>
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
        ["Author"] =
        [
            RoleCaps.PostCap.Add, RoleCaps.PostCap.Update, RoleCaps.PostCap.Delete, RoleCaps.MediaCap.Add,
        ],
        ["Viewer"] = [],
        ["Developer"] =
        [
            RoleCaps.PluginCap.Manage, RoleCaps.FlowCap.Manage, RoleCaps.DatasourceCap.Manage,
            RoleCaps.SchedulerCap.Manage, RoleCaps.FrontCap.Manage, RoleCaps.AiChatCap.Use,
        ],
    };

    public static void SeedFirstData(MarsDbContext ef)
    {
        MigrateManagerToEditor(ef);
        EnsurePresetRoles(ef);
        SeedPresetCaps(ef);
    }

    /// <summary>
    /// Миграция Manager → Editor: если Editor нет — rename (NormalizedName обновляется);
    /// если Editor уже есть — пользователи Manager переносятся в Editor, Manager удаляется.
    /// Повторный запуск ничего не делает (роли Manager уже нет).
    /// </summary>
    static void MigrateManagerToEditor(MarsDbContext ef)
    {
        var manager = ef.Roles.FirstOrDefault(s => s.NormalizedName == "MANAGER");
        if (manager is null) return;

        var editor = ef.Roles.FirstOrDefault(s => s.NormalizedName == "EDITOR");
        if (editor is null)
        {
            manager.Name = "Editor";
            manager.NormalizedName = "EDITOR";
            ef.SaveChanges();
            return;
        }

        // Editor уже существует — сливаем пользователей и удаляем Manager.
        // UserRoleEntity — композитный ключ (UserId, RoleId): переносим через remove+add, не меняя ключ.
        var managerUserRoles = ef.UserRoles.Where(s => s.RoleId == manager.Id).ToList();
        var editorUserIds = ef.UserRoles.Where(s => s.RoleId == editor.Id).Select(s => s.UserId).ToHashSet();
        foreach (var userRole in managerUserRoles)
        {
            ef.UserRoles.Remove(userRole);
            if (editorUserIds.Add(userRole.UserId))
            {
                ef.UserRoles.Add(new UserRoleEntity { UserId = userRole.UserId, RoleId = editor.Id });
            }
        }

        var managerClaims = ef.RoleClaims.Where(s => s.RoleId == manager.Id).ToList();
        ef.RoleClaims.RemoveRange(managerClaims);
        ef.Roles.Remove(manager);
        ef.SaveChanges();
    }

    static void EnsurePresetRoles(MarsDbContext ef)
    {
        var existingNormalized = ef.Roles.AsNoTracking().Select(s => s.NormalizedName).ToHashSet();
        foreach (var name in PresetRoleNames)
        {
            if (existingNormalized.Contains(name.ToUpper())) continue;
            ef.Roles.Add(new RoleEntity
            {
                Name = name,
                NormalizedName = name.ToUpper(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                IsActive = true,
            });
        }
        ef.SaveChanges();
    }

    static void SeedPresetCaps(MarsDbContext ef)
    {
        foreach (var name in PresetRoleNames)
        {
            var caps = PresetCaps[name];
            if (caps.Length == 0) continue; // Viewer — без прав

            var role = ef.Roles.AsNoTracking().First(s => s.NormalizedName == name.ToUpper());
            // пресет сеется только роли без mars_cap-claims — кастомизации не перетираем
            var hasCaps = ef.RoleClaims.Any(s => s.RoleId == role.Id && s.ClaimType == RoleCaps.ClaimType);
            if (hasCaps) continue;

            ef.RoleClaims.AddRange(caps.Select(cap => new RoleClaimEntity
            {
                RoleId = role.Id,
                ClaimType = RoleCaps.ClaimType,
                ClaimValue = cap,
            }));
        }
        ef.SaveChanges();
    }
}
