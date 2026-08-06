using System.Text;
using System.Text.Json;
using Mars.AiChat.Host.Hubs;
using Mars.AiChat.Host.Shared.Interfaces;
using Mars.AiChat.Host.Shared.Models;
using Mars.AiChat.Host.Tools;
using Mars.AiChat.Shared.Dto;
using Mars.AiChat.Shared.Options;
using Mars.AiChat.Shared.SignalR;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Services;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Mars.AiChat.Host.Services;

/// <summary>
/// Harness-цикл агента: приём сообщения пользователя, работа с инструментами,
/// стриминг событий в SignalR, сохранение чата в HybridCache.
/// </summary>
public class AiChatAgentService
{
    private readonly IHubContext<AiChatHub> _hub;
    private readonly IAiChatSessionStore _store;
    private readonly IAiChatClientFactory _clientFactory;
    private readonly IOptionService _optionService;
    private readonly MarsSiteTools _siteTools;
    private readonly ILogger<AiChatAgentService> _logger;

    public AiChatAgentService(
        IHubContext<AiChatHub> hub,
        IAiChatSessionStore store,
        IAiChatClientFactory clientFactory,
        IOptionService optionService,
        MarsSiteTools siteTools,
        ILogger<AiChatAgentService> logger)
    {
        _hub = hub;
        _store = store;
        _clientFactory = clientFactory;
        _optionService = optionService;
        _siteTools = siteTools;
        _logger = logger;
    }

    public async Task RunChatAsync(Guid chatId, Guid userId, string userMessage, CancellationToken ct)
    {
        var state = await _store.GetAsync(chatId, userId, ct)
            ?? throw new NotFoundException($"AiChat session '{chatId}' not found");

        var runId = Guid.NewGuid();
        var group = AiChatHub.GroupName(chatId);

        if (state.Title is "" or "Новый чат")
            state.Title = userMessage.Length <= 40 ? userMessage : userMessage[..40] + "…";
        state.PendingQuestion = null;
        state.Messages.Add(NewMessage(AiChatMessageRole.User, userMessage));

        // Настройка подключения и создание агента
        AIAgent agent;
        try
        {
            var option = _optionService.GetOption<AiChatOption>();
            var connection = option.GetDefaultConnection()
                ?? throw new UserActionException("Подключение к ИИ-сервису не настроено. Добавьте его в Настройки → ИИ-чат.");

            var askUser = new AskUserTool();
            var tools = new AIFunction[]
            {
                AIFunctionFactory.Create(_siteTools.GetSiteSettings),
                AIFunctionFactory.Create(_siteTools.UpdateSiteSettings),
                AIFunctionFactory.Create(askUser.AskUser),
            };

            var client = _clientFactory.CreateClient(connection);
            agent = client.AsHarnessAgent(new HarnessAgentOptions
            {
                Name = "mars-site-agent",
                ChatOptions = new ChatOptions
                {
                    Instructions = AiChatPrompts.BuildInstructions(option),
                    Tools = [.. tools],
                },
                MaximumIterationsPerRequest = 15,
                DisableWebSearch = true,
                DisableTodoProvider = true,
                DisableAgentModeProvider = true,
                DisableFileMemory = true,
                DisableAgentSkillsProvider = true,
                DisableOpenTelemetry = true,
            });

            await RunAgentAsync(agent, state, askUser, userMessage, chatId, runId, group, ct);
        }
        catch (OperationCanceledException)
        {
            await OnStoppedAsync(state, chatId, runId, group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiChat: run of chat {ChatId} failed", chatId);
            await OnFailedAsync(state, chatId, runId, group, ex.GetBaseException().Message);
        }
    }

    private async Task RunAgentAsync(
        AIAgent agent, AiChatSessionState state, AskUserTool askUser,
        string userMessage, Guid chatId, Guid runId, string group, CancellationToken ct)
    {
        var session = await RestoreSessionAsync(agent, state, ct);

        var finalText = new StringBuilder();
        var currentCallName = "";

        await foreach (var update in agent.RunStreamingAsync(userMessage, session, null, ct))
        {
            // Фрагменты текста ассистента
            if (update.Role == ChatRole.Assistant && !string.IsNullOrEmpty(update.Text))
            {
                finalText.Append(update.Text);
                await SendCoreAsync(group, AiChatHubEvents.Chunk, [chatId, runId, update.Text]);
            }

            if (update.Contents is not { } contents) continue;

            foreach (var content in contents)
            {
                switch (content)
                {
                    case FunctionCallContent call:
                        // Текст до вызова инструмента сохраняем отдельным сообщением
                        FlushText(state, finalText);
                        currentCallName = call.Name;

                        var callArgs = call.Arguments;
                        var argsJson = callArgs is null ? "" : JsonSerializer.Serialize(callArgs);
                        state.Messages.Add(NewMessage(AiChatMessageRole.Tool, argsJson, toolName: call.Name));
                        await SendCoreAsync(group, AiChatHubEvents.ToolCall, [chatId, runId, call.Name, argsJson]);

                        if (call.Name == AskUserTool.FunctionName)
                        {
                            var question = callArgs is not null && callArgs.TryGetValue("question", out var q)
                                ? q?.ToString() ?? ""
                                : "";
                            await SendCoreAsync(group, AiChatHubEvents.Question, [chatId, runId, question]);
                        }
                        break;

                    case FunctionResultContent result:
                        var resultText = result.Result?.ToString() ?? "";
                        state.Messages.Add(NewMessage(AiChatMessageRole.Tool, resultText, toolName: currentCallName, isToolResult: true));
                        await SendCoreAsync(group, AiChatHubEvents.ToolResult, [chatId, runId, currentCallName, resultText]);
                        break;
                }
            }
        }

        FlushText(state, finalText);

        // Сохраняем сессию агента только после успешного завершения —
        // при остановке в истории мог остаться незакрытый вызов инструмента.
        try
        {
            var serialized = await agent.SerializeSessionAsync(session, null, ct);
            state.SerializedAgentSession = serialized.GetRawText();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AiChat: failed to serialize agent session of chat {ChatId}", chatId);
        }

        if (askUser.LastQuestion is not null)
            state.PendingQuestion = askUser.LastQuestion;

        await _store.SaveAsync(state, CancellationToken.None);
        await SendCoreAsync(group, AiChatHubEvents.Done, [chatId, runId, state.PendingQuestion ?? ""]);
    }

    private async Task<AgentSession> RestoreSessionAsync(AIAgent agent, AiChatSessionState state, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(state.SerializedAgentSession))
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(state.SerializedAgentSession);
                return await agent.DeserializeSessionAsync(json, null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AiChat: failed to restore agent session of chat {ChatId}, starting a new one", state.Id);
            }
        }

