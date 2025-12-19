using System.Net.Mime;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Nodes.Core.Models.EntityQuery;
using Mars.Nodes.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mars.Nodes.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
