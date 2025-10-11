using System.Reflection;
using Mars.Core.Extensions;
using Mars.Host.Shared.WebSite.Scripts;

namespace Mars.Host.WebSite.Scripts;

public class BlazorSpaWasmHtmlScripts
{
    public ScriptFileInfo Brotli { get; }
    public InlineRawBlock BlazorSpaInlineScipt => _blazorSpaInlineScipt;
    static InlineRawBlock _blazorSpaInlineScipt = default!;

    public BlazorSpaWasmHtmlScripts(Assembly MarsHostAssembly)
    {
        if (_blazorSpaInlineScipt is null)
        {
            using Stream resource = MarsHostAssembly.GetManifestResourceStream("Mars.Host.Options.BlazorScriptsAppend.html")!;
            using var reader = new StreamReader(resource);
            var identedHtml = reader.ReadToEnd().Split(["\r\n", "\n", "\r"], StringSplitOptions.None).Select(s => '\t' + s).JoinStr("\n").Trim();
            _blazorSpaInlineScipt = new InlineRawBlock(identedHtml);
        }

        //https://github.com/google/brotli
        Brotli = new ScriptFileInfo(new Uri("./mars/js/brotli.decode.min.js", UriKind.RelativeOrAbsolute), scriptName: "brotli", version: null, order: 1);

    }
}
