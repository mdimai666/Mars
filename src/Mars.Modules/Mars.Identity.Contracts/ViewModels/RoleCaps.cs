namespace Mars.Identity.Contracts.ViewModels;

/// <summary>
/// Каталог гранулярных прав (ai/RolesPermissionsPlan.md, «Каталог прав (26)»).
/// Хранение: RoleClaimEntity (ClaimType = <see cref="ClaimType"/>, ClaimValue = значение константы).
/// Названия групп/прав для матрицы — resx-ключи AppRes: группа = имя вложенного класса,
/// право = "{имя класса}.{имя константы}" (см. RoleService.GetClaimsBlankList).
/// </summary>
public static class RoleCaps
{
    // Объявлен здесь (Contracts), а не в Abstractions: Contracts не ссылается на Abstractions,
    // а обратная ссылка уже есть — Aliases в RoleCaps из Abstractions дал бы цикл.
    public const string ClaimType = "mars_cap";

    public static class PostCap
    {
        public const string Add = "Post.Add";
        public const string Update = "Post.Update";
        public const string UpdateAny = "Post.UpdateAny";
        public const string Delete = "Post.Delete";
        public const string DeleteAny = "Post.DeleteAny";
        public const string Publish = "Post.Publish";
    }

    public static class MediaCap
    {
        public const string Add = "Media.Add";
        public const string Update = "Media.Update";
        public const string Delete = "Media.Delete";
    }

    public static class PostCategoryCap
    {
        public const string Manage = "PostCategory.Manage";
    }

    public static class PostTypeCap
    {
        public const string Manage = "PostType.Manage";
    }

    public static class NavMenuCap
    {
        public const string Manage = "NavMenu.Manage";
    }

    public static class UserCap
    {
        public const string Manage = "User.Manage";
        public const string UpdateRole = "User.UpdateRole";
    }

    public static class UserTypeCap
    {
        public const string Manage = "UserType.Manage";
    }

    public static class RoleCap
    {
        public const string Manage = "Role.Manage";
    }

    public static class FeedbackCap
    {
        public const string Manage = "Feedback.Manage";
    }

    public static class SettingsCap
    {
        public const string Manage = "Settings.Manage";
    }

    public static class NotificationsCap
    {
        public const string Manage = "Notifications.Manage";
    }

    public static class PluginCap
    {
        public const string Manage = "Plugin.Manage";
    }

    public static class FlowCap
    {
        public const string Manage = "Flow.Manage";
    }

    public static class DatasourceCap
    {
        public const string Manage = "Datasource.Manage";
    }

    public static class SchedulerCap
    {
        public const string Manage = "Scheduler.Manage";
    }

    public static class FrontCap
    {
        public const string Manage = "Front.Manage";
    }

    public static class AiChatCap
    {
        public const string Use = "AiChat.Use";
    }

    public static class SystemCap
    {
        public const string Manage = "System.Manage";
    }
}

public class RoleCapElement
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public RoleCapElement()
    {

    }
    public RoleCapElement(string id, string title, string description)
    {
        Id = id;
        Title = title;
        Description = description;
    }

}

public class RoleCapElementCheckable : RoleCapElement
{
    public bool Checked { get; set; }

    public RoleCapElementCheckable()
    {

    }

    public RoleCapElementCheckable(RoleCapElement cap)
    {
        Id = cap.Id;
        Title = cap.Title;
        Description = cap.Description;

    }
}

public class RoleCapGroup
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RoleCapElement[] RoleCapElements { get; set; } = [];
}

public class RoleCapGroupCheckable
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public RoleCapElementCheckable[] RoleCapElements { get; set; } = [];

    public RoleCapGroupCheckable()
    {

    }
    public RoleCapGroupCheckable(RoleCapGroup roleCapGroup)
    {
        Title = roleCapGroup.Title;
        Description = roleCapGroup.Description;
        RoleCapElements = roleCapGroup.RoleCapElements.Select(cap => new RoleCapElementCheckable(cap)).ToArray();
    }
}
