using System.Text.Json;
using Flurl.Http;
using Flurl.Http.Configuration;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Front.Shared.Contracts.Nodes;
using Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;
using Mars.Nodes.Front.Shared.Services;
using Mars.Shared.Common;
using Mars.WebApiClient.Implements;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Workspace.Services;

internal class NodeServiceClient : INodeServiceClient
{
    protected readonly IFlurlClient _client;
    protected string _basePath;
    protected string _controllerName;

    protected readonly JsonSerializerOptions _jsonSerializerOptions;

    public NodeServiceClient(IFlurlClient client, [FromKeyedServices(typeof(NodeJsonConverter))] JsonSerializerOptions jsonSerializerOptions)
    {
        _basePath = "/api/";
        _controllerName = "Node";

        _jsonSerializerOptions = jsonSerializerOptions;

        _client = new FlurlClient(client.HttpClient.BaseAddress.AbsoluteUri)
            .WithSettings(settings =>
            {
                settings.JsonSerializer = new CustomFlurlJsonSerializer(_jsonSerializerOptions);
            });
    }

    public Task<UserActionResult> Deploy(IEnumerable<Node> nodes)
    {
        if (nodes.Any(s => s.GetType() == typeof(Node)))
            throw new InvalidOperationException("All nodes must be of derived types.");

        return _client.Request($"{_basePath}{_controllerName}", "Deploy")
                    .PostJsonAsync(nodes)
                    .ReceiveJson<UserActionResult>();
    }

    public Task<UserActionResult> Inject(string nodeId)
        => _client.Request($"{_basePath}{_controllerName}", "Inject", nodeId)
                    .GetJsonAsync<UserActionResult>();

    public Task<NodesDataResponse> Load()
        => _client.Request($"{_basePath}{_controllerName}", "Load")
                    .GetJsonAsync<NodesDataResponse>();

    public Task<ListDataResult<NodeTaskResultSummaryResponse>> JobList(ListNodeTaskJobQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/Job/List")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<NodeTaskResultSummaryResponse>>();

    public Task<PagingResult<NodeTaskResultSummaryResponse>> JobListTable(TableNodeTaskJobQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/Job/ListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<NodeTaskResultSummaryResponse>>();

    public Task<NodeTaskResultDetailResponse?> JobDetail(Guid id)
        => _client.Request($"{_basePath}{_controllerName}/Job/Detail", id)
                    .OnError(BasicServiceClient.OnStatus404ReturnNull)
                    .GetJsonAsync<NodeTaskResultDetailResponse?>();

    public Task TerminateAllJobs()
        => _client.Request($"{_basePath}{_controllerName}", "Jobs/TerminateAll")
                    .PostAsync();

}

public class CustomFlurlJsonSerializer : DefaultJsonSerializer
{
    public CustomFlurlJsonSerializer(JsonSerializerOptions jsonSerializerOptions)
        : base(jsonSerializerOptions)
    {
    }
}
