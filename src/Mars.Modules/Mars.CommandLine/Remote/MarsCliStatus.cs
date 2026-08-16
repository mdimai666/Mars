namespace Mars.CommandLine.Remote;

/// <summary>
/// Команда `status`: запущен ли Mars для этой директории. Вся информация берётся
/// из probe UDS-сокета — поднимать приложение не нужно ни в каком случае.
/// Exit code: 0 — сервер запущен, 1 — не запущен (для скриптов).
/// </summary>
public static class MarsCliStatus
{
    public static async Task<int> PrintAsync(string[]? args = null)
    {
        var socketPath = args is null ? MarsCliSocket.GetSocketPath() : MarsCliSocket.GetSocketPath(args);
        var server = await MarsCliSocket.ProbeAsync(socketPath);

        if (server is null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("mars cli: not running");
            Console.ResetColor();
            return 1;
        }

        var uptime = DateTimeOffset.Now - server.StartedAt;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("mars cli: running");
        Console.ResetColor();
        Console.WriteLine($"  pid      : {server.Pid}");
        Console.WriteLine($"  version  : {server.Version}");
        Console.WriteLine($"  started  : {server.StartedAt:yyyy-MM-dd HH:mm:ss} (uptime {FormatUptime(uptime)})");
        Console.WriteLine($"  directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($"  socket   : {server.SocketPath}");
        return 0;
    }

    internal static string FormatUptime(TimeSpan uptime)
    {
        if (uptime < TimeSpan.Zero) uptime = TimeSpan.Zero;
        if (uptime.TotalDays >= 1) return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalHours >= 1) return $"{uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalMinutes >= 1) return $"{uptime.Minutes}m {uptime.Seconds}s";
        return $"{uptime.Seconds}s";
    }

}
