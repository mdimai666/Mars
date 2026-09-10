using System.Runtime.Serialization;

namespace Mars.Docker.Contracts;

[DataContract]
public class HostConfigResponse // (container.HostConfig)
{

    [DataMember(Name = "Binds", EmitDefaultValue = false)]
    public required IList<string> Binds { get; init; }

    [DataMember(Name = "ContainerIDFile", EmitDefaultValue = false)]
    public required string ContainerIDFile { get; init; }

    [DataMember(Name = "LogConfig", EmitDefaultValue = false)]
    public required LogConfigResponse LogConfig { get; init; }

    [DataMember(Name = "NetworkMode", EmitDefaultValue = false)]
    public required string NetworkMode { get; init; }

    [DataMember(Name = "PortBindings", EmitDefaultValue = false)]
    public required IDictionary<string, IList<DockerPortBindingResponse>> PortBindings { get; init; }

    [DataMember(Name = "RestartPolicy", EmitDefaultValue = false)]
    public required RestartPolicyResponse RestartPolicy { get; init; }

    [DataMember(Name = "AutoRemove", EmitDefaultValue = false)]
    public required bool AutoRemove { get; init; }

    [DataMember(Name = "VolumeDriver", EmitDefaultValue = false)]
    public required string VolumeDriver { get; init; }

    [DataMember(Name = "VolumesFrom", EmitDefaultValue = false)]
    public required IList<string> VolumesFrom { get; init; }

    [DataMember(Name = "CapAdd", EmitDefaultValue = false)]
    public required IList<string> CapAdd { get; init; }

    [DataMember(Name = "CapDrop", EmitDefaultValue = false)]
    public required IList<string> CapDrop { get; init; }

    [DataMember(Name = "CgroupnsMode", EmitDefaultValue = false)]
    public required string CgroupnsMode { get; init; }

    [DataMember(Name = "Dns", EmitDefaultValue = false)]
    public required IList<string> DNS { get; init; }

    [DataMember(Name = "DnsOptions", EmitDefaultValue = false)]
    public required IList<string> DNSOptions { get; init; }

    [DataMember(Name = "DnsSearch", EmitDefaultValue = false)]
    public required IList<string> DNSSearch { get; init; }

    [DataMember(Name = "ExtraHosts", EmitDefaultValue = false)]
    public required IList<string> ExtraHosts { get; init; }

    [DataMember(Name = "GroupAdd", EmitDefaultValue = false)]
    public required IList<string> GroupAdd { get; init; }

    [DataMember(Name = "IpcMode", EmitDefaultValue = false)]
    public required string IpcMode { get; init; }

    [DataMember(Name = "Cgroup", EmitDefaultValue = false)]
    public required string Cgroup { get; init; }

    [DataMember(Name = "Links", EmitDefaultValue = false)]
    public required IList<string> Links { get; init; }

    [DataMember(Name = "OomScoreAdj", EmitDefaultValue = false)]
    public required long OomScoreAdj { get; init; }

    [DataMember(Name = "PidMode", EmitDefaultValue = false)]
    public required string PidMode { get; init; }

    [DataMember(Name = "Privileged", EmitDefaultValue = false)]
    public required bool Privileged { get; init; }

    [DataMember(Name = "PublishAllPorts", EmitDefaultValue = false)]
    public required bool PublishAllPorts { get; init; }

    [DataMember(Name = "ReadonlyRootfs", EmitDefaultValue = false)]
    public required bool ReadonlyRootfs { get; init; }

    [DataMember(Name = "SecurityOpt", EmitDefaultValue = false)]
    public required IList<string> SecurityOpt { get; init; }

    [DataMember(Name = "StorageOpt", EmitDefaultValue = false)]
    public required IDictionary<string, string> StorageOpt { get; init; }

    [DataMember(Name = "Tmpfs", EmitDefaultValue = false)]
    public required IDictionary<string, string> Tmpfs { get; init; }

    [DataMember(Name = "UTSMode", EmitDefaultValue = false)]
    public required string UTSMode { get; init; }

    [DataMember(Name = "UsernsMode", EmitDefaultValue = false)]
    public required string UsernsMode { get; init; }

    [DataMember(Name = "ShmSize", EmitDefaultValue = false)]
    public required long ShmSize { get; init; }

    [DataMember(Name = "Sysctls", EmitDefaultValue = false)]
    public required IDictionary<string, string> Sysctls { get; init; }

    [DataMember(Name = "Runtime", EmitDefaultValue = false)]
    public required string Runtime { get; init; }

    [DataMember(Name = "ConsoleSize", EmitDefaultValue = false)]
    public required ulong[] ConsoleSize { get; init; }

    [DataMember(Name = "Isolation", EmitDefaultValue = false)]
    public required string Isolation { get; init; }

    [DataMember(Name = "CpuShares", EmitDefaultValue = false)]
    public required long CPUShares { get; init; }

