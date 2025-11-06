using System.Text.Json;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.Startup;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Implements;
using Mars.Nodes.Core.Implements.Nodes;
using Mars.Nodes.Core.Models;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.Host.Mappings;
using Mars.Shared.Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Mars.Host.Shared.Services.INodeService;
using static Mars.Nodes.Core.Nodes.CallNode;

namespace Mars.Nodes.Host.Services;

internal class NodeService : INodeService, IMarsAppLifetimeService
{
    public static readonly string flowFileName = "flows.json";
    public readonly string flowFilePath = Path.Combine("nodes", flowFileName);
    private readonly IFileStorage _fileStorage;
    protected readonly RED _RED;
    private readonly NodesLocator _nodesLocator;
    protected readonly IServiceProvider _serviceProvider;
    private readonly INodeTaskManager _nodeTaskManager;
    readonly ILogger _logger;

    static JsonSerializerOptions _jsonSerializerOptions = default!;

    public event NodeServiceDeployHandler OnDeploy = default!;
    public event NodeServiceVoidHandler OnAssignNodes = default!;
    public event NodeServiceVoidHandler OnStart = default!;

    public IReadOnlyDictionary<string, INodeImplement> Nodes => _RED.Nodes;
    public IReadOnlyDictionary<string, Node> BaseNodes => _RED.BasicNodesDict;

    public NodeService([FromKeyedServices("data")] IFileStorage fileStorage,
                        RED RED,
                        IServiceProvider serviceProvider,
                        INodeTaskManager nodeTaskManager,
                        NodesLocator nodesLocator,
                        IEventManager eventManager)
    {
        _fileStorage = fileStorage;
        _RED = RED;
        _nodesLocator = nodesLocator;
        _serviceProvider = serviceProvider;
        _nodeTaskManager = nodeTaskManager;
        _logger = MarsLogger.GetStaticLogger<INodeService>();

        _jsonSerializerOptions = NodesLocator.CreateJsonSerializerOptions(_nodesLocator, writeIndented: true);

        var nodesDir = Path.GetDirectoryName(flowFilePath)!;
        if (!_fileStorage.DirectoryExists(nodesDir)) _fileStorage.CreateDirectory(nodesDir);

        eventManager.OnTrigger += EventManager_OnTrigger;
        _nodeTaskManager.OnCurrentTasksCountChanged += _nodeTaskManager_OnCurrentTaskCountChanged;
    }

    void Setup()
    {
        List<Node> nodes;

        if (TryLoadFlowFile(out var fileData))
        {
            nodes = fileData!.Nodes.ToList();
        }
        else
        {
            nodes = GetBlank();
        }

        if (!nodes.Any()) nodes = GetBlank();

        _RED.AssignNodes(ReplaceEmptyStringToDefaultFields(nodes).ToList());
        OnAssignNodes?.Invoke();

        VarNodesSetDefaultValues();
    }

