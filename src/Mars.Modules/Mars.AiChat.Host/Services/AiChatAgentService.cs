using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Mars.AiChat.Host.Hubs;
using Mars.AiChat.Host.Shared.Interfaces;
using Mars.AiChat.Host.Shared.Models;
using Mars.AiChat.Host.Tools;
using Mars.AiChat.Host.Toolsets;
using Mars.AiChat.Shared.Dto;
using Mars.AiChat.Shared.Options;
using Mars.AiChat.Shared.SignalR;
using Mars.Core.Exceptions;
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly IReadOnlyList<IAiToolset> _toolsets;
    private readonly string _aiRoot;
    private readonly AiSkillCatalog _catalog;
    private readonly FileMemoryProvider _fileMemory;
    private readonly ILogger<AiChatAgentService> _logger;

    public AiChatAgentService(
        IHubContext<AiChatHub> hub,
        IAiChatSessionStore store,
        IAiChatClientFactory clientFactory,
        IOptionService optionService,
        IEnumerable<IAiToolset> toolsets,
        [FromKeyedServices("data")] IOptions<FileHostingInfo> dataHostingInfo,
        AiSkillCatalog catalog,
        FileMemoryProvider fileMemory,
        ILogger<AiChatAgentService> logger)
    {
        _toolsets = [.. toolsets];
        _fileMemory = fileMemory;
        _catalog = catalog;
        _aiRoot = Path.Combine(dataHostingInfo.Value.PhysicalPath.LocalPath, "ai");

        _hub = hub;
        _store = store;
        _clientFactory = clientFactory;
        _optionService = optionService;
        _logger = logger;
    }

    public async Task RunChatAsync(Guid chatId, Guid userId, string userMessage, string? pageContext, CancellationToken ct,
        bool? overrideSkills = null, bool? overrideAccess = null)
    {
        var state = await _store.GetAsync(chatId, userId, ct)
            ?? throw new NotFoundException($"AiChat session '{chatId}' not found");

        var runId = Guid.NewGuid();
        var group = AiChatHub.GroupName(chatId);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("AiChat: run {RunId} started in chat {ChatId} (user {UserId}), page: {PageContext}, savedAgentSession: {HasSaved}, uiMessages: {Messages}, message: {Message}",
            runId, chatId, userId, pageContext ?? "-", !string.IsNullOrEmpty(state.SerializedAgentSession), state.Messages.Count, userMessage);

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

            _logger.LogDebug("AiChat: chat {ChatId} uses connection '{ConnectionName}' ({ProviderType}, model '{ModelId}')",
                chatId, connection.Name, connection.ProviderType, connection.ModelId);

            // Файлы фронта — только когда открыт редактор фронта (slug из URL страницы)
            var frontEditorSlug = MarsFrontFilesTools.TryParseSlugFromPageContext(pageContext);
            var skillsOn = overrideSkills ?? true;

            // Инструменты собираются из зарегистрированных тулсетов по контексту запуска
            var askUser = new AskUserTool();
            var toolsetCtx = new AiToolsetContext(userId, chatId, option, pageContext, frontEditorSlug, askUser, skillsOn);
            var activeToolsets = _toolsets.Where(t => t.IsEnabled(toolsetCtx)).ToList();
            var tools = activeToolsets.SelectMany(t => t.Build(toolsetCtx)).ToList();

            // Каталог скиллов: компактный список в контекст + полные инструкции
            // скиллов открытой страницы (детерминированный роутинг)
            var allSkills = await _catalog.GetSkillsAsync(ct);
            var skillsListing = string.Join("\n", allSkills.Select(s => $"- {s.Name}: {s.Description}"));
            var routedNames = PageSkillRouter.Route(pageContext, frontEditorSlug, option);
            var preloaded = new List<(string Name, string Body)>();
            foreach (var name in routedNames)
            {
                var skill = allSkills.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
                if (skill is not null) preloaded.Add((skill.Name, skill.Body));
            }

            _logger.LogDebug("AiChat: chat {ChatId} active toolsets: {Toolsets}, tools: {Tools}, preloaded skills: {Preloaded}",
                chatId, string.Join(", ", activeToolsets.Select(t => t.Name)), tools.Count, string.Join(", ", routedNames));

            var client = _clientFactory.CreateClient(connection);
            agent = client.AsHarnessAgent(new HarnessAgentOptions
            {
                Name = "mars-site-agent",
                ChatOptions = new ChatOptions
                {
                    Instructions = AiChatPrompts.BuildInstructions(option, pageContext, skillsListing, preloaded),
                    Tools = [.. tools],
                },
                MaximumIterationsPerRequest = 15,
                DisableWebSearch = true,
                DisableTodoProvider = true,
                DisableAgentModeProvider = true,
                // Память агента — общие файлы в <data>/ai/memory (не привязана к чату).
                // Встроенный FileMemoryProvider отключён: его дефолт изолирует папку на сессию,
                // вместо него подключаем свой провайдер с общим working folder.
                DisableFileMemory = true,
                AIContextProviders = [_fileMemory],
                // Скиллы — свой тулсет (каталог в контексте + SearchSkills/LoadSkill),
                // MAF-провайдер выключен; рабочая папка file_access_* включена по умолчанию
                // (--access на полигоне для A/B).
                DisableAgentSkillsProvider = true,
                FileAccessStore = (overrideAccess ?? true) ? new FileSystemAgentFileStore(_aiRoot) : null,
                FileAccessProviderOptions = new FileAccessProviderOptions
                {
                    DisableReadOnlyToolApproval = true,
                    DisableWriteToolApproval = true,
                },
                DisableOpenTelemetry = true,
            });

            await RunAgentAsync(agent, state, askUser, userMessage, chatId, runId, group, stopwatch, ct);

            _logger.LogDebug("AiChat: run {RunId} in chat {ChatId} done in {ElapsedMs} ms, history {Messages} messages",
                runId, chatId, stopwatch.ElapsedMilliseconds, state.Messages.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("AiChat: run {RunId} in chat {ChatId} stopped by user after {ElapsedMs} ms",
                runId, chatId, stopwatch.ElapsedMilliseconds);
            await OnStoppedAsync(state, chatId, runId, group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AiChat: run {RunId} of chat {ChatId} failed after {ElapsedMs} ms",
                runId, chatId, stopwatch.ElapsedMilliseconds);
            await OnFailedAsync(state, chatId, runId, group, ex.GetBaseException().Message);
        }
    }

    private async Task RunAgentAsync(
        AIAgent agent, AiChatSessionState state, AskUserTool askUser,
        string userMessage, Guid chatId, Guid runId, string group, Stopwatch stopwatch, CancellationToken ct)
    {
        var session = await RestoreSessionAsync(agent, state, ct);

        var finalText = new StringBuilder();
        var currentCallName = "";
        ChatFinishReason? lastFinish = null;

        await foreach (var update in agent.RunStreamingAsync(userMessage, session, null, ct))
        {
            if (update.FinishReason is not null) lastFinish = update.FinishReason;

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

                        _logger.LogInformation("AiChat: chat {ChatId} tool call {ToolName}, args: {Args} ({ElapsedMs} ms)",
                            chatId, call.Name, Truncate(argsJson), stopwatch.ElapsedMilliseconds);

                        if (call.Name == AskUserTool.FunctionName)
                        {
                            var question = callArgs is not null && callArgs.TryGetValue("question", out var q)
                                ? q?.ToString() ?? ""
                                : "";
                            await SendCoreAsync(group, AiChatHubEvents.Question, [chatId, runId, question]);

                            _logger.LogDebug("AiChat: chat {ChatId} asks user: {Question}", chatId, question);
                        }
                        break;

                    case FunctionResultContent result:
                        var resultText = result.Result?.ToString() ?? "";
                        state.Messages.Add(NewMessage(AiChatMessageRole.Tool, resultText, toolName: currentCallName, isToolResult: true));
                        await SendCoreAsync(group, AiChatHubEvents.ToolResult, [chatId, runId, currentCallName, resultText]);

                        _logger.LogInformation("AiChat: chat {ChatId} tool result {ToolName}: {Result} ({ElapsedMs} ms)",
                            chatId, currentCallName, Truncate(resultText), stopwatch.ElapsedMilliseconds);
                        break;

                    case UsageContent usage:
                        _logger.LogInformation("AiChat: chat {ChatId} usage: {InputTokens} in / {OutputTokens} out tokens",
                            chatId, usage.Details?.InputTokenCount, usage.Details?.OutputTokenCount);
                        break;
                }
            }
        }

        var answerLength = finalText.Length;
        FlushText(state, finalText);
        _logger.LogInformation("AiChat: chat {ChatId} assistant answer {Length} chars, finish {FinishReason} ({ElapsedMs} ms)",
            chatId, answerLength, lastFinish?.ToString() ?? "-", stopwatch.ElapsedMilliseconds);

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
            // История harness могла сохраниться битой (assistant с tool_calls без tool-ответов) —
            // провайдер тогда отвечает 400 invalid_request; лечим сбросом сессии
            if (TryFindOrphanToolCalls(state.SerializedAgentSession, out var orphan))
            {
                _logger.LogWarning("AiChat: chat {ChatId} saved agent session is corrupted (tool_call {Orphan} without tool result); starting a new one", state.Id, orphan);
            }
            else
            {
                try
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(state.SerializedAgentSession);
                    var session = await agent.DeserializeSessionAsync(json, null, ct);
                    _logger.LogInformation("AiChat: chat {ChatId} agent session restored", state.Id);
                    return session;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AiChat: failed to restore agent session of chat {ChatId}, starting a new one", state.Id);
                }
            }
        }

        _logger.LogInformation("AiChat: chat {ChatId} new agent session created", state.Id);
        return await agent.CreateSessionAsync(ct);
    }

    static bool TryFindOrphanToolCalls(string serialized, out string orphanCallId)
    {
        orphanCallId = "";
        try
        {
            using var doc = JsonDocument.Parse(serialized);
            var messages = FindMessagesArray(doc.RootElement);
            if (messages is null) return false;

            for (var i = 0; i < messages.Value.GetArrayLength(); i++)
            {
                var m = messages.Value[i];
                if (!m.TryGetProperty("role", out var role) || role.GetString() != "assistant") continue;

                List<string> callIds;
                if (m.TryGetProperty("contents", out var contents))
                {
                    callIds = [.. contents.EnumerateArray()
                        .Where(c => c.TryGetProperty("$type", out var t) && t.GetString() == "functionCall" && c.TryGetProperty("callId", out _))
                        .Select(c => c.GetProperty("callId").GetString() ?? "")];
                }
                else
                {
                    callIds = [];
                }
                if (callIds.Count == 0) continue;

                var covered = new HashSet<string>();
                if (i + 1 < messages.Value.GetArrayLength())
                {
                    var next = messages.Value[i + 1];
                    if (next.TryGetProperty("role", out var nr) && nr.GetString() == "tool" && next.TryGetProperty("contents", out var nc))
                    {
                        foreach (var c in nc.EnumerateArray())
                        {
                            if (c.TryGetProperty("$type", out var t2) && t2.GetString() == "functionResult" && c.TryGetProperty("callId", out var cid))
                                covered.Add(cid.GetString() ?? "");
                        }
                    }
                }

                var missing = callIds.FirstOrDefault(id => !covered.Contains(id));
                if (missing is not null)
                {
                    orphanCallId = missing;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            // не смогли разобрать — не мешаем восстановлению, ошибка проявится и залогироваться ниже
            return false;
        }
    }

    static JsonElement? FindMessagesArray(JsonElement root)
    {
        var queue = new Queue<(JsonElement Element, int Depth)>();
        queue.Enqueue((root, 0));
        while (queue.Count > 0)
        {
            var (el, depth) = queue.Dequeue();
            if (depth > 4) continue;
            switch (el.ValueKind)
            {
                case JsonValueKind.Array:
                {
                    using var e = el.EnumerateArray();
                    if (e.MoveNext() && e.Current.ValueKind == JsonValueKind.Object && e.Current.TryGetProperty("role", out _))
                        return el;
                    break;
                }
                case JsonValueKind.Object:
                    foreach (var p in el.EnumerateObject()) queue.Enqueue((p.Value, depth + 1));
                    break;
            }
            if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var c in el.EnumerateArray()) queue.Enqueue((c, depth + 1));
            }
        }
        return null;
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

    static string Truncate(string? text, int max = 300)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= max ? text : text[..max] + "…";
    }

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
