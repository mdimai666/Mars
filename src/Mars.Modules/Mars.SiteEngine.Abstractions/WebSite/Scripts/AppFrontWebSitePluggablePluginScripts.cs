using Mars.SiteEngine.Abstractions.Constants.Website;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Abstractions.WebSite.Scripts;

public class AppFrontWebSitePluggablePluginScripts : WebSitePluggablePluginScripts
{
    public AppFrontWebSitePluggablePluginScripts([FromKeyedServices(AppFrontConstants.SiteScriptsBuilderKey)] ISiteScriptsBuilder siteScriptsBuilder) : base(siteScriptsBuilder)
    {

    }
}
