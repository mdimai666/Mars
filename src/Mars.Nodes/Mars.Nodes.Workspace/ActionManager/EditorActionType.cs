namespace Mars.Nodes.Workspace.ActionManager;

internal record EditorActionType
{
    public Type ActionType { get; init; } = default!;
    public EditorActionCommandAttribute Attr { get; init; }
    public Hotkey? DefaultHotkey => Attr.Hotkey;
    public Hotkey? UserHotkey { get; init; }
    public Hotkey? ActiveHotkey => UserHotkey ?? DefaultHotkey;

    public EditorActionType(Type actionType, EditorActionCommandAttribute attr)
    {
        ActionType = actionType;
        Attr = attr;
    }
}
