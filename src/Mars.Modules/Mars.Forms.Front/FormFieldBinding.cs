using Mars.Forms.Contracts;

namespace Mars.Forms.Front;

/// <summary>
/// Привязка поля формы: элемент дерева + дескриптор + доступ к значению в мешке.
/// Контракт редактора: компонент принимает <c>[Parameter] FormFieldBinding Binding</c>
/// и необязательный <c>[Parameter] int Index</c> (индекс элемента множественного поля, -1 = значение целиком).
/// </summary>
public sealed class FormFieldBinding
{
    public required FormItem Item { get; init; }

    public required FormFieldDescriptor Field { get; init; }

    public required IFormValueStore Values { get; init; }

    /// <summary>Резолвер переводимых заголовков (из контекста рендера)</summary>
    public Func<string, string>? TitleResolver { get; init; }

    /// <summary>Заголовок: переопределение из раскладки → перевод по ключу ресурса → заголовок дескриптора</summary>
    public string Title
    {
        get
        {
            if (!string.IsNullOrEmpty(Item.Title)) return Item.Title;
            if (Field.TitleKey is { Length: > 0 } && TitleResolver is not null) return TitleResolver(Field.TitleKey);
            return Field.Title;
        }
    }

    /// <summary>Ключ редактора</summary>
    public string? Editor => Field.Editor;

    public bool ReadOnly => Field.ReadOnly;

    public bool IsList => Field.Multiple || Field.Type == FormFieldType.SelectMany;

    /// <summary>Тип одного элемента значения</summary>
    public FormFieldType ElementType => Field.ElementType;

    public IReadOnlyList<object?> List => Values.GetList(Field);

    public object? Value
    {
        get => ValueAt(-1);
        set => SetValueAt(-1, value);
    }

    public object? ValueAt(int index)
        => index < 0
            ? Values.GetValue(Field)
            : index < List.Count ? List[index] : null;

    public void SetValueAt(int index, object? value)
    {
        if (index < 0)
        {
            Values.SetValue(Field, value);
            return;
        }

        var items = List.ToList();
        while (items.Count <= index) items.Add(null);
        items[index] = value;
        Values.SetList(Field, items);
    }

    public void AddItem(object? value)
    {
        var items = List.ToList();
        items.Add(value);
        Values.SetList(Field, items);
    }

    public void RemoveAt(int index)
    {
        var items = List.ToList();
        if (index < 0 || index >= items.Count) return;
        items.RemoveAt(index);
        Values.SetList(Field, items);
    }

    public void Move(int index, int delta)
    {
        var items = List.ToList();
        var target = index + delta;
        if (index < 0 || target < 0 || index >= items.Count || target >= items.Count) return;

        (items[index], items[target]) = (items[target], items[index]);
        Values.SetList(Field, items);
    }

    /// <summary>Ошибки этого поля (для множественных — с индексом значения)</summary>
    public IReadOnlyCollection<FormError> Errors
        => Values.Errors.Where(e => e.Key == Field.Key).ToList();

    public IReadOnlyCollection<FormError> ErrorsAt(int index)
        => Errors.Where(e => e.Index == index).ToList();
}
