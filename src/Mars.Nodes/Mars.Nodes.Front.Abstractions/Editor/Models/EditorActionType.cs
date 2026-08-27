using Mars.Nodes.Front.Shared.Editor.Attributes;

namespace Mars.Nodes.Front.Shared.Editor.Models;

public record EditorActionType
{
    public Type ActionType { get; init; } = default!;
    public EditorActionCommandAttribute? Attr { get; init; }
    public Hotkey? DefaultHotkey => Attr?.Hotkey;
    public Hotkey? UserHotkey { get; init; }
    public Hotkey? ActiveHotkey => UserHotkey ?? DefaultHotkey;

    public EditorActionType(Type actionType, EditorActionCommandAttribute? attr)
    {
        ActionType = actionType;
        Attr = attr;
    }
}
