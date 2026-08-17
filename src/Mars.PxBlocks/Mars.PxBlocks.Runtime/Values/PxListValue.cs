namespace Mars.PxBlocks.Runtime.Values;

/// <summary>Список значений (controls_forEach, блоки массивов). Как массивы JS,
/// мутабелен по ссылке: переменная и блоки держат один и тот же список.</summary>
public sealed record PxListValue : PxValue
{
    private readonly List<PxValue> items;

    public IReadOnlyList<PxValue> Items => items;

    public PxListValue(IReadOnlyList<PxValue>? items = null)
        => this.items = items as List<PxValue> ?? new List<PxValue>(items ?? []);

    public override string TypeName => "List";

    public override string ToText() => string.Join(",", Items.Select(i => i.ToText()));

    /// <summary>Записать по индексу (0-основному, как в MakeCode); индекс за концом
    /// дорастает список null-ами — как разреженный массив JS.</summary>
    public void SetAt(int index, PxValue value)
    {
        while (items.Count <= index)
            items.Add(PxNullValue.Instance);
        items[index] = value;
    }

    public int Append(PxValue value)
    {
        items.Add(value);
        return items.Count;
    }

    public void InsertFirst(PxValue value) => items.Insert(0, value);

    public int AddFirst(PxValue value)
    {
        items.Insert(0, value);
        return items.Count;
    }

    public void InsertAt(int index, PxValue value) =>
        items.Insert(Math.Clamp(index, 0, items.Count), value);

    public PxValue RemoveLast()
    {
        if (items.Count == 0)
            return PxNullValue.Instance;
        var value = items[^1];
        items.RemoveAt(items.Count - 1);
        return value;
    }

    public PxValue RemoveFirst()
    {
        if (items.Count == 0)
            return PxNullValue.Instance;
        var value = items[0];
        items.RemoveAt(0);
        return value;
    }

    public PxValue RemoveAt(int index)
    {
        if (index < 0 || index >= items.Count)
            return PxNullValue.Instance;
        var value = items[index];
        items.RemoveAt(index);
        return value;
    }

    public void Reverse() => items.Reverse();
}
