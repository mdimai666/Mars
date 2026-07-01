using System.Drawing;
using AppFront.Shared.Interfaces;
using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Utils;
using Mars.Nodes.Workspace.ActionManager;
using Mars.Nodes.Workspace.ActionManager.Actions.NodesWorkspace;
using Mars.Nodes.Workspace.Components;
using Mars.Shared.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Mars.Nodes.Workspace.EditorParts;

public partial class NodeWorkspace1 : INodeWorkspaceApi, IResizeObserver, IScrollObserver
{
    [Inject] ILogger<NodeWorkspace1> _logger { get; set; } = default!;

    [Parameter] public string Style { get; set; } = "";
    [Parameter] public string Class { get; set; } = "";
    [Inject] IJSRuntime JSRuntime { get; set; } = default!;
    NodeWorkspaceJsInterop js = default!;

    public event Action<IEnumerable<string>> OnDragNodesStarted = default!;
    public event Action<IEnumerable<string>> OnDragNodesEnded = default!;

    [CascadingParameter] INodeEditorApi _nodeEditor { get; set; } = default!;

    ElementReference _containerRef = default!;
    ElementReference _svgContainerRef = default!;

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public ScrollInfo ScrollInfo { get; private set; }

    IReadOnlyDictionary<string, Node> _flowNodes { get; set; } = new Dictionary<string, Node>();

    Node? _sel_node;
    List<DragElement> _dragElements { get; set; } = [];
    HashSet<Node> _allNodesInTheDragBundle = [];
    bool _drag;
    bool _isProcessPasteNewNode;

    [Parameter]
    public IReadOnlyDictionary<string, Node> FlowNodes
    {
        get => _flowNodes;
        set
        {
            if (_flowNodes == value) return;
            _flowNodes = value;
            FlowNodesChanged.InvokeAsync(value);
            RecreateWires();
        }
    }

    public NewWire? new_wire;

    /// <summary>
    /// Wires[NodeId, NodeWires]
    /// </summary>
    Dictionary<string, NodeWiresInfo> _nodeWires { get; set; } = [];
    public IEnumerable<Wire> Wires => _nodeWires.Values.SelectMany(s => s.wires.Values);
    public IReadOnlyCollection<Node> SelectedNodes() => _flowNodes.Values.Where(s => s.selected).ToList();
    public IReadOnlyCollection<Wire> SelectedWires() => _nodeWires.Values.SelectMany(s => s.wires.Values).Where(s => s.Selected).ToList();

    [Parameter] public EventCallback<IReadOnlyDictionary<string, Node>> FlowNodesChanged { get; set; }

    [Parameter] public EditorActionManager? ActionManager { get; set; }

    [Parameter] public EventCallback<string> OnInject { get; set; }

    [Parameter] public EventCallback<NodeClickEventArgs> OnClickNode { get; set; }
    [Parameter] public EventCallback<NodeClickEventArgs> OnDblClickNode { get; set; }
    [Parameter] public EventCallback<NodeClickEventArgs> OnNodeContextMenu { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnWorkspaceClick { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnWorkspaceDblClick { get; set; }
    [Parameter] public EventCallback<SelWireEventArgs> OnWireContextMenu { get; set; }

    [Parameter] public EventCallback<MouseEventArgs> OnMouseEnter { get; set; }

    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    Lasso lasso = new();

    MouseEventArgs _lastMouseWorkspaceState = new();

    public MouseEventArgs LastMouseWorkspaceState => _lastMouseWorkspaceState;

    NodeWirePointResolver _nodeWirePointResolver = new();
    private DotNetObjectReference<IResizeObserver> _dotNetRef = default!;
    private DotNetObjectReference<IScrollObserver> _dotNetRef2 = default!;
    float containerOffsetX = 48;
    float containerOffsetY = 40;
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
            await js.ObserveSizeAsync(_containerRef, _dotNetRef);

            _dotNetRef2 = DotNetObjectReference.Create<IScrollObserver>(this);
            await js.ObserveScrollAsync(_containerRef, _dotNetRef2);
        }
    }

