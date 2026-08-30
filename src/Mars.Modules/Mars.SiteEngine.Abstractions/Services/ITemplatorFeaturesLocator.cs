using Mars.SiteEngine.Abstractions.Templators;

namespace Mars.SiteEngine.Abstractions.Services;

public interface ITemplatorFeaturesLocator
{
    public Dictionary<string, TemplatorRegisterFunction> Functions { get; set; }
}
