using System.Text.Json.Serialization;

namespace Mars.Nodes.Core.Nodes;

public abstract class ConfigNode : Node
{
    public override string Color { get; set; } = "#dddddd";
    public override string Icon { get; set; } = "_content/NodeWorkspace/nodes/configfile-48.png";
}

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
