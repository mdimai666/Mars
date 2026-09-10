namespace Mars.Nodes.Front.Abstractions.Editor.Interfaces;

public interface ICopyBufferItem
{
    bool CanPaste();
    void Paste();
}
