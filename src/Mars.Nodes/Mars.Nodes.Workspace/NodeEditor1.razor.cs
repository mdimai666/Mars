using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Web;
using AppFront.Shared.Interfaces;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using Mars.Nodes.EditorApi.Interfaces;
using Mars.Nodes.FormEditor;
using Mars.Nodes.Workspace.Components;
using Mars.Nodes.Workspace.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Toolbelt.Blazor.HotKeys2;

namespace Mars.Nodes.Workspace;

public partial class NodeEditor1 : ComponentBase, IAsyncDisposable, INodeEditorApi
{
    [Inject] IJSRuntime JS { get; set; } = default!;
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IMessageService _messageService { get; set; } = default!;

    NodeWorkspaceJsInterop js = default!;

    List<Node> _nodes = new();

    [Parameter]
    public List<Node> Nodes
    {
        get => _nodes;
        set
        {
            if (value == _nodes) return;
            _nodes = value;
            CalcTabs();
            CalcVarNodes();
            CheckActiveTab();
            NodesChanged.InvokeAsync(_nodes);
        }
    }

    [Parameter] public EventCallback<List<Node>> NodesChanged { get; set; }

    public List<PaletteNode> Palette { get; private set; } = new();
    List<Node> INodeEditorApi.Palette => Palette.Select(s => s.Instance).ToList();


    public List<Type> RegisteredNodes { get; private set; } = new();

    EditorActions editorActions;
    public NodeWorkspace1? NodeWorkspace1 = default!;

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

        editorActions = new EditorActions(this);

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

        this.HotKeysContext = this.HotKeys.CreateContext()
         .Add(ModCode.Ctrl | ModCode.Shift, Code.A, SomeKeyTrigger, "do foo bar.")
         .Add(ModCode.Ctrl, Code.D, DeployClick, "Deploy")
         .Add(ModCode.Ctrl, Code.S, SaveFormClick, "Save Form")
         .Add(ModCode.None, Code.Delete, editorActions.UserAction_DeleteSelected);
        //.Add(...)...;

        //Console.WriteLine(">>>NodeEditor>LoadFromJson");
        //LoadFromJson();
    }

    public async ValueTask DisposeAsync()
    {
        await this.HotKeysContext.DisposeAsync();
    }

    async void OpenOffcanvasEditor()
    {
        await js.ShowOffcanvas("node-editor-offcanvas", true);
    }

    public void CallStateHasChanged()
    {
        StateHasChanged();
    }

    void SomeKeyTrigger()
    {
        Console.WriteLine("hotkey detect!");
        AddDebugMessage(new DebugMessage
        {
            Message = "CTRL+SHIFT+A hotkey detect!"
        });
    }

    void CreateNewNodeFromPalette(MouseEventArgs e, Node paletteNode)
    {
        Node instance = (Node)Activator.CreateInstance(paletteNode.GetType())!;
        instance.Container = activeFlow.Id;
        Nodes.Add(instance);
        CalcFlowNodes();
        NodeWorkspace1?.OnClickPaletteNewNode(e, paletteNode, instance);
    }

    Node CreateConfigNodeFromType(Type nodeType)
    {
        Node instance = (Node)Activator.CreateInstance(nodeType)!;
        var thisTypeCount = Nodes.Count(s => s.Type == instance.Type);
        instance.Container = activeFlow.Id;
        instance.Name = instance.Label + (thisTypeCount + 1);
        Nodes.Add(instance);
        return instance;
    }

    #region DEBUGGER
    const string noderedDebugMessageList = "#nodered-debug-message-list";



    List<DebugMessage> messages = new() { new() };

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
        //Console.WriteLine("SaveFormClick");
        //AddDebugMessage(new DebugMessage { message = "SaveFormClick" });
        //await js.ShowOffcanvas("node-editor-offcanvas", false);
        _ = nodeEditContainer1.FormSaveClick();
    }

    public void DeployClick()
    {
        OnDeploy.InvokeAsync(Nodes);
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
        var node = Nodes.FirstOrDefault(n => n.Id == id);
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
        Console.WriteLine("void SaveNode(Node node)");
        //var n = Nodes.First(s => s.Id == node.Id);
        //n = EditNode = node;
        EditNode = node;
        int index = Nodes.FindIndex(item => item.Id == node.Id);
        Nodes[index] = EditNode;
        EditNode.changed = true;
        Nodes = Nodes.ToList();
        NodesChanged.InvokeAsync(Nodes);

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
        editorActions.UserAction_Delete([nodeId]);
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
        flows = Nodes.Where(node => node is FlowNode).Select(s => s as FlowNode).OrderBy(s => s.Order).ToList()!;
        if (flows.Count == 0)
        {
            ClickAddFlow();
        }
    }

    void CheckActiveTab()
    {
        if (Nodes is not null && activeFlow is null)
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


    List<FlowNode> flows = new();

    FlowNode? activeFlow = null;

    public List<Node> FlowNodes => Nodes.Where(s => s.IsVisual
                                                    && s.Container == activeFlow.Id
                                                    && (s is not UnknownNode || (s is UnknownNode un && !un.IsDefinedAsConfig))).ToList();

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
        Nodes.Add(flow);
        NodesChanged.InvokeAsync(Nodes);
        CalcTabs();
        ChangeFlow(flow);
    }

    IReadOnlyCollection<VarNode> varNodes = new List<VarNode>();

    void CalcVarNodes()
    {
        varNodes = Nodes.Where(s => s is VarNode).Select(s => (VarNode)s).OrderBy(s => s.Name).ToList();
    }

    void OnClickAddVarNode()
    {
        var vname = "var" + Random.Shared.Next(10, 99);
        Nodes.Add(new VarNode() { Container = activeFlow.Id, Name = vname });
        NodesChanged.InvokeAsync(Nodes);
        CalcVarNodes();
    }

    public void RecalcNodes()
    {
        CalcTabs();
        CalcVarNodes();
        CheckActiveTab();
    }

    void OnClickConsoleDebugMessage(DebugMessage msg)
    {
        if (string.IsNullOrEmpty(msg.NodeId)) return;

        NodeWorkspace1.SelectNode(msg.NodeId);
    }
}
