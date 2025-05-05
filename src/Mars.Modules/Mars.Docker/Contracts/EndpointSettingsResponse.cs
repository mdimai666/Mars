using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class EndpointSettingsResponse // (network.EndpointSettings)
{
    [DataMember(Name = "IPAMConfig", EmitDefaultValue = false)]
    public required EndpointIPAMConfigResponse? IPAMConfig { get; init; }

    [DataMember(Name = "Links", EmitDefaultValue = false)]
    public required IList<string> Links { get; init; }

    [DataMember(Name = "Aliases", EmitDefaultValue = false)]
    public required IList<string> Aliases { get; init; }

    [DataMember(Name = "NetworkID", EmitDefaultValue = false)]
    public required string NetworkID { get; init; }

    [DataMember(Name = "EndpointID", EmitDefaultValue = false)]
    public required string EndpointID { get; init; }

    [DataMember(Name = "Gateway", EmitDefaultValue = false)]
    public required string Gateway { get; init; }

    [DataMember(Name = "IPAddress", EmitDefaultValue = false)]
    public required string IPAddress { get; init; }

    [DataMember(Name = "IPPrefixLen", EmitDefaultValue = false)]
    public required long IPPrefixLen { get; init; }

    [DataMember(Name = "IPv6Gateway", EmitDefaultValue = false)]
    public required string IPv6Gateway { get; init; }

    [DataMember(Name = "GlobalIPv6Address", EmitDefaultValue = false)]
    public required string GlobalIPv6Address { get; init; }

    [DataMember(Name = "GlobalIPv6PrefixLen", EmitDefaultValue = false)]
    public required long GlobalIPv6PrefixLen { get; init; }

    [DataMember(Name = "MacAddress", EmitDefaultValue = false)]
    public required string MacAddress { get; init; }

    [DataMember(Name = "DriverOpts", EmitDefaultValue = false)]
    public required IDictionary<string, string> DriverOpts { get; init; }
}
