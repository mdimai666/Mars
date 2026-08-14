namespace Mars.PxBlocks.Runtime.Values;

/// <summary>Отсутствие значения (logic_null, пустой сокет, пропущенный аргумент функции).</summary>
public sealed record PxNullValue : PxValue
{
    public static readonly PxNullValue Instance = new();

    public override string TypeName => "Null";

    public override bool IsTruthy() => false;

    public override double ToNumber() => 0;

    public override string ToText() => "null";

    private PxNullValue() { }
}
