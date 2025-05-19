using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Mars.Host.Shared.Models;

public class WebClientRequest
{
    public DictionaryWithDefault<string, string> Query { get; }
    public IQueryCollection QueryDict { get; }
    public bool HasFormContentType { get; }
    public string? ContentType { get; }
    //public RouteValueDictionary RouteValues { get; set; }
    public string QueryString { get; }
    public PathString Path { get; }
    //public PathString PathBase { get; set; }
    public HostString Host { get; }
    public bool IsHttps { get; }
    public string Scheme { get; }
    public string Method { get; }

    [JsonIgnore]
    public IHeaderDictionary Headers;

    public DictionaryWithDefault<string, string?> Cookies;

    public DictionaryWithDefault<string, string>? Form { get; }

    public bool IsMobile { get; }

    public WebClientRequest(HttpRequest req)
    {
        Query = DictionaryWithDefaultExtension.ToDictionary(req.Query
                .Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString()))
                , s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);
        QueryDict = req.Query;
        HasFormContentType = req.HasFormContentType;
        ContentType = req.ContentType;
        //this.RouteValues  = req.RouteValues ;
        QueryString = req.QueryString.ToString();
        Path = req.Path;
        //this.PathBase = req.PathBase;
        Host = req.Host;
        IsHttps = req.IsHttps;
        Scheme = req.Scheme;
        Method = req.Method;
        if (req.HasFormContentType)
        {
            Form = DictionaryWithDefaultExtension.ToDictionary(req.Form, s => s.Key, s => s.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        }

        Headers = req.Headers;
        Cookies = DictionaryWithDefaultExtension.ToDictionary(req.Cookies, s => s.Key, s => s.Value)!;

        if (req.Headers.ContainsKey("User-Agent"))
        {
            var uag = req.Headers["User-Agent"].ToString().ToLower();
            IsMobile = mobileDevices.Any(s => uag.Contains(s));
        }
    }

    public WebClientRequest(Uri url, string method = "GET", string contentType = "text/html", IHeaderDictionary? headers = null, IFormCollection? formCollection = null)
    {
        var query = QueryHelpers.ParseQuery(url.Query);
        Query = DictionaryWithDefaultExtension.ToDictionary(query
                .Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString()))
                , s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);
        QueryDict = new QueryCollection(query);
        HasFormContentType = formCollection?.Any() ?? false;
        ContentType = contentType;
        QueryString = (new QueryString(url.Query)).ToString();
        Path = url.LocalPath; // TODO: check has neeq query
        Host = new HostString(url.Host);
        IsHttps = url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
        Scheme = url.Scheme;
        Method = method;
        if (HasFormContentType)
        {
            Form = DictionaryWithDefaultExtension.ToDictionary(formCollection, s => s.Key, s => s.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        }
        Headers = headers ?? new HeaderDictionary();
        Cookies = new();

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

static class DictionaryWithDefaultExtension // Если сделать экстеншеном сломает всю систему изза IEnumerable<TSource> source
{
    public static DictionaryWithDefault<string, TElement> ToDictionary<TSource, TElement>(IEnumerable<TSource> source, Func<TSource, string> keySelector, Func<TSource, TElement> elementSelector)
    {
        return ToDictionary(source, keySelector, elementSelector, comparer: StringComparer.OrdinalIgnoreCase);
    }
    public static DictionaryWithDefault<TKey, TElement> ToDictionary<TSource, TKey, TElement>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(keySelector, nameof(keySelector));
        ArgumentNullException.ThrowIfNull(elementSelector, nameof(elementSelector));

        int capacity = 0;
        if (source is ICollection<TSource> collection)
        {
            capacity = collection.Count;
            if (capacity == 0)
            {
                return new DictionaryWithDefault<TKey, TElement>(comparer);
            }

            if (collection is TSource[] array)
            {
                return ToDictionary(array, keySelector, elementSelector, comparer);
            }

            if (collection is List<TSource> list)
            {
                return ToDictionary(list, keySelector, elementSelector, comparer);
            }
        }

        DictionaryWithDefault<TKey, TElement> d = new DictionaryWithDefault<TKey, TElement>(capacity, comparer);
        foreach (TSource element in source)
        {
            d.Add(keySelector(element), elementSelector(element));
        }

        return d;
    }

}
