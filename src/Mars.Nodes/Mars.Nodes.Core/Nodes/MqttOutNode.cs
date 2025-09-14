using System.ComponentModel.DataAnnotations;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/MqttOutNode/MqttOutNode{.lang}.md")]
[Display(GroupName = "network")]
public class MqttOutNode : Node
{
    public InputConfig<MqttBrokerConfigNode> Config { get; set; }

    [Display(Name = "Topic", Description = "the MQTT topic to publish to")]
    public string Topic { get; set; } = "";
    public NodeMqttQualityOfServiceLevel QoS { get; set; } 

    public MqttOutNode()
    {
        Inputs = [new()];
        Color = "#d5c0d8";
        Icon = "_content/Mars.Nodes.Workspace/nodes/mqtt-64.svg";
    }
}

public enum NodeMqttQualityOfServiceLevel
{
    AtMostOnce = 0x00,
    AtLeastOnce = 0x01,
    ExactlyOnce = 0x02
}
