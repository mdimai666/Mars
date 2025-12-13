using System.Drawing;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.EditorApi.Interfaces;
using Mars.Nodes.Workspace.ActionManager;
using Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;
using Mars.Nodes.Workspace.Components;
using Mars.Nodes.Workspace.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.Nodes.Workspace.EditorParts;

public partial class NodeWorkspace1 : INodeWorkspaceApi, IResizeObserver
{
    [Inject] ILogger<NodeWorkspace1> _logger { get; set; } = default!;

    [Parameter] public string Style { get; set; } = "";
    [Parameter] public string Class { get; set; } = "";
    [Inject] IJSRuntime JSRuntime { get; set; } = default!;
    NodeWorkspaceJsInterop js = default!;

    [CascadingParameter] INodeEditorApi _nodeEditor { get; set; } = default!;

    ElementReference _containerRef = default!;
    ElementReference _svgContainerRef = default!;

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    IReadOnlyDictionary<string, Node> _flowNodes { get; set; } = new Dictionary<string, Node>();

    Node? sel_node;
    List<DragElement> dragElements { get; set; } = [];
    bool drag = false;
    bool isProcessPasteNewNode;

    [Parameter]
    public IReadOnlyDictionary<string, Node> FlowNodes
    {
        get => _flowNodes;
        set
        {
            if (_flowNodes == value) return;

            string? oldFlowId = _flowNodes.Values.FirstOrDefault()?.Container;
            string? newFlowId = value.Values.FirstOrDefault()?.Container;

            _flowNodes = value;

            if (oldFlowId != newFlowId)
            {
                RefreshWires();
            }

            //_logger.LogTrace("Workspace set: FlowNodes");
            FlowNodesChanged.InvokeAsync(value);
        }
    }

    public NewWire? new_wire;

    List<Wire> _wires { get; set; } = [];
    public IEnumerable<Wire> Wires => _wires;
    public IReadOnlyCollection<Node> SelectedNodes() => _flowNodes.Values.Where(s => s.selected).ToList();
    public IReadOnlyCollection<Wire> SelectedWires() => _wires.Where(s => s.Selected).ToList();

    [Parameter] public EventCallback<IReadOnlyDictionary<string, Node>> FlowNodesChanged { get; set; }

    [Parameter] public EditorActionManager? ActionManager { get; set; }

    [Parameter] public EventCallback<string> OnInject { get; set; }

