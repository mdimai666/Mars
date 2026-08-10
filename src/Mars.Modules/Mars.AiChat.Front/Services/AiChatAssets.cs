using System.Reflection;

namespace Mars.AiChat.Front.Services;

/// <summary>
/// URL статики модуля с версией в query (?v=…) — cache-busting по конвенции
/// AppAdminSpaHtmlScripts/ScriptFileInfo: статика отдаётся без Cache-Control,
/// и без версии в урле браузер может держать старый js/css из эвристического кеша.
/// После правок js/css поднимать MarsAppVersion в Directory.Packages.props.
/// </summary>
public static class AiChatAssets
{
    public static string Version { get; } = Uri.EscapeDataString(
        typeof(AiChatAssets).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0");

    public static string CssUrl { get; } = $"_content/Mars.AiChat.Front/css/mars-aichat.css?v={Version}";

    public static string JsModuleUrl { get; } = $"./_content/Mars.AiChat.Front/js/mars-aichat.js?v={Version}";
}
