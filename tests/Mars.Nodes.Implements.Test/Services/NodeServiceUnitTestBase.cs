using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoFixture;
using Mars.Host.Shared.Dto.Users;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Dto;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.NodeTasks;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Implements.Test.NodesForTesting;
using Mars.Test.Common.Constants;
using Mars.WebApp.Nodes.Host.Nodes;
using Mars.WebApp.Nodes.Nodes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Mars.Nodes.Implements.Test.Services;

public class NodeServiceUnitTestBase
{
    internal IFixture _fixture = new Fixture();
    internal readonly IServiceProvider _serviceProvider;
    internal readonly IHubContext<ChatHub> _hub;
    internal readonly IFileStorage _fileStorage;
    internal readonly IEventManager _eventManager;
    internal readonly ILogger<NodeService> _loggerNodeService;
    internal readonly ILogger<NodeTaskManager> _loggerManager;
    internal readonly ILogger<NodeTaskJob> _loggerJob;
    internal readonly NodesLocator _nodesLocator;
    internal NodeService? _nodeService;
    internal RED RED;
    internal NodeTaskManager _nodeTaskManager;
    internal JsonSerializerOptions _jsonSerializerOptions;

    static object _lock = new { };

    public NodeServiceUnitTestBase()
    {
        lock (_lock)
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

            // add locators and fabric
            _nodesLocator = new NodesLocator();
            _nodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);
            _nodesLocator.RegisterAssembly(typeof(TestCallBackNode).Assembly);
            _nodesLocator.RegisterAssembly(typeof(CssCompilerNode).Assembly);
            _jsonSerializerOptions = NodesLocator.CreateJsonSerializerOptions(_nodesLocator);
            var nodeImplementFabirc = new NodeImplementFabirc();
            nodeImplementFabirc.RegisterAssembly(typeof(InjectNodeImpl).Assembly);
            nodeImplementFabirc.RegisterAssembly(typeof(TestCallBackNodeImpl).Assembly);
            nodeImplementFabirc.RegisterAssembly(typeof(CssCompilerNodeImplement).Assembly);

            // dependies
            _hub = Substitute.For<IHubContext<ChatHub>>();
            RED = Substitute.ForPartsOf<RED>(_hub, nodeImplementFabirc, _serviceProvider);
            _nodeTaskManager = Substitute.ForPartsOf<NodeTaskManager>(RED, _loggerManager);
            _serviceProvider.GetService(typeof(RED)).Returns(RED);
            _serviceProvider.GetService(typeof(IServiceCollection)).Returns(new ServiceCollection());
            IRequestContext requestContext = new RequestContextImpl { User = _fixture.Create<RequestContextUser>() };
            _serviceProvider.GetService(typeof(IRequestContext)).Returns(requestContext);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(_serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);
            _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

            _fileStorage = Substitute.For<IFileStorage>();
            _eventManager = Substitute.For<IEventManager>();
            //_nodeService = Substitute.For<NodeService>(_fileStorage, RED, _serviceProvider, (IHubContext<ChatHub>)_hub, _eventManager);

            _nodeService = new NodeService(_fileStorage, RED, _serviceProvider, _nodeTaskManager, _nodesLocator, _loggerNodeService, _eventManager);
            //_nodesLocator.Dict.Add(typeof(TestCallBackNode).FullName!, new NodeDictItem { DisplayAttribute = new(), NodeType = typeof(TestCallBackNode) });
        }
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

        await node.Execute(input, exa, new(default, default));

        return new NodeExecutionResult(resultCatcher!, outputPort);
    }

    public record NodeExecutionResult(NodeMsg Msg, int OutputPort);

    public async Task<NodeMsg> ExecuteFunctionNode(string code)
    {
        var (flow, fn, msg) = await ExecuteFunctionNodeEx(code);
        return msg;
    }

    public async Task<(FlowNodeImpl flowNode, FunctionNodeImpl functionNode, NodeMsg msg)> ExecuteFunctionNodeEx(string code)
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
        var result = await ExecuteNodeImplement(fn);
        var flowNodeImpl = (FlowNodeImpl)_nodeService.Nodes[flowNode.Id];

        return (flowNodeImpl, fn, result);
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

    public Task RunUsingTaskManager<T>(T node, NodeMsg? msg = null, FlowNode? flowNode = null, VarNode? varNode = null)
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

        return _nodeTaskManager.CreateJob(_serviceProvider, node.Id);
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
