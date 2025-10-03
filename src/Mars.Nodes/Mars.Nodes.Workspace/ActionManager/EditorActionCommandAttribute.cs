using Toolbelt.Blazor.HotKeys2;

namespace Mars.Nodes.Workspace.ActionManager;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
internal sealed class EditorActionCommandAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; set; } = "";
    public Hotkey? Hotkey { get; }
    public string AllowEditorContext { get; set; } = "global";

    /// <summary>
    /// create
    /// </summary>
    /// <param name="name"></param>
    /// <param name="hotKey">
    /// should be "Ctrl+Delete" | "KeyA"
    /// <see cref="ModCode"/>[] + <see cref="Code"/>
    /// </param>
    public EditorActionCommandAttribute(string name, string hotKey)
    {
        Name = name;
        Hotkey = ActionManager.Hotkey.Parse(hotKey);
    }

    public EditorActionCommandAttribute(string name)
    {
        Name = name;
    }

}
