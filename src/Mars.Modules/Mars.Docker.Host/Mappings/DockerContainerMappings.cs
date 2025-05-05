using Docker.DotNet.Models;
using Mars.Docker.Contracts;
using Mars.Host.Shared.Dto.Common;
using Mars.Shared.Common;

namespace Mars.Docker.Host.Mappings;

public static class DockerContainerMappings
{
    public static ContainerListResponse1 ToResponse(this ContainerListResponse request)
        => new()
        {
            ID = request.ID,
            Names = request.Names,
            Image = request.Image,
            ImageID = request.ImageID,
            Command = request.Command,
            Created = request.Created,
            Ports = request.Ports.ToResponse(),
            SizeRw = request.SizeRw,
            SizeRootFs = request.SizeRootFs,
            Labels = request.Labels.ToDictionary(),
            State = request.State,
            Status = request.Status,
            NetworkSettings = request.NetworkSettings.ToResponse(),
            Mounts = request.Mounts.ToResponse(),

        };

    public static DockerPortResponse ToResponse(this Port request)
        => new()
        {
            IP = request.IP,
            PrivatePort = request.PrivatePort,
            PublicPort = request.PublicPort,
            Type = request.Type,
        };

    public static SummaryNetworkSettingsResponse ToResponse(this SummaryNetworkSettings request)
        => new()
        {
            Networks = request.Networks.ToDictionary(s => s.Key, s => s.Value.ToResponse()),
        };

    public static EndpointSettingsResponse ToResponse(this EndpointSettings request)
        => new()
        {
            IPAMConfig = request.IPAMConfig?.ToResponse(),
            Links = request.Links,
            Aliases = request.Aliases,
            NetworkID = request.NetworkID,
            EndpointID = request.EndpointID,
            Gateway = request.Gateway,
            IPAddress = request.IPAddress,
            IPPrefixLen = request.IPPrefixLen,
            IPv6Gateway = request.IPv6Gateway,
            GlobalIPv6Address = request.GlobalIPv6Address,
            GlobalIPv6PrefixLen = request.GlobalIPv6PrefixLen,
            MacAddress = request.MacAddress,
            DriverOpts = request.DriverOpts,

        };

    public static EndpointIPAMConfigResponse ToResponse(this EndpointIPAMConfig request)
        => new()
        {
            IPv4Address = request.IPv4Address,
            IPv6Address = request.IPv6Address,
            LinkLocalIPs = request.LinkLocalIPs,
        };

    public static MountPointResponse ToResponse(this MountPoint request)
        => new()
        {
            Type = request.Type,
            Name = request.Name,
            Source = request.Source,
            Destination = request.Destination,
            Driver = request.Driver,
            Mode = request.Mode,
            RW = request.RW,
            Propagation = request.Propagation,
        };


    public static ListDataResult<ContainerListResponse1> ToResponse(this ListDataResult<ContainerListResponse> request)
        => request.ToMap(ToResponse);

    public static PagingResult<ContainerListResponse1> ToResponse(this PagingResult<ContainerListResponse> request)
        => request.ToMap(ToResponse);

    public static IList<DockerPortResponse> ToResponse(this IEnumerable<Port> request)
        => request.Select(ToResponse).ToList();

    public static IList<MountPointResponse> ToResponse(this IEnumerable<MountPoint> request)
        => request.Select(ToResponse).ToList();

    public static ContainerInspectResponse1 ToResponse(this ContainerInspectResponse request)
        => new()
        {
            ID = request.ID,
            Created = request.Created,
            Path = request.Path,
            Args = request.Args,
            State = request.State.ToResponse(),
            Image = request.Image,
            ResolvConfPath = request.ResolvConfPath,
            HostnamePath = request.HostnamePath,
            HostsPath = request.HostsPath,
            LogPath = request.LogPath,
            Node = request.Node.ToResponse(),
            Name = request.Name,
            RestartCount = request.RestartCount,
            Driver = request.Driver,
            Platform = request.Platform,
            MountLabel = request.MountLabel,
            ProcessLabel = request.ProcessLabel,
            AppArmorProfile = request.AppArmorProfile,
            ExecIDs = request.ExecIDs,
            HostConfig = request.HostConfig.ToResponse(),
            GraphDriver = request.GraphDriver.ToResponse(),
            SizeRw = request.SizeRw,
            SizeRootFs = request.SizeRootFs,
            Mounts = request.Mounts.ToResponse(),
            Config = request.Config.ToResponse(),
            NetworkSettings = request.NetworkSettings.ToResponse(),
        };

