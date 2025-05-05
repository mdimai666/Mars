using Flurl.Http;
using Mars.Docker.Contracts;
using Mars.Shared.Common;

namespace Mars.Docker.Front.Services;

internal class DockerServiceClient : IDockerServiceClient
{
    protected readonly IFlurlClient _client;
    protected string _basePath;
    protected string _controllerName;

    public DockerServiceClient(IFlurlClient client)
    {
        _basePath = "/api/";
        _controllerName = "Docker";
        _client = client;
    }

    public Task<ContainerListResponse1?> GetContainer(string id)
        => _client.Request($"{_basePath}{_controllerName}", "GetContainer", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<ContainerListResponse1?>();
    public Task<ContainerListResponse1?> GetContainerByName(string name)
        => _client.Request($"{_basePath}{_controllerName}", "GetContainerByName", name)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<ContainerListResponse1?>();

    public Task<ContainerInspectResponse1?> InspectContainer(string id)
        => _client.Request($"{_basePath}{_controllerName}", "InspectContainer", id)
                    .PostAsync()
                    .ReceiveJson<ContainerInspectResponse1?>();

    public Task<ListDataResult<ContainerListResponse1>> ListContainers(ListContainerRequest filter)
        => _client.Request($"{_basePath}{_controllerName}", "ListContainers")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<ContainerListResponse1>>();
    public Task<PagingResult<ContainerListResponse1>> ListContainersTable(ListContainerRequest filter)
        => _client.Request($"{_basePath}{_controllerName}", "ListContainersTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<ContainerListResponse1>>();
    public Task<bool> StartContainer(string id)
        => _client.Request($"{_basePath}{_controllerName}", "StartContainer", id)
                    .PostAsync()
                    .ReceiveJson<bool>();

    public Task<bool> StopContainer(string id)
        => _client.Request($"{_basePath}{_controllerName}", "StopContainer", id)
                    .PostAsync()
                    .ReceiveJson<bool>();

    public Task RestartContainer(string id)
        => _client.Request($"{_basePath}{_controllerName}", "RestartContainer", id)
                    .PostAsync();

    public Task PauseContainer(string id)
        => _client.Request($"{_basePath}{_controllerName}", "PauseContainer", id)
                    .PostAsync();

    public Task UnpauseContainer(string id)
        => _client.Request($"{_basePath}{_controllerName}", "UnpauseContainer", id)
                    .PostAsync();

    public Task DeleteContainer(string id)
        => _client.Request($"{_basePath}{_controllerName}", "DeleteContainer", id)
                    .DeleteAsync();

    protected static Action<FlurlCall> OnStatus404ReturnNull = call =>
    {
        if (call.Response.StatusCode == (int)System.Net.HttpStatusCode.NotFound)
        {
            call.ExceptionHandled = true;
        }
    };
}
