using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Web;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.FormEditor;
using Mars.Nodes.Workspace.ActionManager;
using Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;
using Mars.Nodes.Workspace.Components;
using Mars.Nodes.Workspace.EditorParts;
using Mars.Nodes.Workspace.Models;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Toolbelt.Blazor.HotKeys2;

namespace Mars.Nodes.Workspace;

public partial class NodeEditor1 : ComponentBase, IAsyncDisposable, INodeEditorApi
{
    internal static NodeEditor1? Instance { get; private set; }

    [Inject] IServiceProvider _serviceProvider { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] IJSRuntime JS { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] ILoggerFactory _loggerFactory { get; set; } = default!;
    [Inject] ILogger<NodeEditor1> _logger { get; set; } = default!;
    [Inject] NodesLocator _nodesLocator { get; set; } = default!;
    [Inject] EditorActionLocator _edittorActionLocator { get; set; } = default!;
    [Inject(Key = typeof(NodeJsonConverter))] JsonSerializerOptions _jsonSerializerOptions { get; set; } = default!;

    [Inject] HotKeys HotKeys { get; set; } = default!;
    HotKeysContext _hotKeysContext = default!;
    bool _hotkeysPrevSetState = true;

    [Parameter]
    public IDictionary<string, Node> AllNodes
    {
        get => _allNodes;
        set
        {
            if (value == _allNodes) return;
            _allNodes = (value as Dictionary<string, Node>)!;
            CalcTabs();
            CalcVarNodes();
            CheckActiveTab();
            AllNodesChanged.InvokeAsync(_allNodes);
        }
    }

    [Parameter] public EventCallback<IDictionary<string, Node>> AllNodesChanged { get; set; }
    [Parameter] public EventCallback<string> OnInject { get; set; }
    [Parameter] public EventCallback<IEnumerable<Node>> OnDeploy { get; set; }
    [Parameter] public EventCallback<string> OnCmdClick { get; set; }
    [Parameter] public RenderFragment? SectionActions { get; set; } = null;

    public JsonSerializerOptions NodesJsonSerializerOptions => _jsonSerializerOptions;

    NodeWorkspaceJsInterop js = default!;
    Dictionary<string, Node> _allNodes = [];
    string runningTaskCountDisplayText = "-";

    #region PALETTE
    List<PaletteNode> _palette = [];
    public IReadOnlyCollection<PaletteNode> Palette => _palette;
    IReadOnlyCollection<Node> INodeEditorApi.Palette => Palette.Select(s => s.Instance).ToList();

    public IReadOnlyCollection<Type> RegisteredNodes { get; private set; } = [];
    #endregion

    #region FLOWS
    List<FlowNode> flows = [];

    FlowNode? _activeFlow;
    public FlowNode? ActiveFlow => _activeFlow;

    public IReadOnlyDictionary<string, Node> FlowNodes => GetFlowNodes(_activeFlow?.Id);
    //[Parameter, SupplyParameterFromQuery(Name = "flow")] supplu not work in non route components
    //public string InitialFlowId { get; set; } 
    #endregion

    EditorActionManager _actionManager = default!;
    public IEditorActionManager ActionManager => _actionManager;

    NodeWorkspace1? _nodeWorkspace1 = default!;
    public INodeWorkspaceApi NodeWorkspace => _nodeWorkspace1!;

    Node? EditNode { get; set; }

    QuickNodeAddMenu? quickNodeAddMenu = default!;
    NodeEditContainer1? nodeEditContainer1 = default!;

    bool _showWorkspaceContextMenu;
    FluentMenu _workspaceContextMenu = default!;

    bool _showPaletteNodeContextMenu;
    FluentMenu _paletteNodeContextMenu = default!;

    #region MenuTabs
    string activeMasterTab = "tabs";

    class MasterTab
    {
        public string label;
        public string key;

        public MasterTab(string key, string label)
        {
            this.key = key;
            this.label = label;
        }

    }

    MasterTab[] masterTabs = [new("tabs", "Tabs"), new("nodes", "Nodes"), new("configs", "Configs")];
    #endregion

