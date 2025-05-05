using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class ContainerInspectResponse1
{
    [DataMember(Name = "Id", EmitDefaultValue = false)]
    public required string ID { get; init; }

    [DataMember(Name = "Created", EmitDefaultValue = false)]
    public required DateTime Created { get; init; }

    [DataMember(Name = "Path", EmitDefaultValue = false)]
    public required string Path { get; init; }

    [DataMember(Name = "Args", EmitDefaultValue = false)]
    public required IList<string> Args { get; init; }

    [DataMember(Name = "State", EmitDefaultValue = false)]
    public required ContainerStateResponse State { get; init; }

    [DataMember(Name = "Image", EmitDefaultValue = false)]
    public required string Image { get; init; }

    [DataMember(Name = "ResolvConfPath", EmitDefaultValue = false)]
    public required string ResolvConfPath { get; init; }

    [DataMember(Name = "HostnamePath", EmitDefaultValue = false)]
    public required string HostnamePath { get; init; }

    [DataMember(Name = "HostsPath", EmitDefaultValue = false)]
    public required string HostsPath { get; init; }

    [DataMember(Name = "LogPath", EmitDefaultValue = false)]
    public required string LogPath { get; init; }

    [DataMember(Name = "Node", EmitDefaultValue = false)]
    public required ContainerNodeResponse Node { get; init; }

    [DataMember(Name = "Name", EmitDefaultValue = false)]
    public required string Name { get; init; }

    [DataMember(Name = "RestartCount", EmitDefaultValue = false)]
    public required long RestartCount { get; init; }

    [DataMember(Name = "Driver", EmitDefaultValue = false)]
    public required string Driver { get; init; }

    [DataMember(Name = "Platform", EmitDefaultValue = false)]
    public required string Platform { get; init; }

    [DataMember(Name = "MountLabel", EmitDefaultValue = false)]
    public required string MountLabel { get; init; }

    [DataMember(Name = "ProcessLabel", EmitDefaultValue = false)]
    public required string ProcessLabel { get; init; }

    [DataMember(Name = "AppArmorProfile", EmitDefaultValue = false)]
    public required string AppArmorProfile { get; init; }

    [DataMember(Name = "ExecIDs", EmitDefaultValue = false)]
    public required IList<string> ExecIDs { get; init; }

    [DataMember(Name = "HostConfig", EmitDefaultValue = false)]
    public required HostConfigResponse HostConfig { get; init; }

    [DataMember(Name = "GraphDriver", EmitDefaultValue = false)]
    public required GraphDriverDataResponse GraphDriver { get; init; }

    [DataMember(Name = "SizeRw", EmitDefaultValue = false)]
    public required long? SizeRw { get; init; }

    [DataMember(Name = "SizeRootFs", EmitDefaultValue = false)]
    public required long? SizeRootFs { get; init; }

    [DataMember(Name = "Mounts", EmitDefaultValue = false)]
    public required IList<MountPointResponse> Mounts { get; init; }

    [DataMember(Name = "Config", EmitDefaultValue = false)]
    public required ConfigResponse Config { get; init; }

    [DataMember(Name = "NetworkSettings", EmitDefaultValue = false)]
    public required NetworkSettingsResponse NetworkSettings { get; init; }

}
