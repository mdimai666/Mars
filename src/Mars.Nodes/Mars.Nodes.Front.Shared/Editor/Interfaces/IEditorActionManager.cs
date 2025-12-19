namespace Mars.Nodes.Front.Shared.Editor.Interfaces;

public interface IEditorActionManager
{
    IEnumerable<Type> GetAllActionTypes();
    void ExecuteAction<TAction>() where TAction : IEditorAction;
    void ExecuteAction<TAction>(bool addToHistory) where TAction : IEditorAction;
    void ExecuteAction(Type actionType, bool addToHistory = true);
    void ExecuteAction(IEditorAction actionInstance, bool addToHistory = true);
    void Undo();
    void Redo();

    bool CanUndo { get; }
    bool CanRedo { get; }
    bool IsHaveCopyBuffer { get; }
    void SetCopyBuffer(ICopyBufferItem copyBufferItem);
    void PasteCopiedBuffer();
}
