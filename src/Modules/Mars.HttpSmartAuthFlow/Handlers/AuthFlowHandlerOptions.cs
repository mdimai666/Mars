using System.Net;
using Mars.HttpSmartAuthFlow.Exceptions;

namespace Mars.HttpSmartAuthFlow.Handlers;

public class AuthFlowHandlerOptions
{
    public int MaxRetryAttempts { get; set; } = 2;

    public HashSet<HttpStatusCode> UnauthorizedStatusCodes { get; set; } = new()
    {
        HttpStatusCode.Unauthorized,           // 401
        (HttpStatusCode)407                    // 407 Proxy Authentication Required
    };

    public HashSet<Type> RetryableExceptions { get; set; } = new()
    {
        typeof(HttpRequestException),
        //typeof(TaskCanceledException),
        typeof(IOException),
        //typeof(AuthenticationException)
    };

    //public bool TreatForbiddenAsUnauthorized { get; set; } = false; подумать
}
