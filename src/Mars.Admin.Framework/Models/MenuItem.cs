using Microsoft.AspNetCore.Components.Routing;
using System.Collections.Generic;

namespace AppFront.Shared.Models
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
    }
}
