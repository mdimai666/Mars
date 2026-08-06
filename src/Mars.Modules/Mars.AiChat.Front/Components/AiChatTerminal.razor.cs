using System.Text;
using System.Text.Json.Serialization;
using AppFront.Shared.Interfaces;
using Mars.AiChat.Front.Services;
using Mars.AiChat.Shared.Dto;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Mars.AiChat.Front.Components;

public partial class AiChatTerminal : IAiChatModal, IDisposable
{
    private const double FabMargin = 12;

    [Inject] IMarsWebApiClient _client { get; set; } = default!;
    [Inject] AiChatHubClient _hub { get; set; } = default!;
    [Inject] IJSRuntime _js { get; set; } = default!;
    [Inject] IMessageService _messageService { get; set; } = default!;

    private IJSObjectReference? _module;
    private DotNetObjectReference<AiChatTerminal>? _dotnetRef;
    private bool _subscribed;

    private bool _visible;
    private bool _loading;
    private bool _running;
    private bool _sending;
    private bool _scrollRequested;
    private bool _focusRequested;
    private string? _startupError;

    private Guid? _chatId;
    private List<AiChatSessionSummary> _sessions = [];
    private List<AiChatMessageDto> _messages = [];
    private readonly StringBuilder _stream = new();
    private string _input = "";
    private string? _pendingQuestion;

    private ElementReference _fabEl;
    private ElementReference _messagesEl;
    private ElementReference _inputEl;

    private double _fabX = double.NaN;
    private double _fabY = double.NaN;
    private double _fabW = 120;
    private double _fabH = 40;
    private double _dragStartX;
    private double _dragStartY;

    private string FabStyle => double.IsNaN(_fabX)
        ? "left: calc(50% - 60px); bottom: 18px;"
        : $"left: {_fabX:0}px; top: {_fabY:0}px;";

    private string StatusText => _running
        ? "агент работает…"
        : _pendingQuestion is not null
            ? "агент ждёт ответа"
            : $"чат: {CurrentSessionTitle}";

    private string CurrentSessionTitle => _sessions.FirstOrDefault(s => s.Id == _chatId)?.Title ?? "—";

    private string InputPlaceholder => _pendingQuestion is not null
        ? "ответьте на вопрос агента…"
        : "введите задачу и нажмите Enter…";

