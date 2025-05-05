using System.Reflection;

namespace Mars.Host.Shared.WebSite.Scripts;

public class BlazorSpaWasmHtmlScripts
{
    public ScriptFileInfo Brotli { get; }
    public InlineBlockJavaScript BlazorSpaInlineScipt => _blazorSpaInlineScipt;
    static InlineBlockJavaScript _blazorSpaInlineScipt = default!;

    public BlazorSpaWasmHtmlScripts(Assembly MarsHostAssembly)
    {
        if (_blazorSpaInlineScipt is null)
        {
            using Stream resource = MarsHostAssembly.GetManifestResourceStream("Mars.Host.Options.BlazorScriptsAppend.html")!;
            using var reader = new StreamReader(resource);
            _blazorSpaInlineScipt = new InlineBlockJavaScript(reader.ReadToEnd());
        }

        //https://github.com/google/brotli
        Brotli = new ScriptFileInfo(new Uri("./mars/js/brotli.decode.min.js"), scriptName: "brotli", version: null, order: 1);

    }
}
