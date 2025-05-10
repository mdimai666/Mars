using Microsoft.AspNetCore.Components;

namespace Mars.Nodes.FormEditor;

public class NodeEditForm : ComponentBase, INodeEditForm
{
    [CascadingParameter]
    public NodeEditContainer1 NodeEditContainer { get; set; } = default!;

    public virtual Task OnEditCancel()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnEditDelete()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnEditSave()
    {
        return Task.CompletedTask;
    }
}
