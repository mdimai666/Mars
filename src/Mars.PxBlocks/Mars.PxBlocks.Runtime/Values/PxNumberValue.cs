using System.Globalization;

namespace Mars.PxBlocks.Runtime.Values;

public sealed record PxNumberValue(double Number) : PxValue
{
    public static readonly PxNumberValue Zero = new(0);

    public override string TypeName => "Number";

    public override bool IsTruthy() => Number != 0 && !double.IsNaN(Number);

    public override double ToNumber() => Number;

    public override string ToText() => Number.ToString(CultureInfo.InvariantCulture);
}
