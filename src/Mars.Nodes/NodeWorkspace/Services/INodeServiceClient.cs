using Mars.Nodes.Core;
using Mars.Shared.Common;

namespace NodeWorkspace.Services;

public interface INodeServiceClient
{
    Task<UserActionResult> Deploy(IEnumerable<Node> nodes);
    Task<UserActionResult> Inject(string nodeId);
    Task<List<Node>> Load();

}
