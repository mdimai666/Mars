using System.Collections;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Schedulers;
using Mars.Host.Shared.Scheduler;
using Mars.Shared.Common;
using CronExpressionDescriptor;
using Quartz;
using Quartz.Impl.Matchers;

namespace Mars.Scheduler.Host;

internal class SchedulerManager : ISchedulerManager
{
    private readonly ISchedulerFactory _schedulerFactory;

    public SchedulerManager(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public Task AddDailyJob<T>(string jobName, string jobGroup, TimeOnly dailyTime, IDictionary? data = null, bool startNow = false) where T : IJob
    {
        //var d = new CronExpression();
        //var x = new CronTriggerImpl()
        //var cron = CronScheduleBuilder.DailyAtHourAndMinute(dailyTime.Hour, dailyTime.Minute).Build();
        var cron = CronExpressionConverter.DailyAsQuartz(dailyTime);

        return AddJob<T>(jobName, jobGroup, cron, data, startNow);
    }

    public Task AddIntervalJob<T>(string jobName, string jobGroup, TimeSpan jobInterval, IDictionary? data = null, bool startNow = false) where T : IJob
    {
        SimpleScheduleBuilder sb = SimpleScheduleBuilder.Create()
            .WithInterval(jobInterval)
            .RepeatForever();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity($"{jobName}-trigger", jobGroup)
            .StartNow(startNow)
            .WithSchedule(sb)
            .Build();

        return AddJob<T>(jobName, jobGroup, trigger, data);
    }

    string CronDescription(string cronValue)
    {
        return ExpressionDescriptor.GetDescription(cronValue, new CronExpressionDescriptor.Options()
        {
            DayOfWeekStartIndexZero = false,
            Use24HourTimeFormat = true,
            Locale = "ru"
        });
    }

    public Task AddJob<T>(string jobName, string jobGroup, string cronString, IDictionary? data = null, bool startNow = false) where T : IJob
    {
        if (!CronExpression.IsValidExpression(cronString))//is parse human string like
            throw new ArgumentException("cronString is not valid");

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity($"{jobName}-trigger", jobGroup)
            .StartNow(startNow)
            //.WithSimpleSchedule(x => x.WithIntervalInSeconds(5)
            //                          .RepeatForever())
            .WithCronSchedule(cronString)
            .Build();

        return AddJob<T>(jobName, jobGroup, trigger, data);
    }
    public async Task AddJob<T>(string jobName, string jobGroup, ITrigger trigger, IDictionary? data = null) where T : IJob
    {

        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);

        var jobKey = new JobKey(jobName, jobGroup);

        var builder = Quartz.JobBuilder.Create<T>()
            .WithIdentity(jobKey);
        //.StoreDurably()

        if (data is not null)
        {
            builder.SetJobData(new JobDataMap(data));
        }

        IJobDetail job = builder.Build();

        await scheduler.ScheduleJob(job, trigger);
    }

