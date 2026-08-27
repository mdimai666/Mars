using Mars.SiteEngine.Abstractions.Constants.Website;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Abstractions.WebSite.Scripts;

public class AppAdminWebSitePluggablePluginScripts : WebSitePluggablePluginScripts
{
    public AppAdminWebSitePluggablePluginScripts([FromKeyedServices(AppAdminConstants.SiteScriptsBuilderKey)] ISiteScriptsBuilder siteScriptsBuilder) : base(siteScriptsBuilder)
    {

    }
}
