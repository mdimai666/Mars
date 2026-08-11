using System.Diagnostics;
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
using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Hubs;
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
    private readonly AiChatPageBridge _pageBridge;
    private readonly IPostService _postService;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly MarsSiteTools _siteTools;
    private readonly MarsOptionsTools _optionsTools;
    private readonly MarsSystemTools _systemTools;
    private readonly MarsSqlTools _sqlTools;
    private readonly MarsHttpTools _httpTools;
    private readonly IFrontFilesService _frontFilesService;
    private readonly string _aiRoot;
    private readonly AgentSkillsSource _skillsSource;
    private readonly FileMemoryProvider _fileMemory;
    private readonly ILogger<AiChatAgentService> _logger;

    public AiChatAgentService(
        IHubContext<AiChatHub> hub,
        IAiChatSessionStore store,
        IAiChatClientFactory clientFactory,
        IOptionService optionService,
        AiChatPageBridge pageBridge,
        IPostService postService,
        IHubContext<ChatHub> chatHub,
        MarsSiteTools siteTools,
        MarsOptionsTools optionsTools,
        MarsSystemTools systemTools,
        MarsSqlTools sqlTools,
        MarsHttpTools httpTools,
        IFrontFilesService frontFilesService,
        [FromKeyedServices("data")] IOptions<FileHostingInfo> dataHostingInfo,
        ILoggerFactory loggerFactory,
        FileMemoryProvider fileMemory,
        ILogger<AiChatAgentService> logger)
    {
        _fileMemory = fileMemory;
        _aiRoot = Path.Combine(dataHostingInfo.Value.PhysicalPath.LocalPath, "ai");

        // Скиллы (SKILL.md): кастомные из <data>/ai/skills + bundled рядом со сборкой;
        // агент может дописывать свои через file_access_*
        var customSkills = Path.Combine(_aiRoot, "skills");
        var bundledSkills = Path.Combine(AppContext.BaseDirectory, "skills");
        Directory.CreateDirectory(customSkills);
        Directory.CreateDirectory(bundledSkills);
        _skillsSource = new AggregatingAgentSkillsSource(
        [
            new AgentFileSkillsSource(customSkills, null, null, loggerFactory),
            new AgentFileSkillsSource(bundledSkills, null, null, loggerFactory),
        ]);

        _hub = hub;
        _store = store;
        _clientFactory = clientFactory;
        _optionService = optionService;
        _pageBridge = pageBridge;
        _postService = postService;
        _chatHub = chatHub;
        _siteTools = siteTools;
        _optionsTools = optionsTools;
        _systemTools = systemTools;
        _sqlTools = sqlTools;
        _httpTools = httpTools;
        _frontFilesService = frontFilesService;
        _logger = logger;
    }

    public async Task RunChatAsync(Guid chatId, Guid userId, string userMessage, string? pageContext, CancellationToken ct)
    {
        var state = await _store.GetAsync(chatId, userId, ct)
            ?? throw new NotFoundException($"AiChat session '{chatId}' not found");

        var runId = Guid.NewGuid();
        var group = AiChatHub.GroupName(chatId);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("AiChat: run {RunId} started in chat {ChatId} (user {UserId}), page: {PageContext}, message: {Message}",
            runId, chatId, userId, pageContext ?? "-", userMessage);

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

            var askUser = new AskUserTool();
            var pageTools = new MarsOpenPageTools(_pageBridge, chatId);
            var postTools = new MarsPostTools(_postService, _chatHub, userId);
            var tools = new List<AIFunction>
            {
                AIFunctionFactory.Create(_siteTools.GetSiteSettings),
                AIFunctionFactory.Create(_siteTools.UpdateSiteSettings),
                AIFunctionFactory.Create(_optionsTools.ListSiteOptions),
                AIFunctionFactory.Create(_optionsTools.GetSiteOption),
                AIFunctionFactory.Create(_optionsTools.UpdateSiteOption),
                AIFunctionFactory.Create(_systemTools.GetSystemInfo),
                AIFunctionFactory.Create(_httpTools.HttpRequest),
                AIFunctionFactory.Create(postTools.CreatePost),
                AIFunctionFactory.Create(postTools.GetPost),
                AIFunctionFactory.Create(postTools.ListPosts),
                AIFunctionFactory.Create(pageTools.GetOpenPageInfo),
                AIFunctionFactory.Create(pageTools.GetOpenPageFields),
                AIFunctionFactory.Create(pageTools.SetOpenPageField),
                AIFunctionFactory.Create(pageTools.SaveOpenPage),
                AIFunctionFactory.Create(askUser.AskUser),
            };

            if (option.EnableSqlAccess)
            {
                tools.Add(AIFunctionFactory.Create(_sqlTools.ListDataSources));
                tools.Add(AIFunctionFactory.Create(_sqlTools.GetDatabaseSchema));
                tools.Add(AIFunctionFactory.Create(_sqlTools.ExecuteSql));

                _logger.LogDebug("AiChat: chat {ChatId} SQL tools enabled", chatId);
            }

            // Файлы фронта — только когда открыт редактор фронта (slug из URL страницы)
            var frontEditorSlug = MarsFrontFilesTools.TryParseSlugFromPageContext(pageContext);
            if (frontEditorSlug is not null)
            {
                var frontTools = new MarsFrontFilesTools(_frontFilesService, frontEditorSlug);
                tools.Add(AIFunctionFactory.Create(frontTools.ListFrontFiles));
                tools.Add(AIFunctionFactory.Create(frontTools.ReadFrontFile));
                tools.Add(AIFunctionFactory.Create(frontTools.WriteFrontFile));
                tools.Add(AIFunctionFactory.Create(frontTools.CreateFrontFile));
                tools.Add(AIFunctionFactory.Create(frontTools.RenameFrontFile));
                tools.Add(AIFunctionFactory.Create(frontTools.DeleteFrontFile));

                _logger.LogDebug("AiChat: chat {ChatId} front files tools enabled for front '{FrontSlug}'", chatId, frontEditorSlug);
            }

            var client = _clientFactory.CreateClient(connection);
            agent = client.AsHarnessAgent(new HarnessAgentOptions
            {
                Name = "mars-site-agent",
                ChatOptions = new ChatOptions
                {
                    Instructions = AiChatPrompts.BuildInstructions(option, pageContext, frontEditorSlug),
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
                // Скиллы (SKILL.md) и рабочая папка агента — <data>/ai
                AgentSkillsSource = _skillsSource,
                FileAccessStore = new FileSystemAgentFileStore(_aiRoot),
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

                        _logger.LogDebug("AiChat: chat {ChatId} tool call {ToolName}, args: {Args} ({ElapsedMs} ms)",
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

                        _logger.LogDebug("AiChat: chat {ChatId} tool result {ToolName}: {Result} ({ElapsedMs} ms)",
                            chatId, currentCallName, Truncate(resultText), stopwatch.ElapsedMilliseconds);
                        break;
                }
            }
        }

        var answerLength = finalText.Length;
        FlushText(state, finalText);
        _logger.LogDebug("AiChat: chat {ChatId} assistant answer {Length} chars ({ElapsedMs} ms)",
            chatId, answerLength, stopwatch.ElapsedMilliseconds);

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
                var session = await agent.DeserializeSessionAsync(json, null, ct);
                _logger.LogDebug("AiChat: chat {ChatId} agent session restored", state.Id);
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AiChat: failed to restore agent session of chat {ChatId}, starting a new one", state.Id);
            }
        }

        _logger.LogDebug("AiChat: chat {ChatId} new agent session created", state.Id);
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
