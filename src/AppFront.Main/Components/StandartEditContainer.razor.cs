using System.Text.Json;
using AppFront.Main.Extensions;
using AppFront.Shared.Extensions;
using Flurl.Http;
using Mars.Core.Exceptions;
using Mars.Core.Interfaces;
using Mars.Shared.Resources;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using IMessageService = AppFront.Shared.Interfaces.IMessageService;

namespace AppFront.Shared.Components;

public partial class StandartEditContainer<TModel> : ComponentBase
        where TModel : IHasId, new()
{
    [Inject] NavigationManager NavigationManager { get; set; } = default!;
    [Inject] IMessageService _messageService { get; set; } = default!;

    Guid? _OldId;
    [Parameter] public Guid ID { get; set; }

    [Parameter] public bool CanCreate { get; set; } = true;
    [Parameter] public bool CanDelete { get; set; } = true;
    [Parameter] public string Class { get; set; } = "";
    [Parameter] public string Style { get; set; } = "";
    [Parameter] public string? TitleText { get; set; } = null;
    [Parameter] public bool HideDatesSection { get; set; } = false;
    [Parameter] public bool BlankModelFromGetAction { get; set; } = false;

    [Parameter] public RenderFragment<TModel>? ChildContent { get; set; }
    [Parameter] public RenderFragment<TModel>? SectionSidePublish { get; set; }
    [Parameter] public RenderFragment<TModel>? SectionExtraSidebar { get; set; }

    [Parameter] public RenderFragment<TModel>? SectionActions { get; set; }
    Type? GeneralSectionActions => ContentWrapper.GeneralSectionActions;

    [Parameter] public EventCallback<TModel> OnLoadData { get; set; }
    [Parameter] public EventCallback<TModel> BeforeSave { get; set; }
    [Parameter] public EventCallback<TModel> AfterSave { get; set; }
    [Parameter] public EventCallback AfterDelete { get; set; }

    [Parameter, EditorRequired] public Func<Task<TModel?>> GetAction { get; set; } = default!;
    [Parameter] public Func<TModel, Task>? DeleteAction { get; set; }
    [Parameter] public Func<TModel, bool, Task<TModel>>? SaveAction { get; set; }

    EditForm _editForm = default!;
    EditContext _editContext = default!;
    ValidationMessageStore _validationStore = default!;

    bool _addNewItem = true;
    bool _isInvalidState;
    Exception? OperationError;

    bool IsBusy { get; set; }

    bool saveButtonBusy = false;

    TModel model = new();

    public TModel Model { get => model; set => model = value; }

    public bool IsAddNew => _addNewItem;

    CodeEditor2 codeEditor = default!;
    bool isEditJsonMode = false;

    protected override void OnInitialized()
    {
        SetupModel(model);
    }

    void SetupModel(TModel model)
    {
        _editContext = new(model);
        _validationStore = new(_editContext);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_OldId != ID || (_OldId != ID && BlankModelFromGetAction))
        {
            _OldId = ID;
            _ = Load();
        }
    }

    public virtual async Task Load()
    {
        try
        {
            //решить как получить пустую модель ForceGet
            if (ID != Guid.Empty || (ID == Guid.Empty && BlankModelFromGetAction))
            {
                _addNewItem = ID == Guid.Empty;
                IsBusy = true;
                StateHasChanged();

                var a = await GetAction() ?? throw new NotFoundException();

                model = a;
                SetupModel(model);

                await OnLoadData.InvokeAsync(model);
            }
        }
        catch (NotFoundException ex)
        {
            OperationError = ex;
        }
        finally
        {
            IsBusy = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Save form
    /// </summary>
    /// <returns>is complete</returns>
    public Task<bool> Save()
    {
        return OnSubmit();
    }

    /// <summary>
    /// Submit form
    /// </summary>
    /// <returns>is complete</returns>
    public virtual async Task<bool> OnSubmit()
    {
        _validationStore.Clear();
        _isInvalidState = false;
        if (!_editContext.Validate()) return false;

        saveButtonBusy = true;
        StateHasChanged();

        TModel a;

        await BeforeSave.InvokeAsync(model);

        try
        {
            if (isEditJsonMode) await CodeEditorJsonToModel();

            a = await SaveAction(model, _addNewItem);

            if (a is null) throw new NotFoundException();

            if (_addNewItem)
            {
                string ss = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "").Trim('/');
                var sp = ss.Split('?', 2);
                string newUrl = sp[0];
                if (newUrl.EndsWith("/new"))
                {
                    newUrl = newUrl.Substring(0, newUrl.Length - 4);
                }
                string query = sp.Length > 1 ? $"?{sp[1]}" : "";
                _ = AfterSave.InvokeAsync(a);
                NavigationManager.NavigateTo($"{newUrl}/{a.Id}{query}");
            }
            else
            {
                _ = AfterSave.InvokeAsync(a);

                if (!a.Equals(model))
                {
                    SetupModel(model);
                }
            }

            _ = _messageService.Success(AppRes.SavedSuccessfully);

            return true;
        }
        catch (NotFoundException ex)
        {
            _ = _messageService.Error(ex.Message);
            OperationError = ex;
        }
        catch (MarsValidationException ex)
        {
            //_ = _messageService.Error(ex.Message);

            foreach (var item in ex.Errors)
            {
                var field = _editContext.Field(item.Key);
                _validationStore.Add(field, item.Value);
            }
            _editContext.NotifyValidationStateChanged();
            _isInvalidState = true;
        }
        catch (FlurlHttpException ex)
        {
            //OperationError = ex;
            _ = _messageService.Error(ex.Message);
        }
        finally
        {
            saveButtonBusy = false;
            StateHasChanged();
        }

        return false;
    }

    public virtual async void OnDeleteClick()
    {
        var ok = await DeleteAction(model).SmartDelete();

        if (ok)
        {
            _ = AfterDelete.InvokeAsync();
            NavigationManager.GoBack();
        }
    }

    //json editor
    async void ToggleEditMode()
    {
        if (!isEditJsonMode)
        {
            isEditJsonMode = true;
            StateHasChanged();
            await Task.Delay(10);

            var json = JsonSerializer.Serialize(model, CodeEditor2.SimpleJsonSerializerOptionsIgnoreReadonly);
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
        var obj = JsonSerializer.Deserialize<TModel>(json, CodeEditor2.SimpleJsonSerializerOptionsIgnoreReadonly) ?? throw new ArgumentNullException();
        model = obj;
    }

    void OnSaveFromCodeEditor(string value)
    {
        _ = OnSubmit();
    }

    string GetModelAsJson()
    {
        var json = JsonSerializer.Serialize(model, CodeEditor2.SimpleJsonSerializerOptionsIgnoreReadonly);
        return json;
    }

    void CancelJsonEditMode()
    {
        isEditJsonMode = false;
    }
}
