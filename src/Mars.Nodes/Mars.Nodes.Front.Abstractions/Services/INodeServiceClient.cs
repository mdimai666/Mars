using Mars.Contracts.Common;
using Mars.Nodes.Contracts.NodeTaskJob;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Contracts.Nodes;

namespace Mars.Nodes.Front.Abstractions.Services;

public interface INodeServiceClient
{
    Task<UserActionResult> Deploy(IEnumerable<Node> nodes);
    Task<UserActionResult> Inject(string nodeId);
    Task<UserActionResult> SetDebugMode(bool enabled);
    Task<NodeDebugSnapshotsResponse> DebugSnapshots(IReadOnlyCollection<string> nodeIds);
    Task<NodeDebugFullResponse> DebugNodeFull(string nodeId, bool includeJson = false);
    Task<NodesDataResponse> Load();
    Task<ListDataResult<NodeTaskResultSummaryResponse>> JobList(ListNodeTaskJobQueryRequest request);
    Task<PagingResult<NodeTaskResultSummaryResponse>> JobListTable(TableNodeTaskJobQueryRequest request);
    Task<NodeTaskResultDetailResponse?> JobDetail(Guid id);
    Task TerminateAllJobs();
    Task<KeyValuePair<string, string>[]> FunctionCodeSuggest(string f_action, string? search = null);
}
