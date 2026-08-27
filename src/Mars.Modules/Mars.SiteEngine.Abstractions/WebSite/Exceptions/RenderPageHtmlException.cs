using Mars.Host.Shared.WebSite.Models;

namespace Mars.Host.Shared.WebSite.Exceptions;

[Serializable]
public class RenderPageHtmlException : Exception
{
    public WebPage? Page { get; }
    public List<string> Errors { get; }

    public RenderPageHtmlException(WebPage? page, List<string> templatorErrros,
        string? message, Exception inner) : base(message, inner)
    {
        Page = page;
        Errors = templatorErrros;
    }

}
