using Mars.XActions.Contracts;

namespace Mars.Admin.Framework.Services;

public interface IActAppService
{
    Task<XActResult> Inject(string id, IReadOnlyDictionary<string, string>? args = null);
}
