using Flurl.Http;
using Mars.Nodes.Core.Models.EntityQuery;
using Mars.Nodes.Front.Shared.Services;

namespace Mars.Nodes.Workspace.Services;

internal class NodeEditorToolServiceClient : INodeEditorToolServiceClient
{
    protected readonly IFlurlClient _client;
    protected string _basePath;
    protected string _controllerName;

    public NodeEditorToolServiceClient(IFlurlClient client)
    {
        _basePath = "/api/";
        _controllerName = "NodeEditorTool";
        _client = client;
    }

    public Task<NodeEntityQueryBuilderDictionary> NodeEntityQueryBuilderDictionary()
        => _client.Request($"{_basePath}{_controllerName}", "NodeEntityQueryBuilderDictionary")
                    .GetJsonAsync<NodeEntityQueryBuilderDictionary>();
}
