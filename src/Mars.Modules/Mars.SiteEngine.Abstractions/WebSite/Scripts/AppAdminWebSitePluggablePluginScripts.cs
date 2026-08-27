using Mars.Host.Shared.Constants.Website;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.WebSite.Scripts;

public class AppAdminWebSitePluggablePluginScripts : WebSitePluggablePluginScripts
{
    public AppAdminWebSitePluggablePluginScripts([FromKeyedServices(AppAdminConstants.SiteScriptsBuilderKey)] ISiteScriptsBuilder siteScriptsBuilder) : base(siteScriptsBuilder)
    {

    }
}
