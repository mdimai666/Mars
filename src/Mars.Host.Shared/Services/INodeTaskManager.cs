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

    Task CreateJob(IServiceProvider serviceProvider, string injectNodeId, NodeMsg? msg = null);
    void TryKillTaskJob(Guid taskId);
    void TerminateAllJobs();
}
