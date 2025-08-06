namespace EditorJsBlazored.Core;

public class EditorJsConfig
{
    public string EditorID { get; set; } = "editorjs-" + Guid.NewGuid().ToString();


    public bool Autofocus { get; init; }

    public bool ReadOnly { get; init; }

    public bool InlineToolbar { get; init; } = true;

    public string Placeholder { get; set; } = "Let`s write an awesome story!";

    //public EditorJsContent Content { get; set; } = new();

    public bool PrettyJsonOutput { get; set; }

}
