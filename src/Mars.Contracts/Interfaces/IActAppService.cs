using Mars.Shared.Contracts.XActions;

namespace Mars.Shared.Interfaces;

public interface IActAppService
{
    Task<XActResult> Inject(string id, IReadOnlyDictionary<string, string>? args = null);
}
