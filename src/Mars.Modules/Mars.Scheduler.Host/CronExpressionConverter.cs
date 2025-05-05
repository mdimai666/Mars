using System.Text;

namespace Mars.Scheduler.Host;

public static class CronExpressionConverter
{
    /// <summary>
    /// Возвращает ежедневное CRON выражение для указанного времени суток.
    /// </summary>
    /// <param name="dailyTime">Время суток.</param>
    /// <returns>Ежедневное CRON выражение.</returns>
    public static string Daily(TimeOnly dailyTime)
    {
        return $"{dailyTime.Minute} {dailyTime.Hour} * * *";
    }

    public static string DailyAsQuartz(TimeOnly dailyTime)
    {
        return $"0 {dailyTime.Minute} {dailyTime.Hour} * * ?";
    }

    /// <summary>
    /// Возвращает CRON выражение для заданного временного интервала.
    /// </summary>
    /// <param name="timespan">Временной интервал.</param>
    /// <returns>CRON выражение.</returns>
    public static string Interval(TimeSpan timespan)
    {
        var builder = new StringBuilder();

        // Минуты
        if (timespan.TotalMinutes % 60 != 0)
        {
            builder.Append($"*/{(int)(timespan.TotalMinutes % 60)} ");
        }
        else
        {
            builder.Append("* ");
        }

        // Часы
        if (timespan.TotalHours % 24 != 0)
        {
            builder.Append($"*/{(int)(timespan.TotalHours % 24)} ");
        }
        else
        {
            builder.Append("* ");
        }

        // Дни
        if (timespan.TotalDays != 0)
        {
            builder.Append($"*/{(int)timespan.TotalDays} ");
        }
        else
        {
            builder.Append("* * ");
        }

        // Месяцы и дни недели
        builder.Append("* *");

        return builder.ToString();
    }

    public static string IntervalAsQuartz(TimeSpan timespan)
    {
        return "0 " + Interval(timespan);
    }

}
