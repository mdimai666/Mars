using System.Collections.Concurrent;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Dto.NodeTasks;
using Mars.Nodes.Host.Mappings;
using Mars.Nodes.Host.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.Host.NodeTasks;

/// <summary>
/// Singletone
/// </summary>
internal class NodeTaskManager : INodeTaskManager
{
    private readonly RED _red;
    private readonly ILogger<NodeTaskManager> _logger;
    private ConcurrentDictionary<Guid, NodeTaskJob> _currentTasks = [];
    private ConcurrentDictionary<Guid, NodeTaskResultDetail> _completedTasks = [];

    public int CurrentTasksCount => _currentTasks.Count;

    public event Action<int>? OnCurrentTasksCountChanged;

    public IReadOnlyCollection<NodeTaskResultSummary> CurrentTasks() => _currentTasks.Values.ToSummary();
    public IReadOnlyCollection<NodeTaskResultDetail> CurrentTasksDetails() => _currentTasks.Values.ToDetail();

    public IReadOnlyCollection<NodeTaskResultSummary> CompletedTasks() => _completedTasks.Values.ToList();
    public IReadOnlyCollection<NodeTaskResultDetail> CompletedTasksDetails() => _completedTasks.Values.ToList();

    public NodeTaskManager(RED red, ILogger<NodeTaskManager> logger)
    {
        _red = red;
        _logger = logger;
    }

    public async Task CreateJob(IServiceProvider serviceProvider, string injectNodeId, NodeMsg? msg = null)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<NodeTaskJob>>();
        var taskJob = new NodeTaskJob(serviceProvider, _red, injectNodeId, logger);

        _currentTasks.TryAdd(taskJob.TaskId, taskJob);
        OnCurrentTasksCountChanged?.Invoke(_currentTasks.Count);

        var tcs = new TaskCompletionSource();
        taskJob.OnComplete += tcs.SetResult;
        taskJob.Run(msg);
        await tcs.Task;

        _currentTasks.TryRemove(taskJob.TaskId, out _);
        _completedTasks.TryAdd(taskJob.TaskId, taskJob.ToDetail());
        OnCurrentTasksCountChanged?.Invoke(_currentTasks.Count);

        await taskJob.DisposeAsync();
    }

    public void TerminateAllJobs()
    {
        _logger.LogTrace("🔴 TerminateAllJobs");
        _red.DebugMsg("", DebugMessage.NodeErrorMessage("", "TerminateAllJobs"));
        foreach (var taskJob in _currentTasks.Values)
        {
            taskJob.Terminate();
            _currentTasks.TryRemove(taskJob.TaskId, out _);
            _completedTasks.TryAdd(taskJob.TaskId, taskJob.ToDetail());
        }
        OnCurrentTasksCountChanged?.Invoke(_currentTasks.Count);
    }

}
