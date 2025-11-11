using System.Reflection;
using Mars.Core.Extensions;
using Mars.Host.Shared.WebSite.Scripts;

namespace Mars.Host.WebSite.Scripts;

public class AppAdminSpaHtmlScripts
{
    public const string AppAdminSpaHtmlScriptsCacheKey = "Mars.AppAdminSpaHtmlScripts";

    public IWebSiteInjectContentPart[] Scripts { get; }
    public IWebSiteInjectContentPart[] HeadStyles { get; }
    public ScriptFileInfo[] DefaultFonts { get; }
    public IReadOnlyCollection<IWebSiteInjectContentPart> AllScripts { get; }

    public string CompiledHeader { get; }
    public string CompiledFooter { get; }

    //public static AppAdminSpaHtmlScripts Instance { get; } = new();

    public AppAdminSpaHtmlScripts()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var appVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        var helperLinksOrder = 1f;
        var fontOrder = 2f;
        var defaultOrder = 5f;
        var interactScriptsOrder = 10f;

        DefaultFonts = [
            new(@"<link rel=""preconnect"" href=""https://fonts.googleapis.com""/>", placeInHead: true, order: helperLinksOrder),
            new(@"<link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin/>" , placeInHead: true, order: helperLinksOrder),
            new(new Uri(@"https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;700&display=swap") , placeInHead: true, order: fontOrder, scriptType: ScriptInfoType.Style),
            new(new Uri(@"https://fonts.googleapis.com/css2?family=Montserrat:wght@400;500;600;700&display=swap") , placeInHead: true, order: fontOrder, scriptType: ScriptInfoType.Style),
            new(new Uri(@"https://fonts.googleapis.com/css2?family=Exo+2:wght@400;600&display=swap") , placeInHead: true, order: fontOrder, scriptType: ScriptInfoType.Style),
        ];

        var headStyles = (ScriptFileInfo[])[
            new(@"<link rel=""preconnect"" href=""https://cdnjs.cloudflare.net"">", placeInHead: true, order: helperLinksOrder),
            new(@"<link rel=""dns-prefetch"" href=""https://cdnjs.cloudflare.net"">", placeInHead: true, order: helperLinksOrder),
            new(new Uri(@"/mars/vendor/bootstrap/bootstrap5.min.css", UriKind.Relative), placeInHead: true, order: defaultOrder),
            //new(@"<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/bootstrap-icons/1.11.3/font/bootstrap-icons.min.css"" integrity=""sha512-dPXYcDub/aeb08c63jRq/k6GaKccl256JQy/AnOq7CAnEZ9FzSL9wSbcZkMp4R26vBsMLFYH4kQ67/bbV8XaCQ=="" crossorigin=""anonymous"" referrerpolicy=""no-referrer"" />", placeInHead: true, order: defaultOrder),
            new(new Uri(@"/mars/vendor/bootstrap-icons/bootstrap-icons.min.css", UriKind.Relative), placeInHead: true, order: defaultOrder),

            //new(new Uri(@"//cdn.quilljs.com/1.3.6/quill.snow.css"), placeInHead: true, order: defaultOrder),
            new(new Uri(@"/mars/vendor/quilljs/quill.snow.css", UriKind.Relative), placeInHead: true, order: defaultOrder),
            //new(new Uri(@"_content/mdimai666.Mars.Nodes.FormEditor/css/style.css", UriKind.Relative), placeInHead: true ,version:appVersion , order: defaultOrder),

            new(new Uri(@"css/style.css", UriKind.Relative), placeInHead: true, version:appVersion, order: defaultOrder),
            new(new Uri(@"AppAdmin.styles.css", UriKind.Relative), placeInHead: true, order: defaultOrder),
        ];

        HeadStyles = headStyles;

        var scripts = (ScriptFileInfo[])[
            //new(new Uri(@"/mars/vendor/jquery-3.6.3.min.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"/mars/vendor/bootstrap/bootstrap5.bundle.min.js", UriKind.Relative), order: defaultOrder),
            new(@"<script src=""_content/Microsoft.FluentUI.AspNetCore.Components/Microsoft.FluentUI.AspNetCore.Components.lib.module.js"" type=""module"" async></script>", order: defaultOrder),
            //new(new Uri(@"https://cdn.quilljs.com/1.3.6/quill.js"), order: defaultOrder),
            new(new Uri(@"/mars/vendor/quilljs/quill.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"_content/Blazored.TextEditor/quill-blot-formatter.min.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"_content/Blazored.TextEditor/Blazored-BlazorQuill.js", UriKind.Relative), order: defaultOrder),

            //new(@"<script src=""https://cdnjs.cloudflare.com/ajax/libs/Sortable/1.15.2/Sortable.min.js"" integrity=""sha512-TelkP3PCMJv+viMWynjKcvLsQzx6dJHvIGhfqzFtZKgAjKM1YPqcwzzDEoTc/BHjf43PcPzTQOjuTr4YdE8lNQ=="" crossorigin=""anonymous"" referrerpolicy=""no-referrer""></script>", order: defaultOrder),
            new(new Uri(@"/mars/vendor/Sortable-1.15.6.min.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"_content/BlazorMonaco/jsInterop.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"/mars/js/emmet-monaco.min.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"/mars/js/language-log.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"/mars/vendor/spotlight/spotlight.bundle.js", UriKind.Relative), order: defaultOrder),
            new(new Uri(@"_content/mdimai666.Mars.AppFront.Main/js/highlight-extensions.js", UriKind.Relative), order: defaultOrder),

            new(new Uri(@"js/scripts.js", UriKind.Relative), version:appVersion, order: interactScriptsOrder),
        ];

        var blazorSpaWasmHtmlScripts = new BlazorSpaWasmHtmlScripts(GetType().Assembly);

        Scripts = [
            ..scripts,
            #if DEBUG
            new InlineBlockJavaScript("window._dev=true;"),
            #endif
            blazorSpaWasmHtmlScripts.BlazorSpaInlineScipt,
            ];

        AllScripts = [.. DefaultFonts, .. HeadStyles, .. Scripts];

        CompiledHeader = AllScripts.Where(s => s.PlaceInHead).OrderBy(s => s.Order).Select(s => s.HtmlContent()).JoinStr("\n\t").Trim();
        CompiledFooter = AllScripts.Where(s => !s.PlaceInHead).OrderBy(s => s.Order).Select(s => s.HtmlContent()).JoinStr("\n\t").Trim();

    }
}
