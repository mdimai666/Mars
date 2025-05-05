namespace Mars.Host.Templators;


public class CalendarRowInfo
{
    public List<CalendarMonthInfo> Months { get; set; } = new();
}


public class CalendarMonthInfo
{
    public List<CalendarDayInfo> Days { get; set; }
    public DateTime FirstDay { get; set; }

    public CalendarMonthInfo(DateTime date)
    {
        FirstDay = date.Date;
        var lastDay = DateTime.DaysInMonth(FirstDay.Year, FirstDay.Month);
        int count = lastDay - FirstDay.Day + 1;
        Days = new(count);

        for (int i = 0; i < count; i++)
        {
            Days.Add(new CalendarDayInfo(date.AddDays(i)));
        }
    }
}

public class CalendarDayInfo
{
    public DateTime Date { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsToday { get; set; }
    public string DayOfWeekShortName { get; set; }
    public int Day { get; set; }

    public CalendarDayInfo(DateTime date)
    {
        //int sd = (int)date.DayOfWeek;
        //sd = (sd == 0 ? 7 : sd) - 1;
        Date = date.Date;
        Day = date.Day;
        IsToday = date.Date == DateTime.Now.Date;
        IsWeekend = date.DayOfWeek == System.DayOfWeek.Sunday || date.DayOfWeek == System.DayOfWeek.Saturday;
        DayOfWeek = (date.DayOfWeek == 0) ? 7 : (int)date.DayOfWeek;
        DayOfWeekShortName = date.ToString("ddd").ToUpper();
    }

}