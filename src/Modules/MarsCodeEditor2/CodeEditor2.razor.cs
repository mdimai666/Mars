using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorMonaco;
using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MarsCodeEditor2;

public partial class CodeEditor2 : IDisposable
{
    StandaloneCodeEditor editor1 = default!;

    public StandaloneCodeEditor Monaco => editor1!;

    public class Language
    {
        public static readonly string handlebars = "handlebars";
        public static readonly string html = "html";
        public static readonly string js = "js";
        public static readonly string json = "json";
        public static readonly string less = "less";
        public static readonly string css = "css";
        public static readonly string csharp = "csharp";
        public static readonly string sql = "sql";
        public static readonly string log = "log";

        public static readonly string[] Array = { handlebars, html, js, json, less, css, csharp, sql, log };
    }

    [Parameter] public string Value { get; set; } = "";
    [Parameter] public string Lang { get; set; } = CodeEditor2.Language.handlebars;
    [Parameter] public string MonacoCssClass { get; set; } = "flex-fill";
    [Parameter] public string ContainerCssStyle { get; set; } = "height:80vh;border:1px solid #dfdfdf; border-radius:4px;overflow:hidden;";
    [Parameter] public bool HideToolbarComponents { get; set; } = false;

    [Parameter] public EventCallback<string> OnSave { get; set; }
    [Parameter] public EventCallback OnInit { get; set; }

    public static List<Type> ToolbarComponents { get; set; } = new();

    [Inject]
    IJSRuntime JSRuntime { get; set; } = default!;
    MarsCodeEditor2JsInterop js = default!;

    [Parameter]
    public RenderFragment HeaderArea { get; set; } = default!;

    protected override void OnInitialized()
    {
        Console.WriteLine("CodeEditor2.razor");
        base.OnInitialized();
        js = new MarsCodeEditor2JsInterop(JSRuntime);
    }

    async Task EditorOnDidInit()
    {
        await editor1.AddCommand((int)KeyMod.CtrlCmd | (int)KeyMod.Shift | (int)KeyCode.KeyD, async args =>
        {
            //Console.WriteLine("Ctrl+Shif+D : Initial editor command is triggered.2");

            string actionId = "editor.action.deleteLines";
            //string jsCmd = $"monaco.editor.getEditors()[0].getAction('{actionId}').run()";
            //await jsRuntime.InvokeVoidAsync("window", jsCmd);
            //editor1.ActionCallback(actionId);
            await js.Editor_DoAction(editor1.Id, actionId);
        });

        await editor1.AddCommand((int)KeyMod.CtrlCmd | (int)KeyCode.KeyD, async args =>
        {
            string actionId = "editor.action.duplicateSelection";
            //editor1.ActionCallback(actionId);
            await js.Editor_DoAction(editor1.Id, actionId);
        });

        //await editor1.AddCommand((int)KeyMod.Alt | (int)KeyCode.KeyZ, async args =>
        //{
        //    //editor1.GetModel().Result.
        //    //editor1.GetOptions().Result.
        //});

        await editor1.AddAction(new ActionDescriptor
        {
            Id = "mars.editor.save",
            Label = "Save",
            Keybindings = new int[] { (int)KeyMod.CtrlCmd | (int)KeyCode.KeyS },
            Run = async (ed) =>
            {
                Console.WriteLine("save action");
                //_ = nodeEditContainer1.FormSaveClick();
                var val = await ed.GetValue();
                await OnSave.InvokeAsync(val);
            }
        });

        await js.Editor_activateJSextensions(editor1.Id);

        if (Lang == Language.log)
        {
            _ = JSRuntime.InvokeVoidAsync("monaco.editor.setTheme", "logview");
        }

        _ = OnInit.InvokeAsync();
    }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
#if DEBUG
        _ = JSRuntime.InvokeVoidAsync("registerCsharpProvider"); 
#endif

        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = this.Lang,
            //Language = "html",
            Value = this.Value,
        };
    }

    public Task<string> GetValue()
    {
        return editor1?.GetValue()!;
    }

    public async Task SetValue(string value)
    {
        //await SendEditorCode(value, Lang);
        await editor1?.SetValue(value);
    }

    public async Task SetModelLanguage(string language)
    {
        await js.Editor_setModelLanguage(editor1.Id, language);
    }

    public void Dispose()
    {
        editor1?.Dispose();
    }

    public static readonly JsonSerializerOptions SimpleJsonSerializerOptions = new()
    {
        IncludeFields = false,
        MaxDepth = 0,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static readonly JsonSerializerOptions SimpleJsonSerializerOptionsIgnoreReadonly = new()
    {
        IncludeFields = false,
        MaxDepth = 0,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
    };
}
