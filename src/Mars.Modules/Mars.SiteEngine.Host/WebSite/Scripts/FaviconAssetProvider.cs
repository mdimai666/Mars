using Mars.Options.Abstractions.Services;
using Mars.SiteEngine.Abstractions.WebSite.Scripts;
using Mars.SiteEngine.Contracts.Options;

namespace Mars.SiteEngine.Host.WebSite.Scripts;

public class FaviconAssetProvider(IOptionService optionService) : ISiteAssetPrivider
{
    public string HtmlContent()
    {
        var generatedValue = optionService.GetOption<FaviconOptionGenaratedValues>();
        return generatedValue.GeneratedMetaTags ?? string.Empty;
    }
}