    public static ContainerStateResponse ToResponse(this ContainerState request)
        => new()
        {
            Status = request.Status,
            Running = request.Running,
            Paused = request.Paused,
            Restarting = request.Restarting,
            OOMKilled = request.OOMKilled,
            Dead = request.Dead,
            Pid = request.Pid,
            ExitCode = request.ExitCode,
            Error = request.Error,
            StartedAt = request.StartedAt,
            FinishedAt = request.FinishedAt,
            Health = request.Health.ToResponse(),
        };

    public static HealthResponse ToResponse(this Health request)
        => new()
        {
            Status = request.Status,
            FailingStreak = request.FailingStreak,
            Log = request.Log.ToResponse(),
        };

    public static HealthcheckResultResponse ToResponse(this HealthcheckResult request)
        => new()
        {
            Start = request.Start,
            End = request.End,
            ExitCode = request.ExitCode,
            Output = request.Output,
        };

    public static IList<HealthcheckResultResponse> ToResponse(this IEnumerable<HealthcheckResult> request)
        => request.Select(ToResponse).ToList();

    public static ContainerNodeResponse ToResponse(this ContainerNode request)
        => new()
        {
            ID = request.ID,
            IPAddress = request.IPAddress,
            Addr = request.Addr,
            Name = request.Name,
            Cpus = request.Cpus,
            Memory = request.Memory,
            Labels = request.Labels,
        };

    public static HostConfigResponse ToResponse(this HostConfig request)
        => new()
        {
            Binds = request.Binds,
            ContainerIDFile = request.ContainerIDFile,
            LogConfig = request.LogConfig.ToResponse(),
            NetworkMode = request.NetworkMode,
            PortBindings = request.PortBindings.ToDictionary(s => s.Key, s => s.Value.ToResponse()),
            RestartPolicy = request.RestartPolicy.ToResponse(),
            AutoRemove = request.AutoRemove,
            VolumeDriver = request.VolumeDriver,
            VolumesFrom = request.VolumesFrom,
            CapAdd = request.CapAdd,
            CapDrop = request.CapDrop,
            CgroupnsMode = request.CgroupnsMode,
            DNS = request.DNS,
            DNSOptions = request.DNSOptions,
            DNSSearch = request.DNSSearch,
            ExtraHosts = request.ExtraHosts,
            GroupAdd = request.GroupAdd,
            IpcMode = request.IpcMode,
            Cgroup = request.Cgroup,
            Links = request.Links,
            OomScoreAdj = request.OomScoreAdj,
            PidMode = request.PidMode,
            Privileged = request.Privileged,
            PublishAllPorts = request.PublishAllPorts,
            ReadonlyRootfs = request.ReadonlyRootfs,
            SecurityOpt = request.SecurityOpt,
            StorageOpt = request.StorageOpt,
            Tmpfs = request.Tmpfs,
            UTSMode = request.UTSMode,
            UsernsMode = request.UsernsMode,
            ShmSize = request.ShmSize,
            Sysctls = request.Sysctls,
            Runtime = request.Runtime,
            ConsoleSize = request.ConsoleSize,
            Isolation = request.Isolation,
            CPUShares = request.CPUShares,
            Memory = request.Memory,
            NanoCPUs = request.NanoCPUs,
            CgroupParent = request.CgroupParent,
            BlkioWeight = request.BlkioWeight,
            BlkioWeightDevice = request.BlkioWeightDevice.ToResponse(),
            BlkioDeviceReadBps = request.BlkioDeviceReadBps.ToResponse(),
            BlkioDeviceWriteBps = request.BlkioDeviceWriteBps.ToResponse(),
            BlkioDeviceReadIOps = request.BlkioDeviceReadIOps.ToResponse(),
            BlkioDeviceWriteIOps = request.BlkioDeviceWriteIOps.ToResponse(),
            CPUPeriod = request.CPUPeriod,
            CPUQuota = request.CPUQuota,
            CPURealtimePeriod = request.CPURealtimePeriod,
            CPURealtimeRuntime = request.CPURealtimeRuntime,
            CpusetCpus = request.CpusetCpus,
            CpusetMems = request.CpusetMems,
            Devices = request.Devices.ToResponse(),
            DeviceCgroupRules = request.DeviceCgroupRules,
            DeviceRequests = request.DeviceRequests.ToResponse(),
            KernelMemory = request.KernelMemory,
            KernelMemoryTCP = request.KernelMemoryTCP,
            MemoryReservation = request.MemoryReservation,
            MemorySwap = request.MemorySwap,
            MemorySwappiness = request.MemorySwappiness,
            OomKillDisable = request.OomKillDisable,
            PidsLimit = request.PidsLimit,
            Ulimits = request.Ulimits.ToResponse(),
            CPUCount = request.CPUCount,
            CPUPercent = request.CPUPercent,
            IOMaximumIOps = request.IOMaximumIOps,
            IOMaximumBandwidth = request.IOMaximumBandwidth,
            Mounts = request.Mounts.ToResponse(),
            MaskedPaths = request.MaskedPaths,
            ReadonlyPaths = request.ReadonlyPaths,
            Init = request.Init,
        };

