
using System.Collections.Generic;
using System.Linq;
using Mars.Core.Extensions;
using Microsoft.AspNetCore.Components.Routing;

namespace AppFront.Shared.Models
{
    public class BreadcrumbItem
    {
        public string Text { get; set; }
        public string Url { get; set; }

        public BreadcrumbItem(string text, string url)
        {
            Text = text;
            Url = url;
        }

        public static List<BreadcrumbItem> ListFromString(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return new();

            return path.Split('|').Select(s =>
            {
                int i = s.LastIndexOf(':');
                int r = s.Length - 1 - i;

                return new BreadcrumbItem((s.Right(r) ?? "Text").Trim('/'), s.Left(i) ?? "");
            }).ToList();
        }
    }
}
