using System.Net.Mime;
using Mars.Host.Shared.ExceptionFilters;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
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

    public NodeController(INodeService nodeService, IServiceScopeFactory factory, IServiceProvider serviceProvider)
    {
        _nodeService = nodeService;
        _factory = factory;
        _serviceProvider = serviceProvider;
    }

    [HttpPost(nameof(Deploy))]
    public ActionResult<UserActionResult> Deploy(List<Node> Nodes)
    {
        return _nodeService.Deploy(Nodes);
    }

    [HttpGet(nameof(Load))]
    public IEnumerable<Node> Load()
    {
        return _nodeService.Load().Data;
    }

    [HttpGet(nameof(Inject) + "/{nodeId}")]
    public async Task<ActionResult<UserActionResult>> Inject(string nodeId)
    {
        //using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        //factory.CreateScope();
        //serviceProvider.CreateScope();
        return await _nodeService.Inject(_factory, nodeId);
        //return await nodeService.Inject(serviceProvider, nodeId);
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
            var sc = NodeServiceTemplaryHelper._serviceCollection;

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
}
