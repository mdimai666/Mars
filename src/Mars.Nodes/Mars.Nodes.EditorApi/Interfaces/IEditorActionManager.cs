namespace Mars.Nodes.EditorApi.Interfaces;

public interface IEditorActionManager
{
    IEnumerable<Type> GetAllActionTypes();
    void ExecuteAction<TAction>() where TAction : IEditorAction;
    void ExecuteAction(Type actionType);
    void ExecuteAction(IEditorAction actionInstance);
    void Undo();
    void Redo();
    bool CanUndo { get; }
    bool CanRedo { get; }
}
