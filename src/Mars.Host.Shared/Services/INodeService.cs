using Mars.Nodes.Core;
using Mars.Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Host.Shared.Services;

public interface INodeService
{
    //public List<INodeImplement> Nodes { get; } 
    public IEnumerable<Node> BaseNodes { get; }

    public delegate void NodeServiceDeployHandler();

    public event NodeServiceDeployHandler OnDeploy;

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
