using Flurl.Http;
using Mars.PxBlocks.Host;
using Mars.PxBlocks.Host.Hubs;
using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Runtime.Execution;
using Mars.PxBlocks.Workspace.Run;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Test.Mars.PxBlocks;

/// <summary>
/// Сквозной прогон серверного исполнения: реальный Kestrel (api/PxBlocks + SignalR-хаб)
/// и штатный клиент PxServerRunClient — тот же, что получает редактор в стенде.
/// </summary>
public class PxRunServerIntegrationTests : IAsyncLifetime
{
    private WebApplication? _app;
    private PxServerRunClient? _client;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddPxBlocks();
        // Контроллеры определений/контекстов живут в Mars.PxBlocks.Host; REST запуска
        // PxBlocks как встраиваемый модуль не даёт — его объявляет хост (TestPxRunController).
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(global::Mars.PxBlocks.Host.MainPxBlocks).Assembly)
            .AddApplicationPart(typeof(PxRunServerIntegrationTests).Assembly);

        _app = builder.Build();
        _app.UsePxBlocks();
        _app.MapControllers();
        _app.MapHub<PxBlocksHub>(PxBlocksConstants.HubRoute);
        await _app.StartAsync();

        var address = _app.Urls.First();
        var flurl = new FlurlClient(new HttpClient { BaseAddress = new Uri(address) });
        _client = new PxServerRunClient(flurl);
    }

    public async Task DisposeAsync()
    {
        if (_client != null)
            await _client.DisposeAsync();
        if (_app != null)
            await _app.DisposeAsync();
    }

    [Fact]
    public async Task GetDefinitions_ReturnsCoreEventBlocks()
    {
        var definitions = await _client!.GetDefinitionsAsync();

        Assert.Contains("core.events.start", definitions.DefinitionsJson);
        Assert.Contains("core.events.loop", definitions.DefinitionsJson);
        Assert.Contains(definitions.Toolbox.Contents, c => c is global::Mars.PxBlocks.Shared.Toolbox.PxToolboxCategory { Name: "Основное" });
    }

    [Fact]
    public async Task Run_StreamsEventsOverHub_AndFinishes()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "core.text.print", "id": "p1",
              "inputs": { "TEXT": { "block": { "type": "core.text.text", "id": "t1", "fields": { "TEXT": "server" } } } } }
          ] }
        }
        """;

        var events = new List<PxExecutionEvent>();
        var finished = new TaskCompletionSource<PxRunResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        await _client!.ConnectAsync();
        var runId = Guid.NewGuid();
        using var subscription = _client.Subscribe(runId,
            batch => { lock (events) events.AddRange(batch); },
            result => finished.TrySetResult(result));

        var response = await _client.RunAsync(new PxRunRequest { RunId = runId, BlocksJson = json });

        Assert.True(response.Started);
        Assert.Equal(runId, response.RunId);

        var result = await finished.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.True(result.Success);
        lock (events)
        {
            Assert.Contains(events, e => e.Kind == PxExecutionEventKind.Output && e.Text == "server");
        }
    }

    [Fact]
    public async Task Run_UnknownBlock_ReturnsParseError_AndNothingStreams()
    {
        var json = """
        {
          "blocks": { "languageVersion": 0, "blocks": [
            { "type": "no_such_block", "id": "bad1" }
          ] }
        }
        """;

        var finished = new TaskCompletionSource<PxRunResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _client!.ConnectAsync();
        var runId = Guid.NewGuid();
        using var subscription = _client.Subscribe(runId, _ => { }, result => finished.TrySetResult(result));

        var response = await _client.RunAsync(new PxRunRequest { RunId = runId, BlocksJson = json });

        Assert.False(response.Started);
        Assert.Equal("bad1", response.ErrorBlockId);
        // Запуск не стартовал — RunFinished не приходит.
        await Assert.ThrowsAsync<TimeoutException>(() => finished.Task.WaitAsync(TimeSpan.FromMilliseconds(300)));
    }

    [Fact]
    public async Task Stop_InfiniteLoop_CancelsServerRun()
    {
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

        var events = new List<PxExecutionEvent>();
        var finished = new TaskCompletionSource<PxRunResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        await _client!.ConnectAsync();
        var runId = Guid.NewGuid();
        using var subscription = _client.Subscribe(runId,
            batch => { lock (events) events.AddRange(batch); },
            result => finished.TrySetResult(result));

        var response = await _client.RunAsync(new PxRunRequest { RunId = runId, BlocksJson = json });
        Assert.True(response.Started);

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            lock (events)
            {
                if (events.Count > 0)
                    break;
            }
            await Task.Delay(20);
        }

        await _client.StopAsync(runId);
        var result = await finished.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.True(result.Canceled);
    }
}

/// <summary>
/// REST запуска в тестовом сервере: PxBlocks как встраиваемый модуль исполнения
/// не отдаёт — хост объявляет его сам (образец — PxRunController стенда).
/// Маршрут повторяет ожидание PxServerRunClient.
/// </summary>
[ApiController]
[Route("api/PxBlocks")]
public class TestPxRunController(IPxRunManager runManager) : ControllerBase
{
    [HttpPost(nameof(Run))]
    public PxRunResponse Run(PxRunRequest request) => runManager.Start(request);

    [HttpPost(nameof(Stop) + "/{runId:guid}")]
    public bool Stop(Guid runId) => runManager.Stop(runId);
}
