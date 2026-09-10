using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class TmpfsOptionsResponse // (mount.TmpfsOptions)
{
    [DataMember(Name = "SizeBytes", EmitDefaultValue = false)]
    public required long SizeBytes { get; init; }

    [DataMember(Name = "Mode", EmitDefaultValue = false)]
    public required uint Mode { get; init; }
}
