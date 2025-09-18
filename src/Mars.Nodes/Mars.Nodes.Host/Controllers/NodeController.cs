using System.Net.Mime;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Dto;
using Mars.Nodes.Core.Dto.NodeTasks;
using Mars.Nodes.Host.Mappings;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;
    private readonly INodeTaskManager _nodeTaskManager;

    public NodeController(INodeService nodeService,
                        IServiceScopeFactory factory,
                        IServiceProvider serviceProvider,
                        IServiceCollection services,
                        INodeTaskManager nodeTaskManager)
    {
        _nodeService = nodeService;
        _factory = factory;
        _serviceProvider = serviceProvider;
        _services = services;
        _nodeTaskManager = nodeTaskManager;
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
        return await _nodeService.Inject(_factory, nodeId);
    }

    [AllowAnonymous]
    [HttpGet(nameof(Test1) + "/{message}")]
    public string Test1(string message)
    {
        return $"TEST OK: {message}";
    }

    int TAKE_COUNT = 10;

    [AllowAnonymous]
    [HttpGet(nameof(FunctionCodeSuggest) + "/{f_action}")]
    public Task<List<KeyValuePair<string, string>>> FunctionCodeSuggest(string f_action, [FromQuery] string? search)
    {
        List<KeyValuePair<string, string>> list = new();

        if (f_action == "di:services")
        {
            var sc = _services;

            Func<ServiceDescriptor, KeyValuePair<string, string>> sget =
                (s) => new(s.ServiceType.Name, $"var {FirstCharToLowerCaseAnrVarName(s.ServiceType.Name)} = RED.GetService<{s.ServiceType.Name}>();");

            list = sc.Where(s => s.ServiceType.FullName.StartsWith("Mars"))
                            .Select(sget)
                            .Where(s => string.IsNullOrEmpty(search) || s.Key.Contains(search, StringComparison.OrdinalIgnoreCase))
                            .Take(TAKE_COUNT)
                            .ToList();
        }
        else if (f_action == $"{nameof(IEventManager)}.dict")
        {
            var eventManager = _serviceProvider.GetRequiredService<IEventManager>();

            list = eventManager.DeclaredEvents()
                            .Where(s => string.IsNullOrEmpty(search) || s.Key.Contains(search, StringComparison.OrdinalIgnoreCase))
                            .Take(TAKE_COUNT)
                            .ToList();
        }

        return Task.FromResult(list);
    }

    public static string? FirstCharToLowerCaseAnrVarName(string? str)
    {
        if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
        {
            if (str[0] == 'I') str = str[1..];
            return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];
        }

        return str;
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
