using Mars.Nodes.Front.Abstractions.Editor.Interfaces;
using Mars.Nodes.Core;
using Microsoft.AspNetCore.Components;

namespace Mars.Nodes.Front.Abstractions.Components.NodeViews;

public abstract class NodeComponentExtendBase : ComponentBase
{
    [CascadingParameter] protected INodeEditorApi _editor { get; set; } = default!;
    [CascadingParameter] protected NodeComponent NodeComponent { get; set; } = default!;

    protected Node _node => NodeComponent.Node;
}
