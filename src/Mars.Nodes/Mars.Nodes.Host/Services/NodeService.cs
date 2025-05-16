using System.Text.Encodings.Web;
using System.Text.Json;
using Mars.Host.Shared.Hubs;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.WebApp.Implements;
using Mars.Shared.Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Mars.Host.Shared.Services.INodeService;
using static Mars.Nodes.Core.Nodes.CallNode;

namespace Mars.Nodes.Host.Services;

internal class NodeService : INodeService, IMarsAppLifetimeService
{
    public static string flowFileName = "flows.json";
    private readonly IFileStorage _fileStorage;
    protected readonly RED _RED;
    protected readonly IServiceProvider _serviceProvider;

    //private readonly ChatHub callerHub;// => ChatHub.instance;
    private readonly IHubContext<ChatHub> _hub;
    readonly ILogger _logger;

    public event NodeServiceDeployHandler OnDeploy = default!;
    public event NodeServiceVoidHandler OnAssignNodes = default!;
    public event NodeServiceVoidHandler OnStart = default!;

    public IReadOnlyDictionary<string, INodeImplement> Nodes => _RED.Nodes;
    public IEnumerable<Node> BaseNodes => Nodes.Values.Select(s => s.Node);

    public NodeService(IFileStorage fileStorage, RED RED, IServiceProvider serviceProvider, IHubContext<ChatHub> hub, IEventManager eventManager)
    {
        _fileStorage = fileStorage;
        _RED = RED;
        _serviceProvider = serviceProvider;
        _hub = hub;
        _logger = MarsLogger.GetStaticLogger<INodeService>();

        //Console.WriteLine(">>NodeService.Load()");
        //called in NodeWorkspaceExtensions.AddNodeWorkspace
        //NodesLocator.RegisterAssembly(typeof(InjectNode).Assembly);
        //NodesLocator.RegisterAssembly(typeof(CssCompilerNode).Assembly);
        //NodesLocator.RefreshDict();

        NodeImplementFabirc.RegisterAssembly(typeof(INodeImplement<>).Assembly);
        NodeImplementFabirc.RegisterAssembly(typeof(CssCompilerNodeImplement).Assembly);
        NodeImplementFabirc.RefreshDict();

        try
        {
            LoadFlowFile();
        }
        catch (Exception)
        {
            _logger.LogError("error on load flow file");
        }

        //IEventManager eventManager = serviceProvider.GetRequiredService<IEventManager>();

        eventManager.OnTrigger += EventManager_OnTrigger;

    }

    /// <summary>
    /// FirstFlowsAndConfigNodes
    /// </summary>
    /// <param name="nodes"></param>
    /// <returns></returns>
    public static List<Node> OrderNodesForInitialize(List<Node> nodes)
    {
        return [
            ..nodes.Where(node=>node is FlowNode),
            //..nodes.Where(node=>node is ConfigNode),
            ..nodes.Where(node=> node is not FlowNode /*and ConfigNode*/)
        ];
    }

    void LoadFlowFile()
    {
        try
        {

            if (!_fileStorage.FileExists(flowFileName)) return;
            string json = _fileStorage.ReadAllText(flowFileName);
            var nodes = JsonSerializer.Deserialize<List<Node>>(json)!;

            _RED.AssignNodes(nodes);
            OnAssignNodes?.Invoke();
        }
        catch (Exception ex)
        {
            _RED.Nodes = new();
            _logger.LogError(ex, "LoadFlowFile");
        }
    }

