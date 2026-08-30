using Flurl.Http;
using Mars.Admin.Framework.Extensions;
using Mars.Admin.Framework.Services;
using Mars.Cms.Contracts.MetaFields;
using Mars.Cms.Contracts.Posts;
using Mars.Cms.Contracts.PostTypes;
using Mars.Core.Features;
using Mars.Media.Contracts.Files;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Mars.Admin.Framework.Components.MetaFieldViews;

/// <summary>
/// Секция «Список объектов» в карточке родителя: таблица детей (колонки — поля детского типа),
/// создание и редактирование ребёнка в боковой панели, привязка существующих постов,
/// удаление из списка по режиму поля.
/// </summary>
public partial class MetaValueChildrenList
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] Mars.Admin.Framework.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IServiceProvider _services { get; set; } = default!;

    [Parameter, EditorRequired] public MetaFieldEditModel Meta { get; set; } = default!;
    [CascadingParameter] public List<MetaValueEditModel> MetaValues { get; set; } = default!;

    IChildPostEditor? _editor;
    bool _busy;
    bool _unsupported;
    string? _childTypeName;
    PostTypeDetailResponse? _childType;
    List<PostListItemResponse> _items = [];
    List<(string Key, string Title, MetaFieldType Type)> _metaColumns = [];

    /// <summary>Поле детского типа, принимающее загруженные файлы (указатель картинки, иначе первое Image/File)</summary>
    MetaFieldDetailResponse? _dropField;

    /// <summary>Ключ поля-картинки для превью карточек (указатель картинки типа, иначе первое Image-поле)</summary>
    string? _cardsImageKey;

    readonly string _cardsSortableId = "children-cards-" + Guid.NewGuid().ToString("N");

    /// <summary>Режим карточек (Options.viewMode)</summary>
    bool IsCardsView => Meta.ViewMode == MetaFieldKindCatalog.ViewModes.Cards;

    string GridColumns => $"minmax(180px, 2fr) {string.Concat(_metaColumns.Select(c => c.Type == MetaFieldType.Image ? "56px " : "1fr "))}100px 40px";

    List<MetaValueEditModel> FieldRows()
        => MetaValues.Where(v => v.MetaField.Key == Meta.Key)
                     .Where(v => v.ModelId != Guid.Empty)
                     .OrderBy(v => v.Index)
                     .ToList();

    protected override void OnInitialized()
    {
        _editor = _services.GetService(typeof(IChildPostEditor)) as IChildPostEditor;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender) _ = InitAsync();
    }

    async Task InitAsync()
    {
        _busy = true;
        StateHasChanged();

        try
        {
            // пустая строка необязательного поля (не выбрано) не сохраняется — вместо неё ничего
            if (Meta.IsNullable)
                MetaValues.RemoveAll(v => v.MetaField.Key == Meta.Key && v.ModelId == Guid.Empty);

            _childTypeName = MetaValueListHelper.GetTargetPostTypeName(Meta.ModelName);
            if (_childTypeName is null)
            {
                _unsupported = true;
                return;
            }

            var blank = await client.Post.GetPostBlank(_childTypeName);
            _childType = blank.PostType;
            _dropField = ResolveDropField(_childType);
            _cardsImageKey = ResolveCardsImageKey(_childType);
            await ReloadAsync();
        }
        catch (FlurlHttpException ex)
        {
            _ = _messageService.Error(ex.Message);
        }
        finally
        {
            _busy = false;
            StateHasChanged();
        }
    }

    async Task ReloadAsync()
    {
        if (_childType is null || _childTypeName is null) return;

        var ids = FieldRows().Select(r => r.ModelId).Where(id => id != Guid.Empty).ToArray();
        if (ids.Length == 0)
        {
            _items = [];
            return;
        }

        var fields = _childType.MetaFields
            .Where(s => s.Type != MetaFieldType.Query && s.Type != MetaFieldType.SelectMany)
            .ToList();
        _metaColumns = fields.Select(s => (s.Key, s.Title, s.Type)).ToList();
        var keys = fields.Select(s => s.Key).ToArray();

        var result = await client.Post.List(_childTypeName, new ListPostQueryRequest
        {
            Skip = 0,
            Take = ids.Length,
            Ids = ids,
            MetaFields = keys.Length > 0 ? keys : null,
        });

        var byId = result.Items.ToDictionary(s => s.Id);
        _items = ids.Select(id => byId.GetValueOrDefault(id))
                    .Where(s => s is not null)
                    .Cast<PostListItemResponse>()
                    .ToList();
    }

    void OnRowClick(FluentDataGridRow<PostListItemResponse> row)
    {
        if (row.Item is null || _editor is null || _childTypeName is null) return;

        _editor.Open(row.Item.Id, _childTypeName, id => _ = InvokeAsync(async () =>
        {
            await ReloadAsync();
            StateHasChanged();
        }));
    }

    void OpenCreateAsync()
    {
        if (_editor is null || _childTypeName is null) return;

        _editor.Open(Guid.Empty, _childTypeName, LinkPost);
    }

    async Task OpenSelectAsync()
    {
        if (_childTypeName is null) return;

        DialogParameters parameters = new()
        {
            Title = Meta.ModelName,
            SecondaryAction = null,
            Width = "500px",
            Modal = true,
            PreventScroll = true
        };

        var data = new MetaValueRelationSelectDialogData
        {
            ModelName = Meta.ModelName,
            ValueId = Guid.Empty,
            MultiSelect = true,
            SelectedIds = FieldRows().Select(r => r.ModelId).ToArray(),
        };

        IDialogReference dialog = await _dialogService.ShowDialogAsync<MetaValueRelationSelectDialog>(data, parameters);
        DialogResult? result = await dialog.Result;

        if (result.Cancelled || result.Data is not IReadOnlyCollection<Guid> ids) return;

        foreach (var id in ids) LinkPost(id);
    }

    /// <summary>Привязать пост-ребёнка строкой значения (при создании — после сохранения в панели)</summary>
    void LinkPost(Guid postId)
    {
        if (postId == Guid.Empty || MetaValues.Any(v => v.MetaField.Key == Meta.Key && v.ModelId == postId)) return;

        MetaValues.Add(new MetaValueEditModel
        {
            Id = Guid.NewGuid(),
            Index = FieldRows().Count,
            MetaField = Meta,
            ModelId = postId,
        });

        _ = InvokeAsync(async () =>
        {
            await ReloadAsync();
            StateHasChanged();
        });
    }

    async Task RemoveAsync(Guid postId)
    {
        var row = FieldRows().FirstOrDefault(r => r.ModelId == postId);
        if (row is null) return;

        if (MetaValueListHelper.ResolveRemoveMode(Meta) == MetaFieldKindCatalog.RemoveModes.DeleteConfirm)
        {
            var ok = await _dialogService.MarsDeleteConfirmation(
                "Удалить объект из системы вместе со всеми его данными?");
            if (!ok) return;

            try
            {
                await client.Post.Delete(postId);
            }
            catch (FlurlHttpException ex)
            {
                _ = _messageService.Error(ex.Message);
                return;
            }
        }

        MetaValues.Remove(row);
        var rows = FieldRows();
        for (var i = 0; i < rows.Count; i++) rows[i].Index = i;

        await ReloadAsync();
        StateHasChanged();
    }

    //-------------------------------------------
    // Загрузка файлов: на каждый файл создаётся пост-ребёнок со значением Файл/Изображение

    /// <summary>Поле детского типа, принимающее файлы: указатель картинки, иначе первое Image/File-поле</summary>
    static MetaFieldDetailResponse? ResolveDropField(PostTypeDetailResponse childType)
    {
        if (!string.IsNullOrEmpty(childType.ImageFieldKey))
        {
            var pointer = childType.MetaFields.FirstOrDefault(f => f.Key == childType.ImageFieldKey);
            if (pointer is not null) return pointer;
        }

        return childType.MetaFields.FirstOrDefault(f => f.Type == MetaFieldType.Image)
            ?? childType.MetaFields.FirstOrDefault(f => f.Type == MetaFieldType.File);
    }

    /// <summary>Ключ поля-картинки для превью карточек: указатель картинки типа, иначе первое Image-поле</summary>
    static string? ResolveCardsImageKey(PostTypeDetailResponse childType)
    {
        if (!string.IsNullOrEmpty(childType.ImageFieldKey)
            && childType.MetaFields.Any(f => f.Key == childType.ImageFieldKey))
        {
            return childType.ImageFieldKey;
        }

        return childType.MetaFields.FirstOrDefault(f => f.Type == MetaFieldType.Image)?.Key;
    }

    string? CardPreviewUrl(PostListItemResponse item)
        => _cardsImageKey is null
            ? null
            : item.MetaColumns?.GetValueOrDefault(_cardsImageKey)?.Split(", ").FirstOrDefault();

    void OpenCard(Guid postId)
    {
        if (_editor is null || _childTypeName is null) return;

        _editor.Open(postId, _childTypeName, id => _ = InvokeAsync(async () =>
        {
            await ReloadAsync();
            StateHasChanged();
        }));
    }

    /// <summary>Драг-порядок карточек: порядок строк значения следует за списком</summary>
    void OnSortCards(FluentSortableListEventArgs args)
    {
        if (args is null || args.OldIndex == args.NewIndex) return;

        var item = _items[args.OldIndex];
        _items.RemoveAt(args.OldIndex);
        _items.Insert(args.NewIndex, item);

        for (var i = 0; i < _items.Count; i++)
        {
            var row = FieldRows().FirstOrDefault(r => r.ModelId == _items[i].Id);
            if (row is not null) row.Index = i;
        }

        StateHasChanged();
    }

    async Task OnFilesUploadedAsync(IReadOnlyCollection<FileDetailResponse> files)
    {
        if (_childTypeName is null || _dropField is null) return;

        foreach (var file in files)
        {
            var title = Path.GetFileNameWithoutExtension(file.Name);
            if (string.IsNullOrWhiteSpace(title)) title = file.Name;

            var slugBase = TextTool.TranslateToPostSlug(title);
            if (string.IsNullOrEmpty(slugBase)) slugBase = "file";

            try
            {
                var post = await client.Post.Create(new CreatePostRequest
                {
                    Id = null,
                    Title = title,
                    Type = _childTypeName,
                    Slug = $"{slugBase}-{Guid.NewGuid().ToString("N")[..8]}",
                    Tags = [],
                    Content = null,
                    Status = null,
                    Excerpt = null,
                    LangCode = "",
                    CategoryIds = [],
                    MetaValues =
                    [
                        new CreateMetaValueRequest
                        {
                            Id = Guid.NewGuid(),
                            Index = 0,
                            Bool = null,
                            Int = null,
                            Float = null,
                            Decimal = null,
                            Long = null,
                            StringText = null,
                            StringShort = null,
                            DateTime = null,
                            VariantId = null,
                            VariantsIds = [],
                            ModelId = file.Id,
                            MetaFieldId = _dropField.Id,
                        },
                    ],
                });

                LinkPost(post.Id);
            }
            catch (FlurlHttpException ex)
            {
                _ = _messageService.Error(ex.Message);
            }
        }
    }
}
