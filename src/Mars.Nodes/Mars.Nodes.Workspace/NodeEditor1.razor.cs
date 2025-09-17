using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Web;
using AppFront.Shared.Interfaces;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.EditorApi.Interfaces;
using Mars.Nodes.FormEditor;
using Mars.Nodes.Workspace.ActionManager;
using Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;
using Mars.Nodes.Workspace.Components;
using Mars.Nodes.Workspace.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Toolbelt.Blazor.HotKeys2;

namespace Mars.Nodes.Workspace;

public partial class NodeEditor1 : ComponentBase, IAsyncDisposable, INodeEditorApi
{
    [Inject] IJSRuntime JS { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IMessageService _messageService { get; set; } = default!;
    [Inject] ILoggerFactory _loggerFactory { get; set; } = default!;
    [Inject] ILogger<NodeEditor1> _logger { get; set; } = default!;

    NodeWorkspaceJsInterop js = default!;

    Dictionary<string, Node> _allNodes = [];

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

    public List<PaletteNode> Palette { get; private set; } = [];
    List<Node> INodeEditorApi.Palette => Palette.Select(s => s.Instance).ToList();

    public List<Type> RegisteredNodes { get; private set; } = [];

    EditorActionManager _actionManager = default!;
    public IEditorActionManager ActionManager => _actionManager;

    NodeWorkspace1? _nodeWorkspace1 = default!;
    public INodeWorkspaceApi NodeWorkspace => _nodeWorkspace1!;

    [Inject] HotKeys HotKeys { get; set; } = default!;
    HotKeysContext HotKeysContext = default!;

    [Parameter] public EventCallback<string> OnInject { get; set; }
    [Parameter] public EventCallback<IEnumerable<Node>> OnDeploy { get; set; }
    [Parameter] public EventCallback<string> OnCmdClick { get; set; }
    [Parameter] public RenderFragment? SectionActions { get; set; } = null;

    Node? EditNode { get; set; }

    QuickNodeAddMenu? quickNodeAddMenu = default!;
    NodeEditContainer1? nodeEditContainer1 = default!;

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

    public NodeEditor1()
    {
        RegisteredNodes = NodesLocator.RegisteredNodes();

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
            Palette.Add(item);

        }

    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        js = new(JS);
        _ = js.InitModule();
        //_ = js.Prompt("aefaef");

        HotKeysContext = HotKeys.CreateContext()
         .Add(ModCode.Ctrl, Code.D, DeployClick, "Deploy")
         .Add(ModCode.Ctrl, Code.S, SaveFormClick, "Save Form");
        //.Add(ModCode.None, Code.Delete, editorActions.UserAction_DeleteSelected);
        //.Add(...)...;

        _actionManager = new EditorActionManager(this, HotKeysContext);
        _actionManager.RegisterAssembly(typeof(DeleteSelectedNodesAndWiresAction).Assembly);
        _actionManager.RefreshDict();

        _actionManager.PropertyChanged += (_, __) => InvokeAsync(OnChildComponentPropertyChangedRepaint);
    }

