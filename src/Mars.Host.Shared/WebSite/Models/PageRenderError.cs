namespace Mars.Host.Shared.WebSite.Models;

public class PageRenderError
{
    public string Message { get; init; }
    public string? StackTrace { get; init; }

    public PageRenderError(string message, string? stackTrace = null)
    {
        Message = message;
        StackTrace = stackTrace;
    }

    public PageRenderError(Exception exception) : this(exception.Message, exception.StackTrace) { }

    public override string ToString() => Format();

    public string Format(bool htmlFormat = true)
    {
        if (string.IsNullOrEmpty(StackTrace))
            return Message;

        if (!htmlFormat)
            return Message + "\n" + StackTrace;
        else
            return $"<b>{Message.ReplaceLineEndings("<br/>")}</b>:<br><pre>StackTrace:\n{StackTrace.ReplaceLineEndings("<br/>")}</pre>";
    }
}
