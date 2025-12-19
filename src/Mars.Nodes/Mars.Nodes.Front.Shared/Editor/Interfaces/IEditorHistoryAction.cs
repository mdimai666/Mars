namespace Mars.Nodes.Front.Shared.Editor.Interfaces;

public interface IEditorHistoryAction : IEditorAction
{
    Guid Guid { get; }
    void Undo();
}
