using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoFixture;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.TemplateEngine;
using Mars.HttpSmartAuthFlow;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements.Nodes.Common;
using Mars.Nodes.Core.Implements.Nodes.Functions;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Host.Factories;
using Mars.Nodes.Host.NodeTasks;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Host.Shared;
using Mars.Nodes.Host.Shared.Dto;
using Mars.Nodes.Host.Shared.Services;
using Mars.Nodes.Implements.Test.NodesForTesting;
using Mars.Nodes.Workspace.Locators;
using Mars.TemplateEngine.Host;
using Mars.TemplateEngine.Host.InternalProviders;
using Mars.TemplateEngine.Providers.HandlebarsProvider;
using Mars.Test.Common.Constants;
using Mars.WebApp.Nodes.Host.Nodes;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

[assembly: CollectionBehavior(DisableTestParallelization = true)] //TODO: Убрать и пофиксить парраллелизацию

namespace Mars.Nodes.Implements.Test.Services;

public class NodeServiceUnitTestBase
{
    internal IFixture _fixture = new Fixture();
    internal readonly IServiceProvider _serviceProvider;
    internal readonly IHubContext<ChatHub> _hub;
    internal readonly BroadcastHub _broadcastHub;
    internal readonly IFileStorage _fileStorage;
    internal readonly IEventManager _eventManager;
    internal readonly ILogger<NodeService> _loggerNodeService;
    internal readonly ILogger<NodeTaskManager> _loggerManager;
    internal readonly ILogger<NodeTaskJob> _loggerJob;
    internal readonly INodesLocator _nodesLocator;
    internal readonly INodeImplementFactory _nodeImplementFactory;
    internal NodeService? _nodeService;
    internal INodeRuntime Runtime;
    internal NodeTaskManager _nodeTaskManager;
    internal JsonSerializerOptions _jsonSerializerOptions;

    public NodeServiceUnitTestBase()
    {
        // minimal setup
        _serviceProvider = Substitute.For<IServiceProvider>();
        MarsLogger.Initialize(Substitute.For<ILoggerFactory>());
        _loggerNodeService = MarsLogger.GetStaticLogger<NodeService>();
        _loggerManager = MarsLogger.GetStaticLogger<NodeTaskManager>();
        _loggerJob = MarsLogger.GetStaticLogger<NodeTaskJob>();
        _serviceProvider.GetService(typeof(ILogger<NodeService>)).Returns(_loggerNodeService);
        _serviceProvider.GetService(typeof(ILogger<NodeTaskManager>)).Returns(_loggerManager);
        _serviceProvider.GetService(typeof(ILogger<NodeTaskJob>)).Returns(_loggerJob);

        // add locators and factory
        _nodesLocator = new NodesLocator();
        _nodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);
        _nodesLocator.RegisterAssembly(typeof(TestCallBackNode).Assembly);
        _nodesLocator.RegisterAssembly(typeof(CssCompilerNode).Assembly);
        _jsonSerializerOptions = _nodesLocator.CreateJsonSerializerOptions();
        _nodeImplementFactory = Substitute.ForPartsOf<NodeImplementFactory>();
        _nodeImplementFactory.RegisterAssembly(typeof(InjectNodeImpl).Assembly);
        _nodeImplementFactory.RegisterAssembly(typeof(TestCallBackNodeImpl).Assembly);
        _nodeImplementFactory.RegisterAssembly(typeof(CssCompilerNodeImplement).Assembly);

        // dependies
        _hub = Substitute.For<IHubContext<ChatHub>>();
        _broadcastHub = Substitute.For<BroadcastHub>(_hub);
        Runtime = Substitute.ForPartsOf<NodeRuntime>(_broadcastHub, _nodeImplementFactory, _serviceProvider);
        _nodeTaskManager = Substitute.ForPartsOf<NodeTaskManager>(Runtime, _loggerManager);
        _serviceProvider.GetService(typeof(INodeRuntime)).Returns(Runtime);
        _serviceProvider.GetService(typeof(BroadcastHub)).Returns(_broadcastHub);
        _serviceProvider.GetService(typeof(IServiceCollection)).Returns(new ServiceCollection());
        _serviceProvider.GetService(typeof(INodeTaskManager)).Returns(_nodeTaskManager);
        _serviceProvider.GetService(typeof(INodeImplementFactory)).Returns(_nodeImplementFactory);
        IRequestContext requestContext = new RequestContextImpl { User = _fixture.Create<RequestContextUser>() };
        _serviceProvider.GetService(typeof(IRequestContext)).Returns(requestContext);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_serviceProvider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        _fileStorage = Substitute.For<IFileStorage>();
        _eventManager = Substitute.For<IEventManager>();
        //_nodeService = Substitute.For<NodeService>(_fileStorage, Runtime, _serviceProvider, (IHubContext<ChatHub>)_hub, _eventManager);

        _nodeService = new NodeService(_fileStorage, Runtime, _serviceProvider, _nodeTaskManager, _nodesLocator, factory: null!, _loggerNodeService, _eventManager);
        //_nodesLocator.Dict.Add(typeof(TestCallBackNode).FullName!, new NodeDictItem { DisplayAttribute = new(), NodeType = typeof(TestCallBackNode) });

