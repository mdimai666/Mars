namespace Mars.Core.Attributes;

/// <summary>
/// Use {.lang} or {lang} for replace i18n.
/// <code>
/// '/doc/element{.lang}.md' -> '/doc/element.ru.md'
/// </code>
/// </summary>
[System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
public sealed class FunctionApiDocumentAttribute : Attribute
{
    public string Url { get; }

    public const string LangReplaceName = "{lang}";
    public const string DottedLangReplaceName = "{.lang}";

    // This is a positional argument
    public FunctionApiDocumentAttribute(string url)
    {
        Url = url;
    }

    public static string ReplaceLang(string urlTemplate, string lang)
    {
        return urlTemplate.Replace(DottedLangReplaceName, string.IsNullOrEmpty(lang) ? "" : "." + lang)
                                .Replace(LangReplaceName, lang);
    }
}
