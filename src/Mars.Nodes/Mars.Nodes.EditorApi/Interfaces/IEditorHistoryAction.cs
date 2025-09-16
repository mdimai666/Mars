namespace Mars.Nodes.EditorApi.Interfaces;

public interface IEditorHistoryAction : IEditorAction
{
    Guid Guid { get; }
    void Undo();
}
