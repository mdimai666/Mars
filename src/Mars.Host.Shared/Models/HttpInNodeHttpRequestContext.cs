using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Mars.Host.Shared.Models;

public class HttpInNodeHttpRequestContext
{
    [JsonIgnore]
    public HttpContext HttpContext { get; }
    [JsonIgnore]
    public HttpCatchRegister HttpCatch { get; }

    public WebClientRequest Request { get; }

    public HttpInNodeHttpRequestContext(HttpContext httpContext, HttpCatchRegister httpCatch, RouteValueDictionary? routeValues)
    {
        HttpContext = httpContext;
        HttpCatch = httpCatch;
        Request = new WebClientRequest(httpContext.Request, routeValues: routeValues);
    }
}
