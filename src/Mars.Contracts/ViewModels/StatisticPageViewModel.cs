namespace Mars.Shared.ViewModels;

public class StatisticPageViewModel
{
    public required StatBlock b1 { get; set; }
    public required StatBlock b2 { get; set; }
    public required StatBlock b3 { get; set; }
    public required StatBlock b4 { get; set; }
}

public class StatBlock
{
    public string Title { get; set; } = "";
    public string MainValue { get; set; } = "";
    public string FooterText { get; set; } = "";
    public string FooterValue { get; set; } = "";
    public string Row1 { get; set; } = "";
    public string Row2 { get; set; } = "";
}
