using Mars.Nodes.Core;
using Mars.Nodes.Core.Dto.NodeTasks;

namespace Mars.Host.Shared.Services;

public interface INodeTaskManager
{
    int CurrentTasksCount { get; }
    public event Action<int>? OnCurrentTasksCountChanged;

    IReadOnlyCollection<NodeTaskResultSummary> CurrentTasks();
    IReadOnlyCollection<NodeTaskResultDetail> CurrentTasksDetails();
    IReadOnlyCollection<NodeTaskResultSummary> CompletedTasks();
    IReadOnlyCollection<NodeTaskResultDetail> CompletedTasksDetails();

    /// <summary>
    /// Create and start job. And return TaskId;
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="injectNodeId"></param>
    /// <param name="msg"></param>
    /// <returns>TaskId</returns>
    Task<Guid> CreateJob(IServiceProvider serviceProvider, string injectNodeId, NodeMsg? msg = null);
    NodeTaskResultSummary? Get(Guid taskId);
    NodeTaskResultDetail? GetDetail(Guid taskId);
    void TryKillTaskJob(Guid taskId);
    void TerminateAllJobs();
}
