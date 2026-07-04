using Mars.Nodes.Core;
using Mars.Nodes.Front.Shared.Editor.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Mars.Nodes.Front.Shared.Components.NodeViews;

public abstract class NodeComponentExtendBase : ComponentBase
{
    [CascadingParameter] protected INodeEditorApi _editor { get; set; } = default!;
    [CascadingParameter] protected NodeComponent NodeComponent { get; set; } = default!;

    protected Node _node => NodeComponent.Node;
}
