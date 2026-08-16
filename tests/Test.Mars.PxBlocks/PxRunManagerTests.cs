using Mars.PxBlocks.Host.Services;
using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Runtime.Values;
using Mars.PxBlocks.Shared.Definitions;
using Mars.PxBlocks.Shared.Toolbox;

namespace Test.Mars.PxBlocks;

/// <summary>Приёмник рассылки: собирает события и сигнал завершения запуска.</summary>
internal sealed class FakeBroadcaster : IPxBlocksBroadcaster
{
    private readonly object _gate = new();
    private readonly TaskCompletionSource<PxRunResultDto> _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly List<PxExecutionEvent> _events = [];

    public IReadOnlyList<PxExecutionEvent> Events
    {
        get { lock (_gate) return _events.ToArray(); }
    }

    public int EventCount
    {
        get { lock (_gate) return _events.Count; }
    }

    public Task<PxRunResultDto> Finished => _finished.Task;

    public Task RunEvents(Guid runId, IReadOnlyList<PxExecutionEvent> events)
    {
        lock (_gate)
            _events.AddRange(events);
        return Task.CompletedTask;
    }

    public Task RunFinished(Guid runId, PxRunResultDto result)
    {
        _finished.TrySetResult(result);
        return Task.CompletedTask;
    }
}

/// <summary>Серверный запуск программ: PxRunManager + PxBlockCatalog без ASP.NET-слоя.</summary>
public class PxRunManagerTests
{
    private static (PxRunManager Manager, FakeBroadcaster Broadcaster, PxBlockCatalog Catalog, PxEditorContextRegistry Contexts) CreateManager()
    {
        var catalog = new PxBlockCatalog();
        catalog.RegisterSet(new PxEventBlocks());
        var broadcaster = new FakeBroadcaster();
        var contexts = new PxEditorContextRegistry();
        return (new PxRunManager(catalog, broadcaster, contexts), broadcaster, catalog, contexts);
    }

    private const string PrintJson = """
    {
      "blocks": { "languageVersion": 0, "blocks": [
        { "type": "core.text.print", "id": "print1",
          "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "hello" } } } } }
      ] }
    }
    """;

    /// <summary>Обычный стек (print «plain») + событие Start (print «started»).</summary>
    private const string StartAndPlainJson = """
    {
      "blocks": { "languageVersion": 0, "blocks": [
        { "type": "core.text.print", "id": "printPlain",
          "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "tPlain", "fields": { "TEXT": "plain" } } } } },
        { "type": "core.events.start", "id": "start1",
          "inputs": { "DO": { "block":
            { "type": "core.text.print", "id": "printStart",
              "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "tStart", "fields": { "TEXT": "started" } } } } }
          } }
        }
      ] }
    }
    """;

    /// <summary>События Start (print «started») и Loop (print «looped», бесконечный).</summary>
    private const string StartAndLoopJson = """
    {
      "blocks": { "languageVersion": 0, "blocks": [
        { "type": "core.events.start", "id": "start1",
          "inputs": { "DO": { "block":
            { "type": "core.text.print", "id": "printStart",
              "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "tStart", "fields": { "TEXT": "started" } } } } }
          } }
        },
        { "type": "core.events.loop", "id": "loop1",
          "inputs": { "DO": { "block":
            { "type": "core.text.print", "id": "printLoop",
              "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "tLoop", "fields": { "TEXT": "looped" } } } } }
          } }
        }
      ] }
    }
    """;

    /// <summary>Бесконечное событие Loop (print «tick»).</summary>
    private const string LoopJson = """
    {
      "blocks": { "languageVersion": 0, "blocks": [
        { "type": "core.events.loop", "id": "loop1",
          "inputs": { "DO": { "block":
            { "type": "core.text.print", "id": "pl",
              "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "tl", "fields": { "TEXT": "tick" } } } } }
          } }
        }
      ] }
    }
    """;