    IReadOnlyCollection<NodeExampleInfo> _examplesList = [];
    IReadOnlyCollection<NodeExampleInfo> _currentPaletteNodeExamples = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Instance = this;

        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.S, SaveFormClick, "Save Form");
        js = new(JS);
        _ = js.InitModule();

        _actionManager = new EditorActionManager(this, _serviceProvider, _hotKeysContext, _edittorActionLocator);
        _actionManager.PropertyChanged += (_, __) => InvokeAsync(OnChildComponentPropertyChangedRepaint);

        RegisteredNodes = _nodesLocator.RegisteredNodes();

        Type[] topNodes = { typeof(InjectNode), typeof(DebugNode), typeof(FunctionNode), typeof(TemplateNode) };

        var paletteNodesList = topNodes.Concat(
            RegisteredNodes
                .Where(s => Node.IsVisualNode(s) && s != typeof(UnknownNode))
                .Where(s => !topNodes.Contains(s)));

        foreach (var type in paletteNodesList)
        {
            object handle = Activator.CreateInstance(type)!;
            Node node = (Node)handle;
            node.X = 10;
            var displayAttr = type.GetCustomAttribute<DisplayAttribute>();
            var item = new PaletteNode
            {
                Instance = node,
                DisplayName = node.Label,
                GroupName = displayAttr?.GroupName ?? "other"
            };
            _palette.Add(item);

        }

        _examplesList = _nodesLocator.CreateExamplesList();
    }

    public async ValueTask DisposeAsync()
    {
        Instance = null;
        if (_hotKeysContext is not null)
            await _hotKeysContext.DisposeAsync();
        _actionManager.PropertyChanged -= (_, __) => InvokeAsync(OnChildComponentPropertyChangedRepaint);
    }

    void OnChildComponentPropertyChangedRepaint() => StateHasChanged();

    async void OpenOffcanvasEditor()
    {
        await js.ShowOffcanvas("node-editor-offcanvas", true);
    }

    public void CallStateHasChanged()
    {
        StateHasChanged();
    }

    void OnMouseDownPaletteNode(MouseEventArgs e, Node paletteNode)
    {
        if (e.Button != (long)MouseButton.Left) return;

        if (_showPaletteNodeContextMenu) _showPaletteNodeContextMenu = false;

        CreateNewNodeFromPalette(e, paletteNode);
    }

    void CreateNewNodeFromPalette(MouseEventArgs e, Node paletteNode)
    {
        Node instance = (Node)Activator.CreateInstance(paletteNode.GetType())!;
        instance.Container = _activeFlow.Id;
        AllNodes.Add(instance);
        CalcFlowNodes();
        _nodeWorkspace1?.OnClickPaletteNewNode(e, paletteNode, instance);
    }

    ConfigNode CreateConfigNodeFromType(Type nodeType)
    {
        ConfigNode instance = (ConfigNode)Activator.CreateInstance(nodeType)!;
        var thisTypeCount = AllNodes.Values.Count(s => s.Type == instance.Type);
        instance.Container = _activeFlow.Id;
        instance.Name = instance.Label + (thisTypeCount + 1);
        AllNodes.Add(instance);
        RecalcNodes();
        return instance;
    }

    #region DEBUGGER
    const string noderedDebugMessageList = "#nodered-debug-message-list";

    List<DebugMessage> messages = [new()];

    public void AddDebugMessage(DebugMessage msg)
    {
        messages.Add(msg);
        StateHasChanged();
        _ = js.ScrollDownElement(noderedDebugMessageList);
    }

    void AddDebugMessageTest()
    {
        messages.Add(DebugMessage.Test());
        messages.Add(DebugMessage.Test());
        messages.Add(DebugMessage.Test());
        messages.Add(DebugMessage.Test());
        _ = js.ScrollDownElement(noderedDebugMessageList);
    }

    internal void ClearDebugMessages()
    {
        messages.Clear();
    }
    #endregion

    public void SaveFormClick()
    {
        _logger.LogTrace("SaveFormClick");
        _ = nodeEditContainer1.FormSaveClick();
    }

    public void DeployClick()
    {
        OnDeploy.InvokeAsync(AllNodes.Values);
    }

    void OnClickNode(NodeClickEventArgs e)
    {
        if (_showWorkspaceContextMenu) _showWorkspaceContextMenu = false;
        if (_showPaletteNodeContextMenu) _showPaletteNodeContextMenu = false;
    }

    void OnDblClickNode(NodeClickEventArgs e)
    {
        StartEditNode(e.Node);
    }

    public void StartEditNode(Node node)
    {
        EnableHotkeys(false);
        EditNode = node;
        nodeEditContainer1.StartEditNode(EditNode);
    }

    public void StartCreateNewConfigNode(AppendNewConfigNodeEvent appendNewConfigNodeEvent)
    {
        Type configNodeType = appendNewConfigNodeEvent.ConfigNodeType;

        var found = RegisteredNodes.FirstOrDefault(s => s == configNodeType);
        if (found == null)
        {
            _ = _messageService.Error($"Error newConfigNode command: type:{configNodeType.Name} not found");
            return;
        }
        var instance = CreateConfigNodeFromType(found);
        Task.Run(async () =>
        {
            await Task.Delay(10);
            appendNewConfigNodeEvent.ConfigNodeSetter(instance);
            nodeEditContainer1.StartEditNode(instance);
            StateHasChanged();
        });
    }

    void OnClickEditConfigNode(string id)
    {
        var node = AllNodes.GetValueOrDefault(id);
        if (node == null)
        {
            _ = _messageService.Error($"Error editConfigNode command: id:{id} not found");
            return;
        }
        nodeEditContainer1.StartEditNode(node);
    }

    void OnClickNewConfigNode(AppendNewConfigNodeEvent e)
    {
        StartCreateNewConfigNode(e);
    }

    void OnEditFormSaveNodeClick(Node node)
    {
        var editNodeAction = new EditNodeAction(this, AllNodes[node.Id], node);
        _actionManager.ExecuteAction(editNodeAction);
    }

    public void SaveNode(Node node, bool changed = true)
    {
        _logger.LogTrace("SaveNode(Node node)");
        EditNode = node;
        AllNodes[node.Id] = EditNode;
        EditNode.changed = changed;

        AllNodesChanged.InvokeAsync(AllNodes);

        if (node is FlowNode flow)
        {
            ChangeFlow(flow);
        }
        else
        {
            CalcFlowNodes();
        }
        StateHasChanged();
    }

    void DeleteNode(string nodeId)
    {
        if (_allNodes[nodeId] is FlowNode)
            _actionManager.ExecuteAction(new DeleteFlowNodeAction(this, nodeId));
        else
            _actionManager.ExecuteAction(new DeleteNodesAndWiresAction(this, [_allNodes[nodeId]]));
    }

    void OnWorkspaceClick(MouseEventArgs e)
    {
        if (_showWorkspaceContextMenu) _showWorkspaceContextMenu = false;
        if (_showPaletteNodeContextMenu) _showPaletteNodeContextMenu = false;

        if (quickNodeAddMenu.Visible)
        {
            quickNodeAddMenu.Hide();
        }
    }

    async void OnWorkspaceDblClick(MouseEventArgs e)
    {
        quickNodeAddMenu.Show(e);
        await Task.Delay(100);
        quickNodeAddMenu.Focus();
    }

    void CalcTabs()
    {
        flows = AllNodes.Values.Where(node => node is FlowNode).Select(s => s as FlowNode).OrderBy(s => s.Order).ToList()!;
        if (flows.Count == 0)
        {
            _actionManager.ExecuteAction<CreateFlowNodeAction>(addToHistory: false);
        }
    }

    void CheckActiveTab()
    {
        if (AllNodes is not null && _activeFlow is null)
        {
            var querystring = HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);

            string? flow = querystring["flow"];

            if (string.IsNullOrEmpty(flow) == false)//NOT WORK
            {
                _activeFlow = flows.FirstOrDefault(s => s.Id == flow);
            }

            _activeFlow ??= flows.OrderBy(s => s.Order).FirstOrDefault();

            CalcFlowNodes();
        }
    }

    public void ChangeFlow(FlowNode flowNode)
    {
        _activeFlow = flowNode;
        CalcFlowNodes();
        CalcVarNodes();
        var url = NavigationManager.GetUriWithQueryParameter("flow", flowNode.Id);
        NavigationManager.NavigateTo(url);
    }

    public IReadOnlyDictionary<string, Node> GetFlowNodes(string? flowId)
        => flowId == null
            ? []
            : AllNodes.Values.Where(s => s.IsVisual
                                    && s.Container == flowId
                                    && (s is not UnknownNode || (s is UnknownNode un && !un.IsDefinedAsConfig))).ToDictionary(s => s.Id);

    void CalcFlowNodes()
    {
        //FlowNodes = Nodes.Where(s => s.IsVisual && s.Container == activeFlow.Id).ToList();
    }

    void ClickAddFlow()
    {
        _actionManager.ExecuteAction<CreateFlowNodeAction>();
    }

    IReadOnlyCollection<VarNode> varNodes = [];

    void CalcVarNodes()
    {
        varNodes = AllNodes.Values.Where(s => s is VarNode).Select(s => (VarNode)s).OrderBy(s => s.Name).ToList();
    }

    void OnClickAddVarNode()
    {
        var vname = "var" + Random.Shared.Next(10, 99);
        AllNodes.Add(new VarNode() { Container = _activeFlow.Id, Name = vname });
        AllNodesChanged.InvokeAsync(AllNodes);
        CalcVarNodes();
    }

    public void RecalcNodes()
    {
        CalcTabs();
        CalcVarNodes();
        CheckActiveTab();
        AllNodesChanged.InvokeAsync(_allNodes);
    }

    internal void OnClickConsoleDebugMessage(DebugMessage msg)
    {
        if (string.IsNullOrEmpty(msg.NodeId)) return;

        _nodeWorkspace1.SelectNode(msg.NodeId);
    }

    public void SetNodes(IDictionary<string, Node> nodes)
    {
        AllNodes = nodes;
        RecalcNodes();
    }

    public void SetNodes(IEnumerable<Node> nodes)
    {
        AllNodes = nodes.ToDictionary(s => s.Id);
    }
    public void AddNodes(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
            _allNodes.Add(node);
        AllNodes = _allNodes;
        RecalcNodes();
    }

    public void DeleteNodes(IEnumerable<Node> nodes)
    {
        var nodeIds = nodes.Select(s => s.Id).ToHashSet();

        var linkNodesIds = _nodeWorkspace1.Wires.Where(w => nodeIds.Contains(w.Node1.NodeId) || nodeIds.Contains(w.Node2.NodeId))
                        .SelectMany(s => (IEnumerable<string>)[s.Node1.NodeId, s.Node2.NodeId])
                        .Except(nodeIds)
                        .Distinct();

        //Console.WriteLine($"wires={linkNodesIds.Count()}");

        foreach (var id in linkNodesIds)
        {
            var node = _allNodes[id];
            foreach (var wireOuts in node.Wires)
            {
                var outsToRemove = wireOuts.Where(s => nodeIds.Contains(s.NodeId)).ToList();
                foreach (var x in outsToRemove)
                {
                    wireOuts.Remove(x);
                }
            }
        }

        foreach (var node in nodes)
            _allNodes.Remove(node.Id);
        AllNodes = _allNodes;
        RecalcNodes();
    }

    Type? _selectContext;
    string SelectContextString => _selectContext?.FullName ?? "null";

    public void SetSelectContext(Type? type)
    {
        _selectContext = type;
        StateHasChanged();
        //editor.CallStateHasChanged();
    }

    public ILogger<T> CreateLogger<T>()
        => _loggerFactory.CreateLogger<T>();

    Debouncer _setTaskCountDebouncer = new(200);

    public void SetCurrentTaskCount(int currentTaskCount)
    {
        _setTaskCountDebouncer.Debouce(() =>
        {
            runningTaskCountDisplayText = currentTaskCount.ToString();
            StateHasChanged();
        });
    }

    Task OnNodeContextMenu(NodeClickEventArgs e)
    {
        return _workspaceContextMenu.OpenAsync(_nodeWorkspace1.Width, _nodeWorkspace1.Height, (int)e.MouseEvent.ClientX - 48, (int)e.MouseEvent.ClientY - 40);
    }

    private static readonly Dictionary<string, string> NodeContextMenuItems = new()
    {
        ["DuplicateSelectedNodes"] = "Дублировать",
        ["DeleteSelectedNodes"] = AppRes.Delete,
    };

    void OnNodeContextMenuItemClick(MouseEventArgs e, string actionId)
    {
        if (actionId == "DeleteSelectedNodes") _actionManager.ExecuteAction<DeleteSelectedNodesAndWiresAction>();
        else if (actionId == "DuplicateSelectedNodes") _actionManager.ExecuteAction<DuplicateSelectedNodesAction>();
        else throw new NotImplementedException();
    }

    Task OnPaletteNodeContextMenu(NodeClickEventArgs e)
    {
        _currentPaletteNodeExamples = _examplesList.Where(s => s.NodeType == e.Node.GetType()).ToList();
        return _paletteNodeContextMenu.OpenAsync(_nodeWorkspace1.Width, _nodeWorkspace1.Height, (int)e.MouseEvent.ClientX - 48, (int)e.MouseEvent.ClientY - 40);
    }

    void UseExample(NodeExampleInfo example)
    {
        var nodes = example.ExampleHandlerInstance.Handle();
        var flowId = ActiveFlow?.Id ?? throw new ArgumentNullException("ActiveFlow is null, ActiveFlow should be set");
        foreach (var node in nodes)
            node.Container = flowId;
        _actionManager.ExecuteAction(new CreateNodesAction(this, nodes, startDrag: true));
    }

    Task OnWireContextMenu(SelWireEventArgs e)
    {
        return _workspaceContextMenu.OpenAsync(_nodeWorkspace1.Width, _nodeWorkspace1.Height, (int)e.MouseEvent.ClientX - 48, (int)e.MouseEvent.ClientY - 40);
    }

    private static readonly Dictionary<string, string> WireContextMenuItems = new()
    {
        ["DeleteSelectedNodes"] = AppRes.Delete,
    };

    void OnWireContextMenuItemClick(MouseEventArgs e, string actionId)
    {
        if (actionId == "DeleteSelectedNodes") _actionManager.ExecuteAction<DeleteSelectedNodesAndWiresAction>();
        else throw new NotImplementedException();
    }

    void OnSettingsButtonClick()
    {
        NodeEditorSettingsDialog.ShowDialog(_dialogService);
        EnableHotkeys(false);
    }

    void OnJobListHistoryButtonClick()
    {
        NodeTaskHistoryDialog.ShowDialog(_dialogService);
        EnableHotkeys(false);
    }

    private void SetHotkeysState(bool enable)
    {
        foreach (var k in _hotKeysContext.HotKeyEntries)
        {
            k.State.Disabled = !enable;
        }
    }

    /// <summary>
    /// Включить/Отключить хук горячих клавиш Actions
    /// Надо вызывать EnableHotkeys(false) когда появляются Диалоги или Редактор теряет фокус.
    /// </summary>
    /// <param name="enable"></param>
    public void EnableHotkeys(bool enable)
    {
        if (_hotkeysPrevSetState == enable) return;

        _hotkeysPrevSetState = enable;

        SetHotkeysState(enable);
    }

    void OnWorkspaceMouseEnter()
    {
        EnableHotkeys(true);
    }

}

internal static class NodeEditor1Extension
{
    public static IDictionary<string, Node> Add(this IDictionary<string, Node> nodes, Node node)
    {
        nodes.Add(node.Id, node);
        return nodes;
    }

    public static T? GetValueOrDefault<K, T>(this IDictionary<K, T> dictionary, K key)
        => dictionary.TryGetValue(key, out var value) ? value : default(T);

}
