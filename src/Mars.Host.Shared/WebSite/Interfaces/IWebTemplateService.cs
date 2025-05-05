using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.WebSite.Interfaces;

public interface IWebTemplateService
{
    public void ScanSite();

    public WebSiteTemplate Template { get; set; }
}