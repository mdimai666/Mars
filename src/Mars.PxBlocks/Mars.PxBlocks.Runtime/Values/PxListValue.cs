namespace Mars.PxBlocks.Runtime.Values;

/// <summary>Список значений (для controls_forEach и будущих блоков массивов).</summary>
public sealed record PxListValue : PxValue
{
    public IReadOnlyList<PxValue> Items { get; }

    public PxListValue(IReadOnlyList<PxValue>? items = null)
        => Items = items ?? [];

    public override string TypeName => "List";

    public override string ToText() => string.Join(",", Items.Select(i => i.ToText()));
}
