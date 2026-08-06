using AppFront.Shared;
using Mars.AiChat.Shared.SignalR;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Mars.AiChat.Front.Services;

/// <summary>
/// SignalR-клиент хаба /_ws/aichat: события выполнения агента.
/// </summary>
public class AiChatHubClient : IAsyncDisposable
{
    private const string AuthTokenKey = "authToken";

    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private HubConnection? _connection;

    public event Action<Guid, Guid, string>? OnChunk;
    public event Action<Guid, Guid, string, string>? OnToolCall;
    public event Action<Guid, Guid, string, string>? OnToolResult;
    public event Action<Guid, Guid, string>? OnQuestion;
    public event Action<Guid, Guid, string>? OnDone;
    public event Action<Guid, Guid>? OnStopped;
    public event Action<Guid, Guid, string>? OnError;

    public AiChatHubClient(IJSRuntime js)
    {
        _js = js;
    }

    public async Task EnsureStartedAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is not null) return;

            var connection = new HubConnectionBuilder()
                .WithUrl($"{Q.BackendUrl}{AiChatHubEvents.HubPath}", options =>
                {
                    options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
                    options.AccessTokenProvider = GetAccessTokenAsync;
                })
                .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)])
                .AddJsonProtocol(options =>
                {
                    // сервер настроен на PropertyNamingPolicy = null (AddMarsSignalRConfiguration)
                    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                })
                .Build();

            RegisterHandlers(connection);
            await connection.StartAsync();
            _connection = connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task JoinChatAsync(Guid chatId)
    {
        if (_connection is null) return;
        await _connection.InvokeAsync("JoinChat", chatId);
    }

    public async Task LeaveChatAsync(Guid chatId)
    {
        if (_connection is null) return;
        try
        {
            await _connection.InvokeAsync("LeaveChat", chatId);
        }
        catch
        {
            // соединение могло оборваться — не критично
        }
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", AuthTokenKey);
        }
        catch
        {
            return null;
        }
    }

    private void RegisterHandlers(HubConnection connection)
    {
        connection.On<Guid, Guid, string>(AiChatHubEvents.Chunk,
            (chatId, runId, text) => OnChunk?.Invoke(chatId, runId, text));

        connection.On<Guid, Guid, string, string>(AiChatHubEvents.ToolCall,
            (chatId, runId, toolName, argsJson) => OnToolCall?.Invoke(chatId, runId, toolName, argsJson));

        connection.On<Guid, Guid, string, string>(AiChatHubEvents.ToolResult,
            (chatId, runId, toolName, result) => OnToolResult?.Invoke(chatId, runId, toolName, result));

        connection.On<Guid, Guid, string>(AiChatHubEvents.Question,
            (chatId, runId, question) => OnQuestion?.Invoke(chatId, runId, question));

        connection.On<Guid, Guid, string>(AiChatHubEvents.Done,
            (chatId, runId, pendingQuestion) => OnDone?.Invoke(chatId, runId, pendingQuestion));

        connection.On<Guid, Guid>(AiChatHubEvents.Stopped,
            (chatId, runId) => OnStopped?.Invoke(chatId, runId));

        connection.On<Guid, Guid, string>(AiChatHubEvents.Error,
            (chatId, runId, message) => OnError?.Invoke(chatId, runId, message));
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
