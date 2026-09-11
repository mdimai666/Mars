using Mars.Forms.Contracts;

namespace Mars.Admin.Framework.Components.Forms;

/// <summary>
/// Черновик раскладки формы для дизайнера: плоский список узлов (порядок = порядок отображения),
/// над которым дизайнер выполняет операции переноса. Дерево отдельно не хранится — дети ищутся
/// по <see cref="FormItem.Parent"/>, а допустимость вложения проверяет
/// <see cref="FormLayoutRules"/>. Поля провайдера помнятся отдельно, поэтому удалённый из
/// раскладки узел поля не теряется — он возвращается в палитру (<see cref="UnplacedFields"/>).
/// </summary>
public class FormLayoutDraft
{
    readonly List<FormItem> _items = [];
    readonly List<FormItem> _fields = [];
    readonly List<FormZoneDescriptor> _zones = [];

    /// <param name="items">Действующая раскладка: узлы с дескрипторами полей от провайдера</param>
    /// <param name="zones">Зоны владельца формы</param>
    public FormLayoutDraft(IEnumerable<FormItem> items, IEnumerable<FormZoneDescriptor> zones)
    {
        _zones.AddRange(zones);
        _items.AddRange(items);
        _fields.AddRange(items.Where(item => item.Kind == FormItemKind.Field && item.Field is not null));
    }

    public IReadOnlyList<FormZoneDescriptor> Zones => _zones;

    /// <summary>Узлы раскладки в порядке отображения</summary>
    public IReadOnlyList<FormItem> Items => _items;

    /// <summary>Поля провайдера, которых нет в раскладке: палитра «что можно поставить»</summary>
    public IReadOnlyList<FormItem> UnplacedFields
        => _fields.Where(candidate => _items.All(item => item.Key != candidate.Key)).ToList();

    /// <summary>Корневые узлы зоны — то, что дизайнер показывает вне контейнеров</summary>
    public IReadOnlyList<FormItem> RootsOf(string zone)
        => _items.Where(item => item.Parent is null && (item.Zone ?? "") == zone).ToList();

    public IReadOnlyList<FormItem> ChildrenOf(string parent)
        => _items.Where(item => item.Parent == parent).ToList();

    /// <summary>Соседи узла в порядке отображения (позиция переноса считается по ним)</summary>
    public IReadOnlyList<FormItem> SiblingsOf(FormItem node)
        => node.Parent is null ? RootsOf(node.Zone ?? "") : ChildrenOf(node.Parent);

    public FormItem? Find(string key)
        => _items.FirstOrDefault(item => item.Key == key) ?? _fields.FirstOrDefault(item => item.Key == key);

    /// <summary>Есть ли в зоне контейнер: один контейнер табов не рисует, ноль — легаси-раскладка</summary>
    public bool HasContainer(string zone) => RootsOf(zone).Any(item => item.Kind == FormItemKind.Container);

    //=====================================
    // добавление

    /// <summary>Новый контейнер зоны (таб)</summary>
    public FormItem? AddContainer(string zone, string title)
        => Add(FormItemKind.Container, null, zone, title);

    /// <summary>Ряд в контейнер, колонку или в корень зоны</summary>
    public FormItem? AddRow(string? parent) => Add(FormItemKind.Row, parent);

    /// <summary>Колонка в ряд — со своей шириной (<see cref="FormItemWidths"/>)</summary>
    public FormItem? AddColumn(string rowKey, string? width = null)
        => Add(FormItemKind.Column, rowKey, null, null, width);

    public FormItem? AddHeading(string? parent, string text)
        => Add(FormItemKind.Heading, parent, null, text);

    public FormItem? AddDivider(string? parent) => Add(FormItemKind.Divider, parent);

    /// <summary>
    /// Поле встаёт в конец родителя (parent = null — в корень зоны): уже размещённое переносится,
    /// взятое из палитры — добавляется. Недопустимый родитель или неизвестный ключ — null.
    /// </summary>
    public FormItem? PlaceField(string fieldKey, string? parent, string? zone = null)
    {
        var field = _fields.FirstOrDefault(item => item.Key == fieldKey);
        if (field is null) return null;

        if (_items.Any(item => item.Key == fieldKey))
            return Move(fieldKey, parent, -1, zone) ? Find(fieldKey) : null;

        return Add(field, parent, zone);
    }

    FormItem? Add(FormItemKind kind, string? parent, string? zone = null, string? title = null, string? width = null)
        => Add(new FormItem { Key = NewKey(kind), Kind = kind, Title = title, Width = width }, parent, zone);

