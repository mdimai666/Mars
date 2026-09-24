using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.CommandLine.Scripting.Tests;

public interface IFakeService
{
    string Name { get; }
}

public class FakeService : IFakeService
{
    public string Name => "fake";
}

public class FnScriptRunnerTests
{
    private const string FakeServiceFullName = "Mars.CommandLine.Scripting.Tests.IFakeService";

    private static FnContext CreateContext(Action<IServiceCollection>? configure = null, string[]? args = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        return new FnContext(services.BuildServiceProvider(), args ?? [], CancellationToken.None);
    }

    [Fact]
    public async Task Top_level_return_value_is_captured()
    {
        var state = await FnScriptRunner.RunAsync("return 2 + 2;", CreateContext(), CancellationToken.None);

        Assert.Null(state.Exception);
        Assert.Equal(4, state.ReturnValue);
    }

    [Fact]
    public async Task Required_service_resolves_from_DI()
    {
        var context = CreateContext(s => s.AddSingleton<IFakeService, FakeService>());

        var state = await FnScriptRunner.RunAsync(
            $"return GetRequiredService<{FakeServiceFullName}>().Name;", context, CancellationToken.None);

        Assert.Null(state.Exception);
        Assert.Equal("fake", state.ReturnValue);
    }

    [Fact]
    public async Task Scoped_service_resolves_via_CreateScope()
    {
        var context = CreateContext(s => s.AddScoped<IFakeService, FakeService>());

        // script-парсер Roslyn не поддерживает `using var` declarations — только using-блок
        var state = await FnScriptRunner.RunAsync(
            $"using (var scope = CreateScope()) {{ return scope.ServiceProvider.GetService(typeof({FakeServiceFullName})) is {FakeServiceFullName} svc && svc.Name == \"fake\"; }}",
            context, CancellationToken.None);

        Assert.Null(state.Exception);
        Assert.Equal(true, state.ReturnValue);
    }

    [Fact]
    public async Task Args_are_passed_to_script()
    {
        var context = CreateContext(args: ["prod", "42"]);

        var state = await FnScriptRunner.RunAsync(
            "return Args.Length + \":\" + Args[0] + \":\" + Args[1];", context, CancellationToken.None);

        Assert.Equal("2:prod:42", state.ReturnValue);
    }

    [Fact]
    public async Task Bcl_imports_work_without_explicit_using()
    {
        var state = await FnScriptRunner.RunAsync(
            "return Enumerable.Range(1, 3).Sum() + new List<int> { 1 }.Count;", CreateContext(), CancellationToken.None);

        Assert.Equal(7, state.ReturnValue);
    }

    [Fact]
    public async Task Compilation_error_throws_with_diagnostics()
    {
        var exception = await Assert.ThrowsAsync<CompilationErrorException>(() =>
            FnScriptRunner.RunAsync("var x = undefinedSymbol;", CreateContext(), CancellationToken.None));

        var diagnostic = Assert.Single(exception.Diagnostics);
        Assert.Equal("CS0103", diagnostic.Id);
        Assert.Equal(1, diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1);
    }

    [Fact]
    public async Task Runtime_exception_is_captured_in_state()
    {
        var state = await FnScriptRunner.RunAsync(
            "throw new InvalidOperationException(\"boom\");", CreateContext(), CancellationToken.None);

        var exception = Assert.IsType<InvalidOperationException>(state.Exception);
        Assert.Equal("boom", exception.Message);
    }
}
