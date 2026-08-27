using Mars.Host.Shared.WebSite.Scripts;

namespace Mars.Host.WebSite.Scripts;

public class AppAdminHeadAssetProvider(AppAdminSpaHtmlScripts appAdminSpaHtmlScripts) : ISiteAssetPrivider
{
    public string HtmlContent() => appAdminSpaHtmlScripts.CompiledHeader;
}

