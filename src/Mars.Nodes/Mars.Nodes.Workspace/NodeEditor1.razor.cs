using System.ComponentModel;
using System.Text.Json;
using System.Web;
using Mars.Admin.Framework;
using Mars.Contracts.Resources;
using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Converters;
using Mars.Nodes.Core.Nodes.Common;
using Mars.Nodes.Core.Nodes.Functions;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.FormEditor;
using Mars.Nodes.Front.Abstractions.Editor;
using Mars.Nodes.Front.Abstractions.Editor.Models;
using Mars.Nodes.Front.Abstractions.Services;
using Mars.Nodes.Workspace.ActionManager;
using Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;
using Mars.Nodes.Workspace.Components;
using Mars.Nodes.Workspace.EditorParts;
using Mars.Nodes.Workspace.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Toolbelt.Blazor.HotKeys2;
using static Mars.Nodes.Workspace.Components.QuickNodeAddMenu;

namespace Mars.Nodes.Workspace;

public partial class NodeEditor1 : ComponentBase, IAsyncDisposable, INodeEditorApi
{
    [Inject] IServiceProvider _serviceProvider { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] ILoggerFactory _loggerFactory { get; set; } = default!;
    [Inject] ILogger<NodeEditor1> _logger { get; set; } = default!;
    [Inject] INodesLocator _nodesLocator { get; set; } = default!;
    [Inject] EditorActionLocator _editorActionLocator { get; set; } = default!;
    [Inject(Key = typeof(NodeJsonConverter))] JsonSerializerOptions _jsonSerializerOptions { get; set; } = default!;
    [Inject] AdminJs _adminJs { get; set; } = default!;
    [Inject] NodeWorkspaceJsInterop _js { get; set; } = default!;
    [Inject] INodeFormsLocator _nodeFormsLocator { get; set; } = default!;

    [Inject] HotKeys HotKeys { get; set; } = default!;
    HotKeysContext _hotKeysContext = default!;
    bool _hotkeysPrevSetState = true;

