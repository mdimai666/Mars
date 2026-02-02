using System.Net.Mime;
using Mars.Core.Constants;
using Mars.Host.Shared.Dto.Schedulers;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.Schedulers;
using Mars.Host.Shared.Scheduler;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Schedulers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class SchedulerController : ControllerBase
{
    private readonly ISchedulerManager _scheduler;

    public SchedulerController(ISchedulerManager scheduler)
    {
        _scheduler = scheduler;
    }

    [HttpGet("Job/list/offset")]
    public async Task<ListDataResult<SchedulerJobResponse>> JobList([FromQuery] ListSchedulerJobQueryRequest filter)
    {
        return (await _scheduler.JobList(filter.ToQuery())).ToResponse();
    }

    [HttpGet("Job/list/page")]
    public async Task<PagingResult<SchedulerJobResponse>> JobListTable([FromQuery] TableSchedulerJobQueryRequest filter)
    {
        return (await _scheduler.JobListPaging(filter.ToQuery())).ToResponse();
    }

    [HttpPost("PauseAll")]
    [ProducesResponseType(typeof(UserActionResult), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task PauseAll()
    {
        return _scheduler.PauseAll();
    }

    [HttpPost("ResumeAll")]
    [ProducesResponseType(typeof(UserActionResult), StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task ResumeAll()
    {
        return _scheduler.ResumeAll();
    }

    [HttpPost("InjectJob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task InjectJob([FromQuery] string jobName, [FromQuery] string jobGroup)
    {
        return _scheduler.InjectJob(jobName, jobGroup);
    }

    [HttpPost("PauseJob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task PauseJob([FromQuery] string jobName, [FromQuery] string jobGroup)
    {
        return _scheduler.PauseJob(jobName, jobGroup);
    }
    [HttpPost("ResumeJob")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(HttpConstants.UserActionErrorCode466, Type = typeof(UserActionResult))]
    public Task ResumeJob([FromQuery] string jobName, [FromQuery] string jobGroup)
    {
        return _scheduler.ResumeJob(jobName, jobGroup);
    }
}
