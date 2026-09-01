using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Plugin.Abstractions.Dto.Plugins;
using Mars.Plugin.Abstractions.Mappings.Plugins;
using Mars.Plugin.Abstractions.Services;
using Mars.Plugin.Contracts.Plugins;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Plugin.Controllers;

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

    [HttpPost("InstallFromNuget")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<PluginInstallResponse>> InstallFromNuget(
            [FromBody] InstallPluginRequest request,
            CancellationToken cancellationToken)
    {
        var result = await _pluginService.InstallFromNuget(request.PackageId, request.Version, cancellationToken);
        return result.ToResponse();
    }

    [HttpPost("SetEnabled")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> SetEnabled([FromBody] SetPluginEnabledRequest request)
    {
        await _pluginService.SetEnabled(request.PackageId, request.Enabled);
        return Ok();
    }

    [HttpPost("Uninstall")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult> Uninstall([FromBody] UninstallPluginRequest request)
    {
        await _pluginService.Uninstall(request.PackageId);
        return Ok();
    }
}
