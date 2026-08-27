namespace Mars.Nodes.Front.Abstractions.Editor.Interfaces;

public interface IEditorHistoryAction : IEditorAction
{
    Guid Guid { get; }
    void Undo();
}
