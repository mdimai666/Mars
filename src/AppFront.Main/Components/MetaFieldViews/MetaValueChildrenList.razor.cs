using AppFront.Shared.Extensions;
using AppFront.Shared.Services;
using Flurl.Http;
using Mars.Shared.Contracts.MetaFields;
using Mars.Shared.Contracts.PostTypes;
using Mars.Shared.Contracts.Posts;
using Mars.WebApiClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppFront.Shared.Components.MetaFieldViews;

/// <summary>
/// Секция «Список объектов» в карточке родителя: таблица детей (колонки — поля детского типа),
/// создание и редактирование ребёнка в боковой панели, привязка существующих постов,
/// удаление из списка по режиму поля.
/// </summary>
public partial class MetaValueChildrenList
{
    [Inject] IMarsWebApiClient client { get; set; } = default!;
    [Inject] IDialogService _dialogService { get; set; } = default!;
    [Inject] AppFront.Shared.Interfaces.IMessageService _messageService { get; set; } = default!;
    [Inject] IServiceProvider _services { get; set; } = default!;

    [Parameter, EditorRequired] public MetaFieldEditModel Meta { get; set; } = default!;
    [CascadingParameter] public List<MetaValueEditModel> MetaValues { get; set; } = default!;

    IChildPostEditor? _editor;
    bool _busy;
    bool _unsupported;
    string? _childTypeName;
    PostTypeDetailResponse? _childType;
    List<PostListItemResponse> _items = [];
    List<(string Key, string Title)> _metaColumns = [];

    string GridColumns => $"minmax(180px, 2fr) {string.Concat(Enumerable.Repeat("1fr ", _metaColumns.Count))}100px 40px";

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
        _metaColumns = fields.Select(s => (s.Key, s.Title)).ToList();
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
}
