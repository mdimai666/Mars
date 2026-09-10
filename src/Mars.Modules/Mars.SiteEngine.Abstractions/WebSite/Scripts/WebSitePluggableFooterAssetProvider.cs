namespace Mars.SiteEngine.Abstractions.WebSite.Scripts;

public class WebSitePluggableFooterAssetProvider(IWebSitePluggablePluginScripts webSitePluggablePluginScripts) : ISiteAssetPrivider
{
    public string HtmlContent() => webSitePluggablePluginScripts.CompiledFooter;
}
