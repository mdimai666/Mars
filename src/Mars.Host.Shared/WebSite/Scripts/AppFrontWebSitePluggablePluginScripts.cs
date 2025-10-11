using Mars.Host.Shared.Constants.Website;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.WebSite.Scripts;

public class AppFrontWebSitePluggablePluginScripts : WebSitePluggablePluginScripts
{
    public AppFrontWebSitePluggablePluginScripts([FromKeyedServices(AppFrontConstants.SiteScriptsBuilderKey)] ISiteScriptsBuilder siteScriptsBuilder) : base(siteScriptsBuilder)
    {

    }
}
