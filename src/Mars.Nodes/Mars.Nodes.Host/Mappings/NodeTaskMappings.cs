using Mars.Nodes.Core.Dto.NodeTasks;
using Mars.Nodes.Host.NodeTasks;

namespace Mars.Nodes.Host.Mappings;

internal static class NodeTaskMappings
{
    public static NodeTaskResultSummary ToSummary(this NodeTaskJob entity)
        => new()
        {
            TaskId = entity.TaskId,
            ExecuteCount = entity.ExecuteCount,
            NodesChainCount = entity.NodesChainCount,
            IsDone = entity.IsDone,
            IsTerminated = entity.IsTerminated,
            ErrorCount = entity.ErrorCount,
        };

    public static NodeTaskResultDetail ToDetail(this NodeTaskJob entity)
        => new()
        {
            TaskId = entity.TaskId,
            ExecuteCount = entity.ExecuteCount,
            NodesChainCount = entity.NodesChainCount,
            IsDone = entity.IsDone,
            IsTerminated = entity.IsTerminated,
            ErrorCount = entity.ErrorCount,

            Jobs = entity.Jobs.Values.ToDto(),
        };

    public static NodeJobDto ToDto(this NodeJob entity)
        => new()
        {
            NodeId = entity.NodeImplement.Id,
            Executions = entity.Executions.ToDto(),
        };

    public static IReadOnlyCollection<NodeJobDto> ToDto(this IEnumerable<NodeJob> entities)
        => entities.Select(ToDto).ToList();

    public static NodeJobExecutionTimeDto ToDto(this NodeJobExecutionTime entity)
        => new()
        {
            JobGuid = entity.JobGuid,
            Start = entity.Start,
            End = entity.End,
            Result = entity.Result.ToDto(),
            Exception = entity.Exception != null ? new NodeJobExecutionProblemDetailDto(entity.Exception) : null,
        };

    public static IReadOnlyCollection<NodeJobExecutionTimeDto> ToDto(this IEnumerable<NodeJobExecutionTime> entities)
        => entities.Select(ToDto).ToList();

    public static NodeJobExecutionResultDto ToDto(this NodeJobExecutionResult entity)
        => (NodeJobExecutionResultDto)entity;

    public static IReadOnlyCollection<NodeTaskResultSummary> ToSummary(this IEnumerable<NodeTaskJob> entities)
        => entities.Select(ToSummary).ToList();

    public static IReadOnlyCollection<NodeTaskResultDetail> ToDetail(this IEnumerable<NodeTaskJob> entities)
        => entities.Select(ToDetail).ToList();

}
