using Mars.Contracts.Common;

namespace Mars.WebApiClient.Interfaces;

public interface IAppDebugServiceClient
{
    /// <summary>
    /// Хвост логов (бесшовно по дневным файлам) с опциональной фильтрацией записей:
    /// <paramref name="levels"/> — канонические уровни (TRACE/DEBUG/INFO/WARN/ERROR/CRITICAL),
    /// <paramref name="period"/> — код периода ("1h", "6h", "1d", "7d", "30d"),
    /// <paramref name="from"/>/<paramref name="to"/> — границы диапазона дат ("yyyy-MM-dd",
    /// <paramref name="from"/> имеет приоритет над <paramref name="period"/>).
    /// </summary>
    Task<UserActionResult<string>> GetLogs(int lines = 1000, IReadOnlyCollection<string>? levels = null, string? period = null, string? from = null, string? to = null);
    Task<IReadOnlyCollection<string>> LogFiles();
}
