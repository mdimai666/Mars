using System.Text.Json.Serialization;

namespace Mars.Nodes.Core;

public class NodeInput
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Label { get; set; } = "";

}
