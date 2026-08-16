using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mars.CommandLine.Remote;

/// <summary>
/// Информация о запущенном инстансе Mars, которую сервер отдаёт по /_cli/ping.
/// </summary>
public record CliServerInfo
{
    [JsonPropertyName("v")] public int ProtocolVersion { get; init; }
    [JsonPropertyName("pid")] public int Pid { get; init; }
    [JsonPropertyName("version")] public string Version { get; init; } = "";
    [JsonPropertyName("startedAt")] public DateTimeOffset StartedAt { get; init; }
    [JsonPropertyName("socketPath")] public string SocketPath { get; init; } = "";
}

/// <summary>
/// Запрос на удалённое исполнение CLI-команды (POST /_cli/exec).
/// </summary>
public record CliExecRequest
{
    [JsonPropertyName("v")] public int ProtocolVersion { get; init; }
    [JsonPropertyName("args")] public string[] Args { get; init; } = [];
}

/// <summary>
/// Кадр NDJSON-потока вывода удалённой команды: stdout ("o"), stderr ("e"), код выхода ("x", всегда последний).
/// </summary>
public record CliFrame
{
    public const string Out = "o";
    public const string Error = "e";
    public const string Exit = "x";

    [JsonPropertyName("t")] public string Type { get; init; } = "";
    [JsonPropertyName("d")] public string? Data { get; init; }
    [JsonPropertyName("code")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Code { get; init; }
}

/// <summary>
/// TextWriter, который складывает всё написанное в NDJSON-кадры поверх response-стрима.
/// Перехватывает Console.Out/Console.Error при удалённом исполнении команды на сервере.
/// </summary>
public sealed class CliFrameWriter : TextWriter
{
    readonly Stream _stream;
    readonly string _frameType;
    readonly SemaphoreSlim _lock = new(1, 1);

    public CliFrameWriter(Stream stream, string frameType)
    {
        _stream = stream;
        _frameType = frameType;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(string? value)
        => SendAsync(_frameType, value, null).GetAwaiter().GetResult();

    public override void Write(char value)
        => SendAsync(_frameType, value.ToString(), null).GetAwaiter().GetResult();

    public override void WriteLine(string? value)
        => SendAsync(_frameType, value + Environment.NewLine, null).GetAwaiter().GetResult();

    public override void WriteLine()
        => SendAsync(_frameType, Environment.NewLine, null).GetAwaiter().GetResult();

    public Task WriteExitAsync(int code, CancellationToken ct = default)
        => SendAsync(CliFrame.Exit, null, code, ct);

    public async Task SendAsync(string type, string? data, int? code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(data) && code is null) return;

        var line = JsonSerializer.Serialize(new CliFrame { Type = type, Data = data, Code = code }) + "\n";
        var bytes = Encoding.UTF8.GetBytes(line);

        // Kestrel запрещает синхронный IO, а TextWriter-методы синхронные;
        // синхронных контекстов в Kestrel нет, поэтому блокировка безопасна.
        await _lock.WaitAsync(ct);
        try
        {
            await _stream.WriteAsync(bytes, ct);
            await _stream.FlushAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }
}
