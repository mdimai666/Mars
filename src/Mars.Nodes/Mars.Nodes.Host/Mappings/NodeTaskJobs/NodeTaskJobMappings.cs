using Mars.Host.Shared.Extensions;
using Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;
using Mars.Nodes.Host.Shared.Dto.NodeTasks;

namespace Mars.Nodes.Host.Mappings.NodeTaskJobs;

internal static class NodeTaskJobMappings
{
    public static ListNodeTaskJobQuery ToQuery(this ListNodeTaskJobQueryRequest request)
        => new()
        {
            Skip = request.Skip,
            Take = request.Take,
            Search = request.Search,
            Sort = request.Sort,
        };

    public static ListNodeTaskJobQuery ToQuery(this TableNodeTaskJobQueryRequest request)
        => new()
        {
            Skip = request.ConvertPageAndPageSizeToSkip(),
            Take = request.PageSize,
            Search = request.Search,
            Sort = request.Sort,
        };
}
