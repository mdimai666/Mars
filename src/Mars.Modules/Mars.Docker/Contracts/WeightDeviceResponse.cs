using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class WeightDeviceResponse // (blkiodev.WeightDevice)
{
    [DataMember(Name = "Path", EmitDefaultValue = false)]
    public required string Path { get; init; }

    [DataMember(Name = "Weight", EmitDefaultValue = false)]
    public required ushort Weight { get; init; }
}
