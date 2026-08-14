namespace Mars.PxBlocks.Runtime.Values;

public sealed record PxBooleanValue(bool Value) : PxValue
{
    public static readonly PxBooleanValue True = new(true);
    public static readonly PxBooleanValue False = new(false);

    public override string TypeName => "Boolean";

    public override bool IsTruthy() => Value;

    public override double ToNumber() => Value ? 1 : 0;

    public override string ToText() => Value ? "true" : "false";
}
