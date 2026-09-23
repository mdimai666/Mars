using Mars.Admin.Pages.PostsViews.Forms;
using Mars.Admin.Pages.PostTypeViews;
using Mars.Forms.Front;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Pages.PostsViews;

public partial class EditPostView
{
    [Inject] protected IMarsWebApiClient client { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService messageService { get; set; } = default!;
    [Inject] NavigationManager navigationManager { get; set; } = default!;
    [Inject] ViewModelService viewModelService { get; set; } = default!;
    [Inject] IAIToolAppService aiTool { get; set; } = default!;
    [Inject] IDialogService dialogService { get; set; } = default!;

    [Parameter, EditorRequired] public Guid ID { get; set; }
    [Parameter, EditorRequired] public string PostTypeName { get; set; } = default!;

    /// <summary>Вызывается после каждого сохранения поста (например, дровером секции детей)</summary>
    [Parameter] public EventCallback<PostEditModel> OnSaved { get; set; }

    /// <summary>Переход на URL созданной записи после сохранения (false в боковой панели)</summary>
    [Parameter] public bool NavigateAfterCreate { get; set; } = true;

    [Parameter] public bool HidePublishCard { get; set; }

    StandardEditContainer<PostEditModel> f = default!;

    //==========================================
    // Форма: дерево контейнеров (Mars.Forms)

    /// <summary>Хуки отложенной записи (WYSIWYG, код, блочный редактор) — одни на все зоны формы</summary>
    readonly FormCommitHooks _commits = new();

    /// <summary>Живые редакторы полей (WYSIWYG, код, блочный) — рендерятся внутри дерева формы</summary>
    readonly FormLiveEditors _liveEditors = new();

    PostFormContext? _formContext;
    PostEditModel? _formContextOwner;

    /// <summary>
    /// Контекст формы: значения читаются и пишутся напрямую в модель, поэтому «применять» их
    /// перед сохранением не нужно — только забрать значение у редакторов с отложенной записью.
    /// </summary>
    PostFormContext FormContextOf(PostEditModel model)
    {
        if (_formContext is not null && ReferenceEquals(_formContextOwner, model)) return _formContext;

        _liveEditors.SaveRequest = () => f.OnSubmit();

        var values = new PostFormValueStore(model);
        values.Changed += StateHasChanged;

        _formContext = new PostFormContext(model, values, _liveEditors, _commits);
        _formContextOwner = model;

        return _formContext;
    }

    /// <summary>Заголовки системных слотов — ключи ресурса <see cref="AppRes"/></summary>
    string ResolveTitle(string key) => L[key];

    /// <summary>
    /// Быстрый вход в дизайнер раскладки формы типа. Раскладка общая для типа, а не для поста,
    /// поэтому после сохранения дерево формы подменяется на месте — несохранённые правки поста остаются.
    /// </summary>
    async Task OpenFormLayoutDialog(Guid? postTypeId)
    {
        if (postTypeId is not { } typeId || typeId == Guid.Empty) return;

        DialogParameters parameters = new()
        {
            Title = "Форма редактирования поста",
            SecondaryAction = null,
            Width = "min(1100px, 94vw)",
            Modal = true,
            PreventScroll = true,
        };

        await dialogService.ShowDialogAsync<PostFormLayoutDialog>(new PostFormLayoutDialogData
        {
            PostTypeId = typeId,
            OnSaved = async () =>
            {
                var definition = await client.PostType.GetFormDefinition(typeId);
                if (definition is null || f?.Model is null) return;

                f.Model.Form = definition;
                StateHasChanged();
            },
        }, parameters);
    }

    async Task<PostEditModel> SaveWithCallback(PostEditModel post, bool isNew)
    {
        var result = await PostEditModel.SaveAction(client, post, isNew);
        if (OnSaved.HasDelegate) await OnSaved.InvokeAsync(result);
        return result;
    }

    async Task BeforeSave(PostEditModel post)
        => await _commits.CommitAllAsync();
}
