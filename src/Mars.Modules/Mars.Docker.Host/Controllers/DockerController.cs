using System.Net.Mime;
using System.Web;
using Docker.DotNet.Models;
using Mars.Contracts.Common;
using Mars.Core.Exceptions;
using Mars.Docker.Abstractions.Mapping;
using Mars.Docker.Contracts;
using Mars.Docker.Host.Mappings;
using Mars.Docker.Host.Services;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.Server.Abstractions.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement.Mvc;

namespace Mars.Docker.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
[FeatureGate(FeatureFlags.DockerAgent)]
public class DockerController : ControllerBase
{
    private readonly IDockerService _dockerService;

    public DockerController(IDockerService dockerService)
    {
        _dockerService = dockerService;
    }

    // Container operations

    [HttpGet("GetContainer/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ContainerListResponse1?> GetContainer(string id, CancellationToken cancellationToken)
        => (await _dockerService.GetContainer(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();

    [HttpGet("GetContainerByName/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ContainerListResponse1?> GetContainerByName(string name, CancellationToken cancellationToken)
        => (await _dockerService.GetContainerByName(HttpUtility.UrlDecode(name), cancellationToken))?.ToResponse() ?? throw new NotFoundException();

    [HttpGet("InspectContainer/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ContainerInspectResponse1?> InspectContainer(string id, CancellationToken cancellationToken)
        => (await _dockerService.InspectContainer(id, cancellationToken))?.ToResponse() ?? throw new NotFoundException();

    [HttpGet("ListContainers")]
    public Task<ListDataResult<ContainerListResponse1>> ListContainers([FromQuery] ListContainerRequest query, CancellationToken cancellationToken)
        => _dockerService.ListContainers(query.ToQuery(), cancellationToken);

    [HttpGet("ListTableContainers")]
    public Task<PagingResult<ContainerListResponse1>> ListContainersTable([FromQuery] ListContainerRequest query, CancellationToken cancellationToken)
        => _dockerService.ListContainersTable(query.ToQuery(), cancellationToken);

    [HttpPost("StartContainer/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<bool> StartContainer(string id, CancellationToken cancellationToken)
        => _dockerService.StartContainer(id, cancellationToken);

    [HttpPost("StopContainer/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<bool> StopContainer(string id, CancellationToken cancellationToken)
        => _dockerService.StopContainer(id, cancellationToken);

    [HttpPost("RestartContainer/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task RestartContainer(string id, CancellationToken cancellationToken)
        => _dockerService.RestartContainer(id, cancellationToken);

    [HttpPost("PauseContainer/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task PauseContainer(string id, CancellationToken cancellationToken)
        => _dockerService.PauseContainer(id, cancellationToken);

    [HttpPost("UnpauseContainer/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task UnpauseContainer(string id, CancellationToken cancellationToken)
        => _dockerService.UnpauseContainer(id, cancellationToken);

    [HttpDelete("DeleteContainer/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteContainer(string id, CancellationToken cancellationToken)
        => _dockerService.DeleteContainer(id, cancellationToken);

    [HttpPost("CreateContainer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    public async Task<CreateContainerResponse1> CreateContainer([FromBody] CreateContainerRequest request, CancellationToken cancellationToken)
    {
        var created = await _dockerService.CreateContainer(request.ToQuery(), cancellationToken);
        return new CreateContainerResponse1
        {
            ID = created.ID,
            Name = string.IsNullOrEmpty(request.Name) ? created.ID : request.Name,
            Warnings = created.Warnings ?? [],
        };
    }

    [HttpPost("RunOnce")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    public Task<DockerRunResultResponse> RunOnce([FromBody] DockerRunOnceRequest request, CancellationToken cancellationToken)
        => _dockerService.RunContainerOnce(request.ToQuery(), cancellationToken);

    [HttpPost("Exec/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DockerRunResultResponse> Exec(string id, [FromBody] DockerExecRequest request, CancellationToken cancellationToken)
        => _dockerService.ExecInContainer(id, request.ToQuery(), cancellationToken);

    [HttpPost("Wait/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DockerRunResultResponse> Wait(string id, [FromQuery] int timeout, CancellationToken cancellationToken)
        => _dockerService.WaitContainer(id, timeout, cancellationToken);

    [HttpPost("StartAndCapture/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DockerRunResultResponse> StartAndCapture(string id, [FromQuery] int timeout, [FromQuery] bool removeAfterExit, CancellationToken cancellationToken)
        => _dockerService.StartAndCapture(id, timeout, removeAfterExit, cancellationToken);

    [HttpGet("RunResult/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<DockerRunResultResponse?> GetRunResult(string id)
        => await _dockerService.GetRunResult(id) ?? throw new NotFoundException();

    [HttpGet("GetLogs/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ContainerLogsResponse1> GetLogs(string id, [FromQuery] int tail, CancellationToken cancellationToken)
        => _dockerService.GetContainerLogs(id, tail, cancellationToken);

    // Image operations

    [HttpGet("ListImages")]
    public async Task<ListDataResult<ImageSummaryResponse1>> ListImages([FromQuery] ListImageRequest query, CancellationToken cancellationToken)
        => (await _dockerService.ListImages(query.ToQuery(), cancellationToken)).ToResponse();

    [HttpGet("ListTableImages")]
    public async Task<PagingResult<ImageSummaryResponse1>> ListImagesTable([FromQuery] ListImageRequest query, CancellationToken cancellationToken)
        => (await _dockerService.ListImagesTable(query.ToQuery(), cancellationToken)).ToResponse();

    [HttpPost("PullImage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    public Task PullImage([FromBody] PullImageRequest request, CancellationToken cancellationToken)
        => _dockerService.PullImage(request.Image, request.Tag, null, cancellationToken);

    [HttpDelete("DeleteImage/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task DeleteImage(string name, CancellationToken cancellationToken)
        => _dockerService.DeleteImage(HttpUtility.UrlDecode(name), cancellationToken);
}
