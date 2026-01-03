using System.Text.Json.Serialization;

namespace Mars.Nodes.Core.Fields;

public struct InputConfig<T>
    where T : Node
{
    public string Id { get; set; } = "";

    [JsonIgnore]
    public T? Value;

    public InputConfig()
    {
    }
}
