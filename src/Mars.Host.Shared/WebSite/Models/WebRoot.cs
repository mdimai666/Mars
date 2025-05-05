namespace Mars.Host.Shared.WebSite.Models;

public class WebRoot : WebSitePart
{
    public string? DefaultLayout { get; private init; }
    public string StartPath { get; private init; }

    public WebRoot(WebSitePart part) : base(part)
    {
        var sp = Path.GetDirectoryName(part.FileRelPath);
        StartPath = string.IsNullOrEmpty(sp) ? "/" : sp;
        if (Attributes.TryGetValue(nameof(DefaultLayout), out var defaultLayout))
        {
            if (defaultLayout != "null")
            {
                DefaultLayout = defaultLayout;
            }
        }
    }
}
