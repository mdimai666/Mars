using Mars.Core.Extensions;

namespace Mars.Host.Shared.WebSite.Scripts;

public class AppAdminSpaHtmlScripts
{
    public const string AppAdminSpaHtmlScriptsCacheKey = "Mars.AppAdminSpaHtmlScripts";

    public ScriptFileInfo[] Scripts { get; }
    public ScriptFileInfo[] HeadStyles { get; }
    public ScriptFileInfo[] DefaultFonts { get; }
    public IReadOnlyCollection<ScriptFileInfo> AllScripts { get; }

    public string CompiledHeader { get; }
    public string CompiledFooter { get; }

    public static AppAdminSpaHtmlScripts Instance { get; } = new();

    public AppAdminSpaHtmlScripts()
    {
        var helperLinksOrder = 1f;
        var fontOrder = 2f;
        var defaultOrder = 5f;
        var interactScriptsOrder = 10f;
        var appVersion = new Version(0, 5, 1).ToString();

        DefaultFonts = [
            new(@"<link rel=""preconnect"" href=""https://fonts.googleapis.com""/>", placeInHead: true, order: helperLinksOrder),
            new(@"<link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin/>" , placeInHead: true, order: helperLinksOrder),
            new(new Uri(@"https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;700&display=swap") , placeInHead: true, order: fontOrder),
            new(new Uri(@"https://fonts.googleapis.com/css2?family=Montserrat:wght@400;500;600;700&display=swap") , placeInHead: true, order: fontOrder),
            new(new Uri(@"https://fonts.googleapis.com/css2?family=Exo+2:wght@400;600&display=swap") , placeInHead: true, order: fontOrder),
        ];

        HeadStyles = [
            new(@"<link rel=""preconnect"" href=""https://cdnjs.cloudflare.net"">", placeInHead: true, order: helperLinksOrder),
            new(@"<link rel=""dns-prefetch"" href=""https://cdnjs.cloudflare.net"">", placeInHead: true, order: helperLinksOrder),
            new(new Uri(@"/mars/vendor/bootstrap/bootstrap5.min.css"), placeInHead: true, order: defaultOrder),
            //new(@"<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/bootstrap-icons/1.11.3/font/bootstrap-icons.min.css"" integrity=""sha512-dPXYcDub/aeb08c63jRq/k6GaKccl256JQy/AnOq7CAnEZ9FzSL9wSbcZkMp4R26vBsMLFYH4kQ67/bbV8XaCQ=="" crossorigin=""anonymous"" referrerpolicy=""no-referrer"" />", placeInHead: true, order: defaultOrder),
            new(new Uri(@"/mars/vendor/bootstrap-icons/bootstrap-icons.min.css"), placeInHead: true, order: defaultOrder),

            //new(new Uri(@"//cdn.quilljs.com/1.3.6/quill.snow.css"), placeInHead: true, order: defaultOrder),
            new(new Uri(@"/mars/vendor/quilljs/quill.snow.css"), placeInHead: true, order: defaultOrder),
            new(new Uri(@"_content/mdimai666.Mars.Nodes.FormEditor/css/style.css"), placeInHead: true ,version:appVersion , order: defaultOrder),

            new(new Uri(@"css/style.css"), placeInHead: true, version:appVersion, order: defaultOrder),
            new(new Uri(@"AppAdmin.styles.css"), placeInHead: true, order: defaultOrder),
        ];

        Scripts = [
            new(new Uri(@"/mars/vendor/jquery-3.6.3.min.js"), order: defaultOrder),
            new(new Uri(@"vendor/bootstrap5.bundle.min.js"), order: defaultOrder),
            new(@"<script src=""_content/Microsoft.FluentUI.AspNetCore.Components/Microsoft.FluentUI.AspNetCore.Components.lib.module.js"" type=""module"" async></script>", order: defaultOrder),
            //new(new Uri(@"https://cdn.quilljs.com/1.3.6/quill.js"), order: defaultOrder),
            new(new Uri(@"/mars/vendor/quilljs/quill.js"), order: defaultOrder),
            new(new Uri(@"_content/Blazored.TextEditor/quill-blot-formatter.min.js"), order: defaultOrder),
            new(new Uri(@"_content/Blazored.TextEditor/Blazored-BlazorQuill.js"), order: defaultOrder),

            //new(@"<script src=""https://cdnjs.cloudflare.com/ajax/libs/Sortable/1.15.2/Sortable.min.js"" integrity=""sha512-TelkP3PCMJv+viMWynjKcvLsQzx6dJHvIGhfqzFtZKgAjKM1YPqcwzzDEoTc/BHjf43PcPzTQOjuTr4YdE8lNQ=="" crossorigin=""anonymous"" referrerpolicy=""no-referrer""></script>", order: defaultOrder),
            new(new Uri(@"/mars/vendor/Sortable-1.15.6.min.js"), order: defaultOrder),
            new(new Uri(@"_content/BlazorMonaco/jsInterop.js"), order: defaultOrder),
            new(new Uri(@"_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js"), order: defaultOrder),
            new(new Uri(@"_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js"), order: defaultOrder),
            new(new Uri(@"~/mars/js/emmet-monaco.min.js"), order: defaultOrder),
            new(new Uri(@"~/mars/js/language-log.js"), order: defaultOrder),
            new(new Uri(@"vendor/spotlight/spotlight.bundle.js"), order: defaultOrder),

            new(new Uri(@"js/scripts.js"), version:appVersion, order: interactScriptsOrder),

        ];

        AllScripts = [.. DefaultFonts, .. HeadStyles, .. Scripts];

        CompiledHeader = AllScripts.Where(s => s.PlaceInHead).OrderByDescending(s => s.Order).Select(s => s.HtmlContent()).JoinStr("\n");
        CompiledFooter = AllScripts.Where(s => !s.PlaceInHead).OrderByDescending(s => s.Order).Select(s => s.HtmlContent()).JoinStr("\n");

#if DEBUG
        var devScript = new InlineBlockJavaScript("window._dev=true;");
#endif
    }
}
