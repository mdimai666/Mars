using Mars.Options.Abstractions.Services;
using Mars.Server.Contracts.Options;
using Mars.SiteEngine.Contracts.Options;

namespace Mars.SiteEngine.Host.Handlers;

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
        var appName = _optionService.GetOption<SiteSettings>().SiteName;
        var metaTags = await _faviconGeneratorHandler.Handle(faviconOption, appName, cancellationToken);

        var generatedValue = _optionService.GetOption<FaviconOptionGenaratedValues>();
        generatedValue.GeneratedMetaTags = metaTags;
        _optionService.SaveOption(generatedValue);
    }
}
