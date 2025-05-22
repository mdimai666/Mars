using System.Text.Json.Serialization;
using Mars.Core.Features;

namespace Mars.Nodes.Core.Nodes;

public abstract class ConfigNode : Node
{
    public override string Color { get; set; } = "#dddddd";
    public override string Icon { get; set; } = "_content/Mars.Nodes.Workspace/nodes/configfile-48.png";

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public override bool IsConfigNode => true;


    /// <summary>
    /// compare values as string, not include inherited properties and name
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsEqualPropertyValues(ConfigNode other)
    {
        var thisValues = TextTool.GetPropertiesValueAsString(this);
        var otherValues = TextTool.GetPropertiesValueAsString(other);
        return thisValues.Equals(otherValues);
    }
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
