using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Core.Features;

namespace Mars.Nodes.Core.Nodes;

public abstract class ConfigNode : Node
{
    public override string Color { get; set; } = "#dddddd";
    public override string Icon { get; set; } = "_content/Mars.Nodes.Workspace/nodes/configfile-48.png";

    //Нужен чтобы UnknownNode(когда выгрузили плагины) он определялся правильно. В будущем сделать тип
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public override bool IsConfigNode => true;

    /// <summary>
    /// compare values as string, not include inherited properties and name
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsEqualRootPropertyValues(ConfigNode other)
    {
        var thisValues = TextTool.GetPropertiesValueAsString(this);
        var otherValues = TextTool.GetPropertiesValueAsString(other);
        return thisValues.Equals(otherValues);
    }

    public bool IsEqualAsJsonValues(Node configNode)
    {
        var existConfigJson = JsonSerializer.SerializeToDocument(this, inputType: GetType());
        var newConfigJson = JsonSerializer.SerializeToDocument(configNode, inputType: configNode.GetType());

        //var j1 = existConfigJson.RootElement.GetRawText();
        //var j2 = newConfigJson.RootElement.GetRawText();

        bool areEqual = JsonElement.DeepEquals(existConfigJson.RootElement, newConfigJson.RootElement);

        return areEqual;
    }
}
