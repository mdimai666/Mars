using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class DockerPortBindingResponse // (nat.PortBinding)
{
    [DataMember(Name = "HostIp", EmitDefaultValue = false)]
    public required string HostIP { get; init; }

    [DataMember(Name = "HostPort", EmitDefaultValue = false)]
    public required string HostPort { get; init; }
}
