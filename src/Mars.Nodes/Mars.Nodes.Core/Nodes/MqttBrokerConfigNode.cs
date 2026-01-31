using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mars.Core.Attributes;

namespace Mars.Nodes.Core.Nodes;

[FunctionApiDocument("./_content/mdimai666.Mars.Nodes.FormEditor/Docs/MqttBrokerConfigNode/MqttBrokerConfigNode{.lang}.md")]
[Display(GroupName = "network")]
public class MqttBrokerConfigNode : ConfigNode
{
    //host
    [Required]
    public string Host { get; set; } = "";
    public int Port { get; set; } = 1883;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ConnectAutomatically { get; set; } = true;
    //public bool UseTLS { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UseCleanStart { get; set; } = true;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public NodeMqttProtocolVersion ProtocolVersion { get; set; } = NodeMqttProtocolVersion.V500;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int KeepAlivePeriodInSeconds { get; set; } = 60; //mqtt-client default: 15;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    //client
    public string ClientId { get; set; } = "";

    public enum NodeMqttProtocolVersion
    {
        V310 = 3,
        V311 = 4,
        V500 = 5
    }
}