    public static DriverResponse ToResponse(this Driver request)
        => new()
        {
            Name = request.Name,
            Options = request.Options,
        };

    public static BindOptionsResponse ToResponse(this BindOptions request)
        => new()
        {
            Propagation = request.Propagation,
            NonRecursive = request.NonRecursive,
        };

    public static VolumeOptionsResponse ToResponse(this VolumeOptions request)
        => new()
        {
            NoCopy = request.NoCopy,
            Labels = request.Labels,
            DriverConfig = request.DriverConfig.ToResponse(),
        };

    public static TmpfsOptionsResponse ToResponse(this TmpfsOptions request)
        => new()
        {
            SizeBytes = request.SizeBytes,
            Mode = request.Mode,
        };

    public static MountResponse ToResponse(this Mount request)
        => new()
        {
            Type = request.Type,
            Source = request.Source,
            Target = request.Target,
            ReadOnly = request.ReadOnly,
            Consistency = request.Consistency,
            BindOptions = request.BindOptions.ToResponse(),
            VolumeOptions = request.VolumeOptions.ToResponse(),
            TmpfsOptions = request.TmpfsOptions.ToResponse(),
        };

    public static IList<MountResponse> ToResponse(this IEnumerable<Mount> request)
        => request.Select(ToResponse).ToList();

    public static UlimitResponse ToResponse(this Ulimit request)
        => new()
        {
            Name = request.Name,
            Hard = request.Hard,
            Soft = request.Soft,
        };

    public static IList<UlimitResponse> ToResponse(this IEnumerable<Ulimit> request)
        => request.Select(ToResponse).ToList();

    public static ThrottleDeviceResponse ToResponse(this ThrottleDevice request)
        => new()
        {
            Path = request.Path,
            Rate = request.Rate,
        };

    public static IList<ThrottleDeviceResponse> ToResponse(this IEnumerable<ThrottleDevice> request)
        => request.Select(ToResponse).ToList();

    public static DeviceRequestResponse ToResponse(this DeviceRequest request)
        => new()
        {
            Driver = request.Driver,
            Count = request.Count,
            DeviceIDs = request.DeviceIDs,
            Capabilities = request.Capabilities,
            Options = request.Options,
        };

    public static IList<DeviceRequestResponse> ToResponse(this IEnumerable<DeviceRequest> request)
        => request.Select(ToResponse).ToList();

    public static DeviceMappingResponse ToResponse(this DeviceMapping request)
        => new()
        {
            PathOnHost = request.PathOnHost,
            PathInContainer = request.PathInContainer,
            CgroupPermissions = request.CgroupPermissions,
        };

    public static IList<DeviceMappingResponse> ToResponse(this IEnumerable<DeviceMapping> request)
        => request.Select(ToResponse).ToList();

    public static WeightDeviceResponse ToResponse(this WeightDevice request)
        => new()
        {
            Path = request.Path,
            Weight = request.Weight,
        };
    public static IList<WeightDeviceResponse> ToResponse(this IEnumerable<WeightDevice> request)
        => request.Select(ToResponse).ToList();

