using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class LogConfigResponse // (container.LogConfig)
{
    [DataMember(Name = "Type", EmitDefaultValue = false)]
    public required string Type { get; init; }

    [DataMember(Name = "Config", EmitDefaultValue = false)]
    public required IDictionary<string, string> Config { get; init; }
}
