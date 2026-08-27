using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Mars.AiChat.Host.Hubs;
using Mars.AiChat.Contracts.Dto;
using Mars.AiChat.Contracts.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Mars.AiChat.Host.Services;

/// <summary>
/// Мост «серверный агент → открытая страница админки»: агент вызывает инструмент,
/// запрос уходит SignalR-событием клиенту, клиент выполняет его на странице
/// и возвращает результат методом хаба PageToolResult.
/// </summary>
public class AiChatPageBridge
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private readonly ConcurrentDictionary<string, TaskCompletionSource<AiPageToolResult>> _pending = new();
    private readonly IHubContext<AiChatHub> _hub;
    private readonly ILogger<AiChatPageBridge> _logger;

    public AiChatPageBridge(IHubContext<AiChatHub> hub, ILogger<AiChatPageBridge> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Вызывает инструмент на открытой странице клиента и ждёт ответ.
    /// </summary>
    public async Task<AiPageToolResult> CallPageAsync(Guid chatId, string tool, object? args = null, TimeSpan? timeout = null)
    {
        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<AiPageToolResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var request = new AiPageToolRequest
            {
                RequestId = requestId,
                Tool = tool,
                ArgsJson = args is null ? "" : JsonSerializer.Serialize(args, SerializerOptions),
            };

            _logger.LogDebug("AiChat: page tool {Tool} requested for chat {ChatId} (request {RequestId}), args: {Args}",
                tool, chatId, requestId, request.ArgsJson);

            await _hub.Clients.Group(AiChatHub.GroupName(chatId))
                .SendCoreAsync(AiChatHubEvents.PageToolRequest, [chatId, request]);

            using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(20));
            await using var registration = cts.Token.Register(() => tcs.TrySetCanceled());

            var result = await tcs.Task;

            _logger.LogDebug("AiChat: page tool {Tool} for chat {ChatId} → {Status} in {ElapsedMs} ms: {Result}",
                tool, chatId, result.Ok ? "ok" : "error", stopwatch.ElapsedMilliseconds, Truncate(result.Result));

            return result;
        }
        catch (Exception ex) when (ex is OperationCanceledException or TaskCanceledException)
        {
            _logger.LogDebug("AiChat: page tool {Tool} for chat {ChatId} timed out after {ElapsedMs} ms",
                tool, chatId, stopwatch.ElapsedMilliseconds);

            return new AiPageToolResult
            {
                RequestId = requestId,
                Ok = false,
                Result = "Страница не ответила: чат закрыт, браузер недоступен или страница редактирования не открыта.",
            };
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
        }
    }

    /// <summary>
    /// Вызывается хабом при получении результата от клиента.
    /// </summary>
    public void Complete(AiPageToolResult result)
    {
        if (_pending.TryRemove(result.RequestId, out var tcs))
        {
            tcs.TrySetResult(result);
        }
    }

    static string Truncate(string? text, int max = 300)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= max ? text : text[..max] + "…";
    }
}
