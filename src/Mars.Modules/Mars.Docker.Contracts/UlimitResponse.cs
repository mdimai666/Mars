using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class UlimitResponse
{
    [DataMember(Name = "Name", EmitDefaultValue = false)]
    public required string Name { get; init; }

    [DataMember(Name = "Hard", EmitDefaultValue = false)]
    public required long Hard { get; init; }

    [DataMember(Name = "Soft", EmitDefaultValue = false)]
    public required long Soft { get; init; }
}
