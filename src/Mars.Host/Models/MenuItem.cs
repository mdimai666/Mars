using Microsoft.AspNetCore.Components.Routing;
using System.Collections.Generic;

namespace Mars.Host.Models
{
    public class MenuItem
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Icon { get; set; }
        public List<MenuItem> Items { get; set; }
        public string RouterLink { get; set; }
        public NavLinkMatch RouterMatch { get; set; } = NavLinkMatch.Prefix;


    }
}

namespace Mars.Models
{
}