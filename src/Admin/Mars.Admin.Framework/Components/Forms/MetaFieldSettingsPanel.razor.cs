using Mars.Admin.Framework.Components.MediaViews;
using Mars.Admin.Framework.Components.MetaFieldViews;
using Mars.Cms.Contracts.MetaFields;
using Mars.Forms.Contracts;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Доменные настройки метаполя в общем редакторе определений: язык кода, связь (цель, вид поля,
/// режим удаления, вид списка, дроп-зона), вычислимое поле, папка загрузки, генератор значения
/// и варианты выбора. Панель правит модель источника и сообщает строке редактора о правке;
/// общие параметры (заголовок, ключ, правила, редактор…) правит сам редактор.
/// </summary>
public partial class MetaFieldSettingsPanel
{
    /// <summary>Скоуп регистрации панелей метаполей в <c>IFormFieldTypeSettingsLocator</c></summary>
    public const string Scope = "meta";

    [Parameter, EditorRequired] public FormFieldDefinition Definition { get; set; } = default!;

    [Parameter] public EventCallback<FormFieldDefinition> OnSourceChanged { get; set; }

    [CascadingParameter] public IReadOnlyCollection<MetaRelationModelResponse> MetaRelationModels { get; set; } = [];

    [Inject] IDialogService DialogService { get; set; } = default!;

    MetaFieldEditModel? Field => Definition.Source as MetaFieldEditModel;

    async Task ChangedAsync()
    {
        await OnSourceChanged.InvokeAsync(Definition);
        StateHasChanged();
    }

    Task SetCodeLangAsync(string value) => SetAsync(field => field.CodeLang = value);

    Task SetModelNameAsync(string value) => SetAsync(field => field.ModelName = value);

    Task SetKindAsync(string value) => SetAsync(field => field.Kind = value);

    Task SetRemoveModeAsync(string value) => SetAsync(field => field.RemoveMode = value);

    Task SetViewModeAsync(string value) => SetAsync(field => field.ViewMode = value);

    Task SetDropZoneAsync(bool value) => SetAsync(field => field.DropZoneEnabled = value);

    Task SetQueryTargetAsync(string value) => SetAsync(field => field.QueryTarget = value);

    Task SetQueryBackReferenceAsync(string value) => SetAsync(field => field.QueryBackReferenceKey = value);

    Task SetGeneratorTypeAsync(string value) => SetAsync(field => field.GeneratorType = value);

    Task SetGeneratorPrefixAsync(string value) => SetAsync(field => field.GeneratorPrefix = value);

    /// <summary>Прокси для числового поля: <c>FluentNumberField</c> связывается только через <c>@bind-Value</c></summary>
    int GeneratorPadding
    {
        get => Field?.GeneratorPaddingWidth ?? 4;
        set => _ = SetAsync(model => model.GeneratorPaddingWidth = value);
    }

    Task SetGeneratorModeAsync(string value) => SetAsync(field => field.GeneratorMode = value);

    Task SetCategorySlugAsync(MetaFieldEditModel.MetaFieldGeneratorCategoryRow row, string value)
        => SetAsync(_ => row.CategorySlug = value);

    Task SetCategoryPrefixAsync(MetaFieldEditModel.MetaFieldGeneratorCategoryRow row, string value)
        => SetAsync(_ => row.Prefix = value);

    Task AddGeneratorCategoryAsync()
        => SetAsync(field => field.GeneratorCategoryPrefixes.Add(new MetaFieldEditModel.MetaFieldGeneratorCategoryRow()));

    Task RemoveGeneratorCategoryAsync(MetaFieldEditModel.MetaFieldGeneratorCategoryRow row)
        => SetAsync(field => field.GeneratorCategoryPrefixes.Remove(row));

    Task ResetUploadFolderAsync() => SetAsync(field => field.UploadFolder = "");

    Task OnVariantsChangedAsync(MetaFieldEditModel value) => ChangedAsync();

    Task SetAsync(Action<MetaFieldEditModel> change)
    {
        if (Field is not { } field) return Task.CompletedTask;

        change(field);
        return ChangedAsync();
    }

    /// <summary>Выбор папки загрузки для полей Файл/Изображение</summary>
    async Task OpenFolderPickerAsync()
    {
        if (Field is not { } field) return;

        DialogParameters parameters = new()
        {
            Title = "Папка загрузки",
            SecondaryAction = null,
            Width = "500px",
            Modal = true,
            PreventScroll = true,
        };

        var dialog = await DialogService.ShowDialogAsync<MediaFolderSelectDialog>("", parameters);
        var result = await dialog.Result;

        if (result.Cancelled || result.Data is not string path) return;

        field.UploadFolder = path;
        await ChangedAsync();
    }
}
