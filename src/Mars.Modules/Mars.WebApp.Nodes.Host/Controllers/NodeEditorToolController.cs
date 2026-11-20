using System.Net.Mime;
using Mars.Identity.Contracts.ViewModels;
using Mars.Nodes.Core.Models.EntityQuery;
using Mars.Server.Abstractions.Authorization;
using Mars.Server.Abstractions.ExceptionFilters;
using Mars.WebApp.Nodes.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Mars.WebApp.Nodes.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Cap(RoleCaps.FlowCap.Manage)]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class NodeEditorToolController : ControllerBase
{
    private readonly NodeEditorToolServce _nodeEditorToolServce;

    public NodeEditorToolController(NodeEditorToolServce nodeEditorToolServce)
    {
        _nodeEditorToolServce = nodeEditorToolServce;
    }

    [HttpGet("NodeEntityQueryBuilderDictionary")]
    //[ApiExplorerSettings(IgnoreApi = true)]
    public NodeEntityQueryBuilderDictionary NodeEntityQueryBuilderDictionary()
    {
        return _nodeEditorToolServce.NodeEntityQueryBuilderDictionary();
    }
}
