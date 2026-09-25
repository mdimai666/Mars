namespace Mars.SiteEngine.Abstractions.TemplateData;

public static class SiteBaseHref
{
    /// <summary>
    /// Значение &lt;base href&gt; для шаблона: "" (корень) → "/", "/sbn" → "/sbn/".
    /// Trailing slash обязателен — без него браузер считает base файлом и режет последний сегмент.
    /// </summary>
    public static string FromFrontUrl(string? frontUrl)
    {
        if (string.IsNullOrEmpty(frontUrl)) return "/";
        return frontUrl.EndsWith('/') ? frontUrl : frontUrl + "/";
    }
}
