using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.CommandLine.Scripting.Tests;

public class FnCommandCliTests
{
    private static readonly IServiceProvider EmptyServices = new ServiceCollection().BuildServiceProvider();

    [Fact]
    public async Task ReadSource_returns_inline_code()
    {
        var (code, error) = await FnCommandCli.ReadSourceAsync(null, "return 1;", false, CancellationToken.None);

        Assert.Null(error);
        Assert.Equal("return 1;", code);
    }

    [Fact]
    public async Task ReadSource_rejects_both_file_and_code()
    {
        var (_, error) = await FnCommandCli.ReadSourceAsync("x.cs", "return 1;", false, CancellationToken.None);

        Assert.Contains("mutually exclusive", error);
    }

    [Fact]
    public async Task ReadSource_requires_a_source()
    {
        var (_, error) = await FnCommandCli.ReadSourceAsync(null, null, false, CancellationToken.None);

        Assert.Contains("specify the source", error);
    }

    [Fact]
    public async Task ReadSource_rejects_missing_file()
    {
        var (_, error) = await FnCommandCli.ReadSourceAsync(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs"), null, false, CancellationToken.None);

        Assert.Contains("file not found", error);
    }

    [Fact]
    public async Task ReadSource_reads_existing_file()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");
        await File.WriteAllTextAsync(path, "return 7;");
        try
        {
            var (code, error) = await FnCommandCli.ReadSourceAsync(path, null, false, CancellationToken.None);

            Assert.Null(error);
            Assert.Equal("return 7;", code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadSource_rejects_stdin_in_remote_invocation()
    {
        var (_, error) = await FnCommandCli.ReadSourceAsync("-", null, true, CancellationToken.None);

        Assert.Contains("not available for remote execution", error);
    }

    [Fact]
    public async Task Execute_returns_0_and_prints_return_value()
    {
        var (exitCode, lines) = await ExecuteCapturedAsync(null, "return 2 + 2;", json: false);

        Assert.Equal(0, exitCode);
        Assert.Contains("4", lines);
        Assert.DoesNotContain(FnCommandCli.ResultMarker, lines);
    }

    [Fact]
    public async Task Execute_returns_2_on_compilation_error()
    {
        var (exitCode, _) = await ExecuteCapturedAsync(null, "var x = undefinedSymbol;", json: false);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task Execute_returns_1_on_runtime_exception()
    {
        var (exitCode, _) = await ExecuteCapturedAsync(null, "throw new InvalidOperationException(\"boom\");", json: false);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Execute_returns_1_on_usage_error()
    {
        var (exitCode, _) = await ExecuteCapturedAsync(null, null, json: false);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Json_envelope_replaces_raw_output_on_success()
    {
        var (exitCode, lines) = await ExecuteCapturedAsync(null, "return 2 + 2;", json: true);

        Assert.Equal(0, exitCode);
        Assert.Equal(2, lines.Length);
        Assert.Equal(FnCommandCli.ResultMarker, lines[0]);

        var envelope = ParseEnvelope(lines);
        Assert.True(envelope.GetProperty("ok").GetBoolean());
        Assert.Equal(0, envelope.GetProperty("exitCode").GetInt32());
        Assert.Equal(4, envelope.GetProperty("result").GetInt32());
        Assert.Equal(JsonValueKind.Null, envelope.GetProperty("error").ValueKind);
    }

    [Fact]
    public async Task Json_envelope_serializes_string_result()
    {
        var (_, lines) = await ExecuteCapturedAsync(null, "return \"hello\";", json: true);

        var envelope = ParseEnvelope(lines);
        Assert.Equal("hello", envelope.GetProperty("result").GetString());
    }

    [Fact]
    public async Task Json_envelope_carries_compilation_error()
    {
        var (exitCode, lines) = await ExecuteCapturedAsync(null, "var x = undefinedSymbol;", json: true);

        Assert.Equal(2, exitCode);
        var envelope = ParseEnvelope(lines);
        Assert.False(envelope.GetProperty("ok").GetBoolean());
        Assert.Equal(2, envelope.GetProperty("exitCode").GetInt32());
        Assert.Contains("CS0103", envelope.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Json_envelope_carries_runtime_error()
    {
        var (exitCode, lines) = await ExecuteCapturedAsync(null, "throw new InvalidOperationException(\"boom\");", json: true);

        Assert.Equal(1, exitCode);
        var envelope = ParseEnvelope(lines);
        Assert.False(envelope.GetProperty("ok").GetBoolean());
        Assert.Contains("boom", envelope.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Json_envelope_carries_usage_error()
    {
        var (exitCode, lines) = await ExecuteCapturedAsync(null, null, json: true);

        Assert.Equal(1, exitCode);
        var envelope = ParseEnvelope(lines);
        Assert.False(envelope.GetProperty("ok").GetBoolean());
        Assert.Contains("specify the source", envelope.GetProperty("error").GetString());
    }

    [Fact]
    public void UsageExamples_cover_main_forms()
    {
        Assert.Contains("fn -c", FnCommandCli.UsageExamples);
        Assert.Contains("fn -f", FnCommandCli.UsageExamples);
        Assert.Contains(FnCommandCli.ResultMarker, FnCommandCli.UsageExamples);
        Assert.Contains("--local", FnCommandCli.UsageExamples);
        Assert.Contains("GetRequiredService", FnCommandCli.UsageExamples);
    }

    private static async Task<(int ExitCode, string[] Lines)> ExecuteCapturedAsync(string? file, string? code, bool json)
    {
        var oldOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var exitCode = await FnCommandCli.ExecuteAsync(
                file, code, [], inRemoteInvocation: false, json, EmptyServices, CancellationToken.None);
            var output = writer.ToString();
            return (exitCode, output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        }
        finally
        {
            Console.SetOut(oldOut);
        }
    }

    private static JsonElement ParseEnvelope(string[] lines)
    {
        var markerIndex = Array.IndexOf(lines, FnCommandCli.ResultMarker);
        Assert.True(markerIndex >= 0, "result marker is missing from the output");
        return JsonDocument.Parse(lines[markerIndex + 1]).RootElement;
    }
}
