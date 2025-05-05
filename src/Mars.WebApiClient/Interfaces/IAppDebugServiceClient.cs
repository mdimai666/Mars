using Mars.Shared.Common;

namespace Mars.WebApiClient.Interfaces;

public interface IAppDebugServiceClient
{
    Task<UserActionResult<string>> GetLogs(string filename, int lines = 1000);
    Task<IReadOnlyCollection<string>> LogFiles();
}
