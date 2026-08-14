namespace Mars.PxBlocks.Runtime.Values;

/// <summary>Объект — набор именованных полей (блок «создать объект», rich-редакторы).</summary>
public sealed record PxObjectValue : PxValue
{
    public IReadOnlyDictionary<string, PxValue> Members { get; }

    public PxObjectValue(IReadOnlyDictionary<string, PxValue>? members = null)
        => Members = members ?? new Dictionary<string, PxValue>();

    public override string TypeName => "Object";

    public override string ToText()
        => "{" + string.Join(", ", Members.Select(m => $"{m.Key}: {m.Value.ToText()}")) + "}";
}
