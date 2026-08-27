namespace Mars.Host.Shared.WebSite.Models;

public class DataQueryRequest
{
    public required string Key { get; init; }
    public required List<KeyValuePair<string, string>> Queries { get; init; }
    public Dictionary<string, object?>? ResultDict { get; set; }
    public bool Complete { get; set; }
}
