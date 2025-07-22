using AppFront.Shared.Services;
using Mars.Core.Exceptions;
using Mars.Core.Extensions;
using Mars.Shared.Contracts.AIService;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Mars.SemanticKernel.Front.Components.MarsAITools;

public partial class AIToolChatModal : IAIToolModal
{
    [Inject] IMarsWebApiClient _client { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    FluentDialog _dialog = default!;
    bool _visible = false;
    bool _busy;

    [Parameter] public string ChatInput { get; set; } = "";

    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback OnBackdropCancel { get; set; }

    public bool IsVisible => _visible;
    public string? ScenarioName { get; set; }

    AIServiceResponse? response;
    ElementReference aiResponsePreElement = default!;

    async Task CloseClick()
    {
        _visible = false;
        await OnCancel.InvokeAsync();
    }

    void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.CtrlKey && e.Code == "Enter")
        {
            _ = SendMessage();
        }
    }

    async Task SendMessage()
    {
        if (string.IsNullOrEmpty(ChatInput)) return;

        _busy = true;
        StateHasChanged();
        try
        {
            response = await _client.AITool.ToolPrompt(new AIServiceToolRequest { Prompt = ChatInput, ToolName = ScenarioName.AsNullIfEmpty() });
        }
        catch (NotFoundException ex)
        {
            _ = _messageService.Error(ex.Message);
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    async void OnDialogDismiss(DialogEventArgs e)
    {
        _visible = false;
        await OnBackdropCancel.InvokeAsync();
    }

    async void CopyClick()
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", response?.Content);
        await JS.InvokeVoidAsync("selectTextForElement", aiResponsePreElement);

    }

    public void Open(string text = "", string scenarioName = "")
    {
        if (!string.IsNullOrEmpty(text))
        {
            ChatInput = text;
        }

        ScenarioName = scenarioName.AsNullIfEmpty();

        _visible = true;
        StateHasChanged();
    }

    public void Close()
    {
        _visible = false;
        StateHasChanged();
    }
}