    [Parameter] public EventCallback<NodeClickEventArgs> OnClickNode { get; set; }
    [Parameter] public EventCallback<NodeClickEventArgs> OnDblClickNode { get; set; }
    [Parameter] public EventCallback<NodeClickEventArgs> OnNodeContextMenu { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnWorkspaceClick { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnWorkspaceDblClick { get; set; }
    [Parameter] public EventCallback<SelWireEventArgs> OnWireContextMenu { get; set; }

    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    Lasso lasso = new();

    MouseEventArgs _lastMouseWorkspaceState = new();
    NodeWirePointResolver _nodeWirePointResolver = new();
    private DotNetObjectReference<IResizeObserver> _dotNetRef = default!;

    //--------------------------------------

    protected override void OnInitialized()
    {
        base.OnInitialized();
        js = new NodeWorkspaceJsInterop(JSRuntime);

        //wire_drawWires();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetRef = DotNetObjectReference.Create<IResizeObserver>(this);

            await js.ObserveAsync(_containerRef, _dotNetRef);
        }
    }

    void onMouseMove(MouseEventArgs e)
    {
        _lastMouseWorkspaceState = e;
        if (drag)
        {
            foreach (var d in dragElements)
            {
                d.node.X = (float)(e.ClientX + d.nodeX - d.clickX);
                d.node.Y = (float)(e.ClientY + d.nodeY - d.clickY);

                if (!d.node.changed) d.node.changed = true;

                //OnNodeMoved();
            }

            wire_drawWires();
        }
        wire_dragWire(e);//new wiew

        if (lasso.drag)
        {
            lasso.endX = e.OffsetX;
            lasso.endY = e.OffsetY;
        }

    }

    void CreateNodeMoveAction(MouseEventArgs e)
    {
        var moves = new Dictionary<string, MovePoints>(dragElements.Count);
        dragElements.ForEach(d =>
        {
            var x2 = (float)(e.ClientX + d.nodeX - d.clickX);
            var y2 = (float)(e.ClientY + d.nodeY - d.clickY);

            if (d.nodeX == x2 && d.nodeY == y2) return;

            var p = new MovePoints(
                new(d.nodeX, d.nodeY),
                new(x2, y2)
            );
            moves.Add(d.node.Id, p);
        });
        if (moves.Any())
            _nodeEditor.ActionManager.ExecuteAction(new MoveNodesAction(_nodeEditor, moves));
    }

    void StartDragSelectedNodes(MouseEventArgs e)
    {
        StartDragNodes(FlowNodes.Values.Where(s => s.selected), e, false);
    }

    public void StartDragNodes(IEnumerable<Node> nodes, bool startMoveUnderCursor = true)
        => StartDragNodes(nodes, _lastMouseWorkspaceState, startMoveUnderCursor);

    void StartDragNodes(IEnumerable<Node> nodes, MouseEventArgs e, bool startMoveUnderCursor)
    {
        dragElements.Clear();
        float w = 50;
        float h = 40;

        var minX = nodes.Min(s => s.X);
        var minY = nodes.Min(s => s.Y);

        foreach (var _node in nodes)
        {
            //Console.WriteLine($"_node.X={_node.X}, _node.Y={_node.Y}, e.ClientX={e.ClientX}, e.ClientY={e.ClientY}");

            var drag = new DragElement
            {
                node = _node,
                nodeX = startMoveUnderCursor ? (float)(-w + _node.X - minX) : _node.X,
                nodeY = startMoveUnderCursor ? (float)(-h + _node.Y - minY) : _node.Y,
                clickX = startMoveUnderCursor ? 0 : e.ClientX,
                clickY = startMoveUnderCursor ? 0 : e.ClientY,
            };

            dragElements.Add(drag);
        }

        drag = true;
    }

    void onMouseUp(MouseEventArgs e)
    {
        _logger.LogTrace("onMouseUp");
        if (isProcessPasteNewNode)
        {
            _nodeEditor.ActionManager.ExecuteAction(new CreateNodesAction(_nodeEditor, dragElements.Select(s => s.node).ToList()));
            isProcessPasteNewNode = false;
        }
        else if (drag)
        {
            CreateNodeMoveAction(e);
        }
        drag = false;

        new_wire = null;
        if (sel_node != null)
        {
            OnNodeMoved(sel_node);
        }
        if (lasso.drag)
        {
            lasso.drag = false;

            SelectNodesUnderLasso(lasso);
        }
    }
    void onWorkspaceMouseDown(MouseEventArgs e)
    {
        _logger.LogTrace("onWorkspaceMouseDown");
        dragElements.Clear();
        DeselectAll();

        lasso.startX = e.OffsetX;
        lasso.startY = e.OffsetY;
        lasso.endX = e.OffsetX;
        lasso.endY = e.OffsetY;
        lasso.drag = true;
    }

    void onNodeMouseDown(MouseEventArgs e, Node node)
    {
        _logger.LogTrace("onNodeMouseDown");

        bool isCtrlPress = e.CtrlKey;

        if (!isCtrlPress && sel_node is not null)
        {
            sel_node.selected = false;

        }

        if (!isCtrlPress)
        {
            dragElements.Clear();
        }

        if (e.ShiftKey)
        {
            DeselectAll();
            SelectAllNodesInFlow(node);
        }

        sel_node = node;
        node.selected = true;

        StartDragSelectedNodes(e);
        _nodeEditor?.SetSelectContext(typeof(Node));
    }

    void onNodeMouseUp(MouseEventArgs e, Node node)
    {
        _logger.LogTrace("onNodeMouseUp");

        if (new_wire is not null)
        {
            if (!new_wire.IsOutput)
            {
                wire_StartNewEnd(new NodeWirePointEventArgs(e, 0, false, node));
            }
            else
            {
                wire_StartNewEnd(new NodeWirePointEventArgs(e, 0, true, node));
            }
        }

        onMouseUp(e);
    }

    void OnNodeContextMenuEvent(MouseEventArgs e, Node node)
    {
        _logger.LogTrace($"OnNodeContextMenu={e.Button}");
        var a = new NodeClickEventArgs { MouseEvent = e, Node = node };
        OnNodeContextMenu.InvokeAsync(a);
    }

    void OnWireContextMenuEvent(MouseEventArgs e, Wire wire)
    {
        _logger.LogTrace($"OnWireContextMenuEvent={e.Button}");
        var a = new SelWireEventArgs(e, wire.Node1, wire.Node2);
        OnWireContextMenu.InvokeAsync(a);
    }

    void wire_StartNew(NodeWirePointEventArgs arg)
    {
        _logger.LogTrace("wire_StartNew");

        var e = arg.MouseEvent;
        var index = arg.PinIndex;
        var output = !arg.IsInput;
        var node_id = arg.Node.Id;

        new_wire = new NewWire
        {
            X1 = (float)e.OffsetX,
            Y1 = (float)e.OffsetY,
            X2 = (float)e.OffsetX,
            Y2 = (float)e.OffsetY,
            Node1 = (output ? new(node_id, index) : null)!,
            Node2 = (output ? null : new(node_id, index))!,
            IsOutput = output
        };
    }
    void wire_StartNewEnd(NodeWirePointEventArgs arg)
    {
        _logger.LogTrace("wire_StartNewEnd");

        var e = arg.MouseEvent;
        var index = arg.PinIndex;
        var output = !arg.IsInput;
        var node_id = arg.Node.Id;

        bool is_node1_set = new_wire.Node1 != null;

        new_wire.X2 = (float)e.OffsetX;
        new_wire.Y2 = (float)e.OffsetY;

        if (output)
        {
            new_wire.Node1 = new(node_id, index);
        }
        else
        {
            new_wire.Node2 = new(node_id, index);
        }

        //wire with self
        bool is_self = new_wire.Node1 == new_wire.Node2;
        bool same_slot = is_node1_set == output;

        if (!is_self && !same_slot)
        {
            CreateWireAction(new_wire);
        }

        //end
        new_wire = null;
    }

    void nodes_deselectAll()
    {
        if (FlowNodes != null)
        {
            foreach (var node in FlowNodes.Values) node.selected = false;
        }
    }
    void wire_deselect()
    {
        _wires.ForEach(s => s.Selected = false);
    }
    void OnNodeMoved(Node node)
    {
        wire_drawWires(node.Id);
    }

    void wire_drawWires(string? nodeId = null)
    {
        _wires = NodeWireUtil.DrawWires(FlowNodes, _nodeWirePointResolver).ToList();
    }

    public void RedrawWires()
    {
        wire_drawWires();
        StateHasChanged();
    }

    void CreateWireAction(Wire new_wire)
    {
        var isExist = _wires.Exists(s => s.Node1 == new_wire.Node1 && s.Node2 == new_wire.Node2);
        if (!isExist)
        {
            _nodeEditor.ActionManager.ExecuteAction(new CreateWireAction(_nodeEditor, [new(new_wire.Node1, new_wire.Node2)]));
        }
    }

    void wire_dragWire(MouseEventArgs e)
    {
        if (new_wire != null)
        {
            if (new_wire.IsOutput)
            {
                new_wire.X2 = (float)e.OffsetX;
                new_wire.Y2 = (float)e.OffsetY;
            }
            else
            {
                new_wire.X1 = (float)e.OffsetX;
                new_wire.Y1 = (float)e.OffsetY;
            }
        }
    }

    void onWireMouseDown(MouseEventArgs e, Wire wire)
    {
        _logger.LogTrace("onWireMouseDown");
        wire_deselect();
        nodes_deselectAll();
        wire.Selected = true;
        _nodeEditor?.SetSelectContext(typeof(Wire));
    }

    void onWireMouseUp(MouseEventArgs e, Wire wire)
    {
        bool isNewWire = wire == new_wire;
        _logger.LogTrace($"wire_mouseUP isNewWire:{isNewWire}");
        DeselectAll();
    }

    public void DeselectAll()
    {
        wire_deselect();
        nodes_deselectAll();
        new_wire = null;
        sel_node = null;
        _nodeEditor?.SetSelectContext(null);
    }

    public void SelectNode(string nodeId)
    {
        if (FlowNodes.TryGetValue(nodeId, out var node))
        {
            SelectNode(node);
        }
    }

    public void SelectNode(Node node)
    {
        DeselectAll();
        sel_node = node;
        node.selected = true;
        _nodeEditor?.SetSelectContext(node.GetType());
    }

    /// <summary>
    /// on click palette new node
    /// </summary>
    /// <param name="e"></param>
    /// <param name="node">palette clicked node</param>
    /// <param name="instance">new instance</param>
    public async void OnClickPaletteNewNode(MouseEventArgs e, Node node, Node instance)
    {
        DeselectAll();
        isProcessPasteNewNode = true;
        float w = 250;
        float h = 40;

        var scr = await js.HtmlGetElementScroll("#red-ui-workspace-chart");

        _logger.LogTrace($"SCR={scr.X},{scr.Y}");

        instance.X = (float)(-w + node.X + e.ClientX + 120) + scr.X;
        instance.Y = (float)(node.Y + e.ClientY - h - 30) + scr.Y;

        MouseEventArgs m2 = new()
        {
            Type = e.Type,
            AltKey = e.AltKey,
            Button = e.Button,
            Buttons = e.Buttons,
            Detail = e.Detail,
            ShiftKey = e.ShiftKey,

            CtrlKey = e.CtrlKey,
            ClientX = e.ClientX,
            ClientY = e.ClientY,
            OffsetX = e.OffsetX,
            OffsetY = e.OffsetY,
        };

        onNodeMouseDown(m2, instance);
    }

    void onClickNodeEvent(MouseEventArgs e, Node node)
    {
        var a = new NodeClickEventArgs { MouseEvent = e, Node = node };
        OnClickNode.InvokeAsync(a);
    }

    void onDblClickNodeEvent(MouseEventArgs e, Node node)
    {
        var a = new NodeClickEventArgs { MouseEvent = e, Node = node };
        OnDblClickNode.InvokeAsync(a);
    }

    void SelectAllNodesInFlow(Node node)
    {
        var nodes = NodeWireUtil.GetLinkedNodes(node, FlowNodes);
        foreach (var _node in nodes)
        {
            _node.selected = true;
        }
    }

    public enum SelectWiresMode
    {
        Both,
        Outputs,
        Inputs
    }

    class Lasso
    {
        public double startX;
        public double startY;
        public double endX;
        public double endY;
        public bool drag;

        public double drawX => startX < endX ? startX : endX;
        public double drawY => startY < endY ? startY : endY;
        public double width => startX < endX ? (endX - startX) : (startX - endX);
        public double height => startY < endY ? (endY - startY) : (startY - endY);

    }

    void SelectNodesUnderLasso(Lasso lasso)
    {
        Rectangle _lasso = new((int)lasso.drawX, (int)lasso.drawY, (int)lasso.width, (int)lasso.height);

        foreach (var node in FlowNodes.Values)
        {
            Rectangle rect = new((int)node.X, (int)node.Y, 120,/*(int)node.bodyRectHeight*/30);

            if (_lasso.Contains(rect))
            {
                node.selected = true;
            }
        }
    }

    void WorkspaceClick(MouseEventArgs e)
    {
        OnWorkspaceClick.InvokeAsync(e);
    }

    void WorkspaceDblClick(MouseEventArgs e)
    {
        OnWorkspaceDblClick.InvokeAsync(e);
    }

    public void RefreshWires()
    {
        _wires.Clear();
        wire_drawWires();
    }

    public void CallStateHasChanged()
    {
        StateHasChanged();
    }

    [JSInvokable]
    public void OnElementResize(double width, double height)
    {
        Width = (int)width;
        Height = (int)height;
    }
}

class DragElement
{
    public Node node = default!;
    public double clickX;
    public double clickY;
    public float nodeX;
    public float nodeY;
}
