using System.Net;

namespace Mars.CommandLine.Remote;

/// <summary>
/// Разбор Kestrel-адресов (Urls / ASPNETCORE_URLS) для явного переббиндинга:
/// если у Kestrel есть хотя бы один явный Listen-эндпоинт (наш UDS), настройка Urls
/// игнорируется — HTTP-адреса приходится добавлять вручную.
/// Семантика совпадает с OptionReaderTool.ExtractPortFromUrls: ';' и ',' как разделители,
/// схема по умолчанию http, '+'/'*' как wildcard.
/// </summary>
public static class CliUrlsParser
{
    public static CliUrlsPlan Parse(string? urls)
    {
        var endpoints = new List<CliUrlEndpoint>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(urls))
            return new CliUrlsPlan { Endpoints = endpoints, Warnings = warnings };

        foreach (var raw in urls.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var uriString = raw;
            if (!uriString.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !uriString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                uriString = "http://" + uriString;
            }

            // Uri не принимает '*' и '+' как хост — запоминаем wildcard до парсинга
            // (та же нормализация, что в OptionReaderTool.ExtractPortFromUrls)
            var isWildcard = false;
            if (uriString.Contains("://*") || uriString.Contains("://+"))
            {
                isWildcard = true;
                uriString = uriString.Replace("://*", "://localhost").Replace("://+", "://localhost");
            }

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
            {
                warnings.Add($"cannot parse url '{raw}' — skipped");
                continue;
            }

            if (uri.Scheme != Uri.UriSchemeHttp)
            {
                warnings.Add($"'{raw}': https-адрес не переббиндится вместе с CLI-сокетом — пропущен (запустите с --no-uds, если нужен https)");
                continue;
            }

            var port = uri.Port; // Uri подставляет дефолтный порт схемы (80), если порт не указан
            var host = uri.Host;

            if (isWildcard || host is "*" or "+")
            {
                endpoints.Add(new CliUrlEndpoint { RawUrl = raw, Port = port, Kind = CliUrlHostKind.Any });
            }
            else if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                endpoints.Add(new CliUrlEndpoint { RawUrl = raw, Port = port, Kind = CliUrlHostKind.Localhost });
            }
            else if (IPAddress.TryParse(host.Trim('[', ']'), out var ip))
            {
                endpoints.Add(new CliUrlEndpoint { RawUrl = raw, Port = port, Kind = CliUrlHostKind.Ip, Ip = ip });
            }
            else
            {
                warnings.Add($"'{raw}': hostname '{host}' привязан ко всем интерфейсам");
                endpoints.Add(new CliUrlEndpoint { RawUrl = raw, Port = port, Kind = CliUrlHostKind.Any });
            }
        }

        return new CliUrlsPlan { Endpoints = endpoints, Warnings = warnings };
    }
}

public enum CliUrlHostKind
{
    Localhost,
    Any,
    Ip,
}

public sealed record CliUrlEndpoint
{
    public required string RawUrl { get; init; }
    public required int Port { get; init; }
    public required CliUrlHostKind Kind { get; init; }
    public IPAddress? Ip { get; init; }
}

public sealed class CliUrlsPlan
{
    public IReadOnlyList<CliUrlEndpoint> Endpoints { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
