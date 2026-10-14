using System.Reflection;

namespace Mars.Admin.Framework;

/// <summary>
/// URL lazy-модуля WYSIWYG (Quill) с версией в query (?v=…) — cache-busting по конвенции
/// AppAdminSpaHtmlScripts/ScriptFileInfo. Версия пробрасывается на vendor-ассеты внутри wysiwyg-quill.js.
/// После правок js/css поднимать MarsAppVersion в Directory.Build.props.
/// </summary>
public static class WysiwygQuillAssets
{
    public static string Version { get; } = Uri.EscapeDataString(
        typeof(WysiwygQuillAssets).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0");

    public static string JsModuleUrl { get; } = $"./_content/mdimai666.Mars.Admin.Framework/js/wysiwyg-quill.js?v={Version}";
}
