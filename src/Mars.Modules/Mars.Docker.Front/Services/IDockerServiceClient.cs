using Mars.Docker.Contracts;
using Mars.Shared.Common;
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
    Task<bool> StartContainer(string id);
    Task<bool> StopContainer(string id);
    Task RestartContainer(string id);
    Task PauseContainer(string id);
    Task UnpauseContainer(string id);
    Task DeleteContainer(string id);

}

public static class WebApiClientDatasourceClientExtensions
{
    public static IDockerServiceClient Docker(this IMarsWebApiClient client)
    {
        return new DockerServiceClient(client.Client);
    }
}
