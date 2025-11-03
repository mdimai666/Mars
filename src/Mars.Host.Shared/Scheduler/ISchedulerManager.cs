using System.Collections;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Schedulers;
using Mars.Shared.Common;
using Quartz;

namespace Mars.Host.Shared.Scheduler;

/// <summary>
/// Singletone Service
/// </summary>
public interface ISchedulerManager
{
    public Task AddDailyJob<T>(string jobName, string jobGroup, TimeOnly dailyTime, IDictionary? data = null, bool startNow = false) where T : IJob;
    public Task AddIntervalJob<T>(string jobName, string jobGroup, TimeSpan jobInterval, IDictionary? data = null, bool startNow = false) where T : IJob;
    public Task AddJob<T>(string jobName, string jobGroup, string cronString, IDictionary? data = null, bool startNow = false) where T : IJob;
    public Task AddJob<T>(string jobName, string jobGroup, ITrigger trigger, IDictionary? data = null) where T : IJob;
    public Task DeleteJob(string jobName, string jobGroup);
    public Task DeleteJobGroup(string jobGroup);
    public Task PauseAll();
    public Task ResumeAll();
    public bool IsStarted { get; }

    /// <exception cref="NotFoundException"></exception>
    public Task InjectJob(string jobName, string jobGroup);
    public Task<bool> TryInjectJob(string jobName, string jobGroup);

    /// <exception cref="NotFoundException"></exception>
    public Task PauseJob(string jobName, string jobGroup);

    /// <exception cref="NotFoundException"></exception>
    public Task ResumeJob(string jobName, string jobGroup);
    public Task<bool> TryPauseJob(string jobName, string jobGroup);
    public Task<bool> TryResumeJob(string jobName, string jobGroup);
    public Task<IReadOnlyCollection<SchedulerJobDto>> JobList();
    public Task<SchedulerJobDto?> Job(string jobName, string jobGroup);
    Task<ListDataResult<SchedulerJobDto>> JobList(ListSchedulerJobQuery filter);
    Task<PagingResult<SchedulerJobDto>> JobListPaging(ListSchedulerJobQuery filter);

    /// <summary>
    /// Clears (deletes!) all scheduling data - all Quartz.IJobs, Quartz.ITriggers Quartz.ICalendars.
    /// </summary>
    /// <returns></returns>
    Task Clear();
}
