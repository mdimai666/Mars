using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class AddressResponse // (network.Address)
{
    [DataMember(Name = "Addr", EmitDefaultValue = false)]
    public required string Addr { get; init; }

    [DataMember(Name = "PrefixLen", EmitDefaultValue = false)]
    public required long PrefixLen { get; init; }
}
