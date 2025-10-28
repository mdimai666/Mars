using System.Text.RegularExpressions;
using Mars.Core.Extensions;
using Mars.Shared.Contracts.NavMenus;
using Mars.Shared.Contracts.XActions;
using Microsoft.AspNetCore.Components.Routing;

namespace AppFront.Shared.Models;

public class MyNavMenu
{
    public string Text { get; set; } = "";
    public string NavigateUrl { get; set; } = "";
    /// <summary>
    /// Icon name or image url
    /// </summary>
    public string Icon { get; set; } = "";
    public string Right { get; set; } = "";
    public string HideRole { get; set; } = "";

    public List<MyNavMenu>? SubItems { get; set; }

    public NavLinkMatch Match { get; set; }

    public bool OpenInNewTab { get; set; }

    public bool IsHeader { get; set; }
    public bool IsDivider { get; set; }

    public string Class { get; set; } = "";
    public string Style { get; set; } = "";

    public MyNavMenu() { }

    public MyNavMenu(string text, string navigateUrl, string right = "", string icon = "", List<MyNavMenu>? subItems = null, string hideRole = "", NavLinkMatch match = NavLinkMatch.Prefix)
    {
        Text = text;
        NavigateUrl = navigateUrl;
        Right = right;
        HideRole = hideRole;
        Icon = icon;
        SubItems = subItems;
        Match = match;
    }

    public bool IsIcon => !string.IsNullOrEmpty(Icon) && !IconIsImageUrl();
    public bool IconIsImage => IconIsImageUrl();

    protected bool IconIsImageUrl()
    {
        var reg = new Regex(@"(\.jpg|\.png|\.svg|\.jpeg|\.webp)$");
        return reg.IsMatch(Icon);
    }

    public static List<MyNavMenu> Convert(List<NavMenuItemResponse> items, Guid? parentId = null)
    {
        return items.Where(s => s.ParentId == (parentId ?? Guid.Empty)).Select(s => new MyNavMenu
        {
            Icon = ProcessIconString(s) ?? "",
            Text = s.Title,
            Right = s.Roles.JoinStr(","),
            Match = (s.Url == "/" || s.Url == "#") ? NavLinkMatch.All : NavLinkMatch.Prefix,
            NavigateUrl = s.Url,
            OpenInNewTab = s.OpenInNewTab,
            IsDivider = s.IsDivider,
            IsHeader = s.IsHeader,
            Class = s.Class,
            Style = s.Style,

            SubItems = (items.Any(x => x.ParentId == s.Id)) ? Convert(items, s.Id).ToList() : null,

        }).ToList();
    }

    static string? ProcessIconString(NavMenuItemResponse menu)
    {
        string? icon = menu.Icon;
        if (string.IsNullOrEmpty(icon)) return icon;
        if (icon.StartsWith("http")) return icon;

        //return menu.IsFontIcon ? menu.Icon : Q.ServerUrlJoin(menu.Icon);
        return menu.Icon;
    }

    public static List<MenuItem> Convert(NavMenuDetailResponse menu, Guid? parentId = null)
    {
        parentId ??= Guid.Empty;

        return menu.MenuItems.Where(s => s.ParentId == parentId).Select(s =>
        {
            //var sub = s.GetItems(menu)?.ToList()!;
            var sub = menu.MenuItems.Where(f => f.ParentId == s.Id).ToList()!;
            var m = new MenuItem
            {
                Icon = s.Icon ?? "",
                Title = s.Title,
                Role = !s.RolesInverse ? s.Roles.JoinStr(",") : "",
                HideRole = s.RolesInverse ? s.Roles.JoinStr(",") : "",
                navLinkMatch = (s.Url == "/" || s.Url == "/dev/") ? NavLinkMatch.All : NavLinkMatch.Prefix,
                Url = s.Url,
                //OpenInNewTab = s.OpenInNewTab,
                IsDivider = s.IsDivider,
                menuType = s.IsHeader ? MenuItem.MenuType.Header : MenuItem.MenuType.Link,
                Class = s.Class,
                Style = s.Style,

                SubItems = Convert(menu, s.Id),
                SubItemFlag = sub.Any()

            };
            return m;

        }).ToList();
    }
}
