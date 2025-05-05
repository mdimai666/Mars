using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Mars.Host.Shared.Models;

public class HttpInNodeHttpRequestContext
{
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public HttpContext HttpContext { get; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public HttpCatchRegister HttpCatch { get; }

    public WebClientRequest Request { get; }

    public HttpInNodeHttpRequestContext(HttpContext httpContext, HttpCatchRegister httpCatch)
    {
        HttpContext = httpContext;
        HttpCatch = httpCatch;
        Request = new WebClientRequest(httpContext.Request);
    }
}
