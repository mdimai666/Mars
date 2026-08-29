using Mars.SiteEngine.Abstractions.WebSite.Scripts;

namespace Mars.SiteEngine.WebSite.Scripts;

public class AppAdminHeadAssetProvider(AppAdminSpaHtmlScripts appAdminSpaHtmlScripts) : ISiteAssetPrivider
{
    public string HtmlContent() => appAdminSpaHtmlScripts.CompiledHeader;
}

