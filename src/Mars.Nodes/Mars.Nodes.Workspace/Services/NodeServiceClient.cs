using Flurl.Http;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Dto;
using Mars.Shared.Common;

namespace Mars.Nodes.Workspace.Services;

internal class NodeServiceClient : INodeServiceClient
{
    protected readonly IFlurlClient _client;
    protected string _basePath;
    protected string _controllerName;

    public NodeServiceClient(IFlurlClient client)
    {
        _basePath = "/api/";
        _controllerName = "Node";
        _client = client;
    }

    public Task<UserActionResult> Deploy(IEnumerable<Node> nodes)
        => _client.Request($"{_basePath}{_controllerName}", "Deploy")
                    .PostJsonAsync(nodes)
                    .ReceiveJson<UserActionResult>();
    public Task<UserActionResult> Inject(string nodeId)
        => _client.Request($"{_basePath}{_controllerName}", "Inject", nodeId)
                    .GetJsonAsync<UserActionResult>();

    public Task<NodesDataDto> Load()
        => _client.Request($"{_basePath}{_controllerName}", "Load")
                    .GetJsonAsync<NodesDataDto>();

    public Task TerminateAllJobs()
        => _client.Request($"{_basePath}{_controllerName}", "Jobs/TerminateAll")
                    .PostAsync();

}