    List<Node> GetBlank()
    {
        return [new FlowNode
        {
            Name = "Flow 0",
        }];
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

    bool TryLoadFlowFile(out NodesFlowSaveFile? fileData)
    {
        try
        {
            if (!_fileStorage.FileExists(flowFilePath))
            {
                fileData = null;
                return false;
            }
            string json = _fileStorage.ReadAllText(flowFilePath);
            var flowsFile = JsonSerializer.Deserialize<NodesFlowSaveFile>(json, _jsonSerializerOptions)!;
            {
                fileData = flowsFile;
                return true;
            }
        }
        catch (Exception ex)
        {
            _RED.Nodes = [];
            _logger.LogError(ex, "LoadFlowFile");
        }
        fileData = null;
        return false;
    }

    public UserActionResult Deploy(List<Node> nodes)
    {
        _RED.AssignNodes(nodes);
        OnAssignNodes?.Invoke();
        SaveToFile();
        OnDeploy?.Invoke();

        return new UserActionResult
        {
            Ok = true,
            Message = "saved: nodes " + nodes.Count
        };
    }

    public void SaveToFile()
    {
        var nodes = ReplaceDefaultFieldsToEmptyString(_RED.BasicNodesDict.Values.ToArray()).ToArray();

        var saveFile = new NodesFlowSaveFile
        {
            Nodes = nodes,
        };

        string json = JsonSerializer.Serialize(saveFile, _jsonSerializerOptions);

        _fileStorage.Write(flowFilePath, json);
    }

    internal IEnumerable<Node> ReplaceDefaultFieldsToEmptyString(IEnumerable<Node> nodes)
    {
        var defaultDict = NodeDefaultInstanceDict();

        return nodes.Select(node =>
        {
            if (node is UnknownNode) return node;
            var copy = node.Copy(_jsonSerializerOptions);
            if (defaultDict.TryGetValue(node.GetType(), out var de))
            {
                if (copy.Color == de.Color) copy.Color = "";
                if (copy.Icon == de.Icon) copy.Icon = "";
            }
            return copy;
        });
    }

    internal IEnumerable<Node> ReplaceEmptyStringToDefaultFields(IEnumerable<Node> nodes)
    {
        var defaultDict = NodeDefaultInstanceDict();

        return nodes.Select(node =>
        {
            if (node is UnknownNode) return node;
            if (defaultDict.TryGetValue(node.GetType(), out var de))
            {
                if (string.IsNullOrEmpty(node.Color)) node.Color = de.Color;
                if (string.IsNullOrEmpty(node.Icon)) node.Icon = de.Icon;
            }
            return node;
        });
    }

    private IDictionary<Type, Node> NodeDefaultInstanceDict()
    {
        return _nodesLocator.RegisteredNodes().Select(type =>
        {
            Node instance = (Node)Activator.CreateInstance(type)!;
            return new KeyValuePair<Type, Node>(type, instance);
        }).ToDictionary();
    }

    public IEnumerable<Node> GetNodesForResponse()
    {
        return Nodes.Values.Select(s => s.Node);
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
        //var logger = serviceProvider.GetRequiredService<ILogger<NodeTaskJob>>();

        var requestUserInfo = serviceProvider.GetRequiredService<IRequestContext>().ToRequestUserInfo();
        msg ??= new();
        msg.Add(requestUserInfo);

        _nodeTaskManager.CreateJob(serviceProvider, nodeId, msg);

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

        var msg = new NodeMsg() { Payload = payload };

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var callback = new CallNodeCallbackAction(payload =>
        {
            tcs.TrySetResult(payload); // завершает задачу
        }, implNode.Id);

        msg.Add(callback);

        // запускаем асинхронный процесс
        _ = Inject(serviceProvider, implNode.Id, msg);

        object? data;

        if (node.Timeout == TimeSpan.Zero)
            data = await tcs.Task;
        else
            data = await tcs.Task.WaitAsync(node.Timeout);

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
            List<EventNodeImpl> activateNodeList = [];

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
        List<Task> nodesTask = [];

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
                    await Inject(scope.ServiceProvider, node.Node.Id, msg);
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
        Setup();
        _ = InjectStartupNodes();
        OnStart?.Invoke();
        return Task.CompletedTask;
    }

    public void DebugMsg(string nodeId, DebugMessage msg) => _RED.DebugMsg(nodeId, msg);
    public void DebugMsg(string nodeId, Exception ex) => _RED.DebugMsg(nodeId, ex);
    public void BroadcastStatus(string nodeId, NodeStatus nodeStatus)
    {
        var node = Nodes.GetValueOrDefault(nodeId)?.Node;
        if (node == null) return;
        node.status = nodeStatus.Text;
        _RED.BroadcastStatus(nodeId, nodeStatus);
    }

    Debouncer _sendTaskCountDebouncer = new(100);

    private void _nodeTaskManager_OnCurrentTaskCountChanged(int currentTaskCount)
    {
        _sendTaskCountDebouncer.Debouce(() =>
        {
            _RED.Hub.Clients.All.SendAsync("NodeRunningTaskCountChanged", currentTaskCount);
        });
    }
}
