using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class DockerPortResponse // (types.Port)
{
    [DataMember(Name = "IP", EmitDefaultValue = false)]
    public required string IP { get; init; }

    [DataMember(Name = "PrivatePort", EmitDefaultValue = false)]
    public required ushort PrivatePort { get; init; }

    [DataMember(Name = "PublicPort", EmitDefaultValue = false)]
    public required ushort PublicPort { get; init; }

    [DataMember(Name = "Type", EmitDefaultValue = false)]
    public required string Type { get; init; }
}
