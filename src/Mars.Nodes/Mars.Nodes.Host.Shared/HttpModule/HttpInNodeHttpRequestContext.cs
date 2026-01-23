using System.Text.Json.Serialization;
using Mars.Host.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Mars.Nodes.Host.Shared.HttpModule;

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
