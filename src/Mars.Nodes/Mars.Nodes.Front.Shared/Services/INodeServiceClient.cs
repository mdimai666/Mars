using Mars.Nodes.Core;
using Mars.Nodes.Front.Shared.Contracts.Nodes;
using Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;
using Mars.Shared.Common;

namespace Mars.Nodes.Front.Shared.Services;

public interface INodeServiceClient
{
    Task<UserActionResult> Deploy(IEnumerable<Node> nodes);
    Task<UserActionResult> Inject(string nodeId);
    Task<NodesDataResponse> Load();
    Task<ListDataResult<NodeTaskResultSummaryResponse>> JobList(ListNodeTaskJobQueryRequest request);
    Task<PagingResult<NodeTaskResultSummaryResponse>> JobListTable(TableNodeTaskJobQueryRequest request);
    Task<NodeTaskResultDetailResponse?> JobDetail(Guid id);
    Task TerminateAllJobs();
}
