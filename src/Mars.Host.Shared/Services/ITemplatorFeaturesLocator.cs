using Mars.Host.Shared.Interfaces;

namespace Mars.Host.Shared.Services;

public interface ITemplatorFeaturesLocator
{
    public Dictionary<string, Func<IXTFunctionContext, Task<object>>> Functions { get; set; }
}