    public async ValueTask DisposeAsync()
    {
        await HotKeysContext.DisposeAsync();
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

    void CreateNewNodeFromPalette(MouseEventArgs e, Node paletteNode)
    {
        Node instance = (Node)Activator.CreateInstance(paletteNode.GetType())!;
        instance.Container = activeFlow.Id;
        AllNodes.Add(instance);
        CalcFlowNodes();
        _nodeWorkspace1?.OnClickPaletteNewNode(e, paletteNode, instance);
    }

    Node CreateConfigNodeFromType(Type nodeType)
    {
        Node instance = (Node)Activator.CreateInstance(nodeType)!;
        var thisTypeCount = AllNodes.Values.Count(s => s.Type == instance.Type);
        instance.Container = activeFlow.Id;
        instance.Name = instance.Label + (thisTypeCount + 1);
        AllNodes.Add(instance);
        return instance;
    }

    #region DEBUGGER
    const string noderedDebugMessageList = "#nodered-debug-message-list";

    List<DebugMessage> messages = [new()];

    //string obj1 = JsonSerializer.Serialize(new Msg1());

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

    void ClearDebugMessages()
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

    void OnDblClickNode(NodeClickEventArgs e)
    {
        StartEditNode(e.Node);
    }

    public void StartEditNode(Node node)
    {
        EditNode = node;//.Copy();
        nodeEditContainer1.StartEditNode(EditNode);
    }

    public void OnClickEditConfigNode(string id)
    {
        var node = AllNodes.GetValueOrDefault(id);
        if (node == null)
        {
            _ = _messageService.Error($"Error editConfigNode command: id:{id} not found");
            return;
        }
        nodeEditContainer1.StartEditNode(node);
    }

    public void OnClickNewConfigNode(Type nodeType)
    {
        var found = RegisteredNodes.FirstOrDefault(s => s == nodeType);
        if (found == null)
        {
            _ = _messageService.Error($"Error newConfigNode command: type:{nodeType.Name} not found");
            return;
        }
        var instance = CreateConfigNodeFromType(found);
        nodeEditContainer1.StartEditNode(instance);

    }

    void SaveNode(Node node)
    {
        _logger.LogTrace("SaveNode(Node node)");
        EditNode = node;
        AllNodes[node.Id] = EditNode;
        EditNode.changed = true;

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
        _actionManager.ExecuteAction<DeleteSelectedNodesAndWiresAction>();
    }

    void OnWorkspaceClick(MouseEventArgs e)
    {
        if (quickNodeAddMenu.Visible)
        {
            quickNodeAddMenu.Hide();
        }
    }

    async void OnWorkspaceDblClick(MouseEventArgs e)
    {
        quickNodeAddMenu.Show(e);
        await Task.Delay(100);
        await quickNodeAddMenu.Focus();
    }

    void CalcTabs()
    {
        flows = AllNodes.Values.Where(node => node is FlowNode).Select(s => s as FlowNode).OrderBy(s => s.Order).ToList()!;
        if (flows.Count == 0)
        {
            ClickAddFlow();
        }
    }

    void CheckActiveTab()
    {
        if (AllNodes is not null && activeFlow is null)
        {
            var querystring = HttpUtility.ParseQueryString(new Uri(NavigationManager.Uri).Query);

            string? flow = querystring["flow"];

            if (string.IsNullOrEmpty(flow) == false)//NOT WORK
            {
                activeFlow = flows.FirstOrDefault(s => s.Id == flow);
            }

            activeFlow ??= flows.OrderBy(s => s.Order).FirstOrDefault();

            CalcFlowNodes();
        }
    }

    List<FlowNode> flows = [];

    FlowNode? activeFlow = null;

    public IReadOnlyDictionary<string, Node> FlowNodes => AllNodes.Values.Where(s => s.IsVisual
                                                    && s.Container == activeFlow.Id
                                                    && (s is not UnknownNode || (s is UnknownNode un && !un.IsDefinedAsConfig))).ToDictionary(s => s.Id);

    //[Parameter, SupplyParameterFromQuery(Name = "flow")] supplu not work in non route components
    //public string InitialFlowId { get; set; }

    void ChangeFlow(FlowNode flowNode)
    {
        activeFlow = flowNode;
        CalcFlowNodes();
        CalcVarNodes();
        var url = NavigationManager.GetUriWithQueryParameter("flow", flowNode.Id);
        NavigationManager.NavigateTo(url);
    }

    void CalcFlowNodes()
    {
        //FlowNodes = Nodes.Where(s => s.IsVisual && s.Container == activeFlow.Id).ToList();
    }

    void ClickAddFlow()
    {
        var flow = new FlowNode() { Name = "flow " + flows.Count };
        AllNodes.Add(flow);
        AllNodesChanged.InvokeAsync(AllNodes);
        CalcTabs();
        ChangeFlow(flow);
    }

    IReadOnlyCollection<VarNode> varNodes = [];

    void CalcVarNodes()
    {
        varNodes = AllNodes.Values.Where(s => s is VarNode).Select(s => (VarNode)s).OrderBy(s => s.Name).ToList();
    }

    void OnClickAddVarNode()
    {
        var vname = "var" + Random.Shared.Next(10, 99);
        AllNodes.Add(new VarNode() { Container = activeFlow.Id, Name = vname });
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

    void OnClickConsoleDebugMessage(DebugMessage msg)
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