    [DataMember(Name = "Memory", EmitDefaultValue = false)]
    public required long Memory { get; init; }

    [DataMember(Name = "NanoCpus", EmitDefaultValue = false)]
    public required long NanoCPUs { get; init; }

    [DataMember(Name = "CgroupParent", EmitDefaultValue = false)]
    public required string CgroupParent { get; init; }

    [DataMember(Name = "BlkioWeight", EmitDefaultValue = false)]
    public required ushort BlkioWeight { get; init; }

    [DataMember(Name = "BlkioWeightDevice", EmitDefaultValue = false)]
    public required IList<WeightDeviceResponse> BlkioWeightDevice { get; init; }

    [DataMember(Name = "BlkioDeviceReadBps", EmitDefaultValue = false)]
    public required IList<ThrottleDeviceResponse> BlkioDeviceReadBps { get; init; }

    [DataMember(Name = "BlkioDeviceWriteBps", EmitDefaultValue = false)]
    public required IList<ThrottleDeviceResponse> BlkioDeviceWriteBps { get; init; }

    [DataMember(Name = "BlkioDeviceReadIOps", EmitDefaultValue = false)]
    public required IList<ThrottleDeviceResponse> BlkioDeviceReadIOps { get; init; }

    [DataMember(Name = "BlkioDeviceWriteIOps", EmitDefaultValue = false)]
    public required IList<ThrottleDeviceResponse> BlkioDeviceWriteIOps { get; init; }

    [DataMember(Name = "CpuPeriod", EmitDefaultValue = false)]
    public required long CPUPeriod { get; init; }

    [DataMember(Name = "CpuQuota", EmitDefaultValue = false)]
    public required long CPUQuota { get; init; }

    [DataMember(Name = "CpuRealtimePeriod", EmitDefaultValue = false)]
    public required long CPURealtimePeriod { get; init; }

    [DataMember(Name = "CpuRealtimeRuntime", EmitDefaultValue = false)]
    public required long CPURealtimeRuntime { get; init; }

    [DataMember(Name = "CpusetCpus", EmitDefaultValue = false)]
    public required string CpusetCpus { get; init; }

    [DataMember(Name = "CpusetMems", EmitDefaultValue = false)]
    public required string CpusetMems { get; init; }

    [DataMember(Name = "Devices", EmitDefaultValue = false)]
    public required IList<DeviceMappingResponse> Devices { get; init; }

    [DataMember(Name = "DeviceCgroupRules", EmitDefaultValue = false)]
    public required IList<string> DeviceCgroupRules { get; init; }

    [DataMember(Name = "DeviceRequests", EmitDefaultValue = false)]
    public required IList<DeviceRequestResponse> DeviceRequests { get; init; }

    [DataMember(Name = "KernelMemory", EmitDefaultValue = false)]
    public required long KernelMemory { get; init; }

    [DataMember(Name = "KernelMemoryTCP", EmitDefaultValue = false)]
    public required long KernelMemoryTCP { get; init; }

    [DataMember(Name = "MemoryReservation", EmitDefaultValue = false)]
    public required long MemoryReservation { get; init; }

    [DataMember(Name = "MemorySwap", EmitDefaultValue = false)]
    public required long MemorySwap { get; init; }

    [DataMember(Name = "MemorySwappiness", EmitDefaultValue = false)]
    public required long? MemorySwappiness { get; init; }

    [DataMember(Name = "OomKillDisable", EmitDefaultValue = false)]
    public required bool? OomKillDisable { get; init; }

    [DataMember(Name = "PidsLimit", EmitDefaultValue = false)]
    public required long? PidsLimit { get; init; }

    [DataMember(Name = "Ulimits", EmitDefaultValue = false)]
    public required IList<UlimitResponse> Ulimits { get; init; }

    [DataMember(Name = "CpuCount", EmitDefaultValue = false)]
    public required long CPUCount { get; init; }

    [DataMember(Name = "CpuPercent", EmitDefaultValue = false)]
    public required long CPUPercent { get; init; }

    [DataMember(Name = "IOMaximumIOps", EmitDefaultValue = false)]
    public required ulong IOMaximumIOps { get; init; }

    [DataMember(Name = "IOMaximumBandwidth", EmitDefaultValue = false)]
    public required ulong IOMaximumBandwidth { get; init; }

    [DataMember(Name = "Mounts", EmitDefaultValue = false)]
    public required IList<MountResponse> Mounts { get; init; }

    [DataMember(Name = "MaskedPaths", EmitDefaultValue = false)]
    public required IList<string> MaskedPaths { get; init; }

    [DataMember(Name = "ReadonlyPaths", EmitDefaultValue = false)]
    public required IList<string> ReadonlyPaths { get; init; }

    [DataMember(Name = "Init", EmitDefaultValue = false)]
    public required bool? Init { get; init; }
}
