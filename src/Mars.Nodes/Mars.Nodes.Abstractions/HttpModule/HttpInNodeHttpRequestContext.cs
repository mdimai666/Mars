using System.Text.Json.Serialization;
using Mars.Host.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Mars.Nodes.Host.Shared.HttpModule;

public class HttpInNodeHttpRequestContext : IDisposable
{
    [JsonIgnore]
    public HttpContext HttpContext { get; private set; }
    [JsonIgnore]
    public HttpCatchRegister HttpCatch { get; private set; }

    public WebClientRequest Request { get; private set; }

    public HttpInNodeHttpRequestContext(HttpContext httpContext, HttpCatchRegister httpCatch, RouteValueDictionary? routeValues)
    {
        HttpContext = httpContext;
        HttpCatch = httpCatch;
        Request = new WebClientRequest(httpContext.Request, routeValues: routeValues);
    }

    public void Dispose()
    {
        HttpContext = null!;
        HttpCatch = null!;
        Request = null!;
    }
}
