using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public record VolumesCreateRequest
{
    [DataMember(Name = "Name", EmitDefaultValue = false)]
    public required string Name { get; init; }

    [DataMember(Name = "Driver", EmitDefaultValue = false)]
    public required string Driver { get; init; }

    [DataMember(Name = "DriverOpts", EmitDefaultValue = false)]
    public required IReadOnlyDictionary<string, string> DriverOpts { get; init; }

    [DataMember(Name = "Labels", EmitDefaultValue = false)]
    public required IReadOnlyDictionary<string, string> Labels { get; init; }
}