    public async Task DeleteJob(string jobName, string jobGroup)
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);
        var jobKey = new JobKey(jobName, jobGroup);

        await scheduler.DeleteJob(jobKey);
    }

    public async Task DeleteJobGroup(string jobGroup)
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(jobGroup));
        await scheduler.DeleteJobs(jobKeys);
    }

    public async Task Clear()
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);
        await scheduler.Clear();
    }

    public async Task PauseAll()
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);
        await scheduler.PauseAll();
    }

    public async Task ResumeAll()
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);
        await scheduler.ResumeAll();
    }

    public bool IsStarted => _schedulerFactory.GetScheduler().ConfigureAwait(false).GetAwaiter().GetResult().IsStarted;

    public async Task InjectJob(string jobName, string jobGroup)
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);
        var jkey = new JobKey(jobName, jobGroup);
        _ = await scheduler.GetJobDetail(jkey) ?? throw new NotFoundException();
        await scheduler.TriggerJob(jkey);
    }

    public async Task<bool> TryInjectJob(string jobName, string jobGroup)
    {
        try
        {
            await InjectJob(jobName, jobGroup);
            return true;
        }
        catch (NotFoundException)
        {
        }
        return false;
    }

    public async Task PauseJob(string jobName, string jobGroup)
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);
        await scheduler.PauseJob(new JobKey(jobName, jobGroup));
    }

    public async Task<bool> TryPauseJob(string jobName, string jobGroup)
    {
        try
        {
            await PauseJob(jobName, jobGroup);
            return true;
        }
        catch (NotFoundException)
        {
        }
        return false;
    }

    public async Task ResumeJob(string jobName, string jobGroup)
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);
        await scheduler.ResumeJob(new JobKey(jobName, jobGroup));
    }

    public async Task<bool> TryResumeJob(string jobName, string jobGroup)
    {
        try
        {
            await ResumeJob(jobName, jobGroup);
            return true;
        }
        catch (NotFoundException)
        {
        }
        return false;
    }

    SchedulerMaskDto[] TrgiggersAsDto(IReadOnlyCollection<ITrigger> triggers, Dictionary<TriggerKey, TriggerState> stateDict)
    {
        var triggerMap = triggers.Select(trigger => new SchedulerMaskDto
        {
            IsCron = trigger is ICronTrigger,
            Mask = trigger switch
            {
                ICronTrigger cr => cr.CronExpressionString!,
                ISimpleTrigger sim => $"{sim.RepeatInterval} {sim.RepeatCount}",
                _ => trigger.ToString()!
            },
            Description = trigger switch
            {
                ICronTrigger cr => CronDescription(cr.CronExpressionString!),
                ISimpleTrigger sim => $"every {sim.RepeatInterval} {(sim.RepeatCount > 0 ? $"{sim.RepeatCount} times" : "")}",
                _ => trigger.ToString()!
            },
            TriggerState = stateDict.TryGetValue(trigger.Key, out var state) ? state.ToString() : "<0>"
        }).ToArray()!;

        return triggerMap;
    }

    async Task<Dictionary<TriggerKey, TriggerState>> TriggersStates(IReadOnlyCollection<ITrigger> triggers, IScheduler scheduler)
    {
        var statesTask = triggers.Select(async trigger => new
        {
            Key = trigger.Key,
            State = await scheduler.GetTriggerState(trigger.Key)
        });
        var stateDict = (await Task.WhenAll(statesTask)).ToDictionary(s => s.Key, s => s.State);

        return stateDict;
    }

    public async Task<IReadOnlyCollection<SchedulerJobDto>> JobList()
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);

        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());

        var jobsAndTriggers = await Task.WhenAll(jobKeys.Select(async key => new { Job = await scheduler.GetJobDetail(key), Triggers = await scheduler.GetTriggersOfJob(key) }));

        var stateDict = await TriggersStates(jobsAndTriggers.SelectMany(s => s.Triggers).ToArray(), scheduler);

        return jobsAndTriggers.Select(s =>
            new SchedulerJobDto
            {
                Name = s.Job.Key.Name,
                Group = s.Job.Key.Group,
                Triggers = TrgiggersAsDto(s.Triggers, stateDict),
                NextExecutionTime = s.Triggers.Min(trigger => trigger.GetNextFireTimeUtc()),
            }
        ).ToArray();
    }

    public async Task<SchedulerJobDto?> Job(string jobName, string jobGroup)
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);

        var jobKey = new JobKey(jobName, jobGroup);

        var job = await scheduler.GetJobDetail(jobKey);
        if (job is null) return null;

        var triggers = await scheduler.GetTriggersOfJob(jobKey);

        var stateDict = await TriggersStates(triggers, scheduler);

        return new SchedulerJobDto
        {
            Name = job.Key.Name,
            Group = job.Key.Group,
            Triggers = TrgiggersAsDto(triggers, stateDict),
            NextExecutionTime = triggers.Min(trigger => trigger.GetNextFireTimeUtc())
        };
    }

    public async Task<ListDataResult<SchedulerJobDto>> JobList(ListSchedulerJobQuery filter)
    {
        var all = await JobList();
        var list = all.Where(s => string.IsNullOrEmpty(filter.Search) || s.Name.Contains(filter.Search, System.StringComparison.OrdinalIgnoreCase));

        var items = list.Skip(filter.Skip).Take(filter.Take).ToList();

        return new ListDataResult<SchedulerJobDto>(items, items.Count > list.Count(), list.Count());
    }

    public async Task<PagingResult<SchedulerJobDto>> JobListPaging(ListSchedulerJobQuery filter)
    {
        var all = await JobList();
        var list = all.Where(s => string.IsNullOrEmpty(filter.Search) || s.Name.Contains(filter.Search, System.StringComparison.OrdinalIgnoreCase));

        var items = list.Skip(filter.Skip).Take(filter.Take).ToList();

        return new PagingResult<SchedulerJobDto>(items, filter, items.Count > list.Count(), list.Count());
    }

}

internal static class TriggerBuilderExtensions
{
    internal static TriggerBuilder StartNow(this TriggerBuilder builder, bool startNow) => startNow ? builder.StartNow() : builder;
}
