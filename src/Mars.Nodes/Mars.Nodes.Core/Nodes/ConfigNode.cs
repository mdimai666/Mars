using System.Text.Json.Serialization;

namespace Mars.Nodes.Core.Nodes;

public abstract class ConfigNode : Node
{
    public override string Color { get; set; } = "#dddddd";
    public override string Icon { get; set; } = "_content/Mars.Nodes.Workspace/nodes/configfile-48.png";

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public override bool IsConfigNode => true;
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
