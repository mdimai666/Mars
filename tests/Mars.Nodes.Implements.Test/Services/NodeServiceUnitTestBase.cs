using AutoFixture;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Services;
using Mars.Nodes.Implements.Test.NodesForTesting;
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
    private readonly ILogger<NodeTaskManager> _logger;
    internal NodeService? _nodeService;
    internal RED RED;

    static object _lock = new { };

    public NodeServiceUnitTestBase()
    {
        lock (_lock)
        {
            _serviceProvider = Substitute.For<IServiceProvider>();
            MarsLogger.Initialize(Substitute.For<ILoggerFactory>());
            _hub = Substitute.For<IHubContext<ChatHub>>();
            RED = Substitute.ForPartsOf<RED>(_hub, _serviceProvider);
            _serviceProvider.GetService(typeof(RED)).Returns(RED);
            NodeServiceTemplaryHelper._serviceCollection = new ServiceCollection();

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(_serviceProvider);
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);
            _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

            _fileStorage = Substitute.For<IFileStorage>();
            _eventManager = Substitute.For<IEventManager>();
            _logger = MarsLogger.GetStaticLogger<NodeTaskManager>();
            _serviceProvider.GetService(typeof(ILogger<NodeTaskManager>)).Returns(_logger);
            //_nodeService = Substitute.For<NodeService>(_fileStorage, RED, _serviceProvider, (IHubContext<ChatHub>)_hub, _eventManager);

            NodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);
            //NodeImplementFabirc.RegisterAssembly(typeof(InjectNodeImpl).Assembly);

            //NodesLocator.RegisterAssembly(typeof(TestCallBackNode).Assembly);
            NodeImplementFabirc.RegisterAssembly(typeof(TestCallBackNode).Assembly);

            _nodeService = new NodeService(_fileStorage, RED, _serviceProvider, _hub, _eventManager);

            NodesLocator.RefreshDict();
            NodeImplementFabirc.RefreshDict();

            NodesLocator.dict.Add(typeof(TestCallBackNode).FullName!, typeof(TestCallBackNode));
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
        ExecuteAction exa = (msg, output) =>
        {
            resultCatcher = msg;
            outputPort = output;
        };

        await node.Execute(input, exa);

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
}
