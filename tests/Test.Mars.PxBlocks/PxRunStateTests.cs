using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;

namespace Test.Mars.PxBlocks;

/// <summary>Состояние запуска для проб: объект хоста, который хост передаёт в Start.</summary>
internal sealed class ProbeRunState
{
    public string Label { get; init; } = "";

    public int Calls { get; private set; }

    public List<object> Seen { get; } = [];

    public void Record(object implement)
    {
        Calls++;
        if (!Seen.Contains(implement))
            Seen.Add(implement);
    }
}

/// <summary>Лист с единственным конструктором, принимающим состояние: значения нет — ошибка.</summary>
internal sealed class StateOnlyImplement(ProbeRunState? state) : IPxExpressionImplement
{
    public string TypeId => "test_state_only";

    public ProbeRunState? State { get; } = state;

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
    {
        State?.Record(this);
        return ValueTask.FromResult<PxValue>(new PxStringValue(State?.Label ?? ""));
    }
}

/// <summary>Лист с двумя конструкторами: при состоянии выбирается инъекция.</summary>
internal sealed class DualCtorImplement : IPxExpressionImplement
{
    public DualCtorImplement()
    {
    }

    public DualCtorImplement(ProbeRunState? state) => State = state;

    public string TypeId => "test_dual_ctor";

    public ProbeRunState? State { get; }

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxStringValue(State?.Label ?? ""));
}

/// <summary>Лист без инъекции: состояние берётся через PxContext.GetState.</summary>
internal sealed class GetStateProbeImplement : IPxExpressionImplement
{
    public string TypeId => "test_getstate_probe";

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxStringValue(context.GetState<ProbeRunState>().Label));
}

/// <summary>
/// Состояние запуска: инъекция в имплементацию конструктором, экземпляр на запуск,
/// доступ через PxContext.GetState.
/// </summary>
public class PxRunStateTests
{
    private static string PrintProbeJson(string typeId) => $$"""
    {
      "blocks": { "languageVersion": 0, "blocks": [
        { "type": "core.text.print", "id": "print1",
          "inputs": { "TEXT": { "block": { "type": "{{typeId}}", "id": "probe1" } } } }
      ] }
    }
    """;

    /// <summary>Два листа одного типа в одном стеке — проверка кэша экземпляра на запуск.</summary>
    private const string TwoProbesJson = """
    {
      "blocks": { "languageVersion": 0, "blocks": [
        { "type": "core.text.print", "id": "print1",
          "inputs": { "TEXT": { "block": { "type": "test_state_only", "id": "probe1" } } },
          "next": { "block": { "type": "core.text.print", "id": "print2",
            "inputs": { "TEXT": { "block": { "type": "test_state_only", "id": "probe2" } } } } } }
      ] }
    }
    """;

    private static PxBlockImplementsLocator LocatorWithProbe(Type probe)
    {
        var locator = PxInterpreter.CreateDefaultImplements();
        locator.Register(probe);
        return locator;
    }

    private static PxRunOptions WithState(object? state)
        => PxTestRun.Fast(new PxRunOptions { YieldEvery = 0, State = state });

    [Fact]
    public void Create_WithState_InjectsStateConstructor()
    {
        var locator = new PxBlockImplementsLocator();
        locator.Register(typeof(StateOnlyImplement));
        var state = new ProbeRunState { Label = "браузер" };

        var implement = Assert.IsType<StateOnlyImplement>(locator.Create("test_state_only", state));

        Assert.Same(state, implement.State);
    }

    [Fact]
    public void Create_StateOnlyType_WithoutState_Throws()
    {
        var locator = new PxBlockImplementsLocator();
        locator.Register(typeof(StateOnlyImplement));

        Assert.Throws<InvalidOperationException>(() => locator.Create("test_state_only"));
    }

    [Fact]
    public void Create_UnknownTypeId_Throws()
        => Assert.Throws<InvalidOperationException>(() => new PxBlockImplementsLocator().Create("no_such_block"));

    [Fact]
    public void Create_WithState_PrefersStateConstructorOverParameterless()
    {
        var locator = new PxBlockImplementsLocator();
        locator.Register(typeof(DualCtorImplement));
        var state = new ProbeRunState { Label = "передан" };

        Assert.Same(state, Assert.IsType<DualCtorImplement>(locator.Create("test_dual_ctor", state)).State);
        Assert.Null(Assert.IsType<DualCtorImplement>(locator.Create("test_dual_ctor")).State);
    }

    [Fact]
    public async Task RunAsync_State_InjectedIntoImplementation()
    {
        var state = new ProbeRunState { Label = "из состояния" };

        var result = await PxTestRun.RunAsync(
            PrintProbeJson("test_state_only"), WithState(state), LocatorWithProbe(typeof(StateOnlyImplement)));

        Assert.True(result.Success);
        Assert.Equal(["из состояния"], result.Output);
    }

    [Fact]
    public async Task RunAsync_State_ImplementationIsCreatedPerRun()
    {
        var locator = LocatorWithProbe(typeof(StateOnlyImplement));
        var first = new ProbeRunState { Label = "one" };
        var second = new ProbeRunState { Label = "two" };

        var firstResult = await PxTestRun.RunAsync(PrintProbeJson("test_state_only"), WithState(first), locator);
        var secondResult = await PxTestRun.RunAsync(PrintProbeJson("test_state_only"), WithState(second), locator);

        Assert.True(firstResult.Success);
        Assert.True(secondResult.Success);
        // Имплементации не синглтоны: каждый запуск получил свой экземпляр со своим состоянием.
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
        Assert.NotSame(first.Seen[0], second.Seen[0]);
    }

    [Fact]
    public async Task RunAsync_State_SameTypeLeaves_ShareInstanceWithinRun()
    {
        var locator = LocatorWithProbe(typeof(StateOnlyImplement));
        var state = new ProbeRunState { Label = "x" };

        var result = await PxTestRun.RunAsync(TwoProbesJson, WithState(state), locator);

        Assert.True(result.Success);
        Assert.Equal(["x", "x"], result.Output);
        Assert.Equal(2, state.Calls);
        Assert.Single(state.Seen);
    }

    [Fact]
    public async Task RunAsync_GetState_ReturnsRunState()
    {
        var state = new ProbeRunState { Label = "через GetState" };

        var result = await PxTestRun.RunAsync(
            PrintProbeJson("test_getstate_probe"), WithState(state), LocatorWithProbe(typeof(GetStateProbeImplement)));

        Assert.True(result.Success);
        Assert.Equal(["через GetState"], result.Output);
    }

    [Fact]
    public async Task RunAsync_GetState_WithoutState_Fails()
    {
        var result = await PxTestRun.RunAsync(
            PrintProbeJson("test_getstate_probe"), WithState(null), LocatorWithProbe(typeof(GetStateProbeImplement)));

        Assert.False(result.Success);
        Assert.Contains("Run state is not set", result.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_GetState_StateOfAnotherType_Fails()
    {
        var result = await PxTestRun.RunAsync(
            PrintProbeJson("test_getstate_probe"), WithState(new object()), LocatorWithProbe(typeof(GetStateProbeImplement)));

        Assert.False(result.Success);
        Assert.Contains("Run state is not set", result.ErrorMessage);
    }
}
