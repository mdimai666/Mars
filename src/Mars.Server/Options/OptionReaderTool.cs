namespace Mars.Host.Options;

public static class OptionReaderTool
{
    /// <summary>
    /// Extracts the port number from a list of URLs.
    /// </summary>
    /// <remarks>This method processes multiple URLs provided in the <paramref name="urls"/> parameter,
    /// attempting to extract the port number from the first valid URL. If a URL does not specify a  protocol, "http://"
    /// is assumed. Wildcards ("+","*") in the host portion of the URL are replaced  with "localhost". If no valid URL
    /// is found, the method assigns the default port value of 80.</remarks>
    /// <param name="urls">A string containing one or more URLs separated by semicolons (;) or commas (,). Each URL can optionally include
    /// a protocol (e.g., "http://") or use wildcards ("+","*")  which will be replaced with "localhost".</param>
    /// <param name="port">When the method returns, contains the port number extracted from the first valid URL. If no valid URL is found,
    /// the default value of 80 is assigned.</param>
    /// <returns><see langword="true"/> if a valid port number is successfully extracted from one of the URLs;  otherwise, <see
    /// langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="urls"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
    public static bool ExtractPortFromUrls(string urls, out int port)
    {
        if (string.IsNullOrWhiteSpace(urls))
            throw new ArgumentException("urls is null or empty");

        var parts = urls.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            try
            {
                var uriString = part.Trim();

                // Добавляем схему при отсутствии
                if (!uriString.StartsWith("http://") && !uriString.StartsWith("https://"))
                    uriString = "http://" + uriString;

                // Заменяем + и * на localhost, НО только если это хост, не IPv6
                uriString = uriString
                    .Replace("://+", "://localhost")
                    .Replace("://*", "://localhost");

                var uri = new Uri(uriString);

                port = uri.Port;
                return true;
            }
            catch
            {
                // продолжаем пробовать другие варианты
            }
        }

        port = 80;
        return false;
    }

    /// <summary>
    /// Normalizes a URL from a delimited list of URLs, ensuring it is in a valid format and prefixed with a scheme.
    /// </summary>
    /// <remarks>This method processes a string containing one or more URLs separated by semicolons or commas.
    /// It attempts to normalize the first valid URL by ensuring it has a scheme (e.g., "http://" or "https://") and
    /// replacing wildcard hosts ("+" or "*") with "localhost". If no valid URL is found, the method returns a default
    /// URL of "http://localhost".</remarks>
    /// <param name="urls">A delimited string containing one or more URLs to normalize. Cannot be null, empty, or whitespace.</param>
    /// <param name="url">When the method returns, contains the normalized URL if successful; otherwise, contains "http://localhost".</param>
    /// <returns><see langword="true"/> if a valid URL was successfully normalized; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="urls"/> is null, empty, or consists only of whitespace.</exception>
    public static bool NormalizeUrl(string urls, out string url)
    {
        if (string.IsNullOrWhiteSpace(urls))
            throw new ArgumentException("urls is null or empty");

        var parts = urls.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            try
            {
                var raw = part.Trim();

                if (!raw.StartsWith("http://") && !raw.StartsWith("https://"))
                {
                    raw = "http://" + raw;
                }

                // Заменим + и * только если они не являются частью IPv6
                raw = raw
                    .Replace("://+", "://localhost")
                    .Replace("://*", "://localhost")
                    .Replace("://[::]", "://localhost");

                var uri = new Uri(raw);

                string host = string.IsNullOrWhiteSpace(uri.Host) ? "localhost" : uri.Host;

                // Если это IPv6, оборачиваем в []
                if (uri.HostNameType == UriHostNameType.IPv6 && !host.StartsWith("["))
                {
                    host = $"[{host}]";
                }

                bool isDefaultPort =
                    (uri.Scheme == "http" && uri.Port == 80) ||
                    (uri.Scheme == "https" && uri.Port == 443);

                url = isDefaultPort
                    ? $"{uri.Scheme}://{host}"
                    : $"{uri.Scheme}://{host}:{uri.Port}";

                return true;
            }
            catch
            {
                continue;
            }
        }

        url = "http://localhost"; // По умолчанию
        return false;
    }

}
