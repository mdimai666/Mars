using System.Text.Json.Nodes;
using Mars.PxBlocks.Host.Services;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Test.Mars.PxBlocks;

/// <summary>
/// Начальные переменные запуска: PxRunOptions.InitialVariables в рантайме
/// (перезапись объявленной переменной ПО ИМЕНИ) и PxRunRequest.InitialVariables
/// в хосте (JSON → PxValue через PxValueJson).
/// </summary>
public class PxInitialVariablesTests
{
    /// <summary>Программа печатает переменную «x» (объявлена в workspace).</summary>
    private const string PrintVariableJson = """
    {
      "blocks": { "languageVersion": 0, "blocks": [
        { "type": "core.text.print", "id": "print1",
          "inputs": { "TEXT": { "block": { "type": "core.variables.get", "id": "getX",
            "fields": { "VAR": { "id": "varX" } } } } } }
      ] },
      "variables": [ { "id": "varX", "name": "x" } ]
    }
    """;

    private static PxRunOptions Options(IReadOnlyDictionary<string, PxValue> variables)
        => PxTestRun.Fast(new PxRunOptions { YieldEvery = 0, InitialVariables = variables });

    [Fact]
    public async Task RunAsync_InitialVariables_OverrideDeclaredVariable()
    {
        var result = await PxTestRun.RunAsync(
            PrintVariableJson,
            Options(new Dictionary<string, PxValue> { ["x"] = new PxNumberValue(42) }));

        Assert.True(result.Success);
        Assert.Equal(["42"], result.Output);
    }

    [Fact]
    public async Task RunAsync_WithoutInitialVariables_VariableStartsAtZero()
    {
        var result = await PxTestRun.RunAsync(PrintVariableJson, PxTestRun.Fast());

        Assert.True(result.Success);
        Assert.Equal(["0"], result.Output);
    }

    [Fact]
    public async Task RunAsync_InitialVariables_KeyedByVariableName_NotId()
    {
        // Ключ словаря — ИМЯ переменной; id из workspace не срабатывает.
        var result = await PxTestRun.RunAsync(
            PrintVariableJson,
            Options(new Dictionary<string, PxValue> { ["varX"] = new PxNumberValue(7) }));

        Assert.True(result.Success);
        Assert.Equal(["0"], result.Output);
    }

    [Fact]
    public async Task RunAsync_InitialVariables_UnknownName_Ignored()
    {
        var result = await PxTestRun.RunAsync(
            PrintVariableJson,
            Options(new Dictionary<string, PxValue> { ["y"] = new PxNumberValue(7) }));

        Assert.True(result.Success);
        Assert.Equal(["0"], result.Output);
    }

    [Fact]
    public async Task RunAsync_InitialVariables_ListValue_StoredAsIs()
    {
        var result = await PxTestRun.RunAsync(
            PrintVariableJson,
            Options(new Dictionary<string, PxValue>
            {
                ["x"] = new PxListValue([new PxNumberValue(1), new PxNumberValue(2)])
            }));

        Assert.True(result.Success);
        Assert.Equal(["1,2"], result.Output);
    }

    [Fact]
    public async Task Start_InitialVariables_ReachRunningProgram()
    {
        var catalog = new PxBlockCatalog();
        var broadcaster = new FakeBroadcaster();
        var manager = new PxRunManager(catalog, broadcaster, new PxEditorContextRegistry());

        var response = manager.Start(new PxRunRequest
        {
            BlocksJson = PrintVariableJson,
            InitialVariables = new Dictionary<string, JsonNode?> { ["x"] = JsonValue.Create(42) }
        });

        Assert.True(response.Started, response.ErrorMessage);
        var result = await broadcaster.Finished.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Success);
        Assert.Contains(broadcaster.Events, e => e.Kind == PxExecutionEventKind.Output && e.Text == "42");
    }

    [Fact]
    public async Task Start_InitialVariables_NestedJsonValue_ReachesProgram()
    {
        var catalog = new PxBlockCatalog();
        var broadcaster = new FakeBroadcaster();
        var manager = new PxRunManager(catalog, broadcaster, new PxEditorContextRegistry());

        var response = manager.Start(new PxRunRequest
        {
            BlocksJson = PrintVariableJson,
            InitialVariables = new Dictionary<string, JsonNode?> { ["x"] = JsonNode.Parse("[1,2,3]") }
        });

        Assert.True(response.Started);
        var result = await broadcaster.Finished.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Success);
        Assert.Contains(broadcaster.Events, e => e.Kind == PxExecutionEventKind.Output && e.Text == "1,2,3");
    }

    [Fact]
    public void Start_UnsupportedInitialVariable_NotStarted()
    {
        var catalog = new PxBlockCatalog();
        var manager = new PxRunManager(catalog, new FakeBroadcaster(), new PxEditorContextRegistry());

        var response = manager.Start(new PxRunRequest
        {
            BlocksJson = PrintVariableJson,
            InitialVariables = new Dictionary<string, JsonNode?> { ["x"] = JsonValue.Create(new object()) }
        });

        Assert.False(response.Started);
        Assert.StartsWith("Initial variables:", response.ErrorMessage);
        Assert.Equal(0, manager.ActiveRunCount);
    }
}
