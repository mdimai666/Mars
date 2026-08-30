using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class ThrottleDeviceResponse
{
    [DataMember(Name = "Path", EmitDefaultValue = false)]
    public required string Path { get; init; }

    [DataMember(Name = "Rate", EmitDefaultValue = false)]
    public required ulong Rate { get; init; }
}
