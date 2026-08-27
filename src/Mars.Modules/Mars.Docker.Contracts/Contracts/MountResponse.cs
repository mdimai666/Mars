using System.Net;
using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class MountResponse // (mount.Mount)
{
    [DataMember(Name = "Type", EmitDefaultValue = false)]
    public required string Type { get; init; }

    [DataMember(Name = "Source", EmitDefaultValue = false)]
    public required string Source { get; init; }

    [DataMember(Name = "Target", EmitDefaultValue = false)]
    public required string Target { get; init; }

    [DataMember(Name = "ReadOnly", EmitDefaultValue = false)]
    public required bool ReadOnly { get; init; }

    [DataMember(Name = "Consistency", EmitDefaultValue = false)]
    public required string Consistency { get; init; }

    [DataMember(Name = "BindOptions", EmitDefaultValue = false)]
    public required BindOptionsResponse BindOptions { get; init; }

    [DataMember(Name = "VolumeOptions", EmitDefaultValue = false)]
    public required VolumeOptionsResponse VolumeOptions { get; init; }

    [DataMember(Name = "TmpfsOptions", EmitDefaultValue = false)]
    public required TmpfsOptionsResponse TmpfsOptions { get; init; }
}
