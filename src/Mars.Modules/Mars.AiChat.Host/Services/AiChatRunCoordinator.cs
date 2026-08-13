using System.Collections.Concurrent;
using Mars.AiChat.Host.Shared.Interfaces;
using Mars.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mars.AiChat.Host.Services;

/// <summary>
/// Один активный запуск агента на чат. Запуск выполняется в фоне, вне HTTP-запроса.
/// </summary>
public class AiChatRunCoordinator : IAiChatRunCoordinator
{
    private sealed class Run
    {
        public required Guid ChatId { get; init; }
        public required CancellationTokenSource Cts { get; init; }
        public Task Task { get; set; } = Task.CompletedTask;
    }

    private readonly ConcurrentDictionary<Guid, Run> _runs = new();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AiChatRunCoordinator> _logger;

    public AiChatRunCoordinator(IServiceScopeFactory serviceScopeFactory, ILogger<AiChatRunCoordinator> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public bool IsRunning(Guid chatId) => _runs.ContainsKey(chatId);

    public void Enqueue(Guid chatId, Guid userId, string userMessage, string? pageContext = null, IReadOnlyList<Guid>? attachmentIds = null)
    {
        var run = new Run { ChatId = chatId, Cts = new CancellationTokenSource() };
        if (!_runs.TryAdd(chatId, run))
            throw new UserActionException("Этот чат уже обрабатывается. Дождитесь завершения или нажмите «Стоп».");

        _logger.LogDebug("AiChat: chat {ChatId} enqueued (user {UserId}, message {Length} chars, attachments {Attachments})",
            chatId, userId, userMessage.Length, attachmentIds?.Count ?? 0);

        run.Task = Task.Run(() => ExecuteRunAsync(run, userId, userMessage, pageContext, attachmentIds));
    }

    public bool Stop(Guid chatId)
    {
        if (_runs.TryGetValue(chatId, out var run))
        {
            _logger.LogDebug("AiChat: chat {ChatId} stop requested", chatId);
            run.Cts.Cancel();
            return true;
        }

        return false;
    }

    private async Task ExecuteRunAsync(Run run, Guid userId, string userMessage, string? pageContext, IReadOnlyList<Guid>? attachmentIds)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var agentService = scope.ServiceProvider.GetRequiredService<AiChatAgentService>();
            await agentService.RunChatAsync(run.ChatId, userId, userMessage, pageContext, run.Cts.Token, attachmentIds: attachmentIds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("AiChat: run of chat {ChatId} was stopped", run.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiChat: run of chat {ChatId} failed", run.ChatId);
        }
        finally
        {
            _runs.TryRemove(run.ChatId, out _);
            run.Cts.Dispose();
            _logger.LogDebug("AiChat: chat {ChatId} run finished, active runs: {ActiveRuns}", run.ChatId, _runs.Count);
        }
    }
}
