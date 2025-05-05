using Docker.DotNet;
using Docker.DotNet.Models;
using Mars.Docker.Host.Shared.Dto;
using Mars.Host.Shared.Dto.Common;
using Mars.Shared.Common;

namespace Mars.Docker.Host.Services;

public interface IDockerService
{
    // Container operations
    Task<ContainerListResponse?> GetContainer(string id, CancellationToken cancellationToken);
    Task<ContainerListResponse?> GetContainerByName(string name, CancellationToken cancellationToken);
    Task<ContainerInspectResponse?> InspectContainer(string id, CancellationToken cancellationToken);
    Task<ListDataResult<ContainerListResponse>> ListContainers(ListContainerQuery query, CancellationToken cancellationToken);
    Task<PagingResult<ContainerListResponse>> ListContainersTable(ListContainerQuery query, CancellationToken cancellationToken);
    Task<CreateContainerResponse> CreateContainer(CreateContainerQuery query, CancellationToken cancellationToken);
    Task<bool> StartContainer(string id, CancellationToken cancellationToken);
    Task<bool> StopContainer(string id, CancellationToken cancellationToken);
    Task RestartContainer(string id, CancellationToken cancellationToken);
    Task PauseContainer(string id, CancellationToken cancellationToken);
    Task UnpauseContainer(string id, CancellationToken cancellationToken);
    Task DeleteContainer(string id, CancellationToken cancellationToken);

    // Image operations
    Task<ImageInspectResponse?> InspectImage(string name, CancellationToken cancellationToken);
    Task<ListDataResult<ImagesListResponse>> ListImages(ListImageQuery query, CancellationToken cancellationToken);
    Task<PagingResult<ImagesListResponse>> ListImagesTable(ListImageQuery query, CancellationToken cancellationToken);
    Task PullImage(string name, string tag, CancellationToken cancellationToken);
    Task DeleteImage(string name, CancellationToken cancellationToken);

    // Volume operations
    Task<VolumeResponse?> InspectVolume(string name, CancellationToken cancellationToken);
    Task<ListDataResult<VolumeResponse>> ListVolumes(ListVolumeQuery query, CancellationToken cancellationToken);
    Task<PagingResult<VolumeResponse>> ListVolumesTable(ListVolumeQuery query, CancellationToken cancellationToken);
    Task<VolumeResponse> CreateVolume(CreateVolumeQuery query, CancellationToken cancellationToken);
    Task DeleteVolume(string name, bool force, CancellationToken cancellationToken);

    // Common
    //Task<ContainerUpdateViewModel> GetContainerUpdateModel(string id, CancellationToken cancellationToken);
    //Task<ContainerUpdateViewModel> GetContainerUpdateModelBlank(CancellationToken cancellationToken);
    //Task<ContainerInspectResponse> UpdateContainer(UpdateContainerQuery query, CancellationToken cancellationToken);
    //CreateContainerQuery EnrichQuery(CreateContainerRequest request);
    //UpdateContainerQuery EnrichQuery(UpdateContainerRequest request);
}

/// <summary>
/// Experimental. Not complete;
/// </summary>
public class DockerService : IDockerService
{
    private readonly DockerClient _dockerClient;

