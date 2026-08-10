using System.Globalization;
using System.Text;
using System.Text.Json;
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
    [Inject] NavigationManager _navigation { get; set; } = default!;

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

    private ElementReference _termEl;
    private ElementReference _fabEl;
    private ElementReference _messagesEl;
    private ElementReference _inputEl;

    private double _fabW = 120;
    private double _fabH = 40;

    // Позиция кнопки: край + доля вдоль него (0..1). Сам край задаётся CSS-свойством
    // (left/right/top/bottom), отступ считается через calc() от процента — при ресайзе
    // окна кнопка остаётся у выбранного края без JS-обработчиков.
    // _fabX/_fabY (абсолютные координаты) живут только на время перетаскивания.
    private FabEdge? _fabEdge;
    private double _fabPos;
    private double _fabX = double.NaN;
    private double _fabY = double.NaN;

    private string FabStyle
    {
        get
        {
            if (!double.IsNaN(_fabX))
                return $"left: {_fabX:0}px; top: {_fabY:0}px;";

            return _fabEdge switch
            {
                FabEdge.Left => $"left: {FabMargin:0}px; top: {EdgeOffsetCss(_fabH)};",
                FabEdge.Right => $"right: {FabMargin:0}px; top: {EdgeOffsetCss(_fabH)};",
                FabEdge.Top => $"top: {FabMargin:0}px; left: {EdgeOffsetCss(_fabW)};",
                FabEdge.Bottom => $"bottom: {FabMargin:0}px; left: {EdgeOffsetCss(_fabW)};",
                _ => "left: calc(50% - 60px); bottom: 18px;",
            };
        }
    }

    // Доля свободного пространства вдоль края; max(0px, ...) не даёт уйти в минус
    // в окне, которое меньше кнопки.
    private string EdgeOffsetCss(double buttonSize)
        => $"max(0px, calc({_fabPos.ToString("0.####", CultureInfo.InvariantCulture)} * (100% - {buttonSize + FabMargin:0}px)))";

    // Позиция окна терминала живёт только в памяти: переживает свернуть/развернуть,
    // но после перезагрузки страницы окно снова открывается внизу по центру (NaN → CSS).
    private double _termX = double.NaN;
    private double _termY = double.NaN;

    private string TermStyle => double.IsNaN(_termX)
        ? ""
        : $"left: {_termX:0}px; top: {_termY:0}px; bottom: auto; transform: none;";

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
                _module = await _js.InvokeAsync<IJSObjectReference>("import", AiChatAssets.JsModuleUrl);
                var anchor = await _module.InvokeAsync<FabAnchor?>("getFabPos");
                if (anchor is not null)
                {
                    _fabEdge = ParseFabEdge(anchor.Edge);
                    _fabPos = Math.Clamp(anchor.Pos, 0, 1);
                    StateHasChanged();
                }
            }
            catch
            {
                // без JS-модуля чат продолжает работать с позицией по умолчанию
            }
        }

        // Скроллим только когда лента реально отрисована: пока _loading = true,
        // вместо сообщений рендерится заглушка, и запрос скролла исчез бы впустую —
        // тогда при открытии чата с историей лента оставалась бы сверху.
        if (_scrollRequested && !_loading && _module is not null)
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
        _hub.OnPageToolRequest += HubOnPageToolRequest;
        _hub.OnReconnected += HubOnReconnected;
    }

    private void HubOnReconnected()
    {
        // За время обрыва события (чанки, Done, Question) не доставлялись —
        // перечитываем состояние чата с сервера, иначе UI останется «зависшим».
        _ = ReloadSessionAsync();
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

    // ---------- инструменты на открытой странице ----------

    private async void HubOnPageToolRequest(Guid chatId, AiPageToolRequest request)
    {
        if (chatId != _chatId) return;

        var result = await ExecutePageToolAsync(request);
        await _hub.SendPageToolResultAsync(chatId, result);
    }

    private static async Task<AiPageToolResult> ExecutePageToolAsync(AiPageToolRequest request)
    {
        var handler = AiChatPageHandlerHolder.Current;
        if (handler is null)
        {
            return new AiPageToolResult
            {
                RequestId = request.RequestId,
                Ok = false,
                Result = "Сейчас не открыта страница редактирования поста.",
            };
        }

        try
        {
            var resultText = request.Tool switch
            {
                "get_open_page_info" => handler.GetInfo(),
                "get_open_page_fields" => await handler.GetFields(),
                "set_open_page_field" => await HandleSetFieldAsync(handler, request.ArgsJson),
                "save_open_page" => await handler.Save(),
                _ => throw new InvalidOperationException($"Неизвестный инструмент страницы '{request.Tool}'"),
            };

            return new AiPageToolResult { RequestId = request.RequestId, Ok = true, Result = resultText };
        }
        catch (Exception ex)
        {
            return new AiPageToolResult { RequestId = request.RequestId, Ok = false, Result = ex.GetBaseException().Message };
        }
    }

    private static async Task<string> HandleSetFieldAsync(IAiChatPageHandler handler, string argsJson)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var field = doc.RootElement.TryGetProperty("field", out var f) ? f.GetString() ?? "" : "";
        var value = doc.RootElement.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "";
        return await handler.SetField(field, value);
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
            await _client.AiChat.Send(chatId, text, GetCurrentPageContext());

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

    private string? GetCurrentPageContext()
    {
        try
        {
            var relative = _navigation.ToBaseRelativePath(_navigation.Uri);
            return string.IsNullOrEmpty(relative) ? null : "/" + relative;
        }
        catch
        {
            return null;
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
            _dotnetRef ??= DotNetObjectReference.Create(this);

            // один вызов: JS сам берёт rect и сразу вешает слушатели на window,
            // различая клик и перетаскивание по порогу смещения
            var start = await _module.InvokeAsync<FabRect>("startFabDrag", _dotnetRef, _fabEl, e.PointerId, e.ClientX, e.ClientY);
            _fabW = start.W;
            _fabH = start.H;

            // drag всегда стартует от фактического rect: когда кнопка прижата к краю,
            // в стиле нет left/top, и только rect знает, где она реально
            _fabX = start.X;
            _fabY = start.Y;
        }
        catch
        {
            // drag недоступен — кнопка работает как обычная
        }
    }

    [JSInvokable]
    public void OnFabClick()
    {
        Open();
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
        try
        {
            var viewport = await _module!.InvokeAsync<Viewport>("getViewport");

            x = Math.Clamp(x, FabMargin, viewport.W - _fabW - FabMargin);
            y = Math.Clamp(y, FabMargin, viewport.H - _fabH - FabMargin);

            // прилипание к ближайшему краю; вдоль края запоминаем долю (0..1),
            // а не пиксели — тогда при ресайзе кнопка остаётся у своего края
            var dLeft = x;
            var dRight = viewport.W - x - _fabW;
            var dTop = y;
            var dBottom = viewport.H - y - _fabH;

            var min = Math.Min(Math.Min(dLeft, dRight), Math.Min(dTop, dBottom));

            FabEdge edge;
            double pos;
            if (min == dLeft || min == dRight)
            {
                edge = min == dLeft ? FabEdge.Left : FabEdge.Right;
                pos = AlongEdgeFraction(y, viewport.H - _fabH);
            }
            else
            {
                edge = min == dTop ? FabEdge.Top : FabEdge.Bottom;
                pos = AlongEdgeFraction(x, viewport.W - _fabW);
            }

            _fabEdge = edge;
            _fabPos = pos;
            _fabX = double.NaN;
            _fabY = double.NaN;

            try
            {
                await _module!.InvokeVoidAsync("saveFabPos", FabEdgeName(edge), pos);
            }
            catch
            {
                // позиция просто не сохранится до следующего перетаскивания
            }
        }
        catch
        {
            // вьюпорт недоступен — оставляем кнопку в точке броска
            _fabX = x;
            _fabY = y;
        }

        StateHasChanged();
    }

    // ---------- перетаскивание окна ----------

    private async void OnTermHeaderPointerDown(PointerEventArgs e)
    {
        if (_module is null) return;

        try
        {
            _dotnetRef ??= DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("startTermDrag", _dotnetRef, _termEl, e.PointerId, e.ClientX, e.ClientY);
        }
        catch
        {
            // без JS-модуля окно просто не перетаскивается
        }
    }

    [JSInvokable]
    public void OnTermDragMove(double x, double y)
    {
        _termX = x;
        _termY = y;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnTermDragEnd(double x, double y)
    {
        _termX = x;
        _termY = y;
        StateHasChanged();
    }

    private static double AlongEdgeFraction(double coord, double span)
        => span <= 0 ? 0 : Math.Clamp(coord / span, 0, 1);

    private static string FabEdgeName(FabEdge edge) => edge switch
    {
        FabEdge.Left => "left",
        FabEdge.Right => "right",
        FabEdge.Top => "top",
        _ => "bottom",
    };

    private static FabEdge? ParseFabEdge(string? edge) => edge switch
    {
        "left" => FabEdge.Left,
        "right" => FabEdge.Right,
        "top" => FabEdge.Top,
        "bottom" => FabEdge.Bottom,
        _ => null,
    };

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
            _hub.OnPageToolRequest -= HubOnPageToolRequest;
            _hub.OnReconnected -= HubOnReconnected;
        }

        _dotnetRef?.Dispose();
    }

    private enum FabEdge { Left, Right, Top, Bottom }

    private class FabAnchor
    {
        [JsonPropertyName("edge")] public string Edge { get; set; } = "";
        [JsonPropertyName("pos")] public double Pos { get; set; }
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
