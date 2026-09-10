using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class EndpointIPAMConfigResponse // (network.EndpointIPAMConfig)
{
    [DataMember(Name = "IPv4Address", EmitDefaultValue = false)]
    public required string IPv4Address { get; init; }

    [DataMember(Name = "IPv6Address", EmitDefaultValue = false)]
    public required string IPv6Address { get; init; }

    [DataMember(Name = "LinkLocalIPs", EmitDefaultValue = false)]
    public required IList<string> LinkLocalIPs { get; init; }
}
