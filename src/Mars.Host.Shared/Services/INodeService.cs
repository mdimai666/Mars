using Mars.Nodes.Core;
using Mars.Nodes.Core.Models;
using Mars.Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.Services;

public interface INodeService
{
    IReadOnlyDictionary<string, Node> BaseNodes { get; }

    delegate void NodeServiceDeployHandler();
    delegate void NodeServiceVoidHandler();

    event NodeServiceDeployHandler OnDeploy;
    event NodeServiceVoidHandler OnAssignNodes;
    event NodeServiceVoidHandler OnStart;

    bool TryReadFlowFile(out NodesFlowSaveFile? fileData);
    void Setup();
    UserActionResult Deploy(IReadOnlyCollection<Node> nodes);
    IEnumerable<Node> GetNodesForResponse();

    Task<UserActionResult> Inject(IServiceScopeFactory factory, string nodeId, NodeMsg? msg = null);
    Task<UserActionResult> Inject(IServiceProvider serviceProvider, string nodeId, NodeMsg? msg = null);
    Task<UserActionResult<object?>> CallNode(IServiceProvider serviceProvider, string callNodeName, object? payload = null);

    void DebugMsg(string nodeId, DebugMessage msg);
    void DebugMsg(string nodeId, Exception ex);
    void BroadcastStatus(string nodeId, NodeStatus nodeStatus);
}
