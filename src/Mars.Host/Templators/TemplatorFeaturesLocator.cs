using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Services;

namespace Mars.Host.Templators;

public class TemplatorFeaturesLocator : ITemplatorFeaturesLocator
{
    public Dictionary<string, Func<IXTFunctionContext, Task<object>>> Functions { get; set; } = new();

}
