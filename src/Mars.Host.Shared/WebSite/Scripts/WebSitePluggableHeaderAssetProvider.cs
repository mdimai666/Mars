namespace Mars.Host.Shared.WebSite.Scripts;

public class WebSitePluggableHeaderAssetProvider(IWebSitePluggablePluginScripts webSitePluggablePluginScripts) : ISiteAssetPrivider
{
    public string HtmlContent() => webSitePluggablePluginScripts.CompiledHeader;
}
