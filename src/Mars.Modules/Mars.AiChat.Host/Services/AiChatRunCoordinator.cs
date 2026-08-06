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

    public void Enqueue(Guid chatId, Guid userId, string userMessage)
    {
        var run = new Run { ChatId = chatId, Cts = new CancellationTokenSource() };
        if (!_runs.TryAdd(chatId, run))
            throw new UserActionException("Этот чат уже обрабатывается. Дождитесь завершения или нажмите «Стоп».");

        run.Task = Task.Run(() => ExecuteRunAsync(run, userId, userMessage));
    }

    public bool Stop(Guid chatId)
    {
        if (_runs.TryGetValue(chatId, out var run))
        {
            run.Cts.Cancel();
            return true;
        }

        return false;
    }

    private async Task ExecuteRunAsync(Run run, Guid userId, string userMessage)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var agentService = scope.ServiceProvider.GetRequiredService<AiChatAgentService>();
            await agentService.RunChatAsync(run.ChatId, userId, userMessage, run.Cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AiChat: run of chat {ChatId} was stopped", run.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiChat: run of chat {ChatId} failed", run.ChatId);
        }
        finally
        {
            _runs.TryRemove(run.ChatId, out _);
            run.Cts.Dispose();
        }
    }
}
