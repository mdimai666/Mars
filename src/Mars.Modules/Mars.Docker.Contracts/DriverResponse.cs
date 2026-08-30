using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class DriverResponse // (mount.Driver)
{
    [DataMember(Name = "Name", EmitDefaultValue = false)]
    public required string Name { get; init; }

    [DataMember(Name = "Options", EmitDefaultValue = false)]
    public required IDictionary<string, string> Options { get; init; }
}
