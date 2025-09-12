using System.Drawing;
using System.Linq;
using Mars.Core.Extensions;
using Mars.Nodes.Core;
using Mars.Nodes.Workspace.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Mars.Nodes.Workspace;

public partial class NodeWorkspace1
{
    [Inject] ILogger<NodeWorkspace1> _logger { get; set; } = default!;

    [Parameter] public string Style { get; set; } = "";
    [Parameter] public string Class { get; set; } = "";
    [Inject] IJSRuntime JSRuntime { get; set; } = default!;
    NodeWorkspaceJsInterop js = default!;

    IReadOnlyDictionary<string, Node> _nodes { get; set; } = new Dictionary<string, Node>();

    Node? sel_node;
    List<DragElement> dragElements { get; set; } = [];
    bool drag = false;

    [Parameter]
    public IReadOnlyDictionary<string, Node> Nodes
    {
        get => _nodes;
        set
        {
            if (_nodes == value) return;

            string? oldFlowId = _nodes.Values.FirstOrDefault()?.Container;
            string? newFlowId = value.Values.FirstOrDefault()?.Container;

            _nodes = value;

            if (oldFlowId != newFlowId)
            {
                RefreshWires();
            }

            Console.WriteLine("Workspace set Nodes");
            NodesChanged.InvokeAsync(value);
        }
    }

    public NewWire? new_wire = null;
    //public Node new_node = null;

    public List<Wire> Wires { get; set; } = [];

    [Parameter] public EventCallback<IReadOnlyDictionary<string, Node>> NodesChanged { get; set; }

    [Parameter] public EditorActions? EditorActions { get; set; }

    [Parameter] public EventCallback<string> OnInject { get; set; }

    [Parameter] public EventCallback<NodeClickEventArgs> OnClickNode { get; set; }
    [Parameter] public EventCallback<NodeClickEventArgs> OnDblClickNode { get; set; }

