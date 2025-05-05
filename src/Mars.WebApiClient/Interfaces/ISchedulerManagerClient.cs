using Mars.Core.Exceptions;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Schedulers;

namespace Mars.WebApiClient.Interfaces;

public interface ISchedulerManagerClient
{
    Task<ListDataResult<SchedulerJobResponse>> JobList(ListSchedulerJobQueryRequest filter);
    Task<PagingResult<SchedulerJobResponse>> JobListTable(TableSchedulerJobQueryRequest filter);
    Task PauseAll();
    Task ResumeAll();
    /// <exception cref="NotFoundException"></exception>
    Task InjectJob(string jobName, string jobGroup);
    /// <exception cref="NotFoundException"></exception>
    Task PauseJob(string jobName, string jobGroup);
    /// <exception cref="NotFoundException"></exception>
    Task ResumeJob(string jobName, string jobGroup);

}
