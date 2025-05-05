using Mars.Docker.Contracts;
using Mars.Docker.Host.Shared.Dto;

namespace Mars.Docker.Host.Shared.Mapping;

public static class DockerQueryMapping
{
    public static CreateVolumeQuery ToQuery(this VolumesCreateRequest request)
        => new()
        {
            Name = request.Name,
            Labels = request.Labels,
            Driver = request.Driver,
            DriverOpts = request.DriverOpts,
        };

    public static CreateContainerQuery ToQuery(this CreateContainerRequest request)
        => new()
        {
            Name = request.Name,
            Platform = request.Platform,
            Hostname = request.Hostname,
            Domainname = request.Domainname,
            User = request.User,
            AttachStdin = request.AttachStdin,
            AttachStdout = request.AttachStdout,
            AttachStderr = request.AttachStderr,
            ExposedPorts = request.ExposedPorts,
            Tty = request.Tty,
            OpenStdin = request.OpenStdin,
            StdinOnce = request.StdinOnce,
            Env = request.Env,
            Cmd = request.Cmd,
            //Healthcheck = request.Healthcheck,
            ArgsEscaped = request.ArgsEscaped,
            Image = request.Image,
            Volumes = request.Volumes,
            WorkingDir = request.WorkingDir,
            Entrypoint = request.Entrypoint,
            NetworkDisabled = request.NetworkDisabled,
            MacAddress = request.MacAddress,
            OnBuild = request.OnBuild,
            Labels = request.Labels,
            StopSignal = request.StopSignal,
            StopTimeout = request.StopTimeout,
            Shell = request.Shell,
            //HostConfig = request.HostConfig,
            //NetworkingConfig = request.NetworkingConfig,
        };

    public static ListContainerQuery ToQuery(this ListContainerRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

}
