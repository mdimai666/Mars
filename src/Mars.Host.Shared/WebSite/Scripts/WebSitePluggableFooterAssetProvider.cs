namespace Mars.Host.Shared.WebSite.Scripts;

public class WebSitePluggableFooterAssetProvider(IWebSitePluggablePluginScripts webSitePluggablePluginScripts) : ISiteAssetPrivider
{
    public string HtmlContent() => webSitePluggablePluginScripts.CompiledFooter;
}
