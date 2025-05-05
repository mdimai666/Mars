using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class DeviceRequestResponse // (container.DeviceRequest)
{
    [DataMember(Name = "Driver", EmitDefaultValue = false)]
    public required string Driver { get; init; }

    [DataMember(Name = "Count", EmitDefaultValue = false)]
    public required long Count { get; init; }

    [DataMember(Name = "DeviceIDs", EmitDefaultValue = false)]
    public required IList<string> DeviceIDs { get; init; }

    [DataMember(Name = "Capabilities", EmitDefaultValue = false)]
    public required IList<IList<string>> Capabilities { get; init; }

    [DataMember(Name = "Options", EmitDefaultValue = false)]
    public required IDictionary<string, string> Options { get; init; }
}
