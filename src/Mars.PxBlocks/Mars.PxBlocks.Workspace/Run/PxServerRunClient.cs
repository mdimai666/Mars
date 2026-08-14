using System.Collections.Concurrent;
using System.Text.Json;
using Flurl.Http;
using Flurl.Http.Configuration;
using Mars.PxBlocks.Host.Shared;
using Mars.PxBlocks.Host.Shared.Dto;
using Mars.PxBlocks.Host.Shared.Services;
using Mars.PxBlocks.Runtime.Execution;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Mars.PxBlocks.Workspace.Run;

/// <summary>
/// Клиент серверного исполнения PxBlocks: REST api/PxBlocks (Flurl) + SignalR-хаб
/// событий запуска. Редактор получает его параметром RunTransport; подписка на
/// события оформляется до запроса Run (RunId назначает клиент), поэтому события
/// не теряются. Регистрация в DI стенда — scoped, на IFlurlClient.
/// </summary>
public sealed class PxServerRunClient : IPxBlocksApiClient, IPxRunTransport, IAsyncDisposable
{
    private static readonly TimeSpan[] ReconnectDelays =
    [
        TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)
    ];

    private readonly IFlurlClient _http;
    private readonly string _hubUrl;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, (Action<IReadOnlyList<PxExecutionEvent>> Events, Action<PxRunResultDto> Finished)> _subscriptions = new();
    private HubConnection? _connection;

    public PxServerRunClient(IFlurlClient flurlClient)
    {
        var baseAddress = flurlClient.HttpClient.BaseAddress?.AbsoluteUri
            ?? throw new ArgumentException("IFlurlClient без BaseAddress", nameof(flurlClient));

        // MVC сервера сериализует camelCase-ом — читаем Web-дефолтами (без учёта регистра).
        _http = new FlurlClient(baseAddress)
            .WithSettings(settings => settings.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        _hubUrl = baseAddress.TrimEnd('/') + PxBlocksConstants.HubRoute;
    }

    // ── REST: IPxBlocksApiClient ────────────────────────────────────────────

    public Task<PxDefinitionsResponse> GetDefinitionsAsync(CancellationToken cancellationToken = default)
        => _http.Request("api/PxBlocks", "Definitions").GetJsonAsync<PxDefinitionsResponse>(cancellationToken: cancellationToken);

    public Task<PxRunResponse> RunAsync(PxRunRequest request, CancellationToken cancellationToken = default)
        => _http.Request("api/PxBlocks", "Run").PostJsonAsync(request, cancellationToken: cancellationToken).ReceiveJson<PxRunResponse>();

    public async Task<bool> StopAsync(Guid runId, CancellationToken cancellationToken = default)
        => await _http.Request("api/PxBlocks", "Stop", runId).PostAsync(cancellationToken: cancellationToken).ReceiveJson<bool>();

    // ── События: IPxRunTransport ────────────────────────────────────────────

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
        => await EnsureConnectionAsync(cancellationToken);

    public IDisposable Subscribe(
        Guid runId,
        Action<IReadOnlyList<PxExecutionEvent>> onEvents,
        Action<PxRunResultDto> onFinished)
    {
        _subscriptions[runId] = (onEvents, onFinished);
        return new Subscription(_subscriptions, runId);
    }

    private async Task<HubConnection> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        // Disconnected = автопереподключение исчерпано: события больше не придут,
        // соединение пересоздаём (иначе запуски «зависают» без итога).
        if (_connection is { State: not HubConnectionState.Disconnected })
            return _connection;

        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { State: not HubConnectionState.Disconnected })
                return _connection;

            if (_connection != null)
                await _connection.DisposeAsync();

            // Дефолт клиента — JsonHubProtocol c Web-опциями (без учёта регистра):
            // читает и PascalCase сервера (конвенция Mars), настройка не нужна.
            var connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl, HttpTransportType.WebSockets | HttpTransportType.LongPolling)
                .WithAutomaticReconnect(ReconnectDelays)
                .Build();

            connection.KeepAliveInterval = TimeSpan.FromSeconds(15);
            connection.ServerTimeout = TimeSpan.FromSeconds(30);

            connection.On<Guid, PxExecutionEvent[]>(PxBlocksHubMethods.RunEvents, (runId, events) =>
            {
                if (_subscriptions.TryGetValue(runId, out var handlers))
                    handlers.Events(events);
            });
            connection.On<Guid, PxRunResultDto>(PxBlocksHubMethods.RunFinished, (runId, result) =>
            {
                if (_subscriptions.TryRemove(runId, out var handlers))
                    handlers.Finished(result);
            });

            await connection.StartAsync(cancellationToken);
            _connection = connection;
            return _connection;
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
        _connectionGate.Dispose();
    }

    private sealed class Subscription(
        ConcurrentDictionary<Guid, (Action<IReadOnlyList<PxExecutionEvent>> Events, Action<PxRunResultDto> Finished)> subscriptions,
        Guid runId) : IDisposable
    {
        public void Dispose() => subscriptions.TryRemove(runId, out _);
    }
}
