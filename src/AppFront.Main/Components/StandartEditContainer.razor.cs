using AppFront.Main.Extensions;
using AppFront.Shared.Extensions;
using Mars.Core.Exceptions;
using Mars.Shared.Models.Interfaces;
using Mars.Shared.Resources;
using Flurl.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using IMessageService = AppFront.Shared.Interfaces.IMessageService;

namespace AppFront.Shared.Components;

public partial class StandartEditContainer<TModel> : ComponentBase
        where TModel : IBasicEntity, new()
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

                if (a != null) _ = OnLoadData.InvokeAsync(a);
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

    public virtual async Task OnSubmit()
    {
        _validationStore.Clear();
        _isInvalidState = false;
        if (!_editContext.Validate()) return;

        saveButtonBusy = true;
        StateHasChanged();

        TModel a;

        await BeforeSave.InvokeAsync(model);

        try
        {
            a = await SaveAction(model, _addNewItem);

            if (a is null) throw new NotFoundException();

            if (a is not null)
            {
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
}