    [Fact]
    public async Task Start_SimpleProgram_StreamsOutputAndFinishes()
    {
        var (manager, broadcaster, _, _) = CreateManager();

        var response = manager.Start(new PxRunRequest { BlocksJson = PrintJson });

        Assert.True(response.Started);
        var result = await broadcaster.Finished.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Success);
        Assert.Contains(broadcaster.Events, e => e.Kind == PxExecutionEventKind.Output && e.Text == "hello");
        Assert.Equal(0, manager.ActiveRunCount);
    }

    [Fact]
    public void Start_UnknownBlock_NotStarted()
    {
        var (manager, _, _, _) = CreateManager();
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "no_such_block", "id": "bad1" }
          ] }
        }
        """;

        var response = manager.Start(new PxRunRequest { BlocksJson = json });

        Assert.False(response.Started);
        Assert.Equal("bad1", response.ErrorBlockId);
        Assert.Contains("no_such_block", response.ErrorMessage);
        Assert.Equal(0, manager.ActiveRunCount);
    }

    [Fact]
    public async Task Stop_InfiniteLoop_CancelsRun()
    {
        var (manager, broadcaster, _, _) = CreateManager();
        // Бесконечное событие Loop: выход только через Stop.
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "core.events.loop", "id": "loop1",
              "inputs": { "DO": { "block":
                { "type": "core.text.print", "id": "pl",
                  "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "tl", "fields": { "TEXT": "tick" } } } } }
              } }
            }
          ] }
        }
        """;

        var response = manager.Start(new PxRunRequest { BlocksJson = json });
        Assert.True(response.Started);

        // Дождаться первых событий, затем остановить.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline && broadcaster.EventCount == 0)
            await Task.Delay(20);

        Assert.True(manager.Stop(response.RunId));
        var result = await broadcaster.Finished.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Canceled);
        Assert.Equal(0, manager.ActiveRunCount);
    }

    [Fact]
    public void Stop_UnknownRunId_ReturnsFalse()
    {
        var (manager, _, _, _) = CreateManager();
        Assert.False(manager.Stop(Guid.NewGuid()));
    }

    [Fact]
    public void Start_UnknownContext_NotStarted()
    {
        var (manager, _, _, _) = CreateManager();

        var response = manager.Start(new PxRunRequest { BlocksJson = PrintJson, ContextName = "нет-такого" });

        Assert.False(response.Started);
        Assert.Contains("нет-такого", response.ErrorMessage);
        Assert.Equal(0, manager.ActiveRunCount);
    }

    [Fact]
    public async Task Start_WithContext_AppliesEventPolicy()
    {
        var (manager, broadcaster, _, contexts) = CreateManager();
        contexts.Register(PxEditorContext.Define("events-only").Events("start"));

        var response = manager.Start(new PxRunRequest { BlocksJson = StartAndPlainJson, ContextName = "events-only" });

        Assert.True(response.Started);
        var result = await broadcaster.Finished.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Success);
        // Политика контекста: исполняются только события «start», обычный стек пропущен.
        Assert.Contains(broadcaster.Events, e => e.Kind == PxExecutionEventKind.Output && e.Text == "started");
        Assert.DoesNotContain(broadcaster.Events, e => e.Kind == PxExecutionEventKind.Output && e.Text == "plain");
    }

    [Fact]
    public async Task Start_WithContext_RequestPolicyOverridesContext()
    {
        var (manager, broadcaster, _, contexts) = CreateManager();
        // Если бы победила политика контекста («loop»), запуск ушёл бы в бесконечный цикл.
        contexts.Register(PxEditorContext.Define("override").Events("loop"));

        var response = manager.Start(new PxRunRequest
        {
            BlocksJson = StartAndLoopJson,
            ContextName = "override",
            EventNames = ["start"]
        });

        Assert.True(response.Started);
        var result = await broadcaster.Finished.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Success);
        Assert.Contains(broadcaster.Events, e => e.Kind == PxExecutionEventKind.Output && e.Text == "started");
    }

    [Fact]
    public async Task Start_WithContext_StepLimitApplies()
    {
        var (manager, broadcaster, _, contexts) = CreateManager();
        contexts.Register(PxEditorContext.Define("limited").StepLimit(10));

        var response = manager.Start(new PxRunRequest { BlocksJson = LoopJson, ContextName = "limited" });

        Assert.True(response.Started);
        var result = await broadcaster.Finished.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(result.Success);
        Assert.Contains("лимит шагов", result.ErrorMessage);
        Assert.Equal(0, manager.ActiveRunCount);
    }

    [Fact]
    public void Catalog_RegisterAssembly_PicksUpSetsAndImplements()
    {
        var catalog = new PxBlockCatalog();

        catalog.RegisterAssembly(typeof(CatalogProbeSet).Assembly);

        Assert.Contains(catalog.Definitions, d => d.TypeId == "test_host_probe");
        Assert.True(catalog.Implements.Knows("test_host_probe"));
    }

    [Fact]
    public void Catalog_Toolbox_DomainCategoryBeforeSeparator()
    {
        var catalog = new PxBlockCatalog();
        catalog.RegisterToolboxCategory(new PxToolboxCategory { Name = "Домен" });

        var contents = catalog.Toolbox.Contents;
        var domainIndex = contents.FindIndex(i => i is PxToolboxCategory { Name: "Домен" });
        var separatorIndex = contents.FindIndex(i => i is PxToolboxSeparator);

        Assert.True(domainIndex >= 0);
        Assert.True(domainIndex < separatorIndex);
    }
}

/// <summary>Зонд-набор: подкласс PxBlockSet в тестовой сборке (попадает под RegisterAssembly).</summary>
internal sealed class CatalogProbeSet : PxBlockSet
{
    public CatalogProbeSet()
    {
        Add(PxMaster.Define("test_host_probe").Output("Number").Message("зонд"));
    }
}

/// <summary>Зонд-имплементация: значение 7 (попадает под RegisterAssembly).</summary>
internal sealed class CatalogProbeImplement : IPxExpressionImplement
{
    public string TypeId => "test_host_probe";

    public ValueTask<PxValue> EvaluateAsync(PxContext context, PxCall call)
        => ValueTask.FromResult<PxValue>(new PxNumberValue(7));
}
