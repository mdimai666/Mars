using System.Net.Mime;
using System.Web;
using Docker.DotNet.Models;
using Mars.Core.Exceptions;
using Mars.Docker.Contracts;
using Mars.Docker.Host.Mappings;
using Mars.Docker.Host.Services;
using Mars.Docker.Host.Shared.Mapping;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Features;
using Mars.Shared.Common;
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
    public Task<ContainerInspectResponse?> InspectContainer(string id, CancellationToken cancellationToken)
        => _dockerService.InspectContainer(id, cancellationToken) ?? throw new NotFoundException();

    [HttpGet("ListContainers")]
    public async Task<ListDataResult<ContainerListResponse1>> ListContainers([FromQuery] ListContainerRequest query, CancellationToken cancellationToken)
        => (await _dockerService.ListContainers(query.ToQuery(), cancellationToken)).ToResponse();

    [HttpGet("ListTableContainers")]
    public async Task<PagingResult<ContainerListResponse1>> ListContainersTable([FromQuery] ListContainerRequest query, CancellationToken cancellationToken)
        => (await _dockerService.ListContainersTable(query.ToQuery(), cancellationToken)).ToResponse();

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
}
