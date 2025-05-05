using Mars.Shared.Common;
using Mars.Shared.Contracts.Schedulers;
using Mars.WebApiClient.Interfaces;
using Flurl.Http;

namespace Mars.WebApiClient.Implements;

internal class SchedulerManagerClient : BasicServiceClient, ISchedulerManagerClient
{
    public SchedulerManagerClient(IServiceProvider serviceProvider, IFlurlClient flurlClient) : base(serviceProvider, flurlClient)
    {
        _controllerName = "Scheduler";
    }

    public Task InjectJob(string jobName, string jobGroup)
        => _client.Request($"{_basePath}{_controllerName}", "InjectJob")
                    .AppendQueryParam(new { jobName, jobGroup })
                    .OnError(OnStatus404ThrowException)
                    .PostAsync();

    public Task<ListDataResult<SchedulerJobResponse>> JobList(ListSchedulerJobQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/JobList")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<ListDataResult<SchedulerJobResponse>>();
    
    public Task<PagingResult<SchedulerJobResponse>> JobListTable(TableSchedulerJobQueryRequest filter)
        => _client.Request($"{_basePath}{_controllerName}/JobListTable")
                    .AppendQueryParam(filter)
                    .GetJsonAsync<PagingResult<SchedulerJobResponse>>();

    public Task PauseAll()
        => _client.Request($"{_basePath}{_controllerName}", "PauseAll")
                    .PostAsync();

    public Task PauseJob(string jobName, string jobGroup)
        => _client.Request($"{_basePath}{_controllerName}", "PauseJob")
                    .AppendQueryParam(new { jobName, jobGroup })
                    .PostAsync();

    public Task ResumeAll()
        => _client.Request($"{_basePath}{_controllerName}", "ResumeAll")
                    .PostAsync();

    public Task ResumeJob(string jobName, string jobGroup)
        => _client.Request($"{_basePath}{_controllerName}", "ResumeJob")
                    .AppendQueryParam(new { jobName, jobGroup })
                    .PostAsync();
}
