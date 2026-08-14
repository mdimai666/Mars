using System.Globalization;

namespace Mars.PxBlocks.Runtime.Values;

public sealed record PxStringValue(string Value) : PxValue
{
    public static readonly PxStringValue Empty = new("");

    public override string TypeName => "String";

    public override bool IsTruthy() => Value.Length > 0;

    public override double ToNumber()
        => double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : double.NaN;

    public override string ToText() => Value;
}
