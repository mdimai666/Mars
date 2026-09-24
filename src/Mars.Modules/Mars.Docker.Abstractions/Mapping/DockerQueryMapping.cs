using Mars.Docker.Abstractions.Dto;
using Mars.Docker.Contracts;

namespace Mars.Docker.Abstractions.Mapping;

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
            Image = request.Image,
            Name = request.Name,
            Cmd = request.Cmd,
            Env = request.Env,
            Ports = request.Ports,
            WorkingDir = request.WorkingDir,
            RestartPolicy = request.RestartPolicy,
            AutoRemove = request.AutoRemove,
        };

    public static RunContainerQuery ToQuery(this DockerRunOnceRequest request)
        => new()
        {
            Image = request.Image,
            Cmd = request.Cmd,
            Env = request.Env,
            WorkingDir = request.WorkingDir,
            User = request.User,
            Stdin = request.Stdin,
            TimeoutSeconds = request.TimeoutSeconds,
            KeepContainer = request.KeepContainer,
        };

    public static ExecContainerQuery ToQuery(this DockerExecRequest request)
        => new()
        {
            Cmd = request.Cmd,
            Env = request.Env,
            WorkingDir = request.WorkingDir,
            User = request.User,
            Stdin = request.Stdin,
            TimeoutSeconds = request.TimeoutSeconds,
        };

    public static ListContainerQuery ToQuery(this ListContainerRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListImageQuery ToQuery(this ListImageRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };
}
