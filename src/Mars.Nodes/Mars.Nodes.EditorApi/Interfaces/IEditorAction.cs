namespace Mars.Nodes.EditorApi.Interfaces;

public interface IEditorAction
{
    void Execute();
    bool CanExecute();
}
