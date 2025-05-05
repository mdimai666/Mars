using System;

namespace Mars.Core.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ChangeTime(this DateTime dateTime, int hours, int minutes, int seconds = default, int milliseconds = default)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hours, minutes, seconds, milliseconds, dateTime.Kind);
    }

    public static DateTime ChangeTime(this DateTime dateTime, TimeOnly time)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, time.Hour, time.Minute, time.Second, time.Millisecond, dateTime.Kind);
    }

    public static DateTime ResetSecondAndMillisecond(this DateTime dateTime)
    {
        return dateTime.ChangeTime(dateTime.Hour, dateTime.Minute, 0, 0);
    }

    public static int DayOfWeekRus(this DateTime dateTime)
    {
        int w = (int)dateTime.DayOfWeek;
        --w;
        return w == -1 ? 6 : w;

    }

    public static DateTime FirstDate(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1, 0, 0, 0, 0, dateTime.Kind);
    }
}
