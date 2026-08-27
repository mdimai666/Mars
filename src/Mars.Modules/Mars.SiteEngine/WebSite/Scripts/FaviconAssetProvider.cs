using Mars.Options.Services;
using Mars.SiteEngine.Abstractions.WebSite.Scripts;
using Mars.Options.Models;

namespace Mars.SiteEngine.WebSite.Scripts;

public class FaviconAssetProvider(IOptionService optionService) : ISiteAssetPrivider
{
    public string HtmlContent()
    {
        var generatedValue = optionService.GetOption<FaviconOptionGenaratedValues>();
        return generatedValue.GeneratedMetaTags ?? string.Empty;
    }
}
