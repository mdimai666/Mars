namespace Mars.SiteEngine.Abstractions.WebSite.Scripts;

public class WebSitePluggableHeaderAssetProvider(IWebSitePluggablePluginScripts webSitePluggablePluginScripts) : ISiteAssetPrivider
{
    public string HtmlContent() => webSitePluggablePluginScripts.CompiledHeader;
}
