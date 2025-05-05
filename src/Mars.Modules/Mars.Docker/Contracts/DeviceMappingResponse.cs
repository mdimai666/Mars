using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class DeviceMappingResponse
{
    [DataMember(Name = "PathOnHost", EmitDefaultValue = false)]
    public required string PathOnHost { get; init; }

    [DataMember(Name = "PathInContainer", EmitDefaultValue = false)]
    public required string PathInContainer { get; init; }

    [DataMember(Name = "CgroupPermissions", EmitDefaultValue = false)]
    public required string CgroupPermissions { get; init; }
}
