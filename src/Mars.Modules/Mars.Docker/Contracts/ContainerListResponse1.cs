using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class ContainerListResponse1 // (types.Container)
{
    [DataMember(Name = "Id", EmitDefaultValue = false)]
    public required string ID { get; init; }

    [DataMember(Name = "Names", EmitDefaultValue = false)]
    public required IList<string> Names { get; init; }

    [DataMember(Name = "Image", EmitDefaultValue = false)]
    public required string Image { get; init; }

    [DataMember(Name = "ImageID", EmitDefaultValue = false)]
    public required string ImageID { get; init; }

    [DataMember(Name = "Command", EmitDefaultValue = false)]
    public required string Command { get; init; }

    [DataMember(Name = "Created", EmitDefaultValue = false)]
    public required DateTime Created { get; init; }

    [DataMember(Name = "Ports", EmitDefaultValue = false)]
    public required IList<DockerPortResponse> Ports { get; init; }

    [DataMember(Name = "SizeRw", EmitDefaultValue = false)]
    public required long SizeRw { get; init; }

    [DataMember(Name = "SizeRootFs", EmitDefaultValue = false)]
    public required long SizeRootFs { get; init; }

    [DataMember(Name = "Labels", EmitDefaultValue = false)]
    public required IReadOnlyDictionary<string, string> Labels { get; init; }

    [DataMember(Name = "State", EmitDefaultValue = false)]
    public required string State { get; init; }

    [DataMember(Name = "Status", EmitDefaultValue = false)]
    public required string Status { get; init; }

    [DataMember(Name = "NetworkSettings", EmitDefaultValue = false)]
    public required SummaryNetworkSettingsResponse NetworkSettings { get; init; }

    [DataMember(Name = "Mounts", EmitDefaultValue = false)]
    public required IList<MountPointResponse> Mounts { get; init; }
}
