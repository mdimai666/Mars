using Mars.Host.Shared.Dto.Common;
using Mars.Nodes.Front.Shared.Contracts.NodeTaskJob;
using Mars.Nodes.Host.NodeTasks;
using Mars.Nodes.Host.Shared.Dto.NodeTasks;
using Mars.Shared.Common;

namespace Mars.Nodes.Host.Mappings.NodeTaskJobs;

public static class NodeTaskResponseMappings
{
    public static NodeTaskResultSummaryResponse ToSummaryResponse(this NodeTaskResultSummary entity)
        => new()
        {
            TaskId = entity.TaskId,
            ExecuteCount = entity.ExecuteCount,
            NodesChainCount = entity.NodesChainCount,
            InjectNodeId = entity.InjectNodeId,
            FlowNodeId = entity.FlowNodeId,
            IsDone = entity.IsDone,
            IsTerminated = entity.IsTerminated,
            ErrorCount = entity.ErrorCount,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,

            FlowName = entity.FlowName,
            InjectNodeDisplayName = entity.InjectNodeDisplayName,
        };

    public static NodeTaskResultDetailResponse ToDetailResponse(this NodeTaskResultDetail entity)
        => new()
        {
            TaskId = entity.TaskId,
            ExecuteCount = entity.ExecuteCount,
            NodesChainCount = entity.NodesChainCount,
            InjectNodeId = entity.InjectNodeId,
            FlowNodeId = entity.FlowNodeId,
            IsDone = entity.IsDone,
            IsTerminated = entity.IsTerminated,
            ErrorCount = entity.ErrorCount,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,

            FlowName = entity.FlowName,
            InjectNodeDisplayName = entity.InjectNodeDisplayName,

            Jobs = entity.Jobs.ToResponse(),
        };

    public static NodeJobResponse ToResponse(this NodeJobDto entity)
        => new()
        {
            NodeId = entity.NodeId,
            Executions = entity.Executions.ToResponse(),
        };

    public static IReadOnlyCollection<NodeJobResponse> ToResponse(this IEnumerable<NodeJobDto> entities)
        => entities.Select(ToResponse).ToList();

    public static NodeJobExecutionTimeResponse ToResponse(this NodeJobExecutionTimeDto entity)
        => new()
        {
            JobGuid = entity.JobGuid,
            Start = entity.Start,
            End = entity.End,
            Result = entity.Result.ToResponse(),
            Exception = entity.Exception?.ToResponse(),
        };

    public static NodeJobExecutionProblemDetailResponse ToResponse(this NodeJobExecutionProblemDetailDto entity)
        => new()
        {
            Message = entity.Message,
            StackTrace = entity.StackTrace,
        };

    public static NodeJobExecutionResultResponse ToResponse(this NodeJobExecutionResultDto entity)
        => (NodeJobExecutionResultResponse)entity;

    public static IReadOnlyCollection<NodeJobExecutionTimeResponse> ToResponse(this IEnumerable<NodeJobExecutionTimeDto> entities)
        => entities.Select(ToResponse).ToList();

    public static NodeJobExecutionResultResponse ToResponse(this NodeJobExecutionResult entity)
        => (NodeJobExecutionResultResponse)entity;

    public static IReadOnlyCollection<NodeTaskResultSummaryResponse> ToSummaryResponse(this IEnumerable<NodeTaskResultSummary> entities)
        => entities.Select(ToSummaryResponse).ToList();

    public static IReadOnlyCollection<NodeTaskResultDetailResponse> ToDetailResponse(this IEnumerable<NodeTaskResultDetail> entities)
        => entities.Select(ToDetailResponse).ToList();

    public static ListDataResult<NodeTaskResultSummaryResponse> ToResponse(this ListDataResult<NodeTaskResultSummary> list)
        => list.ToMap((Func<IEnumerable<NodeTaskResultSummary>, IReadOnlyCollection<NodeTaskResultSummaryResponse>>)ToSummaryResponse);

    public static ListDataResult<NodeTaskResultDetailResponse> ToResponse(this ListDataResult<NodeTaskResultDetail> list)
        => list.ToMap((Func<IEnumerable<NodeTaskResultDetail>, IReadOnlyCollection<NodeTaskResultDetailResponse>>)ToDetailResponse);

    public static PagingResult<NodeTaskResultSummaryResponse> ToResponse(this PagingResult<NodeTaskResultSummary> list)
        => list.ToMap((Func<IEnumerable<NodeTaskResultSummary>, IReadOnlyCollection<NodeTaskResultSummaryResponse>>)ToSummaryResponse);

    public static PagingResult<NodeTaskResultDetailResponse> ToResponse(this PagingResult<NodeTaskResultDetail> list)
        => list.ToMap((Func<IEnumerable<NodeTaskResultDetail>, IReadOnlyCollection<NodeTaskResultDetailResponse>>)ToDetailResponse);

}
