using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Abstractions.Templators;

namespace Mars.SiteEngine.Host.Templators;

public class TemplatorFeaturesLocator : ITemplatorFeaturesLocator
{
    public Dictionary<string, TemplatorRegisterFunction> Functions { get; set; } = [];

}
