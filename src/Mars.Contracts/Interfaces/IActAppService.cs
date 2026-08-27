using Mars.Contracts.XActions;

namespace Mars.Contracts.Interfaces;

public interface IActAppService
{
    Task<XActResult> Inject(string id, IReadOnlyDictionary<string, string>? args = null);
}
