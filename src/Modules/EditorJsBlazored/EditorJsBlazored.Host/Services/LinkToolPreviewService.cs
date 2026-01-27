using System.Net;
using System.Net.Sockets;
using EditorJsBlazored.Host.Dto;
using HtmlAgilityPack;

namespace EditorJsBlazored.Host.Services;

internal class LinkToolPreviewService : ILinkToolPreviewService
{
    private readonly HttpClient _client;

    public LinkToolPreviewService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("EditorJsLinkTool");
    }

    public async Task<LinkToolPreviewResult?> GetPreviewAsync(
        string url,
        CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        if (!IsAllowedUri(uri))
            return null;

        string html;
        try
        {
            html = await _client.GetStringAsync(uri, ct);
        }
        catch
        {
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        string? GetMeta(string name)
            => doc.DocumentNode
                .SelectSingleNode($"//meta[@property='{name}']|//meta[@name='{name}']")
                ?.GetAttributeValue("content", null!);

        string? ResolveUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (Uri.TryCreate(value, UriKind.Absolute, out var abs))
                return abs.ToString();

            return new Uri(uri, value).ToString();
        }

        var title =
            GetMeta("og:title") ??
            doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();

        var description =
            GetMeta("og:description") ??
            GetMeta("description");

        var imageUrl =
            ResolveUrl(GetMeta("og:image"));

        return new LinkToolPreviewResult
        {
            Meta = new()
            {
                Title = title,
                Description = description,
                Image = imageUrl != null ? new() { Url = imageUrl } : null
            }
        };
    }

    // ================= SSRF =================

    private static bool IsAllowedUri(Uri uri)
    {
        if (uri.Scheme is not ("http" or "https"))
            return false;

        if (uri.IsLoopback)
            return false;

        if (IPAddress.TryParse(uri.Host, out var ip))
            return IsPublicIp(ip);

        try
        {
            var addresses = Dns.GetHostAddresses(uri.Host);
            return addresses.All(IsPublicIp);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPublicIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return false;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10) return false;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) return false;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return false;

            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254) return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6Multicast || ip.IsIPv6SiteLocal)
                return false;
        }

        return true;
    }
}
