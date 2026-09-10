using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class ContainerNodeResponse // (types.ContainerNode)
{
    [DataMember(Name = "ID", EmitDefaultValue = false)]
    public required string ID { get; init; }

    [DataMember(Name = "IP", EmitDefaultValue = false)]
    public required string IPAddress { get; init; }

    [DataMember(Name = "Addr", EmitDefaultValue = false)]
    public required string Addr { get; init; }

    [DataMember(Name = "Name", EmitDefaultValue = false)]
    public required string Name { get; init; }

    [DataMember(Name = "Cpus", EmitDefaultValue = false)]
    public required long Cpus { get; init; }

    [DataMember(Name = "Memory", EmitDefaultValue = false)]
    public required long Memory { get; init; }

    [DataMember(Name = "Labels", EmitDefaultValue = false)]
    public required IDictionary<string, string> Labels { get; init; }
}
