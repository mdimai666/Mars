using AppFront.Main.Extensions;
using Mars.Core.Exceptions;
using Mars.Shared.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using IMessageService = AppFront.Shared.Interfaces.IMessageService;

namespace AppFront.Shared.Components;

public partial class StandartEditForm1<TModel> : ComponentBase
        where TModel : new()
{
    [Inject] IMessageService _messageService { get; set; } = default!;


    [Parameter] public string Class { get; set; } = "";
    [Parameter] public string Style { get; set; } = "";


    [Parameter] public RenderFragment<TModel>? ChildContent { get; set; }

    [Parameter] public EventCallback<TModel> OnLoadData { get; set; }
    [Parameter] public EventCallback<TModel> BeforeSave { get; set; }
    [Parameter] public EventCallback<TModel> AfterSave { get; set; }
    [Parameter] public EventCallback AfterDelete { get; set; }

    [Parameter, EditorRequired] public Func<Task<TModel?>> GetAction { get; set; } = default!;
    [Parameter] public Func<TModel, Task>? DeleteAction { get; set; }
    [Parameter, EditorRequired] public Func<TModel, bool, Task<TModel>> SaveAction { get; set; } = default!;


    EditForm _editForm = default!;
    EditContext _editContext = default!;
    ValidationMessageStore _validationStore = default!;

    //bool _addNewItem = true;
    bool _isInvalidState;
    Exception? OperationError;

    bool IsBusy { get; set; }
    bool saveButtonBusy = false;

    TModel model = new();
    public TModel Model { get => model; set => SetupModel(model = value); }
    //public bool IsAddNew => _addNewItem;

    [Parameter] public bool IsAddNew { get; set; } = true;

    void SetupModel(TModel model)
    {
        _editContext = new(model!);
        _validationStore = new(_editContext);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetupModel(model);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
        {
            _ = Load();
        }
    }

    public virtual async Task Load()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(GetAction, nameof(GetAction));

            //_addNewItem = false;
            IsBusy = true;
            StateHasChanged();

            var a = await GetAction() ?? throw new NotFoundException();

            model = a;
            SetupModel(model);

            if (a != null) _ = OnLoadData.InvokeAsync(a);

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

    public virtual async Task OnSubmit(EditContext editContext)
    {
        _validationStore.Clear();
        _isInvalidState = false;
        if (!editContext.Validate()) return;

        saveButtonBusy = true;
        StateHasChanged();

        TModel a;

        await BeforeSave.InvokeAsync(model);

        try
        {
            var _addNewItem = IsAddNew; //TODO: исправить
            a = await SaveAction(model, _addNewItem);

            if (a is null) throw new NotFoundException();

            if (a is not null)
            {
                if (_addNewItem)
                {
                    _ = AfterSave.InvokeAsync(a);
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
            }
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
                var field = editContext.Field(item.Key);
                _validationStore.Add(field, item.Value);
            }
            editContext.NotifyValidationStateChanged();
            _isInvalidState = true;
        }
        finally
        {
            saveButtonBusy = false;
            StateHasChanged();
        }

    }

    public virtual async void OnDeleteClick()
    {
        var ok = await DeleteAction(model).SmartDelete();

        if (ok)
        {
            _ = AfterDelete.InvokeAsync();
        }
    }
}