    public UserActionResult Deploy(List<Node> nodes)
    {
        _RED.AssignNodes(nodes);
        OnAssignNodes?.Invoke();
        string json = JsonSerializer.Serialize(nodes, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        _fileStorage.Write(flowFileName, json);

        OnDeploy?.Invoke();

        return new UserActionResult
        {
            Ok = true,
            Message = "saved: nodes " + nodes.Count
        };

    }

    public UserActionResult<IEnumerable<Node>> Load()
    {
        return new UserActionResult<IEnumerable<Node>>
        {
            Ok = true,
            Message = "loaded",
            Data = Nodes.Values.Select(s => s.Node)
        };
    }

    public Task<UserActionResult> Inject(IServiceScopeFactory factory, string nodeId, NodeMsg? msg = null)
    {
        var scope = factory.CreateScope();

        return Inject(scope.ServiceProvider, nodeId, msg);//TODO: wait complete

    }

    public Task<UserActionResult> Inject(IServiceProvider serviceProvider, string nodeId, NodeMsg? msg = null)
    {
        //var fs = serviceProvider.GetService<FlowExecutionBackgroundService>();
        //fs.Setup(nodeId, msg);
        //fs.StartAsync(new CancellationToken());
        var logger = serviceProvider.GetRequiredService<ILogger<NodeTaskManager>>();

        NodeTaskManager manager = new NodeTaskManager(serviceProvider, _hub, Nodes, _RED, logger);

        manager.Run(nodeId, msg);//TODO: wait complete

        return Task.FromResult(new UserActionResult
        {
            Ok = true,
            Message = "scope work, nodes="
        });

    }

    public async Task<UserActionResult<object?>> CallNode(IServiceProvider serviceProvider, string callNodeName, object? payload = null)
    {
        if (string.IsNullOrEmpty(callNodeName))
        {
            throw new ArgumentNullException(nameof(callNodeName));
        }

        string typeName = typeof(CallNode).FullName!;
        var implNode = Nodes.Values.FirstOrDefault(s => s.Node.Type == typeName && s.Node.Name == callNodeName);

        if (implNode is null)
        {
            return new UserActionResult<object?>
            {
                Message = $"callNodeName Name = {callNodeName} not found"
            };
        }

        CallNode node = (implNode.Node as CallNode)!;


        NodeMsg msg = new NodeMsg();
        msg.Payload = payload;

        bool isComplete = false;
        object? returnData = null;


        var task = new Task<object?>(() =>
        {
            while (!isComplete) { }
            return returnData;
        });

        CallNodeCallbackAction callback = new CallNodeCallbackAction(payload => { returnData = payload; isComplete = true; }, implNode.Id);
        msg.Add(callback);

        _ = Inject(serviceProvider, implNode.Id, msg);
        task.Start();

        object? data;

        if (node.Timeout == TimeSpan.Zero)
            data = await task;
        else
            data = await task.WaitAsync(node.Timeout);

        return new UserActionResult<object?>
        {
            Ok = true,
            Data = data,
        };

    }

    private void EventManager_OnTrigger(ManagerEventPayload payload)
    {
        var findList = Nodes.Values.Where(node => node is EventNodeImpl).ToList();

        if (findList.Any())
        {
            List<EventNodeImpl> activateNodeList = new();

            foreach (var _node in findList)
            {
                var node = (EventNodeImpl)_node;
                if (node.TestTopic(payload.Topic))
                {
                    activateNodeList.Add(node);
                }
            }

            if (activateNodeList.Any())
            {
                TriggerEventNodes(activateNodeList, payload);
            }
        }
    }

    void TriggerEventNodes(IEnumerable<EventNodeImpl> nodes, ManagerEventPayload payload)
    {
        List<Task> nodesTask = new();

        foreach (var node in nodes)
        {
            nodesTask.Add(new Task(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                try
                {

                    NodeMsg msg = new();
                    msg.Add(payload);
                    //Console.WriteLine("11="+node.Node.Label);
                    await this.Inject(scope.ServiceProvider, node.Node.Id, msg);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    scope.Dispose();
                }
            }));
        }
        nodesTask.ForEach(task => task.Start());
    }

    public async Task InjectStartupNodes()
    {
        await Task.Delay(100);

        var startupNodes = Nodes.Values.Where(node => node is InjectNodeImpl injectNodeImpl && !injectNodeImpl.Node.Disabled && injectNodeImpl.Node.RunAtStartup).Select(node => (InjectNodeImpl)node).ToList();

        var tasks = startupNodes.Select(async node =>
        {
            await Task.Delay(Math.Max(1, node.Node.StartupDelayMillis));

            using var scope = _serviceProvider.CreateScope();

            await Inject(scope.ServiceProvider, node.Node.Id.ToString());

        }).ToArray();

        await Task.WhenAll(tasks);
    }

    internal void VarNodesSetDefaultValues()
    {
        var varNodesImpl = Nodes.Values.Where(node => node.Node is VarNode).Select(s => (VarNodeImpl)s).ToList();
        foreach (var flowGroup in varNodesImpl.GroupBy(s => s.RED.Flow))
        {
            var ppt = VariableSetNodeImpl.CreateInterpreter(flowGroup.Key.RED, new NodeMsg());

            foreach (var nodeImpl in flowGroup)
            {
                var valueExpression = nodeImpl.Node.DefaultValue;
                if (string.IsNullOrEmpty(valueExpression)) continue;
                var calcedValue = ppt.Get.Eval(valueExpression);
                nodeImpl.Node.TrySetValue(calcedValue);
            }
        }
    }

    //public RED_Context CreateContextForNode(string nodeId)
    //{
    //    var node = Nodes[nodeId];
    //    var flow = node is FlowNodeImpl ? node : Nodes[node.Node.Container];
    //    return new RED_Context(nodeId, (FlowNodeImpl)flow, _serviceProvider);
    //}

    //public RED_Context CreateContextForNode(Node node, FlowNodeImpl flow)
    //{
    //    ArgumentNullException.ThrowIfNull(flow, nameof(flow));
    //    if (node is FlowNode && flow.Node.Id != node.Id) throw new ArgumentException("For FlowNode flow must be self");
    //    //var flow = Nodes[node.Container] as FlowNodeImpl;
    //    return new RED_Context(node.Id, flow!, _serviceProvider);
    //}

    [StartupOrder(10)]
    public Task OnStartupAsync()
    {
        VarNodesSetDefaultValues();
        _ = InjectStartupNodes();
        OnStart?.Invoke();
        return Task.CompletedTask;
    }

}
