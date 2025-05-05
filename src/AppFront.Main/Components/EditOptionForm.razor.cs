using System.Text.Json;
using AppFront.Main.Extensions;
using Mars.Core.Exceptions;
using Mars.WebApiClient.Interfaces;
using MarsCodeEditor2;
using Microsoft.AspNetCore.Components;

namespace AppFront.Shared.Components;

public partial class EditOptionForm<TModel> where TModel : class, new()
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] Interfaces.IMessageService _messageService { get; set; } = default!;

    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public string? FormClass { get; set; }
    [Parameter] public string? FormStyle { get; set; }

    [Parameter] public RenderFragment<TModel>? ChildContent { get; set; }

    [Parameter] public EventCallback<TModel> OnLoadData { get; set; }
    [Parameter] public EventCallback<TModel> BeforeSave { get; set; }
    [Parameter] public EventCallback<TModel> AfterSave { get; set; }

    protected Exception? exError = null;
    protected bool errorOptionNotFound = false;

    protected bool IsBusy = true;

    protected bool saveButtonBusy = false;

    protected TModel _model = new();

    public TModel Model { get => _model; set => _model = value; }

    CodeEditor2 codeEditor = default!;
    bool isEditJsonMode = false;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && !Q.IsPrerender)
        {
            _ = Load();
        }
    }

    public virtual async Task Load()
    {
        IsBusy = true;
        StateHasChanged();

        try
        {
            _model = await client.Option.GetOption<TModel>() ?? throw new NotFoundException();
        }
        catch (NotFoundException ex)
        {
            Console.WriteLine($"Option {typeof(TModel).Name} not found; " + ex.Message);
            errorOptionNotFound = true;
            exError = ex;
        }
        catch (Exception ex)
        {
            exError = ex;
            //Console.Error.WriteLine(ex.Message);
        }

        IsBusy = false;
        StateHasChanged();

        if (_model != null) _ = OnLoadData.InvokeAsync(_model);
    }

    public virtual async Task Save()
    {
        saveButtonBusy = true;
        StateHasChanged();

        if (isEditJsonMode) await CodeEditorJsonToModel();

        await BeforeSave.InvokeAsync(_model);

        try
        {
            var ok = await client.Option.SaveOption<TModel>(_model).SmartSave();

            if (ok)
            {
                if (isEditJsonMode)
                {
                    var json = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
                    await codeEditor.SetValue(json);
                }

                await AfterSave.InvokeAsync(_model);
            }
        }
        catch (Exception ex)
        {
            exError = ex;
            _ = _messageService.Error(ex.Message);
        }

        saveButtonBusy = false;
        StateHasChanged();
    }

    async void ToggleEditMode()
    {
        if (!isEditJsonMode)
        {
            isEditJsonMode = true;
            StateHasChanged();
            await Task.Delay(10);

            var json = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
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
        var obj = JsonSerializer.Deserialize<TModel>(json) ?? throw new ArgumentNullException();
        _model = obj;
    }

    void OnSaveFromCodeEditor(string value)
    {
        _ = Save();
    }

    string GetModelAsJson()
    {
        var json = JsonSerializer.Serialize(_model, new JsonSerializerOptions { WriteIndented = true });
        return json;
    }

    void CancelJsonEditMode()
    {
        isEditJsonMode = false;
    }
}
