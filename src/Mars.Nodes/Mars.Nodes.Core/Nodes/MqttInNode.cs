using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/MqttInNode/MqttInNode{.lang}.md")]
public class MqttInNode : Node
{
    public InputConfig<MqttBrokerConfigNode> Config { get; set; }

    public string Topic { get; set; } = "#";
    public NodeMqttQualityOfServiceLevel QoS { get; set; }

    public MqttInNode()
    {
        HaveInput = false;
        Color = "#d5c0d8";
        Outputs = new List<NodeOutput> { new NodeOutput() };
        Icon = "_content/Mars.Nodes.Workspace/nodes/mqtt-64.svg";
    }

}
