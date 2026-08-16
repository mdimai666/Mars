using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Mars.CommandLine.Remote;

/// <summary>
/// Тонкий клиент: передаёт аргументы команды запущенному инстансу Mars поверх UDS
/// и печатает стрим результата в консоль. Долгие команды (aichat, будущие синхронизации)
/// стримятся по мере исполнения — кадр приходит на каждую запись в консоль команды.
/// </summary>
public static class CliRemoteClient
{
    public static async Task<int> ExecAsync(string socketPath, string[] args, CancellationToken ct = default)
    {
        using var http = MarsCliSocket.CreateHttpClient(socketPath, Timeout.InfiniteTimeSpan);
        using var request = new HttpRequestMessage(HttpMethod.Post, MarsCliSocket.ExecPath)
        {
            Content = JsonContent.Create(new CliExecRequest
            {
                ProtocolVersion = MarsCliSocket.ProtocolVersion,
                Args = args,
            }),
        };

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            await Console.Error.WriteLineAsync($"mars cli: server returned {(int)response.StatusCode}: {body}");
            return 1;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var exitCode = 0;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (line.Length == 0) continue;

            CliFrame? frame;
            try
            {
                frame = JsonSerializer.Deserialize<CliFrame>(line);
            }
            catch (JsonException)
            {
                continue;
            }
            if (frame is null) continue;

            switch (frame.Type)
            {
                case CliFrame.Out when frame.Data is not null:
                    await Console.Out.WriteAsync(frame.Data);
                    break;
                case CliFrame.Error when frame.Data is not null:
                    await Console.Error.WriteAsync(frame.Data);
                    break;
                case CliFrame.Exit:
                    exitCode = frame.Code ?? 0;
                    break;
            }
        }

        return exitCode;
    }
}
