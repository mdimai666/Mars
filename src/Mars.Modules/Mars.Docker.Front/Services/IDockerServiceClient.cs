using Mars.Contracts.Common;
using Mars.Docker.Contracts;
using Mars.WebApiClient.Interfaces;

namespace Mars.Docker.Front.Services;

public interface IDockerServiceClient
{
    // Container operations
    Task<ContainerListResponse1?> GetContainer(string id);
    Task<ContainerListResponse1?> GetContainerByName(string name);
    Task<ContainerInspectResponse1?> InspectContainer(string id);
    Task<ListDataResult<ContainerListResponse1>> ListContainers(ListContainerRequest filter);
    Task<PagingResult<ContainerListResponse1>> ListContainersTable(ListContainerRequest filter);
    Task<CreateContainerResponse1> CreateContainer(CreateContainerRequest request);
    Task<bool> StartContainer(string id);
    Task<bool> StopContainer(string id);
    Task RestartContainer(string id);
    Task PauseContainer(string id);
    Task UnpauseContainer(string id);
    Task DeleteContainer(string id);
    Task<DockerRunResultResponse> WaitContainer(string id, int timeoutSeconds);
    Task<DockerRunResultResponse> StartAndCapture(string id, int timeoutSeconds, bool removeAfterExit);
    Task<DockerRunResultResponse?> GetRunResult(string id);
    Task<ContainerLogsResponse1> GetLogs(string id, int tail);

    // Image operations
    Task<ListDataResult<ImageSummaryResponse1>> ListImages(ListImageRequest filter);
    Task PullImage(PullImageRequest request);
    Task DeleteImage(string name);
}

public static class WebApiClientDockerClientExtensions
{
    public static IDockerServiceClient Docker(this IMarsWebApiClient client)
    {
        return new DockerServiceClient(client.Client);
    }
}