        return await agent.CreateSessionAsync(ct);
    }

    static void FlushText(AiChatSessionState state, StringBuilder text)
    {
        if (text.Length == 0) return;
        state.Messages.Add(NewMessage(AiChatMessageRole.Assistant, text.ToString()));
        text.Clear();
    }

    static AiChatMessageDto NewMessage(AiChatMessageRole role, string content, string? toolName = null, bool isToolResult = false) => new()
    {
        Id = Guid.NewGuid(),
        Role = role,
        Content = content,
        ToolName = toolName,
        IsToolResult = isToolResult,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private async Task OnStoppedAsync(AiChatSessionState state, Guid chatId, Guid runId, string group)
    {
        state.Messages.Add(NewMessage(AiChatMessageRole.Info, "Остановлено пользователем."));
        await _store.SaveAsync(state, CancellationToken.None);
        await SendCoreAsync(group, AiChatHubEvents.Stopped, [chatId, runId]);
    }

    private async Task OnFailedAsync(AiChatSessionState state, Guid chatId, Guid runId, string group, string error)
    {
        state.Messages.Add(NewMessage(AiChatMessageRole.Error, error));
        await _store.SaveAsync(state, CancellationToken.None);
        await SendCoreAsync(group, AiChatHubEvents.Error, [chatId, runId, error]);
    }

    private Task SendCoreAsync(string group, string eventName, object?[] args)
        => _hub.Clients.Group(group).SendCoreAsync(eventName, args);
}
