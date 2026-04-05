using BlazorMonaco;
using BlazorMonaco.Editor;
using Mars.Nodes.Core;
using Mars.Nodes.Core.Nodes;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;

namespace Mars.Nodes.FormEditor.EditForms;

public partial class FunctionNodeForm
{
    [CascadingParameter] Node Value { get; set; } = default!;
    FunctionNode Node { get => (FunctionNode)Value; set => Value = value; }

    CodeEditor2 editor1 = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    public override async Task OnEditSave()
    {
        Node.Code = await editor1.GetValue();
    }

    void OnSave(string value)
    {
        _ = NodeEditContainer.FormSaveClick();
    }

    public static async Task EditorOnDidInit(StandaloneCodeEditor editor1, NodeFormEditorJsInterop js, NodeEditContainer1 nodeEditContainer1)
    {
        await editor1.AddCommand((int)KeyMod.CtrlCmd | (int)KeyMod.Shift | (int)KeyCode.KeyD, async args =>
        {
            //Console.WriteLine("Ctrl+Shif+D : Initial editor command is triggered.2");

            string actionId = "editor.action.deleteLines";
            //string jsCmd = $"monaco.editor.getEditors()[0].getAction('{actionId}').run()";
            //await jsRuntime.InvokeVoidAsync("window", jsCmd);
            //editor1.ActionCallback();
            await js.Editor_DoAction(actionId);

        });

        await editor1.AddCommand((int)KeyMod.CtrlCmd | (int)KeyCode.KeyD, async args =>
        {
            string actionId = "editor.action.duplicateSelection";
            await js.Editor_DoAction(actionId);

        });

        await editor1.AddAction(new ActionDescriptor
        {
            Id = "mars.editor.save",
            Label = "Save",
            Keybindings = new int[] { (int)KeyMod.CtrlCmd | (int)KeyCode.KeyS },
            Run = (ed) =>
            {
                Console.WriteLine("save action");
                _ = nodeEditContainer1.FormSaveClick();
            }
        });
    }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {

        return new StandaloneEditorConstructionOptions
        {

            AutomaticLayout = true,
            Language = "csharp",
            Value = Node.Code,

        };
    }

}
