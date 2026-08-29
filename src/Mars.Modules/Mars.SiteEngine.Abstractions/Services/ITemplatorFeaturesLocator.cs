using Mars.SiteEngine.Abstractions.Interfaces;

namespace Mars.SiteEngine.Abstractions.Services;

public interface ITemplatorFeaturesLocator
{
    public Dictionary<string, TemplatorRegisterFunction> Functions { get; set; }
}
