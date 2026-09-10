using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class NetworkSettingsResponse // (types.NetworkSettings)
{

    [DataMember(Name = "Bridge", EmitDefaultValue = false)]
    public required string Bridge { get; init; }

    [DataMember(Name = "SandboxID", EmitDefaultValue = false)]
    public required string SandboxID { get; init; }

    [DataMember(Name = "HairpinMode", EmitDefaultValue = false)]
    public required bool HairpinMode { get; init; }

    [DataMember(Name = "LinkLocalIPv6Address", EmitDefaultValue = false)]
    public required string LinkLocalIPv6Address { get; init; }

    [DataMember(Name = "LinkLocalIPv6PrefixLen", EmitDefaultValue = false)]
    public required long LinkLocalIPv6PrefixLen { get; init; }

    [DataMember(Name = "Ports", EmitDefaultValue = false)]
    public required IDictionary<string, IList<DockerPortBindingResponse>> Ports { get; init; }

    [DataMember(Name = "SandboxKey", EmitDefaultValue = false)]
    public required string SandboxKey { get; init; }

    [DataMember(Name = "SecondaryIPAddresses", EmitDefaultValue = false)]
    public required IList<AddressResponse> SecondaryIPAddresses { get; init; }

    [DataMember(Name = "SecondaryIPv6Addresses", EmitDefaultValue = false)]
    public required IList<AddressResponse> SecondaryIPv6Addresses { get; init; }

    [DataMember(Name = "EndpointID", EmitDefaultValue = false)]
    public required string EndpointID { get; init; }

    [DataMember(Name = "Gateway", EmitDefaultValue = false)]
    public required string Gateway { get; init; }

    [DataMember(Name = "GlobalIPv6Address", EmitDefaultValue = false)]
    public required string GlobalIPv6Address { get; init; }

    [DataMember(Name = "GlobalIPv6PrefixLen", EmitDefaultValue = false)]
    public required long GlobalIPv6PrefixLen { get; init; }

    [DataMember(Name = "IPAddress", EmitDefaultValue = false)]
    public required string IPAddress { get; init; }

    [DataMember(Name = "IPPrefixLen", EmitDefaultValue = false)]
    public required long IPPrefixLen { get; init; }

    [DataMember(Name = "IPv6Gateway", EmitDefaultValue = false)]
    public required string IPv6Gateway { get; init; }

    [DataMember(Name = "MacAddress", EmitDefaultValue = false)]
    public required string MacAddress { get; init; }

    [DataMember(Name = "Networks", EmitDefaultValue = false)]
    public required IDictionary<string, EndpointSettingsResponse> Networks { get; init; }
}