    [Parameter] public EventCallback<MouseEventArgs> OnWorkspaceClick { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnWorkspaceDblClick { get; set; }

    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    Lasso lasso = new();

    //--------------------------------------

    protected override void OnInitialized()
    {
        base.OnInitialized();
        js = new NodeWorkspaceJsInterop(JSRuntime);

        //wire_drawWires();
    }

    void onMouseMove(MouseEventArgs e)
    {
        //Console.WriteLine("onMouseMove");
        if (drag)
        {
            foreach (var d in dragElements)
            {
                d.node.X = (float)(e.ClientX + d.nodeX - d.clickX);
                d.node.Y = (float)(e.ClientY + d.nodeY - d.clickY);

                if (!d.node.changed) d.node.changed = true;

                //if (sel_node != null)
                //{
                //    OnNodeMoved(sel_node);
                //}
                OnNodeMoved(d.node);
            }
        }

        wire_dragWire(e);//new wiew

        if (lasso.drag)
        {
            lasso.endX = e.OffsetX;
            lasso.endY = e.OffsetY;
        }

    }
    void onMouseUp(MouseEventArgs e)
    {
        Console.WriteLine("onMouseUp");
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
        Console.WriteLine("onWorkspaceMouseDown");
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

        foreach (var _node in Nodes.Values.Where(s => s.selected))
        {
            //Console.WriteLine("onNodeMouseDown");
            var drag = new DragElement
            {
                node = _node,
                nodeX = _node.X,
                nodeY = _node.Y,
                clickX = e.ClientX,
                clickY = e.ClientY,
            };

            dragElements.Add(drag);
        }

        drag = true;
        EditorActions?.SetSelectContext(typeof(Node));
    }

    void onNodeMouseUp(MouseEventArgs e, Node node)
    {

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

    void wire_StartNew(NodeWirePointEventArgs arg)
    {
        Console.WriteLine("wire_StartNew");

        var e = arg.e;
        var index = arg.pinIndex;
        var output = !arg.isInput;
        var node_id = arg.node.Id;

        new_wire = new NewWire
        {
            X1 = (float)e.OffsetX,
            Y1 = (float)e.OffsetY,
            X2 = (float)e.OffsetX,
            Y2 = (float)e.OffsetY,
            Node1 = (output ? node_id : null)!,
            Node2 = (output ? null : node_id)!,
            Node1Output = output ? index : 0,
            IsOutput = output
        };
    }
    void wire_StartNewEnd(NodeWirePointEventArgs arg)
    {
        Console.WriteLine("wire_StartNewEnd");

        var e = arg.e;
        var index = arg.pinIndex;
        var output = !arg.isInput;
        var node_id = arg.node.Id;

        bool is_node1_set = new_wire.Node1 != null;

        new_wire.X2 = (float)e.OffsetX;
        new_wire.Y2 = (float)e.OffsetY;

        if (output)
        {
            new_wire.Node1 = node_id;
            new_wire.Node1Output = index;
        }
        else
        {
            new_wire.Node2 = node_id;
        }

        //wire with self
        bool is_self = new_wire.Node1 == new_wire.Node2;
        bool same_slot = is_node1_set == output;

        if (!is_self && !same_slot)
        {
            wire_addWire(new_wire);
        }

        //end
        new_wire = null;
    }

    void nodes_deselectAll()
    {
        if (Nodes != null)
        {
            foreach (var node in Nodes.Values) node.selected = false;
        }
    }
    void wire_deselect()
    {
        Wires.ForEach(s => s.Selected = false);
    }
    void OnNodeMoved(Node node)
    {
        wire_drawWires(node.Id);
    }

    void wire_drawWires(string? nodeId = null)
    {
        var nodes_all = Nodes.Values;
        IEnumerable<Node> nodes = nodes_all;

        if (nodeId != null)
        {
            Node snode = Nodes[nodeId];
            var to_update_wires = Wires.Where(s => s.Node2 == nodeId);
            var input_nodes_ids = to_update_wires.Select(s => s.Node1);
            var input_nodes = input_nodes_ids.Select(id => Nodes[id]);

            nodes = input_nodes.Append(snode);

        }

        foreach (var node in nodes)
        {

            // let [x1, y1] = [node.x + 135, node.y + 23]

            var out_index = 0;
            foreach (var wire_one in node.Wires)
            { //outputs

                foreach (var wnode_id in wire_one)
                { // putputs is may be multiple

                    //float x1 = node.X + 135;
                    float x1 = node.X + NodeComponent.CalcBodyWidth(node) + 15f;
                    float y1 = 0;

                    if (node.Wires.Count <= 1)
                    {
                        y1 = node.Y + 23;
                    }
                    else
                    {
                        y1 = node.Y + 16 + out_index * 16;
                    }

                    var node2 = Nodes.GetValueOrDefault(wnode_id);

                    if (node2 != null)
                    {

                        float x2 = node2.X + 8;
                        float y2 = node2.Y + 23;

                        var find_wire = Wires.Find(s => s.Node1 == node.Id
                          && s.Node2 == node2.Id
                          && s.Node1Output == out_index
                        );

                        var is_update = find_wire != null;

                        // console.warn('>>>>is_update');

                        if (!is_update)
                        {

                            var wire = new Wire
                            {
                                Id = Guid.NewGuid().ToString(),
                                X1 = x1,
                                Y1 = y1,
                                X2 = x2,
                                Y2 = y2,
                                Node1 = node.Id,
                                Node2 = node2.Id,
                                Node1Output = out_index
                            };

                            Wires.Add(wire);
                        }
                        else
                        {
                            find_wire.X1 = x1;
                            find_wire.Y1 = y1;
                            find_wire.X2 = x2;
                            find_wire.Y2 = y2;
                        }

                    }
                }
                out_index++;

            }
        }
    }

    void wire_addWire(Wire new_wire)
    {
        var wire = new Wire
        {
            Id = Guid.NewGuid().ToString(),
            X1 = new_wire.X1,
            X2 = new_wire.X2,
            Y1 = new_wire.Y1,
            Y2 = new_wire.Y2,
            Node1 = new_wire.Node1,
            Node2 = new_wire.Node2,
            Node1Output = new_wire.Node1Output
        };

        bool is_exist = Wires.Exists(s => s.Node1 == wire.Node1
            && s.Node2 == wire.Node2
            && s.Node1Output == wire.Node1Output);

        if (!is_exist)
        {
            Node node = Nodes[wire.Node1];

            Wires.Add(wire);
            if (node.Wires == null) node.Wires = [];

            node.Wires[wire.Node1Output].Add(wire.Node2);

            wire_drawWires(node.Id);
        }

        StateHasChanged();
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
        Console.WriteLine("onWireMouseDown");
        wire_deselect();
        nodes_deselectAll();
        wire.Selected = true;
        EditorActions?.SetSelectContext(typeof(Wire));
    }

    void onWireMouseUp(MouseEventArgs e, Wire wire)
    {
        bool isNewWire = wire == new_wire;
        Console.WriteLine($"wire_mouseUP isNewWire:{isNewWire}");
        DeselectAll();
    }

    public void DeselectAll()
    {
        wire_deselect();
        nodes_deselectAll();
        new_wire = null;
        sel_node = null;
        EditorActions?.SetSelectContext(null);
    }

    public void SelectNode(string nodeId)
    {
        if (Nodes.TryGetValue(nodeId, out var node))
        {
            SelectNode(node);
        }
    }

    public void SelectNode(Node node)
    {
        DeselectAll();
        sel_node = node;
        node.selected = true;
        EditorActions?.SetSelectContext(node.GetType());
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
        //new_node = null;
        //Node instance = (Node)Activator.CreateInstance(node.GetType())!;
        //new_node = instance;
        //Nodes.Add(instance);
        //double cx = e.ClientX;
        //double cy = e.ClientY;
        //e.ClientX = 230;
        //e.ClientY = 100;

        //e.ClientX = =e.OffsetX;
        //e.ClientY -= e.OffsetY;

        float w = 250;
        //float h = 57 + 39;
        float h = 40;

        var scr = await js.HtmlGetElementScroll("#red-ui-workspace-chart");

        Console.WriteLine($"SCR={scr.X},{scr.Y}");

        instance.X = (float)(-w + node.X + e.ClientX + 120) + scr.X;
        instance.Y = (float)(node.Y + e.ClientY - h - 30) + scr.Y;

        //Console.WriteLine($"x={e.ClientX}, y={e.ClientY}, offx={e.OffsetX}, offy={e.OffsetY}");

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
        //dragElements.First().nodeY = 0;
        //var d = dragElements.First();

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
        var nodes_ids = GetWiredNodes(node, SelectWiresMode.Both);
        var nodes = nodes_ids.Select(id => Nodes[id]);
        foreach (var _node in nodes)
        {
            _node.selected = true;
        }
    }

    IEnumerable<NodeWire> GetWiredNodes(Node node, SelectWiresMode mode)
    {
        var sel_nodes_ids = new List<NodeWire>() { node.Id };

        //Console.WriteLine(getWiredNodes(node).JoinStr(","));

        _logger.LogDebug("node=" + node.Id);

        var w = _getWiredNodes(node, mode);

        _logger.LogDebug("W0= " + w.Select(s=>s.ToString()).JoinStr(","));

        while (w.Any())
        {
            w = w.Except(sel_nodes_ids).ToList();
            sel_nodes_ids.AddRange(w);
            _logger.LogDebug("0sel_nodes_ids= " + sel_nodes_ids.Select(s => s.ToString()).JoinStr(","));

            _logger.LogDebug("W1= " + w.Select(s => s.ToString()).JoinStr(","));

            var next = w.Select(id => Nodes[id]);

            if (next.Any())
            {
                w = next.Select(_node => _getWiredNodes(_node, mode)).SelectMany(x => x).ToList();
                _logger.LogDebug("W2= " + w.Select(s => s.ToString()).JoinStr(","));

                w = w.Except(sel_nodes_ids).ToList();
            }
            else
            {
                w = Enumerable.Empty<NodeWire>();
            }

            _logger.LogDebug("W= " + w.Select(s => s.ToString()).JoinStr(","));
        }

        _logger.LogDebug("sel_nodes_ids= " + sel_nodes_ids.Select(s => s.ToString()).JoinStr(","));

        return sel_nodes_ids;
    }

    IEnumerable<NodeWire> _getWiredNodesOutput(Node node)
    {
        var output_nodes_ids = node.Wires.SelectMany(s => s);
        return output_nodes_ids;
    }

    IEnumerable<NodeWire> _getWiredNodesInput(Node node)
    {
        //var input_nodes_ids = Nodes.Values.Where(_node => _node.Wires.SelectMany(s => s).Contains(node.Id)).Select(s => s.Id);
        //return input_nodes_ids;
        foreach(var fnode in Nodes.Values)
        {
            foreach(var wires in fnode.Wires)
            {
                foreach(var w in wires)
                {
                    if (w == node.Id)
                    {
                        yield return fnode.Id;
                    }
                }
            }
        }
    }

    IEnumerable<NodeWire> _getWiredNodes(Node node, SelectWiresMode mode)
    {
        return mode switch
        {
            SelectWiresMode.Both => _getWiredNodesOutput(node).Concat(_getWiredNodesInput(node)),
            SelectWiresMode.Inputs => _getWiredNodesInput(node),
            SelectWiresMode.Outputs => _getWiredNodesOutput(node),
            _ => throw new NotImplementedException()
        };
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

        foreach (var node in Nodes.Values)
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
        Wires.Clear();
        wire_drawWires();
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
