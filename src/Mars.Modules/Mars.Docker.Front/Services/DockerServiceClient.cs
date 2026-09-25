using Flurl.Http;
using Mars.Contracts.Common;
using Mars.Docker.Contracts;

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
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<ContainerInspectResponse1?>();

    public Task<ListDataResult<ContainerListResponse1>> ListContainers(ListContainerRequest filter)
        => _client.Request($"{_basePath}{_controllerName}", "ListContainers")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<ContainerListResponse1>>();
    public Task<PagingResult<ContainerListResponse1>> ListContainersTable(ListContainerRequest filter)
        => _client.Request($"{_basePath}{_controllerName}", "ListTableContainers")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<ContainerListResponse1>>();

    public Task<CreateContainerResponse1> CreateContainer(CreateContainerRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "CreateContainer")
                    .PostJsonAsync(request)
                    .ReceiveJson<CreateContainerResponse1>();

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

    public Task<DockerRunResultResponse> WaitContainer(string id, int timeoutSeconds)
        => _client.Request($"{_basePath}{_controllerName}", "Wait", id)
                    .SetQueryParam("timeout", timeoutSeconds)
                    .PostAsync()
                    .ReceiveJson<DockerRunResultResponse>();

    public Task<DockerRunResultResponse> StartAndCapture(string id, int timeoutSeconds, bool removeAfterExit)
        => _client.Request($"{_basePath}{_controllerName}", "StartAndCapture", id)
                    .SetQueryParam("timeout", timeoutSeconds)
                    .SetQueryParam("removeAfterExit", removeAfterExit)
                    .PostAsync()
                    .ReceiveJson<DockerRunResultResponse>();

    public Task<DockerRunResultResponse?> GetRunResult(string id)
        => _client.Request($"{_basePath}{_controllerName}", "RunResult", id)
                    .OnError(OnStatus404ReturnNull)
                    .GetJsonAsync<DockerRunResultResponse?>();

    public Task<ContainerLogsResponse1> GetLogs(string id, int tail)
        => _client.Request($"{_basePath}{_controllerName}", "GetLogs", id)
                    .SetQueryParam("tail", tail)
                    .GetJsonAsync<ContainerLogsResponse1>();

    public Task<ListDataResult<ImageSummaryResponse1>> ListImages(ListImageRequest filter)
        => _client.Request($"{_basePath}{_controllerName}", "ListImages")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<ImageSummaryResponse1>>();

    public Task PullImage(PullImageRequest request)
        => _client.Request($"{_basePath}{_controllerName}", "PullImage")
                    .PostJsonAsync(request);

    public Task DeleteImage(string name)
        => _client.Request($"{_basePath}{_controllerName}", "DeleteImage", name)
                    .DeleteAsync();

    protected static Action<FlurlCall> OnStatus404ReturnNull = call =>
    {
        if (call.Response.StatusCode == (int)System.Net.HttpStatusCode.NotFound)
        {
            call.ExceptionHandled = true;
        }
    };
}