    FormItem? Add(FormItem node, string? parent, string? zone = null)
    {
        var parentNode = parent is null ? null : _items.FirstOrDefault(item => item.Key == parent);
        if (parent is not null && parentNode is null) return null;
        if (!FormLayoutRules.CanContain(parentNode?.Kind, node.Kind)) return null;

        var placed = node with
        {
            Parent = parent,
            // поле из палитры возвращается в свою зону по умолчанию, структура — в зону родителя
            Zone = parentNode?.Zone ?? zone ?? node.Zone ?? _zones.FirstOrDefault()?.Key ?? "",
        };

        _items.Add(placed);
        return placed;
    }

    //=====================================
    // перенос и удаление

    /// <summary>
    /// Переносит узел вместе с поддеревом к новому родителю (null — корень зоны) на позицию
    /// <paramref name="index"/> среди детей (-1 — в конец). Перенос в собственное поддерево и на
    /// недопустимого родителя отменяется; зона узла переезжает вместе с ним.
    /// </summary>
    public bool Move(string key, string? parent, int index = -1, string? zone = null)
    {
        var indexInItems = _items.FindIndex(item => item.Key == key);
        if (indexInItems < 0) return false;

        var parentNode = parent is null ? null : _items.FirstOrDefault(item => item.Key == parent);
        if (parent is not null && parentNode is null) return false;

        var node = _items[indexInItems];
        if (!FormLayoutRules.CanContain(parentNode?.Kind, node.Kind)) return false;
        if (parent is not null && DescendantsOf(key).Contains(parent)) return false;

        var targetZone = parentNode?.Zone ?? zone ?? node.Zone;
        var block = new List<FormItem> { node with { Parent = parent, Zone = targetZone } };
        block.AddRange(SubtreeOf(key).Where(item => item.Key != key)
                                        .Select(item => item with { Zone = targetZone }));

        _items.RemoveAll(item => block.Any(moved => moved.Key == item.Key));

        var siblings = parent is null ? RootsOf(targetZone ?? "") : ChildrenOf(parent);
        var position = index >= 0 && index < siblings.Count
            ? _items.IndexOf(siblings[index])
            : _items.Count;

        _items.InsertRange(position, block);
        return true;
    }

    /// <summary>Убирает узел вместе с поддеревом; поля из поддерева возвращаются в палитру</summary>
    public bool Remove(string key)
    {
        if (_items.All(item => item.Key != key)) return false;

        var removed = DescendantsOf(key);
        removed.Add(key);

        return _items.RemoveAll(item => removed.Contains(item.Key)) > 0;
    }

    /// <summary>Поддерево узла: сам узел и все потомки в порядке отображения</summary>
    public IReadOnlyList<FormItem> SubtreeOf(string key)
    {
        var keys = DescendantsOf(key);
        keys.Add(key);

        return _items.Where(item => keys.Contains(item.Key)).ToList();
    }

    //=====================================
    // свойства узлов

    /// <summary>Ширина колонки (или неявной ячейки поля); пусто — на всю ширину</summary>
    public bool SetWidth(string key, string? width)
        => Update(key, item => item with { Width = string.IsNullOrEmpty(width) ? null : width });

    /// <summary>Текст заголовка: текст у заголовка, имя таба у контейнера, переопределение у поля</summary>
    public bool SetTitle(string key, string? title)
        => Update(key, item => item with { Title = string.IsNullOrWhiteSpace(title) ? null : title });

    public bool SetVisible(string key, bool visible) => Update(key, item => item with { Visible = visible });

    /// <summary>Раскладка для сохранения: дескрипторы не пишутся</summary>
    public FormLayoutSettings ToSettings() => _items.ToLayout();

    //=====================================

    bool Update(string key, Func<FormItem, FormItem> change)
    {
        var index = _items.FindIndex(item => item.Key == key);
        if (index < 0) return false;

        _items[index] = change(_items[index]);
        return true;
    }

    HashSet<string> DescendantsOf(string key)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>([key]);

        while (queue.Count > 0)
        {
            foreach (var child in ChildrenOf(queue.Dequeue()))
                if (result.Add(child.Key)) queue.Enqueue(child.Key);
        }

        return result;
    }

    string NewKey(FormItemKind kind)
    {
        string key;
        do
        {
            key = FormItem.NewKey(kind);
        }
        while (_items.Any(item => item.Key == key) || _fields.Any(field => field.Key == key));

        return key;
    }
}