    public static LogConfigResponse ToResponse(this LogConfig request)
        => new()
        {
            Type = request.Type,
            Config = request.Config,
        };

    public static DockerPortBindingResponse ToResponse(this PortBinding request)
        => new()
        {
            HostIP = request.HostIP,
            HostPort = request.HostPort,
        };

    public static IList<DockerPortBindingResponse> ToResponse(this IEnumerable<PortBinding> request)
        => request.Select(ToResponse).ToList();

    public static RestartPolicyResponse ToResponse(this RestartPolicy request)
        => new()
        {
            Name = request.Name.ToResponse(),
            MaximumRetryCount = request.MaximumRetryCount,
        };

    public static RestartPolicyKindResponse ToResponse(this RestartPolicyKind request)
        => (RestartPolicyKindResponse)request;

    public static ConfigResponse ToResponse(this Config request)
        => new()
        {
            Hostname = request.Hostname,
            Domainname = request.Domainname,
            User = request.User,
            AttachStdin = request.AttachStdin,
            AttachStdout = request.AttachStdout,
            AttachStderr = request.AttachStderr,
            ExposedPorts = request.ExposedPorts.ToDictionary(s => s.Key, s => s.Value.ToResponse()),
            Tty = request.Tty,
            OpenStdin = request.OpenStdin,
            StdinOnce = request.StdinOnce,
            Env = request.Env,
            Cmd = request.Cmd,
            Healthcheck = request.Healthcheck.ToResponse(),
            ArgsEscaped = request.ArgsEscaped,
            Image = request.Image,
            Volumes = request.Volumes.ToDictionary(s => s.Key, s => s.Value.ToResponse()),
            WorkingDir = request.WorkingDir,
            Entrypoint = request.Entrypoint,
            NetworkDisabled = request.NetworkDisabled,
            MacAddress = request.MacAddress,
            OnBuild = request.OnBuild,
            Labels = request.Labels,
            StopSignal = request.StopSignal,
            StopTimeout = request.StopTimeout,
            Shell = request.Shell,
        };

    public static EmptyStructResponse ToResponse(this EmptyStruct request)
        => new();

    public static HealthConfigResponse ToResponse(this HealthConfig request)
        => new()
        {
            Test = request.Test,
            Interval = request.Interval,
            Timeout = request.Timeout,
            StartPeriod = request.StartPeriod,
            Retries = request.Retries,
        };

    public static GraphDriverDataResponse ToResponse(this GraphDriverData request)
        => new()
        {
            Data = request.Data,
            Name = request.Name,
        };

    public static AddressResponse ToResponse(this Address request)
        => new()
        {
            Addr = request.Addr,
            PrefixLen = request.PrefixLen,
        };

    public static IList<AddressResponse> ToResponse(this IEnumerable<Address> request)
        => request.Select(ToResponse).ToList();

    public static NetworkSettingsResponse ToResponse(this NetworkSettings request)
        => new()
        {
            Bridge = request.Bridge,
            SandboxID = request.SandboxID,
            HairpinMode = request.HairpinMode,
            LinkLocalIPv6Address = request.LinkLocalIPv6Address,
            LinkLocalIPv6PrefixLen = request.LinkLocalIPv6PrefixLen,
            Ports = request.Ports.ToDictionary(s => s.Key, s => s.Value.ToResponse()),
            SandboxKey = request.SandboxKey,
            SecondaryIPAddresses = request.SecondaryIPAddresses.ToResponse(),
            SecondaryIPv6Addresses = request.SecondaryIPv6Addresses.ToResponse(),
            EndpointID = request.EndpointID,
            Gateway = request.Gateway,
            GlobalIPv6Address = request.GlobalIPv6Address,
            GlobalIPv6PrefixLen = request.GlobalIPv6PrefixLen,
            IPAddress = request.IPAddress,
            IPPrefixLen = request.IPPrefixLen,
            IPv6Gateway = request.IPv6Gateway,
            MacAddress = request.MacAddress,
            Networks = request.Networks.ToDictionary(s => s.Key, s => s.Value.ToResponse()),
        };


}

