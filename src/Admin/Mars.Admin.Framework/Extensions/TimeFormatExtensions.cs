namespace Mars.Admin.Framework.Extensions;

/// <summary>
/// Формат времени для списков/журналов: «сегодня/вчера + время» для свежих записей,
/// полная дата для остальных.
/// </summary>
public static class TimeFormatExtensions
{
    public static string FormatTime(this DateTimeOffset ts) => ts.LocalDateTime.FormatTime();

    public static string FormatTime(this DateTime ts)
    {
        var time = ts.ToString("HH:mm:ss");

        if (ts.Date == DateTime.Today) return $"сегодня {time}";
        if (ts.Date == DateTime.Today.AddDays(-1)) return $"вчера {time}";

        return $"{ts:dd.MM.yy} {time}";
    }
}
