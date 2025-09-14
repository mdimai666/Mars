using Mars.Nodes.Core;

namespace Mars.Nodes.Workspace;

public class EditorActions
{

    Type? _selectContext = null;
    private readonly NodeEditor1 editor;

    public string SelectContextString => _selectContext?.FullName ?? "null";

    public EditorActions(NodeEditor1 editor)
    {
        this.editor = editor;
    }

    public void SetSelectContext(Type? type)
    {
        _selectContext = type;
        editor.CallStateHasChanged();
    }

    public void UserAction_Delete(string[] nodeIds)
    {
        Console.WriteLine("UserAction_Delete");

        var nestedWires = editor.NodeWorkspace1.Wires.Where(wire => nodeIds.Contains(wire.Node1.NodeId) || nodeIds.Contains(wire.Node2.NodeId));

        RemoveWires(nestedWires);

        editor.Nodes.RemoveAll(s => nodeIds.Contains(s.Id));
        editor.RecalcNodes();
        editor.CallStateHasChanged();
    }

    public void UserAction_DeleteSelected()
    {
        Console.WriteLine("UserAction_DeleteSelected");
        var selNodes = editor.NodeWorkspace1.Nodes.Values.Where(s => s.selected);
        var selNodesIds = selNodes.Select(node => node.Id);

        // this method also delete already selected
        var nestedWires = editor.NodeWorkspace1.Wires.Where(wire => selNodesIds.Contains(wire.Node1.NodeId) || selNodesIds.Contains(wire.Node2.NodeId));
        nestedWires.ToList().ForEach(s => s.Selected = true);
        var wires = editor.NodeWorkspace1.Wires.Where(s => s.Selected);

        RemoveWires(wires);

        editor.Nodes.RemoveAll(s => s.selected);
        editor.CallStateHasChanged();
    }

    void RemoveWires(IEnumerable<Wire> wires)
    {
        foreach (var w in wires)
        {
            var i1 = w.Node1;
            var i2 = w.Node2;

            if (editor.NodeWorkspace1.Nodes.TryGetValue(i1, out var node))
            {
                foreach (var wireOutput in node.Wires)
                {
                    wireOutput.Remove(i2);
                }
            }
        }

        editor.NodeWorkspace1.Wires.RemoveAll(s => wires.Contains(s));
    }
}

public enum EEditorActionContext
{
    None,
    NodeWorkspace,
}
