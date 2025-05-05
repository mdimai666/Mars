using System.Text.Json.Serialization;

namespace Mars.Shared.Options;

public class FrontOptions
{
    public List<FrontOptionHostItem> HostItems { get; set; } = [new()];
}

public class FrontOptionHostItem
{
    string _url = "";
    public string Url { get => _url; set => _url = value.ToLower().TrimEnd('/'); }
    public string HostHtml { get; set; } = "@Body";
}

