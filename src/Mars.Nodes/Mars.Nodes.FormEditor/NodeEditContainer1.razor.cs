using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mars.Nodes.Core;
using Mars.Nodes.Front.Abstractions.Editor.Interfaces;
using Mars.Nodes.Front.Abstractions.Services;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Mars.Nodes.FormEditor;

public partial class NodeEditContainer1
{
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] ILogger<NodeEditContainer1> _logger { get; set; } = default!;
    [Inject] INodesLocator _nodesLocator { get; set; } = default!;
    [Inject] IServiceProvider Services { get; set; } = default!;

    [CascadingParameter] public INodeEditorApi _nodeEditorApi { get; set; } = default!;

    bool _visible;

    Node? _node = default!;

    public Node? Node => _node;

    /// <summary>Растёт, когда приходят новые данные хоста — по нему компоненты сбрасывают кэш подсказок.</summary>
    public int ValueFieldsVersion => HintService?.Version ?? 0;

    IHostValueHints? HintService => Services.GetService(typeof(IHostValueHints)) as IHostValueHints;

    /// <summary>
    /// Подсказки для поля значения редактируемой ноды; пусто, если провайдер не зарегистрирован
    /// (хост без NodeWorkspace, напр. пререндер на сервере).
    /// </summary>
    public IReadOnlyCollection<ValueFieldInfo> GetValueFields(string? fieldName)
    {
        if (_node is null || _nodeEditorApi?.AllNodes is not { } nodes) return [];
        if (Services.GetService(typeof(IValueFieldProvider)) is not IValueFieldProvider provider) return [];

        return provider.GetFields(new ValueFieldContext(nodes, _node, fieldName));
    }

    [Parameter] public EventCallback<Node> OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<string> OnDelete { get; set; }
    [Parameter] public EventCallback<string> OnClickEditConfigNode { get; set; } //TODO: Убрать это и все делать через _nodeEditorApi
    [Parameter] public EventCallback<AppendNewConfigNodeEvent> OnClickNewConfigNode { get; set; } // и это

    /// <summary>Панель открылась/закрылась — хост (NodeEditor1) переключает правую панель между формой и консолью.</summary>
    [Parameter] public EventCallback<bool> EditingChanged { get; set; }

    CodeEditor2 codeEditor = default!;
    bool isEditJsonMode;
    static JsonSerializerOptions? _nodeJsonSerializerOptionsWithIdent;

    NodeFormEditor1 nodeFormEditor1 = default!;
    bool saveLoading;

    /// <summary>Стек вложенных форм (напр. создание конфига из формы ноды): закрытие верхней возвращает предыдущую.</summary>
    private readonly Stack<Node> _windowStack = new();

    protected override void OnInitialized()
    {
        _nodeJsonSerializerOptionsWithIdent = new JsonSerializerOptions
        {
            IncludeFields = false,
            MaxDepth = 0,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            Converters = { new Mars.Nodes.Core.Converters.NodeJsonConverter(_nodesLocator) }
        };
    }

    void SetVisible(bool show)
    {
        if (_visible == show) return;
        _visible = show;
        _ = EditingChanged.InvokeAsync(show);
    }

    /// <summary>Обычное открытие ноды (dblclick/палитра): БЕЗ стека — заменяет текущую форму.</summary>
    public void StartEditNode(Node node)
    {
        _windowStack.Clear();
        OpenNode(node);
    }

    /// <summary>Вложенное открытие (создание/редактирование конфига из формы): в стек, закрытие вернёт предыдущую форму.</summary>
    public void StartEditNodeStacked(Node node)
    {
        OpenNode(node);
    }

    void OpenNode(Node node)
    {
        _node = node.Copy(_nodeEditorApi.NodesJsonSerializerOptions);
        _windowStack.Push(_node);
        _mode = NepMode.Form;
        isEditJsonMode = false;
        SetVisible(true);
    }

    void CloseEditNode()
    {
        SetVisible(false);
        _node = null;

        _logger.LogTrace("CloseEditNode");
        if (_windowStack.Count > 0)
            _windowStack.Pop();
        if (_windowStack.Any())
        {
            _node = _windowStack.Peek();
            _mode = NepMode.Form;
            isEditJsonMode = false;
            SetVisible(true);
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

    #region panel modes (Form / Docs) + json toggle inside Form

    enum NepMode { Form, Docs }

    NepMode _mode = NepMode.Form;

    async Task SetModeAsync(NepMode mode)
    {
        if (_mode == mode) return;

        if (isEditJsonMode && !await TryApplyJsonAsync()) return;

        _mode = mode;
        StateHasChanged();
    }

    /// <summary>Тумблер JSON в шапке формы (как в старом диалоге): туда — сериализуем модель, обратно — применяем текст.</summary>
    async Task ToggleEditMode()
    {
        if (!isEditJsonMode)
        {
            isEditJsonMode = true;
            StateHasChanged();
            await Task.Delay(10);

            var json = JsonSerializer.Serialize(_node, _nodeJsonSerializerOptionsWithIdent);
            await codeEditor.SetValue(json);
        }
        else
        {
            await TryApplyJsonAsync();
        }
    }

    void CancelJsonEditMode()
    {
        isEditJsonMode = false;
    }

    async Task<bool> TryApplyJsonAsync()
    {
        try
        {
            await CodeEditorJsonToModel();
            isEditJsonMode = false;
            StateHasChanged();
            return true;
        }
        catch (Exception ex)
        {
            _ = _messageService.Error("Проблема json: " + ex.Message);
            return false;
        }
    }

    async Task CodeEditorJsonToModel()
    {
        var json = await codeEditor.GetValue();
        var obj = JsonSerializer.Deserialize(json, _node.GetType(), _nodeJsonSerializerOptionsWithIdent) ?? throw new ArgumentNullException();
        _node = (Node)obj;
    }

    void OnSaveFromCodeEditor(string value)
    {
        _ = FormSaveClick();
    }

    string GetModelAsJson()
    {
        var json = JsonSerializer.Serialize(_node, _nodeJsonSerializerOptionsWithIdent);
        return json;
    }

    #endregion
}
