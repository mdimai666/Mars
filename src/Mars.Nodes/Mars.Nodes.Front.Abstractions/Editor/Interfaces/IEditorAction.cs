namespace Mars.Nodes.Front.Abstractions.Editor.Interfaces;

public interface IEditorAction
{
    void Execute();
    bool CanExecute();
}