        var templateManager = new TemplateManager([new PlainTextTemplateEngine(), new HandlebarsTemplateEngine()]);
        _serviceProvider.GetService(typeof(ITemplateManager)).Returns(templateManager);
        var authClientManager = Substitute.ForPartsOf<AuthClientManager>((IAuthStrategyFactory?)null);
        _serviceProvider.GetService(typeof(AuthClientManager)).Returns(authClientManager);

    }

    public async Task<NodeMsg> ExecuteNodeImplement(INodeImplement node, NodeMsg? msg = null)
    {
        var result = await ExecuteNodeImplementEx(node, msg);
        return result.Msg;
    }

    public async Task<NodeExecutionResult> ExecuteNodeImplementEx(INodeImplement node, NodeMsg? msg = null)
    {
        NodeMsg? resultCatcher = null;
        int outputPort = 0;
        var input = msg ?? new NodeMsg();
        input.Add(new RequestUserInfo
        {
            IsAuthenticated = true,
            UserId = UserConstants.TestUserId,
            UserName = UserConstants.TestUserUsername
        });
        ExecuteAction exa = (msg, output) =>
        {
            resultCatcher = msg;
            outputPort = output;
        };

        await node.Execute(input, exa, new(default, default, 0, default, 0));

        return new NodeExecutionResult(resultCatcher!, outputPort);
    }

    public record NodeExecutionResult(NodeMsg Msg, int OutputPort);

    public async Task<NodeMsg> ExecuteFunctionNode(string code, NodeMsg? msg = null)
    {
        var (flow, result) = await ExecuteFunctionNodeEx(code, msg);
        return result;
    }

    public async Task<(FlowNodeImpl flowNode, NodeMsg msg)> ExecuteFunctionNodeEx(string code, NodeMsg? msg = null)
    {
        var flowNode = new FlowNode();
        var functionSetValueNode = new FunctionNode()
        {
            Container = flowNode.Id,
            Code = code,
        };
        var nodes = new List<Node> { flowNode, functionSetValueNode };
        _nodeService.Deploy(nodes);

        var fn = (FunctionNodeImpl)_nodeService.Nodes[functionSetValueNode.Id];
        var result = await ExecuteNodeImplement(fn, msg);
        var flowNodeImpl = (FlowNodeImpl)_nodeService.Nodes[flowNode.Id];

        return (flowNodeImpl, result);
    }

    public async Task<NodeMsg> ExecuteNode<T>(T node, NodeMsg? msg = null)
        where T : Node
    {
        var result = await ExecuteNodeEx(node, msg);
        return result.Msg;
    }

    public async Task<NodeExecutionResult> ExecuteNodeEx<T>(T node, NodeMsg? msg = null, FlowNode? flowNode = null, VarNode? varNode = null)
        where T : Node
    {
        flowNode ??= new FlowNode();
        node.Container = flowNode.Id;
        var nodes = new List<Node> { flowNode, node };
        if (varNode != null)
        {
            varNode.Container = flowNode.Id;
            nodes.Add(varNode);
        }
        _nodeService.Deploy(nodes);

        var nodeImpl = _nodeService.Nodes[node.Id];
        var result = await ExecuteNodeImplementEx(nodeImpl, msg);

        return result;
    }

    public Task<NodeMsg?> RunUsingTaskManager(Node node, NodeMsg? msg = null)
    {
        return RunUsingTaskManager(NodesWorkflowBuilder.Create().AddNext(node), msg);
    }

    public async Task<NodeMsg?> RunUsingTaskManager(NodesWorkflowBuilder builder, NodeMsg? msg = null)
        => (await RunUsingTaskManagerEx(builder, msg))?.Msg;

    public async Task<NodeExecutionResult?> RunUsingTaskManagerEx(NodesWorkflowBuilder builder, NodeMsg? msg = null)
    {
        NodeMsg? result = null;
        ExecutionParameters? executionParameters = null;
        var callbackNode = new TestCallBackNode() { Callback = (input, param) => { result = input; executionParameters = param; } };
        var nodes = NodesWorkflowBuilder.Create()
                .AddNext(builder)
                .AddNext([callbackNode], catchAllWires: true)
                .BuildWithFlowNode();
        var injectNode = nodes.First(node => node is not FlowNode);
        _nodeService.Deploy(nodes);
        await _nodeTaskManager.CreateJob(_serviceProvider, injectNode.Id, msg, throwOnError: true);
        return executionParameters is null ? null : new(result!, executionParameters.SourceOutputPort);
    }

    private class RequestContextImpl : IRequestContext
    {
        [JsonIgnore]
        [JsonPropertyName("ClaimsOld")]
        public ClaimsPrincipal Claims { get; init; } = default!;

        [JsonPropertyName("Claims")]
        public IReadOnlyCollection<KeyValuePair<string, string>> Claims2 { get; init; } = default!;

        public string Jwt { get; init; } = "jwtString";
        public string UserName { get; init; } = UserConstants.TestUserUsername;
        public bool IsAuthenticated { get; init; } = true;
        public HashSet<string>? Roles { get; init; } = ["Admin"];
        public RequestContextUser? User { get; init; }
    }
}
