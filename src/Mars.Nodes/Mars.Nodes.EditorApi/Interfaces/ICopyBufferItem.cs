namespace Mars.Nodes.EditorApi.Interfaces;

public interface ICopyBufferItem
{
    bool CanPaste();
    void Paste();
}
