using System.Diagnostics.Metrics;
using System.Net.Mime;
using Mars.Contracts.Common;
using Mars.Core.Exceptions;
using Mars.Nodes.Abstractions.Services;
using Mars.Nodes.Contracts.NodeTaskJob;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Contracts.Nodes;
using Mars.Nodes.Host.Mappings.Nodes;
using Mars.Nodes.Host.Mappings.NodeTaskJobs;
using Mars.Nodes.Host.Services;
using Mars.Server.Abstractions.Constants;
using Mars.Server.Abstractions.ExceptionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.Nodes.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
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
    private readonly INodeDebugStore _debugStore;
    private readonly INodeDebugMode _debugMode;

    private static readonly Meter Meter = new(MetricsConstants.AppName);
    private static readonly Counter<long> InjectCounter =
        Meter.CreateCounter<long>("node_inject_calls", description: "Сколько раз вызывался Inject");

    public NodeController(INodeService nodeService,
                        IServiceScopeFactory factory,
                        INodeTaskManager nodeTaskManager,
                        FunctionCodeSuggestService functionCodeSuggestService,
                        INodeDebugStore debugStore,
                        INodeDebugMode debugMode)
    {
        _nodeService = nodeService;
        _factory = factory;
        _nodeTaskManager = nodeTaskManager;
        _functionCodeSuggestService = functionCodeSuggestService;
        _debugStore = debugStore;
        _debugMode = debugMode;
    }

    [HttpPost(nameof(Deploy))]
    public ActionResult<UserActionResult> Deploy(List<Node> Nodes)
    {
        return _nodeService.Deploy(Nodes);
    }

    [HttpGet(nameof(Load))]
    public NodesDataResponse Load()
    {
        return _nodeService.GetNodesData().ToResponse() with { DebugMode = _debugMode.Enabled };
    }

    [HttpGet(nameof(DebugSnapshots))]
    public NodeDebugSnapshotsResponse DebugSnapshots([FromQuery] string[]? nodeIds)
    {
        return new NodeDebugSnapshotsResponse
        {
            ServerTimeUtc = DateTime.UtcNow,
            DebugMode = _debugMode.Enabled,
            Snapshots = _debugStore.Get(nodeIds ?? []),
        };
    }

    [HttpGet(nameof(DebugNodeFull) + "/{nodeId}")]
    public NodeDebugFullResponse DebugNodeFull(string nodeId, [FromQuery] bool includeJson = false)
    {
        var snapshot = _debugStore.GetFull(nodeId);

        return new NodeDebugFullResponse
        {
            ServerTimeUtc = DateTime.UtcNow,
            CapturedAt = snapshot?.CapturedAt,
            Size = snapshot?.Json.Length ?? 0,
            Truncated = snapshot?.Truncated ?? false,
            Json = includeJson ? snapshot?.Json : null,
        };
    }

    [HttpPost(nameof(SetDebugMode))]
    public UserActionResult SetDebugMode([FromQuery] bool enabled)
    {
        _debugMode.Enabled = enabled;

        return UserActionResult.Success(enabled ? "Debug mode on" : "Debug mode off");
    }

    [HttpGet(nameof(Inject) + "/{nodeId}")]
    public UserActionResult Inject(string nodeId)
    {
        InjectCounter.Add(1, new KeyValuePair<string, object?>("nodeId", nodeId));
        _ = _nodeService.InjectAsync(_factory, nodeId);

        return UserActionResult.Success("Injectend");
    }

    [AllowAnonymous]
    [HttpGet(nameof(FunctionCodeSuggest) + "/{f_action}")]
    public async Task<List<KeyValuePair<string, string>>> FunctionCodeSuggest(string f_action, [FromQuery] string? search)
    {
        return (await _functionCodeSuggestService.FunctionCodeSuggest(f_action, search))
                ?? throw new NotFoundException();
    }

    [HttpGet("Job/list/offset")]
    public ListDataResult<NodeTaskResultSummaryResponse> JobList([FromQuery] ListNodeTaskJobQueryRequest request)
    {
        return _nodeTaskManager.List(request.ToQuery()).ToResponse();
    }

    [HttpGet("Job/list/page")]
    public PagingResult<NodeTaskResultSummaryResponse> JobListTable([FromQuery] TableNodeTaskJobQueryRequest request)
    {
        return _nodeTaskManager.ListTable(request.ToQuery()).ToResponse();
    }

    [HttpGet("Job/Detail/{id:guid}")]
    public NodeTaskResultDetailResponse JobDetail(Guid id)
    {
        return _nodeTaskManager.GetDetail(id)?.ToDetailResponse() ?? throw new NotFoundException();
    }

    [HttpPost("Jobs/TerminateAll")]
    public void TerminateAllJobs()
    {
        _nodeTaskManager.TerminateAllJobs();
    }
}