    public DockerService(DockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    // Container operations implementation
    public async Task<ContainerListResponse?> GetContainer(string id, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true, Size = true }, cancellationToken);
        return containers.FirstOrDefault(c => c.ID == id);
    }

    public async Task<ContainerListResponse?> GetContainerByName(string name, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true, Size = true }, cancellationToken);
        return containers.FirstOrDefault(c => c.Names.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    public Task<ContainerInspectResponse?> InspectContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.InspectContainerAsync(id, cancellationToken);
    }

    public async Task<ListDataResult<ContainerListResponse>> ListContainers(ListContainerQuery query, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true, Size = true }, cancellationToken);
        return containers.Where(s => query.Search == null || s.Names.Any(x => x.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
            .AsListDataResult(query);
    }

    public async Task<PagingResult<ContainerListResponse>> ListContainersTable(ListContainerQuery query, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true, Size = true }, cancellationToken);
        return containers.Where(s => query.Search == null || s.Names.Any(x => x.Contains(query.Search, StringComparison.OrdinalIgnoreCase)))
            .AsPagingResult(query);
    }

    public Task<CreateContainerResponse> CreateContainer(CreateContainerQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        //    return _dockerClient.Containers.CreateContainerAsync(
        //        new CreateContainerParameters
        //        {
        //            Name = query.Name,
        //            Platform = query.Platform,
        //            Hostname = query.Hostname,
        //            Domainname = query.Domainname,
        //            User = query.User,
        //            AttachStdin = query.AttachStdin,
        //            AttachStdout = query.AttachStdout,
        //            AttachStderr = query.AttachStderr,
        //            ExposedPorts = query.ExposedPorts,
        //            Tty = query.Tty,
        //            OpenStdin = query.OpenStdin,
        //            StdinOnce = query.StdinOnce,
        //            Env = query.Env,
        //            Cmd = query.Cmd,
        //            //Healthcheck = query.Healthcheck,
        //            ArgsEscaped = query.ArgsEscaped,
        //            Image = query.Image,
        //            Volumes = query.Volumes,
        //            WorkingDir = query.WorkingDir,
        //            Entrypoint = query.Entrypoint,
        //            NetworkDisabled = query.NetworkDisabled,
        //            MacAddress = query.MacAddress,
        //            OnBuild = query.OnBuild,
        //            Labels = query.Labels,
        //            StopSignal = query.StopSignal,
        //            StopTimeout = query.StopTimeout,
        //            Shell = query.Shell,
        //            //HostConfig = query.HostConfig,
        //            //NetworkingConfig = query.NetworkingConfig,
        //        },
        //        cancellationToken);
    }

    public Task<bool> StartContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.StartContainerAsync(id, new ContainerStartParameters(), cancellationToken);
    }

    public Task<bool> StopContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.StopContainerAsync(id, new ContainerStopParameters(), cancellationToken);
    }

    public Task RestartContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.RestartContainerAsync(id, new ContainerRestartParameters(), cancellationToken);
    }

    public Task PauseContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.PauseContainerAsync(id, cancellationToken);
    }
    public Task UnpauseContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.UnpauseContainerAsync(id, cancellationToken);
    }

    public Task DeleteContainer(string id, CancellationToken cancellationToken)
    {
        return _dockerClient.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters(), cancellationToken);
    }

    // Image operations implementation
    public Task<ImageInspectResponse?> InspectImage(string name, CancellationToken cancellationToken)
    {
        return _dockerClient.Images.InspectImageAsync(name, cancellationToken);
    }

    public async Task<ListDataResult<ImagesListResponse>> ListImages(ListImageQuery query, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Images.ListImagesAsync(
            new ImagesListParameters { All = true }, cancellationToken);
        return containers.AsListDataResult(query);
    }

    public async Task<PagingResult<ImagesListResponse>> ListImagesTable(ListImageQuery query, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Images.ListImagesAsync(
            new ImagesListParameters { All = true }, cancellationToken);
        return containers.AsPagingResult(query);
    }

    public async Task PullImage(string name, string tag, CancellationToken cancellationToken)
    {
        await _dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = name, Tag = tag },
            null,
            new Progress<JSONMessage>(),
            cancellationToken);
    }

    public Task DeleteImage(string name, CancellationToken cancellationToken)
    {
        return _dockerClient.Images.DeleteImageAsync(name, new ImageDeleteParameters(), cancellationToken);
    }

    // Volume operations implementation
    public Task<VolumeResponse?> InspectVolume(string name, CancellationToken cancellationToken)
    {
        return _dockerClient.Volumes.InspectAsync(name, cancellationToken);
    }

    public async Task<ListDataResult<VolumeResponse>> ListVolumes(ListVolumeQuery query, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Volumes.ListAsync(
            new VolumesListParameters { }, cancellationToken);
        return containers.Volumes.AsListDataResult(query);
    }

    public async Task<PagingResult<VolumeResponse>> ListVolumesTable(ListVolumeQuery query, CancellationToken cancellationToken)
    {
        var containers = await _dockerClient.Volumes.ListAsync(
            new VolumesListParameters { }, cancellationToken);
        return containers.Volumes.AsPagingResult(query);
    }

    public async Task<VolumeResponse> CreateVolume(CreateVolumeQuery query, CancellationToken cancellationToken)
    {
        return await _dockerClient.Volumes.CreateAsync(
            new VolumesCreateParameters
            {
                Name = query.Name,
                Labels = query.Labels.ToDictionary(),
                Driver = query.Driver,
                DriverOpts = query.DriverOpts.ToDictionary(),
            },
            cancellationToken);
    }

    public Task DeleteVolume(string name, bool force, CancellationToken cancellationToken)
    {
        return _dockerClient.Volumes.RemoveAsync(name, force, cancellationToken);
    }

    // Other methods implementation would go here...
    // ListContainers, ListImages, ListVolumes, etc.
    // EnrichQuery methods, etc.
}
