using Mars.Cms.Contracts.NavMenus;
using Mars.Core.Extensions;
using Microsoft.AspNetCore.Components.Routing;

namespace Mars.Admin.Framework.Models
{
    public class MenuItem
    {
        public string Title { get; set; } = default!;
        public string Url { get; set; } = default!;
        public string Icon { get; set; } = default!;


        public bool SubItemFlag { get; set; }
        public bool IsDivider { get; set; }

        public List<MenuItem> SubItems { get; set; } = default!;

        public NavLinkMatch navLinkMatch { get; set; } = NavLinkMatch.Prefix;

        public MenuType menuType { get; set; }

        public enum MenuType
        {
            Link,
            Header
        }

        public string Role { get; set; } = default!;
        public string HideRole { get; set; } = default!;
        public string Class { get; set; } = default!;
        public string Style { get; set; } = default!;

        public static List<MenuItem> Convert(NavMenuDetailResponse menu, Guid? parentId = null)
        {
            parentId ??= Guid.Empty;

            return menu.MenuItems.Where(s => s.ParentId == parentId).Select(s =>
            {
                var sub = menu.MenuItems.Where(f => f.ParentId == s.Id).ToList()!;
                var m = new MenuItem
                {
                    Icon = s.Icon ?? "",
                    Title = s.Title,
                    Role = !s.RolesInverse ? s.Roles.JoinStr(",") : "",
                    HideRole = s.RolesInverse ? s.Roles.JoinStr(",") : "",
                    navLinkMatch = (s.Url == "/" || s.Url == "/dev/") ? NavLinkMatch.All : NavLinkMatch.Prefix,
                    Url = s.Url,
                    IsDivider = s.IsDivider,
                    menuType = s.IsHeader ? MenuType.Header : MenuType.Link,
                    Class = s.Class,
                    Style = s.Style,

                    SubItems = Convert(menu, s.Id),
                    SubItemFlag = sub.Any()

                };
                return m;

            }).ToList();
        }
    }
}
