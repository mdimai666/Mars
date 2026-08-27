using System.Net.NetworkInformation;
using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class MountPointResponse // (types.MountPoint)
{
    [DataMember(Name = "Type", EmitDefaultValue = false)]
    public required string Type { get; init; }

    [DataMember(Name = "Name", EmitDefaultValue = false)]
    public required string Name { get; init; }

    [DataMember(Name = "Source", EmitDefaultValue = false)]
    public required string Source { get; init; }

    [DataMember(Name = "Destination", EmitDefaultValue = false)]
    public required string Destination { get; init; }

    [DataMember(Name = "Driver", EmitDefaultValue = false)]
    public required string Driver { get; init; }

    [DataMember(Name = "Mode", EmitDefaultValue = false)]
    public required string Mode { get; init; }

    [DataMember(Name = "RW", EmitDefaultValue = false)]
    public required bool RW { get; init; }

    [DataMember(Name = "Propagation", EmitDefaultValue = false)]
    public required string Propagation { get; init; }
}
