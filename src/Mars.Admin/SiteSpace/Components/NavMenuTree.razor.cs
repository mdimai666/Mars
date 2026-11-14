using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;
using NavMenuItem = Mars.Admin.SiteSpace.Pages.NavMenuViews.EditNavMenuPage.NavMenuItem;

namespace Mars.Admin.SiteSpace.Components;

public partial class NavMenuTree : ComponentBase, IAsyncDisposable
{
    [Inject] IJSRuntime JS { get; set; } = default!;
    [Inject] NavigationManager Nav { get; set; } = default!;

    IJSObjectReference? _jsModule;
    DotNetObjectReference<NavMenuTree>? _selfRef;
    [Parameter, EditorRequired]
    public IEnumerable<NavMenuItem> Items { get; set; } = [];

    [Parameter]
    public EventCallback<IEnumerable<NavMenuItem>> ItemsChanged { get; set; }

    [Parameter]
    public NavMenuItem? SelectedItem { get; set; }

    [Parameter]
    public EventCallback<NavMenuItem?> SelectedItemChanged { get; set; }

    [Parameter]
    public EventCallback OnAdd { get; set; }

    [Parameter]
    public EventCallback<NavMenuItem> OnAddChild { get; set; }

    [Parameter]
    public EventCallback<NavMenuItem> OnCopy { get; set; }

    [Parameter]
    public EventCallback<NavMenuItem> OnDelete { get; set; }

    [Parameter]
    public EventCallback<NavMenuItem> OnToggle { get; set; }

    enum DropZone { Before, Inside, After }

    sealed record Row(NavMenuItem Item, int Depth, bool HasChildren);

    List<Row> _rows = [];
    int _totalCount;
    readonly HashSet<Guid> _collapsed = [];
    HashSet<Guid> _knownIds = [];

    NavMenuItem? _dragItem;
    NavMenuItem? _dropTarget;
    DropZone _dropZone;
    NavMenuItem? _menuItem;

    static readonly Icon ChevronIcon = new Icons.Regular.Size16.ChevronRight();
    static readonly Icon GripIcon = new Icons.Regular.Size16.ReOrderDotsVertical();
    static readonly Icon KebabIcon = new Icons.Regular.Size16.MoreHorizontal();
    static readonly Icon CopyIcon = new Icons.Regular.Size16.Copy();
    static readonly Icon DeleteIcon = new Icons.Regular.Size16.Delete();
    static readonly Icon AddIcon = new Icons.Regular.Size16.Add();
    static readonly Icon EyeIcon = new Icons.Regular.Size16.Eye();
    static readonly Icon EyeOffIcon = new Icons.Regular.Size16.EyeOff();
    static readonly Icon LinkIcon = new Icons.Regular.Size16.Link();
    static readonly Icon FolderIcon = new Icons.Regular.Size16.Folder();
    static readonly Icon MinusIcon = new Icons.Regular.Size16.Subtract();
    static readonly Icon HeaderIcon = new Icons.Regular.Size16.TextBold();
    static readonly Icon ExpandAllIcon = new Icons.Regular.Size16.ChevronDown();
    static readonly Icon CollapseAllIcon = new Icons.Regular.Size16.ChevronUp();

    protected override void OnParametersSet() => Rebuild();

    void Rebuild()
    {
        var list = Items.ToList();
        _totalCount = list.Count;

        var ids = list.Select(x => x.Id).ToHashSet();
        foreach (var item in list)
        {
            if (!_knownIds.Contains(item.Id) && item.ParentId != Guid.Empty)
            {
                _collapsed.Remove(item.ParentId);
            }
        }
        _knownIds = ids;

        var byParent = list.GroupBy(x => x.ParentId).ToDictionary(g => g.Key, g => g.ToList());
        _rows = [];

        void Walk(NavMenuItem node, int depth)
        {
            var hasChildren = byParent.ContainsKey(node.Id);
            _rows.Add(new Row(node, depth, hasChildren));
            if (hasChildren && !_collapsed.Contains(node.Id))
            {
                foreach (var child in byParent[node.Id])
                {
                    Walk(child, depth + 1);
                }
            }
        }

        foreach (var root in list.Where(x => x.ParentId == Guid.Empty || !ids.Contains(x.ParentId)))
        {
            Walk(root, 0);
        }
    }

    bool IsCollapsed(NavMenuItem item) => _collapsed.Contains(item.Id);

    void ToggleCollapse(NavMenuItem item)
    {
        if (!_collapsed.Remove(item.Id))
        {
            _collapsed.Add(item.Id);
        }
        Rebuild();
    }

    void ExpandAll()
    {
        _collapsed.Clear();
        Rebuild();
    }

    void CollapseAll()
    {
        foreach (var row in _rows.Where(r => r.HasChildren))
        {
            _collapsed.Add(row.Item.Id);
        }
        Rebuild();
    }

