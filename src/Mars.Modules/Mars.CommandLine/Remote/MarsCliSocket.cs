using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Mars.CommandLine.Remote;

/// <summary>
/// Unix domain socket — канал между CLI-вызовом и запущенным инстансом Mars.
/// Путь сокета детерминирован (cwd + конфиг), поэтому CLI и сервер находят друг друга без pid-файла:
/// живой слушатель отвечает на /_cli/ping, stale-файл от упавшего процесса отвечает ConnectionRefused.
/// </summary>
public static class MarsCliSocket
{
    public const int ProtocolVersion = 1;
    public const string SocketFileNamePrefix = "mars-";
    public const string PingPath = "/_cli/ping";
    public const string ExecPath = "/_cli/exec";

    static readonly Lazy<bool> _supportsUnixDomainSockets = new(() =>
    {
        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            return true;
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or SocketException)
        {
            return false;
        }
    });

    /// <summary>Проверено созданием AF_UNIX-сокета: Linux/macOS и Windows 10 1803+.</summary>
    public static bool SupportsUnixDomainSockets => _supportsUnixDomainSockets.Value;

    /// <summary>Живой инстанс Mars для этой директории, если он обнаружен probe'ом до сборки приложения.</summary>
    public static CliServerInfo? RunningServer { get; private set; }

    /// <summary>
    /// Дешёвый UDS-ping до сборки приложения: есть ли уже запущенный Mars для этой директории?
    /// Результат сохраняется в <see cref="RunningServer"/>. Тонкому клиенту файловый лог всё равно
    /// не нужен, а файл удерживает сервер (FileShare.Read) — флаг позволяет пропустить его до Build.
    /// </summary>
    public static Task DetectRunningServerAsync(string[]? args = null)
        => DetectRunningServerAsync(GetSocketPath(args));

    public static async Task DetectRunningServerAsync(string socketPath)
    {
        if (!SupportsUnixDomainSockets) return;
        RunningServer = await ProbeAsync(socketPath);
    }

    /// <summary>sun_path ограничен (~108 байт), поэтому путь короткий: temp + метка каталога + hash.</summary>
    public static string GetSocketPath(string[]? args = null)
        => GetSocketPath(Directory.GetCurrentDirectory(), args);

    public static string GetSocketPath(string workingDirectory, string[]? args)
    {
        var cwd = Path.GetFullPath(workingDirectory);
        var cfg = ResolveCfgPath(args);

        var fingerprint = $"{NormalizeForFingerprint(cwd)}|{cfg ?? ""}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)))[..16].ToLowerInvariant();
        var label = SanitizeLabel(Path.GetFileName(cwd.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

        return Path.Combine(Path.GetTempPath(), $"{SocketFileNamePrefix}{label}-{hash}.sock");
    }

    /// <summary>
    /// Повторяет логику ConfigureAppConfigurationExtensiions.ConfigureAppConfiguration:
    /// инстансы с разными конфигами должны получать разные сокеты.
    /// </summary>
    public static string? ResolveCfgPath(string[]? args)
    {
        if (args is not null)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] is "-cfg" or "--config")
                {
                    var path = args[i + 1];
                    if (!Path.IsPathRooted(path)) path = Path.Combine(Directory.GetCurrentDirectory(), path);
                    return NormalizeForFingerprint(path);
                }
            }
        }

        var envCfg = Environment.GetEnvironmentVariable("MARS_CFG");
        return envCfg is null ? null : NormalizeForFingerprint(envCfg);
    }

    static string NormalizeForFingerprint(string path)
    {
        var full = Path.GetFullPath(path);
        return OperatingSystem.IsWindows() ? full.Replace('\\', '/').ToLowerInvariant() : full;
    }

    static string SanitizeLabel(string name)
    {
        var sb = new StringBuilder();
        foreach (var ch in name)
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.') sb.Append(ch);
            if (sb.Length >= 24) break;
        }
        return sb.Length == 0 ? "app" : sb.ToString();
    }

    /// <summary>
    /// Проверка «сервер запущен»: файла нет → null; слушатель отвечает → CliServerInfo;
    /// stale-файл (сервер упал без unlink) → ConnectionRefused → null.
    /// </summary>
    public static async Task<CliServerInfo?> ProbeAsync(string socketPath, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        if (!File.Exists(socketPath)) return null;

        try
        {
            using var http = CreateHttpClient(socketPath, timeout ?? TimeSpan.FromMilliseconds(500));
            using var response = await http.GetAsync(PingPath, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CliServerInfo>(cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }

    public static HttpClient CreateHttpClient(string socketPath, TimeSpan timeout)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = (_, token) => ConnectUnixAsync(socketPath, token),
        };
        // Host заведомо не резолвится: все соединения идут через ConnectCallback в сокет.
        return new HttpClient(handler)
        {
            Timeout = timeout,
            BaseAddress = new Uri("http://mars-cli-uds"),
        };
    }

    public static async ValueTask<Stream> ConnectUnixAsync(string socketPath, CancellationToken ct)
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
