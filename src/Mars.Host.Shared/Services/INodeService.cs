using Mars.Nodes.Core;
using Mars.Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.Services;

public interface INodeService
{
    public IReadOnlyDictionary<string, Node> BaseNodes { get; }

    public delegate void NodeServiceDeployHandler();
    public delegate void NodeServiceVoidHandler();

    public event NodeServiceDeployHandler OnDeploy;
    public event NodeServiceVoidHandler OnAssignNodes;
    public event NodeServiceVoidHandler OnStart;

    public UserActionResult Deploy(List<Node> nodes);
    public UserActionResult<IEnumerable<Node>> Load();

    public Task<UserActionResult> Inject(IServiceScopeFactory factory, string nodeId, NodeMsg? msg = null);
    public Task<UserActionResult> Inject(IServiceProvider serviceProvider, string nodeId, NodeMsg? msg = null);
    public Task<UserActionResult<object?>> CallNode(IServiceProvider serviceProvider, string callNodeName, object? payload = null);

}

public class NodeServiceTemplaryHelper
{
    public static IServiceCollection _serviceCollection = default!;
}