    Task Select(NavMenuItem item)
        => SelectedItemChanged.InvokeAsync(SelectedItem == item ? null : item);

    void OnRowKey(KeyboardEventArgs e, NavMenuItem item)
    {
        if (e.Key is "Enter" or " ")
        {
            _ = Select(item);
        }
    }

    Icon KindIcon(Row row) => row.Item switch
    {
        { IsDivider: true } => MinusIcon,
        { IsHeader: true } => HeaderIcon,
        _ => row.HasChildren ? FolderIcon : LinkIcon,
    };

    string TitleText(NavMenuItem item) => item switch
    {
        { IsDivider: true } => string.IsNullOrWhiteSpace(item.Title) ? "Разделитель" : item.Title,
        { IsHeader: true } => string.IsNullOrWhiteSpace(item.Title) ? "Заголовок" : item.Title,
        _ => string.IsNullOrWhiteSpace(item.Title) ? "(без названия)" : item.Title,
    };

    ///////////// drag & drop

    async Task StartDrag(NavMenuItem item, DragEventArgs e)
    {
        await CloseMenu();
        _dragItem = item;
        e.DataTransfer.DropEffect = "move";
        e.DataTransfer.EffectAllowed = "move";
    }

    void EndDrag()
    {
        _dragItem = null;
        _dropTarget = null;
    }

    bool CanDrop(NavMenuItem target)
    {
        if (_dragItem is null || target == _dragItem) return false;

        var seen = new HashSet<Guid>();
        var cursor = target;
        while (cursor.ParentId != Guid.Empty && seen.Add(cursor.Id))
        {
            if (cursor.ParentId == _dragItem.Id) return false;
            cursor = Items.FirstOrDefault(x => x.Id == cursor.ParentId)!;
            if (cursor is null) break;
        }
        return true;
    }

    void SetDrop(NavMenuItem target, DropZone zone)
    {
        if (!CanDrop(target)) return;
        _dropTarget = target;
        _dropZone = zone;
    }

    async Task Drop(NavMenuItem target, DropZone zone)
    {
        if (_dragItem is null || !CanDrop(target)) return;

        var drag = _dragItem;
        var list = Items.ToList();
        list.Remove(drag);

        if (zone == DropZone.Inside)
        {
            drag.ParentId = target.Id;
            list.Insert(list.IndexOf(target) + 1, drag);
            _collapsed.Remove(target.Id);
        }
        else
        {
            drag.ParentId = target.ParentId;
            var index = list.IndexOf(target);
            list.Insert(zone == DropZone.Before ? index : index + 1, drag);
        }

        EndDrag();
        Rebuild();
        await ItemsChanged.InvokeAsync(list);
    }

    string RowClass(Row row)
    {
        var cls = "nmt-row";
        if (SelectedItem == row.Item) cls += " nmt-row--selected";
        if (row.Item.Disabled) cls += " nmt-row--disabled";
        if (_dragItem == row.Item) cls += " nmt-row--dragging";
        if (_menuItem == row.Item) cls += " nmt-row--menuopen";
        if (_dropTarget == row.Item && _dragItem is not null)
        {
            cls += _dropZone switch
            {
                DropZone.Before => " nmt-row--drop-before",
                DropZone.Inside => " nmt-row--drop-inside",
                _ => " nmt-row--drop-after",
            };
        }
        return cls;
    }

    ///////////// row menu

    async Task ToggleMenu(NavMenuItem item)
    {
        if (_menuItem == item)
        {
            await CloseMenu();
            return;
        }

        _menuItem = item;
        // абсолютный URL: в WASM относительный specifier резолвится от _framework, не от base href
        _jsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", Nav.BaseUri + "js/navmenu-tree.js");
        _selfRef ??= DotNetObjectReference.Create(this);
        await _jsModule.InvokeVoidAsync("watchClose", _selfRef);
    }

    async Task CloseMenu()
    {
        _menuItem = null;
        if (_jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("unwatch");
        }
    }

    [JSInvokable]
    public void CloseMenuFromJs()
    {
        _menuItem = null;
        StateHasChanged();
    }

    async Task MenuAction(Func<NavMenuItem, Task> action, NavMenuItem item)
    {
        await CloseMenu();
        await action(item);
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("unwatch");
            await _jsModule.DisposeAsync();
        }
        _selfRef?.Dispose();
    }

    Task MenuCopy(NavMenuItem item) => MenuAction(i => OnCopy.InvokeAsync(i), item);
    Task MenuAddChild(NavMenuItem item) => MenuAction(i => OnAddChild.InvokeAsync(i), item);
    Task MenuDelete(NavMenuItem item) => MenuAction(i => OnDelete.InvokeAsync(i), item);
    Task MenuToggle(NavMenuItem item) => MenuAction(i => OnToggle.InvokeAsync(i), item);
}
