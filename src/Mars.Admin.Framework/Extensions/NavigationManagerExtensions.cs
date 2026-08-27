using Mars.Admin.Framework.Features;
using Microsoft.AspNetCore.Components;

namespace Mars.Admin.Framework.Extensions;

public static class NavigationManagerExtensions
{
    public static void GoBack(this NavigationManager navigationManager)
    {
        Q.Root.Emit("GoBack");
    }

    public static string Path(this NavigationManager navigationManager)
    {
        string url = navigationManager.Uri.Replace(navigationManager.BaseUri, "").Split("?")[0];
        return url;
    }
}
