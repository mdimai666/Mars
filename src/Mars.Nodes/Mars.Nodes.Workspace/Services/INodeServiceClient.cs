using Mars.Nodes.Core;
using Mars.Shared.Common;

namespace Mars.Nodes.Workspace.Services;

public interface INodeServiceClient
{
    Task<UserActionResult> Deploy(IEnumerable<Node> nodes);
    Task<UserActionResult> Inject(string nodeId);
    Task<List<Node>> Load();

}
