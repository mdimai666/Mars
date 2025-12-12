using System.Text.Json;
using Mars.Nodes.Core;
using Mars.Nodes.EditorApi.Interfaces;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Nodes.FormEditor;

public partial class NodeEditContainer1
{
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] ILogger<NodeEditContainer1> _logger { get; set; } = default!;

    [CascadingParameter] public INodeEditorApi _nodeEditorApi { get; set; } = default!;

    FluentDialog _dialog = default!;
    bool _visible;

    Node? _node = default!;
    [Parameter] public EventCallback<Node> OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback OnBackdropCancel { get; set; }
    [Parameter] public EventCallback<string> OnDelete { get; set; }
    [Parameter] public EventCallback<string> OnClickEditConfigNode { get; set; } //TODO: Убрать это и все делать через _nodeEditorApi
    [Parameter] public EventCallback<AppendNewConfigNodeEvent> OnClickNewConfigNode { get; set; } // и это

    [Parameter] public bool DisableSaveOnBackdropClick { get; set; } = false;

    CodeEditor2 codeEditor = default!;
    bool isEditJsonMode;

    NodeFormEditor1 nodeFormEditor1 = default!;
    bool saveLoading;

    private readonly Stack<Node> _windowStack = new();

    void OpenOffcanvasEditor(bool show)
    {
        _visible = show;
    }

    public void StartEditNode(Node node)
    {
        _node = node.Copy(_nodeEditorApi.NodesJsonSerializerOptions);
        _windowStack.Push(_node);
        OpenOffcanvasEditor(true);
    }

    void CloseEditNode()
    {
        OpenOffcanvasEditor(false);
        _node = null;

        _logger.LogTrace("CloseEditNode");
        _windowStack.Pop();
        if (_windowStack.Any())
        {
            _node = _windowStack.Peek();
            OpenOffcanvasEditor(true);
        }
    }

    public async Task FormSaveClick()
    {
        if (nodeFormEditor1.Form is not null)
        {
            saveLoading = true;
            StateHasChanged();

            if (isEditJsonMode) await CodeEditorJsonToModel();

            await nodeFormEditor1.Form.OnEditSave();

            saveLoading = false;
            StateHasChanged();

            if (!nodeFormEditor1.EditForm.EditContext.Validate())
            {
                return;
            }
        }

        await OnSave.InvokeAsync(_node);
        CloseEditNode();
        StateHasChanged();
    }

    async Task FormCloseClick()
    {
        if (nodeFormEditor1.Form is not null)
        {
            await nodeFormEditor1.Form.OnEditCancel();
        }
        await OnCancel.InvokeAsync();
        CloseEditNode();
        StateHasChanged();
    }

    async Task FormDeleteClick()
    {
        if (nodeFormEditor1.Form is not null)
        {
            await nodeFormEditor1.Form.OnEditDelete();
        }
        await OnDelete.InvokeAsync(_node.Id);
        CloseEditNode();
        StateHasChanged();
    }

    Task OnValidSubmit()
    {
        return FormSaveClick();
    }

    async void OnDialogDismiss(DialogEventArgs e)
    {
        if (DisableSaveOnBackdropClick)
        {
            await FormCloseClick();
        }
        else
        {
            await FormSaveClick();
        }

        await OnBackdropCancel.InvokeAsync();
    }

    //json editor
    async void ToggleEditMode()
    {
        if (!isEditJsonMode)
        {
            isEditJsonMode = true;
            StateHasChanged();
            await Task.Delay(10);

            var json = JsonSerializer.Serialize(_node, CodeEditor2.SimpleJsonSerializerOptionsIgnoreReadonly);
            await codeEditor.SetValue(json);
        }
        else
        {
            try
            {
                await CodeEditorJsonToModel();
                isEditJsonMode = false;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                _ = _messageService.Error("Проблема json: " + ex.Message);
            }
        }
    }

    async Task CodeEditorJsonToModel()
    {
        var json = await codeEditor.GetValue();
        var obj = JsonSerializer.Deserialize(json, _node.GetType(), CodeEditor2.SimpleJsonSerializerOptionsIgnoreReadonly) ?? throw new ArgumentNullException();
        _node = (Node)obj;
    }

    void OnSaveFromCodeEditor(string value)
    {
        _ = FormSaveClick();
    }

    string GetModelAsJson()
    {
        var json = JsonSerializer.Serialize(_node, CodeEditor2.SimpleJsonSerializerOptionsIgnoreReadonly);
        return json;
    }

    void CancelJsonEditMode()
    {
        isEditJsonMode = false;
    }
}
