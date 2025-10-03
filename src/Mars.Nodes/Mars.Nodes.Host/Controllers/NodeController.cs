using System.Diagnostics.Metrics;
using System.Net.Mime;
using Mars.Host.Shared.Constants;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Dto;
using Mars.Nodes.Core.Dto.NodeTasks;
using Mars.Nodes.Host.Mappings;
using Mars.Nodes.Host.Services;
using Mars.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
[UserActionResultExceptionFilter]
[NotFoundExceptionFilter]
[FluentValidationExceptionFilter]
[AllExceptionCatchToUserActionResultFilter]
public class NodeController : ControllerBase
{
    private readonly INodeService _nodeService;
    private readonly IServiceScopeFactory _factory;
    private readonly INodeTaskManager _nodeTaskManager;
    private readonly FunctionCodeSuggestService _functionCodeSuggestService;

    private static readonly Meter Meter = new(MetricsConstants.AppName);
    private static readonly Counter<long> InjectCounter =
        Meter.CreateCounter<long>("node_inject_calls", description: "Сколько раз вызывался Inject");

    public NodeController(INodeService nodeService,
                        IServiceScopeFactory factory,
                        INodeTaskManager nodeTaskManager,
                        FunctionCodeSuggestService functionCodeSuggestService)
    {
        _nodeService = nodeService;
        _factory = factory;
        _nodeTaskManager = nodeTaskManager;
        _functionCodeSuggestService = functionCodeSuggestService;
    }

    [HttpPost(nameof(Deploy))]
    public ActionResult<UserActionResult> Deploy(List<Node> Nodes)
    {
        return _nodeService.Deploy(Nodes);
    }

    [HttpGet(nameof(Load))]
    public NodesDataDto Load()
    {
        return _nodeService.GetNodesForResponse().ToNodeDataDto();
    }

    [HttpGet(nameof(Inject) + "/{nodeId}")]
    public async Task<ActionResult<UserActionResult>> Inject(string nodeId)
    {
        InjectCounter.Add(1, new KeyValuePair<string, object?>("nodeId", nodeId));
        return await _nodeService.Inject(_factory, nodeId);
    }

    [AllowAnonymous]
    [HttpGet(nameof(FunctionCodeSuggest) + "/{f_action}")]
    public Task<List<KeyValuePair<string, string>>> FunctionCodeSuggest(string f_action, [FromQuery] string? search)
    {
        return _functionCodeSuggestService.FunctionCodeSuggest(f_action, search);
    }

    [HttpGet("JobList/all")]
    public IEnumerable<NodeTaskResultDetail> JobList()
    {
        return _nodeTaskManager.CurrentTasksDetails().Concat(_nodeTaskManager.CompletedTasksDetails());
    }

    [HttpPost("Jobs/TerminateAll")]
    public void TerminateAllJobs()
    {
        _nodeTaskManager.TerminateAllJobs();
    }
}
