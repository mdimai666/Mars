using Mars.SiteEngine.Abstractions.WebSite.Scripts;

namespace Mars.SiteEngine.Host.WebSite.Scripts;

public class AppAdminHeadAssetProvider(AppAdminSpaHtmlScripts appAdminSpaHtmlScripts) : ISiteAssetPrivider
{
    public string HtmlContent() => appAdminSpaHtmlScripts.CompiledHeader;
}

