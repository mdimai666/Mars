using Mars.Shared.Common;

namespace Mars.WebApiClient.Interfaces;

public interface IAppDebugServiceClient
{
    /// <summary>
    /// Хвост логов (бесшовно по дневным файлам) с опциональной фильтрацией записей:
    /// <paramref name="levels"/> — канонические уровни (TRACE/DEBUG/INFO/WARN/ERROR/CRITICAL),
    /// <paramref name="period"/> — код периода ("1h", "6h", "1d", "7d", "30d").
    /// </summary>
    Task<UserActionResult<string>> GetLogs(int lines = 1000, IReadOnlyCollection<string>? levels = null, string? period = null);
    Task<IReadOnlyCollection<string>> LogFiles();
}
