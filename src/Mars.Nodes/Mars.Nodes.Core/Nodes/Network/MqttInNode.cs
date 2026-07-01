using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;
using Mars.Nodes.Core.Fields;

namespace Mars.Nodes.Core.Nodes.Network;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/MqttInNode/MqttInNode{.lang}.md")]
[Display(GroupName = "network")]
public class MqttInNode : Node
{
    public override string TypeId => "core.MqttInNode";

    public InputConfig<MqttBrokerConfigNode> Config { get; set; }

    public string Topic { get; set; } = "#";
    public NodeMqttQualityOfServiceLevel QoS { get; set; }

    public MqttInNode()
    {
        Color = "#d5c0d8";
        Outputs = [new()];
        Icon = "_content/Mars.Nodes.Workspace/nodes/mqtt-64.svg";
    }

}
