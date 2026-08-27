using System.Text.Json.Serialization;
using Mars.Host.Shared.JsonConverters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Mars.Host.Shared.Models;

public class WebClientRequest
{
    public HeaderDictionaryWithDefault<string, string> Query { get; }
    public IQueryCollection QueryDict { get; }
    public bool HasFormContentType { get; }
    public string? ContentType { get; }
    public string QueryString { get; }

    [JsonConverter(typeof(PathStringJsonConverter))]
    public PathString Path { get; }
    //public PathString PathBase { get; set; }
    public HostString Host { get; }
    public bool IsHttps { get; }
    public string Scheme { get; }
    public string Method { get; }

    [JsonIgnore]
    public IHeaderDictionary Headers;

    public HeaderDictionaryWithDefault<string, string?> Cookies;

    public HeaderDictionaryWithDefault<string, string>? Form { get; }
    public HeaderDictionaryWithDefault<string, object?>? RouteValues { get; }

    public bool IsMobile { get; }

    [JsonIgnore]
    public IDictionary<object, object?> Items = new Dictionary<object, object?>();

    public WebClientRequest(HttpRequest req, PathString? replacePath = null, IReadOnlyDictionary<string, object?>? routeValues = null, string? replaceQueryString = null)
    {
        if (replaceQueryString is not null)
        {
            var querystringDict = QueryHelpers.ParseQuery(replaceQueryString);
            QueryString = replaceQueryString;
            Query = querystringDict.ToHeaderDictionary(s => s.Key, s => s.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            QueryDict = new QueryCollection(querystringDict);
        }
        else
        {
            QueryString = req.QueryString.ToString();
            Query = req.Query.ToHeaderDictionary(s => s.Key, s => s.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            QueryDict = new QueryCollection(req.Query.ToDictionary(q => q.Key, q => q.Value));
        }

        HasFormContentType = req.HasFormContentType;
        ContentType = req.ContentType;
        //this.RouteValues  = req.RouteValues ;
        Path = replacePath ?? req.Path;
        //this.PathBase = req.PathBase;
        Host = req.Host;
        IsHttps = req.IsHttps;
        Scheme = req.Scheme;
        Method = req.Method;
        if (req.HasFormContentType)
        {
            Form = req.Form.ToHeaderDictionary(s => s.Key, s => s.Value.ToString());
        }

        Headers = new HeaderDictionary(req.Headers.ToDictionary(h => h.Key, h => h.Value));
        Cookies = req.Cookies.ToHeaderDictionary(s => s.Key, s => s.Value)!;

        if (req.Headers.ContainsKey("User-Agent"))
        {
            var uag = req.Headers["User-Agent"].ToString().ToLower();
            IsMobile = mobileDevices.Any(s => uag.Contains(s));
        }
        if (routeValues is not null)
        {
            RouteValues = routeValues.ToDefaultObjectDictionary(s => s.Key, s => s.Value);
        }
    }

    public WebClientRequest(Uri url, string method = "GET", string contentType = "text/html", IHeaderDictionary? headers = null, IFormCollection? formCollection = null)
    {
        var query = QueryHelpers.ParseQuery(url.Query);
        //Query = DictionaryWithDefaultExtension.ToDictionary(query
        //        .Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString()))
        //        , s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);
        Query = query.ToHeaderDictionary(s => s.Key, s => s.Value.ToString());
        QueryDict = new QueryCollection(query);
        HasFormContentType = formCollection?.Any() ?? false;
        ContentType = contentType;
        QueryString = (new QueryString(url.Query)).ToString();
        Path = url.LocalPath;
        Host = new HostString(url.Host);
        IsHttps = url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
        Scheme = url.Scheme;
        Method = method;
        if (HasFormContentType)
        {
            Form = formCollection!.ToHeaderDictionary(s => s.Key, s => s.Value.ToString());
        }
        Headers = headers ?? new HeaderDictionary();
        Cookies = [];

        if (Headers.ContainsKey("User-Agent"))
        {
            var uag = Headers["User-Agent"].ToString().ToLower();
            IsMobile = mobileDevices.Any(s => uag.Contains(s));
        }
    }

    public string FullUrl(string path)
    {
        return $"{Scheme}://{Host.Value}/{path.TrimStart('/')}";
        //#if DEBUG
        //#else
        //        return $"https://stroylogic.itpolza.com:81/{path.TrimStart('/')}";//TODO: решить
        //#endif
    }

    private static string[] mobileDevices = ["iphone","ppc",
                                            "windows ce","blackberry",
                                            "opera mini","mobile","palm",
                                            "portable","opera mobi" ];
}
