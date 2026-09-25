using System.Reflection;

namespace Mars.CodeCompletion.Front;

/// <summary>
/// URL статики модуля с версией в query (?v=…) — cache-busting по конвенции AiChatAssets.
/// </summary>
public static class CodeCompletionAssets
{
    public static string Version { get; } = Uri.EscapeDataString(
        typeof(CodeCompletionAssets).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0");

    public static string JsModuleUrl { get; } = $"./_content/Mars.CodeCompletion.Front/js/codeCompletion.js?v={Version}";
}
