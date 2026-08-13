using System.Security.Cryptography;
using System.Text;
using Mars.Host.Shared.Dto.NavMenus;
using Mars.Host.Shared.Dto.PostTypes;
using Mars.Shared.Resources;

namespace Mars.Host.Services;

/// <summary>
/// Дефолтное dev-меню: источник правды дефолтного состояния — код.
/// Сохранённая в БД копия только переопределяет его (merge).
/// </summary>
public static class DevMenuFactory
{
    public static readonly Guid DevMenuId = new("9596ffe0-f688-452c-885e-e72f1123e50d");
    public const string DevMenuSlug = "dev";
    public const string DevMenuTitle = "Dev admin menu";
    public const string SystemTag = "system";

    static readonly Guid RazdelsId = new("9f34a009-e39e-4c7c-80bf-be6efa4dc8da");
    static readonly Guid ManageId = new("a7bc610c-412d-4292-9e0c-dd126725e285");

    const string StableIdPrefix = "mars/dev-menu/";

    public static NavMenuDetail Build(IReadOnlyCollection<PostTypeSummary> postTypes)
    {
        var d = "/dev/";
        List<string> adminRoles = ["Admin"];

        List<NavMenuItemDto> items =
        [
            Item("home", "Главная", d),
            Item("media", AppRes.Media, d + "Media"),
            Divider("divider-1"),
            Item("post-list", "Записи", d + "Post/post"),

            ..postTypes.Where(s => s.TypeName != "post").OrderBy(s => s.Title)
                       .Select(postType => Item($"post-type:{postType.TypeName}", postType.Title, d + $"Post/{postType.TypeName}")),

            Divider("divider-2"),
            Item("post-types", "Типы", d + "PostType", roles: adminRoles),
            Item("nav-menus", "Меню", d + "NavMenu", roles: adminRoles),
            Item(RazdelsId, "Разделы", "#razdels", roles: adminRoles),
                Item("feedback", "Письма", d + "FeedbackList", parentId: RazdelsId),
                //Item("geo", "Geo", d + "geo/GeoRegion", parentId: RazdelsId),
            Item(ManageId, "Управление", d + "Manage", roles: adminRoles),
                //Item("anketa", "Анкета", d + "Manage/AnketaManage", parentId: ManageId),
                Item("users", AppRes.Users, d + "Users", parentId: ManageId),
                Item("user-types", AppRes.UserTypes, d + "UserType", parentId: ManageId),
                Item("post-category-types", AppRes.PostCategoryTypes, d + "PostCategoryType", parentId: ManageId),
                //Item("contacts", "Контакты", d + "ContactsManagement", parentId: ManageId),
                //Item("roles", "Роли", d + "RoleManagement", parentId: ManageId),
                //Item("comments", "Комментарии", d + "Comments", parentId: ManageId),
            Divider("divider-3"),
            Item("plugins", AppRes.Plugins, d + "Plugins", roles: adminRoles),
            Item("settings", "Настройки", d + "Settings", roles: adminRoles),
        ];

        return new NavMenuDetail
        {
            Id = DevMenuId,
            Title = DevMenuTitle,
            Slug = DevMenuSlug,
            MenuItems = items,
            CreatedAt = DateTimeOffset.Now,
            ModifiedAt = null,
            Disabled = false,
            Roles = [],
            RolesInverse = false,
            Class = "",
            Style = "",
            Tags = [SystemTag],
            IsPersisted = false,
        };
    }

    /// <summary>
    /// Merge сохранённой копии с дефолтным состоянием:
    /// пункты из БД переопределяют дефолтные, отсутствующие дефолтные пункты
    /// (например новый тип записей) добавляются в дефолтную позицию.
    /// </summary>
    public static NavMenuDetail Merge(NavMenuDetail dbMenu, NavMenuDetail defaultMenu)
    {
        var defaults = defaultMenu.MenuItems.ToList();
        var defaultIds = defaults.Select(s => s.Id).ToHashSet();

        var result = dbMenu.MenuItems.ToList();
        var resultIds = result.Select(s => s.Id).ToHashSet();

        for (int i = 0; i < defaults.Count; i++)
        {
            var def = defaults[i];
            if (resultIds.Contains(def.Id)) continue;

            int insertAt = 0;
            for (int j = i - 1; j >= 0; j--)
            {
                int idx = result.FindIndex(s => s.Id == defaults[j].Id);
                if (idx >= 0)
                {
                    insertAt = idx + 1;
                    break;
                }
            }

            result.Insert(insertAt, def);
            resultIds.Add(def.Id);
        }

        var tags = dbMenu.Tags.Contains(SystemTag) ? dbMenu.Tags : [.. dbMenu.Tags, SystemTag];

        return dbMenu with
        {
            Slug = DevMenuSlug,
            Tags = tags,
            MenuItems = result.Select(s => s with { IsSystem = defaultIds.Contains(s.Id) }).ToList(),
            IsPersisted = true,
        };
    }

    static NavMenuItemDto Item(string key, string title, string url, Guid? parentId = null, IReadOnlyCollection<string>? roles = null)
        => Item(StableItemGuid(key), title, url, parentId, roles);

    static NavMenuItemDto Item(Guid id, string title, string url, Guid? parentId = null, IReadOnlyCollection<string>? roles = null)
        => new()
        {
            Id = id,
            ParentId = parentId ?? Guid.Empty,
            Title = title,
            Url = url,
            Icon = "",
            Roles = roles ?? [],
            RolesInverse = false,
            Class = "",
            Style = "",
            OpenInNewTab = false,
            Disabled = false,
            IsHeader = false,
            IsDivider = false,
            IsSystem = true,
        };

    static NavMenuItemDto Divider(string key)
        => new()
        {
            Id = StableItemGuid(key),
            ParentId = Guid.Empty,
            Title = "",
            Url = "",
            Icon = "",
            Roles = [],
            RolesInverse = false,
            Class = "",
            Style = "",
            OpenInNewTab = false,
            Disabled = false,
            IsHeader = false,
            IsDivider = true,
            IsSystem = true,
        };

    static Guid StableItemGuid(string key)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(StableIdPrefix + key), hash);
        return new Guid(hash[..16]);
    }
}
