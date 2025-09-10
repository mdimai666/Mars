using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Scripts;
using Mars.Options.Models;

namespace Mars.Host.WebSite.Scripts;

public class FaviconAssetProvider(IOptionService optionService) : ISiteAssetPrivider
{
    public string HtmlContent()
    {
        var generatedValue = optionService.GetOption<FaviconOptionGenaratedValues>();
        return generatedValue.GeneratedMetaTags ?? string.Empty;
    }
}
