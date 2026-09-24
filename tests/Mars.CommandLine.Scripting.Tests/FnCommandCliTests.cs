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
        var exitCode = await FnCommandCli.ExecuteAsync(
            null, "return 2 + 2;", [], false, EmptyServices, CancellationToken.None);

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Execute_returns_2_on_compilation_error()
    {
        var exitCode = await FnCommandCli.ExecuteAsync(
            null, "var x = undefinedSymbol;", [], false, EmptyServices, CancellationToken.None);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task Execute_returns_1_on_runtime_exception()
    {
        var exitCode = await FnCommandCli.ExecuteAsync(
            null, "throw new InvalidOperationException(\"boom\");", [], false, EmptyServices, CancellationToken.None);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Execute_returns_1_on_usage_error()
    {
        var exitCode = await FnCommandCli.ExecuteAsync(
            null, null, [], false, EmptyServices, CancellationToken.None);

        Assert.Equal(1, exitCode);
    }
}
