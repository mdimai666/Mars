using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class BindOptionsResponse // (mount.BindOptions)
{
    [DataMember(Name = "Propagation", EmitDefaultValue = false)]
    public required string Propagation { get; init; }

    [DataMember(Name = "NonRecursive", EmitDefaultValue = false)]
    public required bool NonRecursive { get; init; }
}