    [Parameter]
    public IDictionary<string, Node> AllNodes
    {
        get => _document.Nodes;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!_document.SetNodes(value)) return;
            _document.Recalculate();
            CheckActiveTab();
            _ = AllNodesChanged.InvokeAsync(_document.Nodes);
        }
    }

    [Parameter] public EventCallback<IDictionary<string, Node>> AllNodesChanged { get; set; }

    /// <summary>Глобальный режим отладки: хранит последнее сообщение, которое каждая нода отдала дальше.</summary>
    [Parameter] public bool DebugMode { get; set; }
    [Parameter] public EventCallback<bool> DebugModeChanged { get; set; }
    [Parameter] public EventCallback<string> OnInject { get; set; }
    [Parameter] public EventCallback<IEnumerable<Node>> OnDeploy { get; set; }
    [Parameter] public EventCallback<string> OnCmdClick { get; set; }
    [Parameter] public RenderFragment? SectionActions { get; set; } = null;
    [Parameter] public IReadOnlyDictionary<string, InlineFunctionNodeSchema> InlineFunctionNodeSchemas { get; set; } = default!;

    public JsonSerializerOptions NodesJsonSerializerOptions => _jsonSerializerOptions;
    public JsonSerializerOptions NodesJsonSerializerOptionsFormatted { get; private set; } = default!;

    readonly NodesDocument _document = new();
    Node? _selectedNode;
    string runningTaskCountDisplayText = "-";

    #region PALETTE
    List<PaletteNode> _palette = [];
    public IReadOnlyCollection<PaletteNode> Palette => _palette;
    IReadOnlyCollection<Node> INodeEditorApi.Palette => Palette.Select(s => s.Instance).ToList();

    public IReadOnlyCollection<Type> RegisteredNodes { get; private set; } = [];
    #endregion

    #region FLOWS
    IReadOnlyList<FlowNode> flows => _document.Flows;

    public FlowNode? ActiveFlow => _document.ActiveFlow;

    public IReadOnlyDictionary<string, Node> FlowNodes => _document.FlowNodes;
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
    ElementReference _paletteSidebarRef;
    // fallback until the first JS measurement of the palette sidebar position
    float _paletteOffsetX = 48;
    float _paletteOffsetY = 40;

    public IReadOnlyDictionary<string, LinkInNode[]> InboundLinkOutNodesDict => _document.InboundLinkOutNodes;

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
    Orientation? _editorSpaceOrientation;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.S, SaveFormClick, "Save Form");
        _ = _js.InitModule();

        NodesJsonSerializerOptionsFormatted = _nodesLocator.CreateJsonSerializerOptions(writeIndented: true);

        _actionManager = new EditorActionManager(this, _serviceProvider, _hotKeysContext, _editorActionLocator, _adminJs);
        _actionManager.PropertyChanged += OnActionManagerPropertyChanged;

        RegisteredNodes = _nodesLocator.RegisteredNodes();

        _palette = PaletteBuilder.Build(RegisteredNodes, InlineFunctionNodeSchemas);

        _examplesList = _nodesLocator.CreateExamplesList();
    }

    protected override async Task OnParametersSetAsync()
    {
        EnsureDefaultFlow();

        if (_editorSpaceOrientation is null)
        {
            var viewPort = await _js.GetViewportMetricsAsync();
            _editorSpaceOrientation = viewPort.Orientation == ScreenOrientationType.PortraitPrimary ? Orientation.Vertical : Orientation.Horizontal;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hotKeysContext is not null)
            await _hotKeysContext.DisposeAsync();
        if (_actionManager is not null)
            _actionManager.PropertyChanged -= OnActionManagerPropertyChanged;
    }

    void OnActionManagerPropertyChanged(object? sender, PropertyChangedEventArgs e) => InvokeAsync(OnChildComponentPropertyChangedRepaint);

    void OnChildComponentPropertyChangedRepaint() => StateHasChanged();

    public InlineFunctionNode? CreateInlineFunctionNodeById(string nodeTypeId)
    {
        var inlineNodeDef = InlineFunctionNodeSchemas.GetValueOrDefault(nodeTypeId);
        if (inlineNodeDef is null) return null;
        var node = InlineFunctionNode.CreateInlineFunctionNode(inlineNodeDef);
        return node;
    }

    void OnMouseDownPaletteNode(NodeComponentMouseEventArgs e)
    {
        if (e.MouseEvent.Button != (long)MouseButton.Left) return;

        if (_showPaletteNodeContextMenu) _showPaletteNodeContextMenu = false;

        CreateNewNodeFromPalette(e);
    }

    void QuickNodeAddMenuOnSelectNode(SelectNodeEvent e)
    {
        CreateNewNodeFromPalette(new(e.MouseEventArgs, e.Node));
    }

    void CreateNewNodeFromPalette(NodeComponentMouseEventArgs e)
    {
        var paletteNode = e.Node;

        var instance = paletteNode.Copy(_jsonSerializerOptions);
        instance.Id = Guid.NewGuid().ToString();
        instance.Container = _document.ActiveFlow!.Id;

        if (instance is LinkInNode) instance.Name = "link in " + AllNodes.Values.Count(s => s is LinkInNode);
        else if (instance is LinkOutNode) instance.Name = "link out " + AllNodes.Values.Count(s => s is LinkOutNode);

        _document.AddNode(instance);
        _document.Recalculate();
        _nodeWorkspace1?.OnClickPaletteNewNode(e.MouseEvent, instance);
    }

    ConfigNode CreateConfigNodeFromType(Type nodeType)
    {
        ConfigNode instance = (ConfigNode)Activator.CreateInstance(nodeType)!;
        var thisTypeCount = AllNodes.Values.Count(s => s.TypeId == instance.TypeId);
        instance.Container = string.Empty;
        instance.Name = instance.Label + (thisTypeCount + 1);
        _document.AddNode(instance);
        RecalcNodes();
        return instance;
    }

    #region DEBUGGER
    DebugMessagesConsole _debugMessagesConsole = default!;

    public void AddDebugMessage(string text) => AddDebugMessage(DebugMessage.ConsoleMessage(text));
    public void AddDebugMessage(DebugMessage msg) => _debugMessagesConsole.AddMessage(msg);

    internal void ToggleConsolePosition()
    {
        _editorSpaceOrientation = _editorSpaceOrientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
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

    void OnClickNode(NodeComponentMouseEventArgs e)
    {
        _selectedNode = e.Node;

        if (_showWorkspaceContextMenu) _showWorkspaceContextMenu = false;
        if (_showPaletteNodeContextMenu) _showPaletteNodeContextMenu = false;

        _debugSnapshotsDebouncer.Debounce(RefreshDebugSnapshots);
    }

    async Task OnDebugModeSwitchChanged(bool enabled)
    {
        if (_serviceProvider.GetService(typeof(INodeServiceClient)) is INodeServiceClient client)
            await client.SetDebugMode(enabled);

        await DebugModeChanged.InvokeAsync(enabled);
    }

    readonly Debouncer _debugSnapshotsDebouncer = new(300);

    /// <summary>
    /// Тянет снимки DebugMode для выбранной и редактируемой ноды вместе с upstream-замыканием.
    /// Вызывается при открытии формы, смене выбора и по сигналу хаба DebugSnapshotsChanged —
    /// эфир данных не несёт, после реконнекта достаточно перетянуть.
    /// </summary>
    public void RefreshDebugSnapshots() => _ = RefreshDebugSnapshotsAsync();

    async Task RefreshDebugSnapshotsAsync()
    {
        if (_serviceProvider.GetService(typeof(INodeServiceClient)) is not INodeServiceClient client) return;
        if (_serviceProvider.GetService(typeof(IHostValueHints)) is not IHostValueHints hints) return;

        var nodeIds = DebugSnapshotScope();
        if (nodeIds.Count == 0) return;

        try
        {
            var response = await client.DebugSnapshots(nodeIds);
            hints.SetDebugSnapshots(response);

            if (response.DebugMode != DebugMode)
                await DebugModeChanged.InvokeAsync(response.DebugMode);

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "debug snapshots are not available");
        }
    }

    HashSet<string> DebugSnapshotScope()
        => DebugSnapshotScopeUtil.BuildUpstreamScope(AllNodes.Values, _selectedNode?.Id, EditNode?.Id);

    void OnDblClickNode(NodeComponentMouseEventArgs e)
    {
        StartEditNode(e.Node);
    }

    public void StartEditNode(Node node)
    {
        EnableHotkeys(false);
        EditNode = node;
        nodeEditContainer1.StartEditNode(EditNode);
        RefreshDebugSnapshots();
    }

    public async Task StartCreateNewConfigNode(AppendNewConfigNodeEvent appendNewConfigNodeEvent)
    {
        Type configNodeType = appendNewConfigNodeEvent.ConfigNodeType;

        var found = RegisteredNodes.FirstOrDefault(s => s == configNodeType);
        if (found == null)
        {
            _ = _messageService.Error($"Error newConfigNode command: type:{configNodeType.Name} not found");
            return;
        }
        var instance = CreateConfigNodeFromType(found);
        await Task.Delay(10);
        appendNewConfigNodeEvent.ConfigNodeSetter(instance);
        EditNode = instance;
        nodeEditContainer1.StartEditNode(instance);
        RefreshDebugSnapshots();
        StateHasChanged();
    }

    void OnClickEditConfigNode(string id)
    {
        _document.Nodes.TryGetValue(id, out var node);
        if (node == null)
        {
            _ = _messageService.Error($"Error editConfigNode command: id:{id} not found");
            return;
        }
        EditNode = node;
        nodeEditContainer1.StartEditNode(node);
        RefreshDebugSnapshots();
    }

    Task OnClickNewConfigNode(AppendNewConfigNodeEvent e)
        => StartCreateNewConfigNode(e);

    void OnEditFormSaveNodeClick(Node node)
    {
        var editNodeAction = new EditNodeAction(this, AllNodes[node.Id], node);
        _actionManager.ExecuteAction(editNodeAction);
    }

    public void SaveNode(Node node, bool changed = true)
    {
        _logger.LogTrace("SaveNode(Node node)");
        EditNode = node;
        EditNode.changed = changed;
        _document.SaveNode(node);

        _ = OnNodeSavedAsync(node);
    }

    async Task OnNodeSavedAsync(Node node)
    {
        try
        {
            await AllNodesChanged.InvokeAsync(AllNodes);

            if (node is FlowNode flow)
                ChangeFlow(flow);

            _nodeWorkspace1.RedrawWires();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveNode continuation failed for node {NodeId}", node.Id);
        }
    }

    void DeleteNode(string nodeId)
    {
        if (_document.Nodes[nodeId] is FlowNode)
            _actionManager.ExecuteAction(new DeleteFlowNodeAction(this, nodeId));
        else
            _actionManager.ExecuteAction(new DeleteNodesAndWiresAction(this, [_document.Nodes[nodeId]]));
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

    void OnWorkspaceDblClick(MouseEventArgs e)
    {
        quickNodeAddMenu.Show(e);
    }

    void EnsureDefaultFlow()
    {
        if (_document.Flows.Count == 0)
            _actionManager.ExecuteAction<CreateFlowNodeAction>(addToHistory: false);
    }

    void CheckActiveTab()
    {
        var querystring = HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);
        _document.TryResolveActiveFlow(querystring["flow"]);
    }

    public void ChangeFlow(FlowNode flowNode)
    {
        _document.ChangeFlow(flowNode);
        var url = NavigationManager.GetUriWithQueryParameter("flow", flowNode.Id);
        NavigationManager.NavigateTo(url);
    }

    public IReadOnlyDictionary<string, Node> GetFlowNodes(string? flowId)
        => _document.GetFlowNodes(flowId);

    void ClickAddFlow()
    {
        _actionManager.ExecuteAction<CreateFlowNodeAction>();
    }

    IReadOnlyCollection<VarNode> varNodes => _document.VarNodes;

    void OnClickAddVarNode()
    {
        var vname = "var" + Random.Shared.Next(10, 99);
        _document.AddNode(new VarNode() { Container = _document.ActiveFlow!.Id, Name = vname });
        _document.Recalculate();
        _ = AllNodesChanged.InvokeAsync(_document.Nodes);
    }

    public void RecalcNodes()
    {
        _document.Recalculate();
        EnsureDefaultFlow();
        CheckActiveTab();
        _ = AllNodesChanged.InvokeAsync(_document.Nodes);
    }

    internal void OnClickConsoleDebugMessage(DebugMessage msg)
    {
        if (string.IsNullOrEmpty(msg.NodeId)) return;
        if (AllNodes.TryGetValue(msg.NodeId, out var node))
        {
            if (_document.ActiveFlow!.Id == node.Container)
                _nodeWorkspace1.SelectNode(msg.NodeId);
        }
    }

    internal void OnDblClickConsoleDebugMessage(DebugMessage msg)
    {
        if (string.IsNullOrEmpty(msg.NodeId)) return;
        FocusNode(msg.NodeId);
    }

    public void SetNodes(IDictionary<string, Node> nodes)
    {
        _document.SetNodes(nodes);
        RecalcNodes();
    }

    public void SetNodes(IEnumerable<Node> nodes)
    {
        SetNodes(nodes.ToDictionary(s => s.Id));
    }

    public void AddNodes(IEnumerable<Node> nodes)
    {
        AddNodesAndWires(nodes, []);
    }

    public void AddNodesAndWires(IEnumerable<Node> nodes, IEnumerable<NodeConnect> connects)
    {
        _document.AddNodesAndWires(nodes, connects);
        RecalcNodes();
    }

    public void DeleteNodes(IEnumerable<Node> nodes)
    {
        DeleteNodesAndWires(nodes, []);
    }

    public void DeleteNodesAndWires(IEnumerable<Node> nodes, IEnumerable<NodeConnect> connects)
    {
        _document.DeleteNodesAndWires(nodes, connects);
        RecalcNodes();
    }

    public void RedrawWires()
    {
        _nodeWorkspace1.RedrawWires();
    }

    public void FocusNode(string nodeId)
    {
        if (AllNodes.TryGetValue(nodeId, out var node))
        {
            FocusNode(node);
        }
    }

    public void FocusNode(Node node)
    {
        if (_document.ActiveFlow!.Id != node.Container && node.Container.IsNotNullOrEmpty())
        {
            ChangeFlow((FlowNode)AllNodes[node.Container]);
        }
        var wh = _nodeWorkspace1.Width / 2;
        var hh = _nodeWorkspace1.Height / 2;
        var x = Math.Clamp(node.X - wh, 0f, node.X);
        var y = Math.Clamp(node.Y - hh, 0f, node.Y);
        _nodeWorkspace1.ScrollTo(x, y);
        _nodeWorkspace1.SelectNode(node);
    }

    Type? _selectContext;
    string SelectContextString => _selectContext?.FullName ?? "null";

    public void SetSelectContext(Type? type)
    {
        _selectContext = type;
        StateHasChanged();
    }

    public ILogger<T> CreateLogger<T>()
        => _loggerFactory.CreateLogger<T>();

    Debouncer _setTaskCountDebouncer = new(200);

    public void SetCurrentTaskCount(int currentTaskCount)
    {
        _setTaskCountDebouncer.Debounce(() =>
        {
            runningTaskCountDisplayText = currentTaskCount.ToString();
            _ = InvokeAsync(StateHasChanged);
        });
    }

    Task OnNodeContextMenu(NodeComponentMouseEventArgs e)
    {
        PrepareNodeExampleListForContextMenu(e.Node.GetType());
        return OpenWorkspaceContextMenuAsync(_workspaceContextMenu, e.MouseEvent);
    }

    async Task OpenWorkspaceContextMenuAsync(FluentMenu menu, MouseEventArgs e)
    {
        await _nodeWorkspace1.RefreshContainerOffsetAsync();
        await menu.OpenAsync(_nodeWorkspace1.Width, _nodeWorkspace1.Height,
                             (int)(e.ClientX - _nodeWorkspace1.ContainerOffsetX + _nodeWorkspace1.ScrollInfo.ScrollLeft),
                             (int)(e.ClientY - _nodeWorkspace1.ContainerOffsetY + _nodeWorkspace1.ScrollInfo.ScrollTop));
    }

    private static readonly Dictionary<string, string> NodeContextMenuItems = new()
    {
        ["DuplicateSelectedNodes"] = "Дублировать",
        ["DeleteSelectedNodes"] = AppRes.Delete,
        ["CopySelectedNodesAsJsonAction"] = "Копировать JSON",
    };

    void OnNodeContextMenuItemClick(MouseEventArgs e, string actionId)
    {
        if (actionId == "DeleteSelectedNodes") _actionManager.ExecuteAction<DeleteSelectedNodesAndWiresAction>();
        else if (actionId == "DuplicateSelectedNodes") _actionManager.ExecuteAction<DuplicateSelectedNodesAction>();
        else if (actionId == "CopySelectedNodesAsJsonAction") _actionManager.ExecuteAction<CopySelectedNodesAsJsonAction>();
        else throw new NotImplementedException();
    }

    Task OnPaletteNodeContextMenu(NodeComponentMouseEventArgs e)
    {
        PrepareNodeExampleListForContextMenu(e.Node.GetType());
        return OpenPaletteContextMenuAsync(e.MouseEvent);
    }

    async Task OpenPaletteContextMenuAsync(MouseEventArgs e)
    {
        await UpdatePaletteOffsetAsync();
        await _paletteNodeContextMenu.OpenAsync(_nodeWorkspace1.Width, _nodeWorkspace1.Height,
                                                (int)(e.ClientX - _paletteOffsetX),
                                                (int)(e.ClientY - _paletteOffsetY));
    }

    async Task UpdatePaletteOffsetAsync()
    {
        var bounds = await _js.GetElementBounds(_paletteSidebarRef);
        if (bounds is null) return;
        _paletteOffsetX = (float)bounds.Value.Left;
        _paletteOffsetY = (float)bounds.Value.Top;
    }

    void PrepareNodeExampleListForContextMenu(Type nodeType)
    {
        _currentPaletteNodeExamples = _examplesList.Where(s => s.NodeType == nodeType).ToList();

    }

    void UseExample(NodeExampleInfo example)
    {
        var editorState = new EditorStateForExampleCreator(this);
        var nodes = example.ExampleHandlerInstance.Handle(editorState);
        var flowId = ActiveFlow?.Id ?? throw new ArgumentNullException("ActiveFlow is null, ActiveFlow should be set");
        foreach (var node in nodes)
            node.Container = flowId;
        quickNodeAddMenu.Hide();
        _actionManager.ExecuteAction(new CreateNodesAction(this, nodes, startDrag: true));
    }

    Task OnWireContextMenu(SelWireEventArgs e)
        => OpenWorkspaceContextMenuAsync(_workspaceContextMenu, e.MouseEvent);

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
        ShowSettingsDialog();
    }

    public void ShowSettingsDialog()
    {
        NodeEditorSettingsDialog.ShowDialog(_dialogService, this);
        EnableHotkeys(false);
    }

    void OnJobListHistoryButtonClick()
    {
        NodeTaskHistoryDialog.ShowDialog(_dialogService, this);
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

    public void TouchNodeInjectedEffect(Guid taskId, string nodeId, NodeExecutionTrigger trigger)
    {
        _ = _js.TouchFlashAnimationBySelector($"#node-{nodeId} .red-ui-flow-node__body__animation_backdrop");
    }

    public void TouchNodeHighlightEffect(string nodeId)
    {
        _ = _js.TouchHighlightBySelector($"#node-{nodeId}", "red-ui-flow-node--highlight", 1000);
    }

    internal Type? GetNodeComponentExtender(Node node)
    {
        return _nodeFormsLocator.GetNodeComponentExtender(node.GetType());
    }
}
