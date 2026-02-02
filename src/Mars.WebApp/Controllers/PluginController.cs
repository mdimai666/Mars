using System.Net.Mime;
using Mars.Host.Shared.Dto.Plugins;
using Mars.Host.Shared.Dto.Schedulers;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Mappings.Plugins;
using Mars.Host.Shared.Services;
using Mars.Shared.Common;
using Mars.Shared.Contracts.Plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin, Developer")]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class PluginController : ControllerBase
{
    private readonly IPluginService _pluginService;

    public PluginController(IPluginService pluginService)
    {
        _pluginService = pluginService;
    }

    [HttpGet("list/offset")]
    public ListDataResult<PluginInfoResponse> List([FromQuery] ListPluginQueryRequest request)
    {
        return _pluginService.List(request.ToQuery()).ToResponse();
    }

    [HttpGet("list/page")]
    public PagingResult<PluginInfoResponse> ListTable([FromQuery] TablePluginQueryRequest request)
    {
        return _pluginService.ListTable(request.ToQuery()).ToResponse();
    }

    [AllowAnonymous]
    [HttpGet("RuntimePluginManifests")]
    public IReadOnlyCollection<PluginManifestInfoResponse> RuntimePluginManifests()
    {
        return _pluginService.RuntimePluginManifests().Values.ToResponse();
    }

    [HttpPost("UploadPlugin")]
    [RequestSizeLimit(150_000_000)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]

    public async Task<ActionResult<PluginsUploadOperationResultResponse>> UploadPlugin(
            IFormFileCollection files,
            CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        return (await _pluginService.UploadPlugin(files, cancellationToken)).ToResponse();
    }
}
