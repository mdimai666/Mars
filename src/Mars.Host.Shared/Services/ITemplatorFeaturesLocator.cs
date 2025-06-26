using Mars.Host.Shared.Interfaces;

namespace Mars.Host.Shared.Services;

public interface ITemplatorFeaturesLocator
{
    public Dictionary<string, TemplatorRegisterFunction> Functions { get; set; }
}
