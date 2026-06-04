using System.Text.Json.Serialization;
using Flurl.Http;
using Mars.Core.Extensions;
using Mars.Host.Shared.JsonConverters;
using Microsoft.AspNetCore.Http;
using static Mars.Nodes.Core.Nodes.HttpRequestNode;

namespace Mars.Nodes.Core.Implements.Models;

public class HttpRequestInfo
{
    public HeaderDictionaryWithDefault<string, string> Query { get; }
    public IQueryCollection QueryDict { get; }
    public bool HasFormContentType { get; }
    public string? ContentType { get; }
    public string QueryString { get; }

    [JsonConverter(typeof(PathStringJsonConverter))]
    public PathString Path { get; }
    //public PathString PathBase { get; }
    public HostString Host { get; }
    public bool IsHttps { get; }
    public string Scheme { get; }
    public string Method { get; }

    public ReturnResponseType ReturnResponseType { get; }

    public HeaderDictionaryWithDefault<string, string> Headers { get; }

    public string Url { get; }
    public int StatusCode { get; }
    public bool IsSuccess { get; }
    public HttpResponseInfo? Response { get; }

    public HttpRequestInfo(IFlurlRequest request, IFlurlResponse response, ReturnResponseType returnResponseType)
    {
        // Request properties
        Url = request.Url?.ToString() ?? string.Empty;
        Method = request.Verb?.ToString() ?? "GET";
        StatusCode = response.StatusCode;
        IsSuccess = response.StatusCode >= 200 && response.StatusCode <= 299;

        // Query string and query parameters
        QueryString = request.Url?.Query ?? string.Empty;
        Query = [];
        QueryDict = new QueryCollection();

        if (request.Url?.QueryParams != null)
        {
            foreach (var param in request.Url.QueryParams)
            {
                if (param.Name != null)
                {
                    Query[param.Name] = param.Value?.ToString() ?? string.Empty;
                }
            }
        }

        // Headers
        Headers = request.Headers.ToHeaderDictionary(s => s.Name, s => s.Value, StringComparer.OrdinalIgnoreCase);

        // Content type
        ContentType = request.Content?.Headers.ContentType?.ToString();
        HasFormContentType = ContentType?.Contains("application/x-www-form-urlencoded") == true ||
                             ContentType?.Contains("multipart/form-data") == true;

        // Path and URL components
        var uri = request.Url?.ToUri();
        Path = uri != null ? new PathString(uri.AbsolutePath) : PathString.Empty;
        Host = uri != null ? new HostString(uri.Host, uri.Port) : new HostString();
        Scheme = uri?.Scheme ?? "http";
        IsHttps = string.Equals(Scheme, "https", StringComparison.OrdinalIgnoreCase);

        // Response properties from flurlResponse
        ReturnResponseType = returnResponseType;

        Response = new HttpResponseInfo(response);
    }

    public class HttpResponseInfo
    {
        public int StatusCode { get; }
        public string? StatusDescription { get; }
        public string? ContentType { get; }
        public long? ContentLength { get; }
        public string ContentLengthHuman { get; } = string.Empty;
        public HeaderDictionaryWithDefault<string, string> Headers { get; }
        public string? Content { get; }
        public TimeSpan? Duration { get; }
        public bool IsSuccessStatusCode { get; }
        public string? ReasonPhrase { get; }
        public string? ResponseMessage { get; }

        public HttpResponseInfo()
        {
            Headers = [];
            StatusCode = 0;
            IsSuccessStatusCode = false;
        }

        public HttpResponseInfo(IFlurlResponse response, long? contentLength = null) : this()
        {
            if (response == null) return;

            // Status information
            StatusCode = response.StatusCode;
            StatusDescription = response.StatusCode.ToString();
            IsSuccessStatusCode = response.StatusCode >= 200 && response.StatusCode < 300;
            ReasonPhrase = response.ResponseMessage.ReasonPhrase;

            ContentType = response.ResponseMessage.Content.Headers.ContentType.MediaType;
            ContentLength = contentLength ?? response.ResponseMessage.Content.Headers.ContentLength;
            ContentLengthHuman = ContentLength.HasValue ? ContentLength.Value.ToHumanizedSize() : "0";
            Headers = response.Headers.ToHeaderDictionary(s => s.Name, s => s.Value, StringComparer.OrdinalIgnoreCase);

            // Response message
            ResponseMessage = $"HTTP {(int)StatusCode} {ReasonPhrase}";
        }

        public override string ToString()
        {
            return $"{StatusCode} {ReasonPhrase}";
        }
    }
}