    void onMouseMove(MouseEventArgs e)
    {
        _lastMouseWorkspaceState = e;
        if (_drag)
        {
            foreach (var d in _dragElements)
            {
                d.node.X = (float)(e.ClientX + d.nodeX - d.clickX + ScrollInfo.ScrollLeft);
                d.node.Y = (float)(e.ClientY + d.nodeY - d.clickY + ScrollInfo.ScrollTop);

                //Console.WriteLine($"e.ClientY={e.ClientY}, d.nodeY={d.nodeY}, d.clickY={d.clickY}, ScrollInfo.ScrollTop={ScrollInfo.ScrollTop}");

                if (!d.node.changed) d.node.changed = true;
            }

            MoveWiresForDragElements();
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
        var moves = new Dictionary<string, MovePoints>(_dragElements.Count);
        _dragElements.ForEach(d =>
        {
            var x2 = (float)(e.ClientX + d.nodeX - d.clickX + ScrollInfo.ScrollLeft);
            var y2 = (float)(e.ClientY + d.nodeY - d.clickY + ScrollInfo.ScrollTop);

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

    public void StartDragNodes(IEnumerable<Node> nodes, bool startMoveUnderCursor = false, float offsetX = 0, float offsetY = 0)
        => StartDragNodes(nodes, _lastMouseWorkspaceState, startMoveUnderCursor, offsetX, offsetY);

    /// <summary>
    /// Начать тащить ноды
    /// </summary>
    /// <param name="startMoveUnderCursor">Это если истина перемещается под мышку, если нет относительно где был</param>
    void StartDragNodes(IEnumerable<Node> nodes, MouseEventArgs e, bool startMoveUnderCursor, float offsetX = 0, float offsetY = 0)
    {
        _dragElements.Clear();
        _allNodesInTheDragBundle.Clear();

        var minX = nodes.Min(s => s.X);
        var minY = nodes.Min(s => s.Y);

        foreach (var _node in nodes)
        {
            //Console.WriteLine($"_node.X={_node.X}, _node.Y={_node.Y}, e.ClientX={e.ClientX}, e.ClientY={e.ClientY}");

            var drag = new DragElement
            {
                node = _node,
                nodeX = startMoveUnderCursor ? (float)(-containerOffsetX + _node.X - minX + ScrollInfo.ScrollLeft) : _node.X,
                nodeY = startMoveUnderCursor ? (float)(-containerOffsetY + _node.Y - minY + ScrollInfo.ScrollTop) : _node.Y,
                clickX = (startMoveUnderCursor ? offsetX : e.ClientX + offsetX) + ScrollInfo.ScrollLeft,
                clickY = (startMoveUnderCursor ? offsetY : e.ClientY + offsetY) + ScrollInfo.ScrollTop,
            };

            _dragElements.Add(drag);
        }

        _allNodesInTheDragBundle = nodes.SelectMany(s => NodeWireUtil.GetInputNodes(s, FlowNodes)).ToHashSet();
        _drag = true;
        OnDragNodesStarted?.Invoke(_dragElements.Select(s => s.node.Id));
    }

    void onMouseUp(MouseEventArgs e)
    {
        _logger.LogTrace("onMouseUp");
        if (_isProcessPasteNewNode)
        {
            _nodeEditor.ActionManager.ExecuteAction(new CreateNodesAction(_nodeEditor, _dragElements.Select(s => s.node).ToList()));
            _isProcessPasteNewNode = false;
        }
        else if (_drag)
        {
            CreateNodeMoveAction(e);
            OnDragNodesEnded?.Invoke(_dragElements.Select(s => s.node.Id));
        }
        _drag = false;

        new_wire = null;
        if (_sel_node != null)
        {
            OnNodeMoved(_sel_node);
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
        _dragElements.Clear();
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

        if (!isCtrlPress && _sel_node is not null)
        {
            _sel_node.selected = false;

        }

        if (!isCtrlPress)
        {
            _dragElements.Clear();
        }

        if (e.ShiftKey)
        {
            DeselectAll();
            SelectAllNodesInFlow(node);
        }

        _sel_node = node;
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
        bool is_together_linkNodes = _flowNodes[new_wire.Node1.NodeId].IsLinkNode && _flowNodes[new_wire.Node2.NodeId].IsLinkNode;

        if (!is_self && !same_slot && !is_together_linkNodes)
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
        foreach (var n in _nodeWires.Values)
        {
            foreach (var w in n.wires.Values)
            {
                w.Selected = false;
            }
        }
    }
    void OnNodeMoved(Node node)
    {
    }

    void RecreateWires()
    {
        //_nodeEditor?.AddDebugMessage(DebugMessage.ConsoleMessage(">RecreateWires"));
        _logger.LogTrace(">RecreateWires");
        if (FlowNodes is null || FlowNodes.None())
        {
            _nodeWires = [];
            return;
        }

        _nodeWires = NodeWireUtil.DrawWires(FlowNodes, _nodeWirePointResolver)
                                    .GroupBy(s => s.Node1.NodeId)
                                    .ToDictionary(s => s.Key, s => new NodeWiresInfo
                                    {
                                        node = FlowNodes[s.Key],
                                        wires = s.ToDictionary(w => (w.Node1, w.Node2), w => w)
                                    });
    }

    void MoveWiresForDragElements()
    {
        foreach (var d in _dragElements)
        {
            if (_nodeWires.TryGetValue(d.node.Id, out var nodeWires))
            {
                NodeWireUtil.UpdateWiresPosition(nodeWires.node, nodeWires.wires, FlowNodes, _nodeWirePointResolver);
            }
        }

        foreach (var d in _allNodesInTheDragBundle)
        {
            if (_nodeWires.TryGetValue(d.Id, out var nodeWires))
            {
                NodeWireUtil.UpdateWiresPosition(nodeWires.node, nodeWires.wires, FlowNodes, _nodeWirePointResolver);
            }
        }
    }

    public void RedrawWires()
    {
        RecreateWires();
    }

    void CreateWireAction(Wire new_wire)
    {
        var isExist = _nodeWires.GetValueOrDefault(new_wire.Node1.NodeId)?.wires.ContainsKey((new_wire.Node1, new_wire.Node2)) ?? false;
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
        _sel_node = null;
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
        _sel_node = node;
        node.selected = true;
        _nodeEditor?.SetSelectContext(node.GetType());
    }

    public void ScrollTo(float x, float y)
    {
        js.ScrollToCoordinates(_containerRef, x, y);
    }

    /// <summary>
    /// on click palette new node
    /// </summary>
    /// <param name="e"></param>
    /// <param name="clickedPaletteNode">palette clicked node</param>
    /// <param name="instance">new instance</param>
    public void OnClickPaletteNewNode(MouseEventArgs e, Node clickedPaletteNode, Node instance)
    {
        DeselectAll();
        _isProcessPasteNewNode = true;

        //_logger.LogTrace($"SCR={ScrollInfo.ScrollLeft},{ScrollInfo.ScrollTop}");

        //_containerRef.getBoundingClientRect().x = 48
        //_containerRef.getBoundingClientRect().y = 40

        instance.X = (float)(e.PageX - containerOffsetX - e.OffsetX + ScrollInfo.ScrollLeft);
        instance.Y = (float)(e.PageY - containerOffsetY - e.OffsetY + ScrollInfo.ScrollTop);

        _nodeEditor?.SetSelectContext(typeof(Node));
        StartDragNodes([instance], startMoveUnderCursor: true, offsetX: (float)e.OffsetX - 8, offsetY: (float)e.OffsetY);
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

    [JSInvokable]
    public void OnElementResize(double width, double height)
    {
        Width = (int)width;
        Height = (int)height;
    }

    [JSInvokable]
    public void OnElementScroll(int scrollTop, int scrollLeft, int scrollHeight, int clientHeight, int scrollWidth, int clientWidth)
    {
        ScrollInfo = new ScrollInfo
        {
            ScrollTop = scrollTop,
            ScrollLeft = scrollLeft,
            ScrollHeight = scrollHeight,
            ClientHeight = clientHeight,
            ScrollWidth = scrollWidth,
            ClientWidth = clientWidth
        };

        //Console.WriteLine($"ScrollInfo: Top={ScrollInfo.ScrollTop}, Left={ScrollInfo.ScrollLeft}, Height={ScrollInfo.ScrollHeight}, ClientHeight={ScrollInfo.ClientHeight}, Width={ScrollInfo.ScrollWidth}, ClientWidth={ScrollInfo.ClientWidth}");
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

class NodeWiresInfo
{
    public required Node node;
    public required Dictionary<(NodeWire, NodeWire), Wire> wires;
}
