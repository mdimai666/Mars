using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class VolumeOptionsResponse // (mount.VolumeOptions)
{
    [DataMember(Name = "NoCopy", EmitDefaultValue = false)]
    public required bool NoCopy { get; init; }

    [DataMember(Name = "Labels", EmitDefaultValue = false)]
    public required IDictionary<string, string> Labels { get; init; }

    [DataMember(Name = "DriverConfig", EmitDefaultValue = false)]
    public required DriverResponse DriverConfig { get; init; }
}
