using Mars.Host.Shared.Handlers;
using Mars.Host.Shared.Services;
using Mars.Options.Models;

namespace Mars.Host.Handlers;

public class SiteFaviconConfiguratorHandler
{
    private readonly IFaviconGeneratorHandler _faviconGeneratorHandler;
    private readonly IOptionService _optionService;

    public SiteFaviconConfiguratorHandler(IFaviconGeneratorHandler faviconGeneratorHandler, IOptionService optionService)
    {
        _faviconGeneratorHandler = faviconGeneratorHandler;
        _optionService = optionService;
    }

    public async Task Handle(FaviconOption faviconOption, CancellationToken cancellationToken)
    {
        var appName = _optionService.SysOption.SiteName;
        var metaTags = await _faviconGeneratorHandler.Handle(faviconOption, appName, cancellationToken);

        var generatedValue = _optionService.GetOption<FaviconOptionGenaratedValues>();
        generatedValue.GeneratedMetaTags = metaTags;
        _optionService.SaveOption(generatedValue);
    }
}
