using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AppFront.Shared.Components;

public partial class DTreeView<TModel, TKey> : FluentComponentBase where TKey : notnull
{
    [Parameter, EditorRequired]
    public IEnumerable<TModel> Items { get; set; } = default!;

    [Parameter, EditorRequired]
    public Func<TModel, TKey> KeyExp { get; set; } = default!;

    [Parameter, EditorRequired]
    public Func<TModel, TKey?> ParentExp { get; set; } = default!;

    [Parameter]
    public RenderFragment<DTreeNode<TModel>> ChildContent { get; set; } = default!;

    DTreeNode<TModel>? _selectedItem = default!;
    string? _selectedItemKey;

    FluentTreeView _fluentTreeView = default!;

    [Parameter]
    public DTreeNode<TModel>? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value) return;
            _selectedItem = value;
            _selectedItemKey = _selectedItem?.Id;
            SelectedItemChanged.InvokeAsync(_selectedItem);

        }
    }

    [Parameter]
    public EventCallback<DTreeNode<TModel>?> SelectedItemChanged { get; set; } = default!;

    [Parameter]
    public string ItemHeight { get; set; } = "32px";


    private IEnumerable<DTreeNode<TModel>> _tree = Enumerable.Empty<DTreeNode<TModel>>();
    private IEnumerable<DTreeNode<TModel>> _flatTreeList = Enumerable.Empty<DTreeNode<TModel>>();
    public IEnumerable<DTreeNode<TModel>> TreeNodes => _tree;
    public IEnumerable<DTreeNode<TModel>> FlatTreeNodesList => _flatTreeList;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var oldItems = Items;

        parameters.SetParameterProperties(this);

        if (oldItems != Items)
        {
            Regenerate();
            if (_selectedItemKey is not null)
            {
                _selectedItem = _flatTreeList.FirstOrDefault(s => s.Id == _selectedItemKey);
                await SelectedItemChanged.InvokeAsync(_selectedItem);
            }
        }

        await base.SetParametersAsync(ParameterView.Empty);
    }

    public void Regenerate()
    {
        if (KeyExp == null || ParentExp == null) return;
        _tree = TreeHelper.CreateTree(Items, KeyExp, ParentExp!, out _flatTreeList);
        Console.WriteLine(">Regenerate");
    }

}

public static class TreeHelper
{
    public static IEnumerable<DTreeNode<T>> CreateTree<T, TKey>(IEnumerable<T> items, Func<T, TKey> keyExp, Func<T, TKey> parentExp, out IEnumerable<DTreeNode<T>> flatTreeList)
        where TKey : notnull
    {
        var itemLookup = items.ToDictionary(keyExp);
        var nodeLookup = new Dictionary<TKey, DTreeNode<T>>(items.Count());

        foreach (var item in items)
        {
            var key = keyExp(item);
            var parentKey = parentExp(item);

            var node = new DTreeNode<T>
            {
                Id = key.ToString() ?? throw new ArgumentNullException("Id cannot be empty"),
                Text = item.ToString() ?? string.Empty,
                Items = new List<DTreeNode<T>>(),
                Node = item,
                Disabled = false,
                Expanded = true
            };

            nodeLookup[key] = node;

            // Если это корневой элемент (parentKey равен default или отсутствует в словаре), 
            // то он будет добавлен в результат позже
        }

        // Связываем узлы в дерево
        foreach (var item in items)
        {
            var key = keyExp(item);
            var parentKey = parentExp(item);

            if (itemLookup.TryGetValue(parentKey, out var parentItem) && nodeLookup.TryGetValue(parentKey, out var parentNode))
            {
                if (parentNode.Items is ICollection<DTreeNode<T>> children)
                {
                    children.Add(nodeLookup[key]);
                }
            }
        }

        // Возвращаем только корневые узлы (те, у которых нет родителя или родитель не найден)
        flatTreeList = nodeLookup.Values;
        return items
            .Where(item =>
            {
                var parentKey = parentExp(item);
                return !itemLookup.ContainsKey(parentKey) || EqualityComparer<TKey>.Default.Equals(parentKey, default);
            })
            .Select(item => nodeLookup[keyExp(item)]);
    }
}

public class DTreeNode<T> : ITreeViewItem
{
    public required string Id { get; set; }
    public required string Text { get; set; }
    public required IEnumerable<ITreeViewItem>? Items { get; set; }
    public required T Node { get; set; }
    public Icon? IconCollapsed { get; set; }
    public Icon? IconExpanded { get; set; }
    public bool Disabled { get; set; }
    public bool Expanded { get; set; }
    public Func<TreeViewItemExpandedEventArgs, Task>? OnExpandedAsync { get; set; }

}
