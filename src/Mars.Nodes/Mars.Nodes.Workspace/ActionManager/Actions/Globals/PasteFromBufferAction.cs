using Mars.Nodes.EditorApi.Interfaces;

namespace Mars.Nodes.Workspace.ActionManager.Actions.Globals;

[EditorActionCommand("PasteFromBuffer", "Ctrl+KeyV")]
public class PasteFromBufferAction : IEditorAction
{
    private readonly INodeEditorApi _editor;

    public PasteFromBufferAction(INodeEditorApi editor)
    {
        _editor = editor;
    }

    public bool CanExecute() => _editor.ActionManager.IsHaveCopyBuffer;

    public void Execute()
    {
        _editor.ActionManager.PasteCopiedBuffer();
    }

}
