using Mars.HttpSmartAuthFlow.Strategies;

namespace Mars.HttpSmartAuthFlow.Handlers;

public class AuthFlowHandlerContext
{
    public HttpRequestMessage Request { get; set; } = null!;
    public IAuthStrategy Strategy { get; set; } = null!;
    public int Attempt { get; set; }
    public int MaxAttempts { get; set; }
    public HttpResponseMessage? LastResponse { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public string ConfigId { get; set; } = string.Empty;
}
