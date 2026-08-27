using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Abstractions.Interfaces;
using Mars.SiteEngine.Abstractions.Services;

namespace Mars.SiteEngine.Templators;

public class TemplatorFeaturesLocator : ITemplatorFeaturesLocator
{
    public Dictionary<string, TemplatorRegisterFunction> Functions { get; set; } = [];

}