    public bool IsVisible => _visible;

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/Mars.AiChat.Front/js/mars-aichat.js");
                var pos = await _module.InvokeAsync<FabRect?>("getFabPos");
                if (pos is not null)
                {
                    _fabX = pos.X;
                    _fabY = pos.Y;
                    StateHasChanged();
                }
            }
            catch
            {
                // без JS-модуля чат продолжает работать с позицией по умолчанию
            }
        }

        if (_scrollRequested && _module is not null)
        {
            _scrollRequested = false;
            try { await _module.InvokeVoidAsync("scrollToBottom", _messagesEl); } catch { }
        }

        if (_focusRequested && _module is not null)
        {
            _focusRequested = false;
            try { await _module.InvokeVoidAsync("focusElement", _inputEl); } catch { }
        }
    }

    // ---------- открытие / закрытие ----------

    public void Open()
    {
        if (_visible) return;
        _visible = true;
        _startupError = null;
        StateHasChanged();
        _ = InitChatAsync();
    }

    public void Close()
    {
        _visible = false;
        StateHasChanged();
    }

    public void Toggle()
    {
        if (_visible) Close();
        else Open();
    }

    private async Task InitChatAsync()
    {
        _loading = true;
        StateHasChanged();

        try
        {
            SubscribeHub();
            await _hub.EnsureStartedAsync();

            _sessions = (await _client.AiChat.GetSessions()).ToList();
            if (_sessions.Count == 0)
            {
                var created = await _client.AiChat.CreateSession();
                _sessions.Insert(0, created);
            }

            await SwitchToChatAsync(_sessions[0].Id);
            _focusRequested = true;
        }
        catch (Exception ex)
        {
            _startupError = ex.GetBaseException().Message;
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task SwitchToChatAsync(Guid chatId)
    {
        if (_chatId is { } old && old != chatId)
            await _hub.LeaveChatAsync(old);

        _chatId = chatId;
        await _hub.JoinChatAsync(chatId);

        var dto = await _client.AiChat.GetSession(chatId);
        _messages = dto.Messages;
        _running = dto.IsRunning;
        _pendingQuestion = dto.PendingQuestion;
        _stream.Clear();
        _scrollRequested = true;
        StateHasChanged();
    }

    private async Task ReloadSessionAsync()
    {
        if (_chatId is not { } chatId) return;

        try
        {
            var dto = await _client.AiChat.GetSession(chatId);
            _messages = dto.Messages;
            _running = dto.IsRunning;
            _pendingQuestion = dto.PendingQuestion;
            _stream.Clear();

            // заголовок чата мог измениться
            _sessions = (await _client.AiChat.GetSessions()).ToList();
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }

        _scrollRequested = true;
        _focusRequested = true;
        StateHasChanged();
    }

    // ---------- SignalR ----------

    private void SubscribeHub()
    {
        if (_subscribed) return;
        _subscribed = true;

        _hub.OnChunk += HubOnChunk;
        _hub.OnToolCall += HubOnToolCall;
        _hub.OnToolResult += HubOnToolResult;
        _hub.OnQuestion += HubOnQuestion;
        _hub.OnDone += HubOnDone;
        _hub.OnStopped += HubOnStopped;
        _hub.OnError += HubOnError;
    }

    private void HubOnChunk(Guid chatId, Guid runId, string text)
    {
        if (chatId != _chatId) return;
        _stream.Append(text);
        _scrollRequested = true;
        StateHasChanged();
    }

    private void HubOnToolCall(Guid chatId, Guid runId, string toolName, string argsJson)
    {
        if (chatId != _chatId) return;
        FlushStreamToMessages();
        _messages.Add(new AiChatMessageDto { Role = AiChatMessageRole.Tool, ToolName = toolName, Content = argsJson });
        _scrollRequested = true;
        StateHasChanged();
    }

    private void HubOnToolResult(Guid chatId, Guid runId, string toolName, string result)
    {
        if (chatId != _chatId) return;
        _messages.Add(new AiChatMessageDto { Role = AiChatMessageRole.Tool, ToolName = toolName, Content = result, IsToolResult = true });
        _scrollRequested = true;
        StateHasChanged();
    }

    private void HubOnQuestion(Guid chatId, Guid runId, string question)
    {
        if (chatId != _chatId) return;
        _pendingQuestion = question;
        _scrollRequested = true;
        StateHasChanged();
    }

    private void HubOnDone(Guid chatId, Guid runId, string pendingQuestion)
    {
        if (chatId != _chatId) return;
        _ = ReloadSessionAsync();
    }

    private void HubOnStopped(Guid chatId, Guid runId)
    {
        if (chatId != _chatId) return;
        _ = ReloadSessionAsync();
    }

    private void HubOnError(Guid chatId, Guid runId, string message)
    {
        if (chatId != _chatId) return;
        _ = ReloadSessionAsync();
    }

    private void FlushStreamToMessages()
    {
        if (_stream.Length == 0) return;
        _messages.Add(new AiChatMessageDto { Role = AiChatMessageRole.Assistant, Content = _stream.ToString() });
        _stream.Clear();
    }

    // ---------- ввод ----------

    private async void OnInputKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendAsync();
        }
    }

    private async Task SendAsync()
    {
        if (_chatId is not { } chatId || _running || _sending) return;

        var text = _input.Trim();
        if (text == "") return;

        _sending = true;
        try
        {
            await _client.AiChat.Send(chatId, text);

            _input = "";
            _pendingQuestion = null;
            _messages.Add(new AiChatMessageDto { Role = AiChatMessageRole.User, Content = text });
            _running = true;
            _stream.Clear();
            _scrollRequested = true;
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }
        finally
        {
            _sending = false;
            StateHasChanged();
        }
    }

    private async Task StopAsync()
    {
        if (_chatId is not { } chatId) return;

        try
        {
            await _client.AiChat.Stop(chatId);
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }
    }

    private async Task NewChatAsync()
    {
        try
        {
            var created = await _client.AiChat.CreateSession();
            _sessions.Insert(0, created);
            await SwitchToChatAsync(created.Id);
            _focusRequested = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _ = _messageService.Error(ex.Message);
        }
    }

    private async void OnSessionSelectChanged(ChangeEventArgs e)
    {
        if (Guid.TryParse(e.Value?.ToString(), out var id) && id != _chatId)
        {
            try
            {
                await SwitchToChatAsync(id);
                _focusRequested = true;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _ = _messageService.Error(ex.Message);
            }
        }
    }

    // ---------- плавающая кнопка ----------

    private async void OnFabPointerDown(PointerEventArgs e)
    {
        if (_module is null) return;

        try
        {
            var rect = await _module.InvokeAsync<FabRect>("getFabRect", _fabEl);
            _fabW = rect.W;
            _fabH = rect.H;
            _dragStartX = rect.X;
            _dragStartY = rect.Y;

            if (double.IsNaN(_fabX))
            {
                _fabX = rect.X;
                _fabY = rect.Y;
            }

            _dotnetRef ??= DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("startFabDrag", _dotnetRef, e.PointerId, e.ClientX - rect.X, e.ClientY - rect.Y);
        }
        catch
        {
            // drag недоступен — кнопка работает как обычная
        }
    }

    [JSInvokable]
    public void OnFabDragMove(double x, double y)
    {
        _fabX = x;
        _fabY = y;
        StateHasChanged();
    }

    [JSInvokable]
    public async void OnFabDragEnd(double x, double y)
    {
        var moved = Math.Abs(x - _dragStartX) > 5 || Math.Abs(y - _dragStartY) > 5;
        if (!moved)
        {
            Open();
            return;
        }

        try
        {
            var viewport = await _module!.InvokeAsync<Viewport>("getViewport");

            x = Math.Clamp(x, FabMargin, viewport.W - _fabW - FabMargin);
            y = Math.Clamp(y, FabMargin, viewport.H - _fabH - FabMargin);

            // прилипание к ближайшему краю экрана
            var dLeft = x;
            var dRight = viewport.W - x - _fabW;
            var dTop = y;
            var dBottom = viewport.H - y - _fabH;

            var min = Math.Min(Math.Min(dLeft, dRight), Math.Min(dTop, dBottom));
            if (min == dLeft) x = FabMargin;
            else if (min == dRight) x = viewport.W - _fabW - FabMargin;
            else if (min == dTop) y = FabMargin;
            else y = viewport.H - _fabH - FabMargin;

            _fabX = x;
            _fabY = y;
            await _module!.InvokeVoidAsync("saveFabPos", x, y);
        }
        catch
        {
            // остаёмся на последней позиции
        }

        StateHasChanged();
    }

    private static string Truncate(string? text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= max ? text : text[..max] + "…";
    }

    public void Dispose()
    {
        if (_subscribed)
        {
            _hub.OnChunk -= HubOnChunk;
            _hub.OnToolCall -= HubOnToolCall;
            _hub.OnToolResult -= HubOnToolResult;
            _hub.OnQuestion -= HubOnQuestion;
            _hub.OnDone -= HubOnDone;
            _hub.OnStopped -= HubOnStopped;
            _hub.OnError -= HubOnError;
        }

        _dotnetRef?.Dispose();
    }

    private class FabRect
    {
        [JsonPropertyName("x")] public double X { get; set; }
        [JsonPropertyName("y")] public double Y { get; set; }
        [JsonPropertyName("w")] public double W { get; set; }
        [JsonPropertyName("h")] public double H { get; set; }
    }

    private class Viewport
    {
        [JsonPropertyName("w")] public double W { get; set; }
        [JsonPropertyName("h")] public double H { get; set; }
    }
}
