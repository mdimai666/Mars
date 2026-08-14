using Mars.PxBlocks.Runtime.Values;

namespace Test.Mars.PxBlocks;

public class PxValueTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(double.NaN, false)]
    [InlineData(1, true)]
    [InlineData(-3.5, true)]
    public void Number_Truthiness(double number, bool expected)
        => Assert.Equal(expected, new PxNumberValue(number).IsTruthy());

    [Fact]
    public void String_Truthiness()
    {
        Assert.False(PxStringValue.Empty.IsTruthy());
        Assert.True(new PxStringValue("0").IsTruthy());
    }

    [Fact]
    public void Null_FalsyAndZero()
    {
        Assert.False(PxNullValue.Instance.IsTruthy());
        Assert.Equal(0, PxNullValue.Instance.ToNumber());
        Assert.Equal("null", PxNullValue.Instance.ToText());
    }

    [Fact]
    public void String_ToNumber_ParsesInvariant()
    {
        Assert.Equal(42.5, new PxStringValue("42.5").ToNumber());
        Assert.True(double.IsNaN(new PxStringValue("abc").ToNumber()));
    }

    [Fact]
    public void Boolean_Conversions()
    {
        Assert.Equal(1, PxBooleanValue.True.ToNumber());
        Assert.Equal("false", PxBooleanValue.False.ToText());
    }

    [Fact]
    public void Add_Numbers_Sums()
    {
        var sum = new PxNumberValue(2).Add(new PxNumberValue(3));
        Assert.Equal(new PxNumberValue(5), sum);
    }

    [Fact]
    public void Add_WithString_Concatenates()
    {
        var joined = new PxNumberValue(2).Add(new PxStringValue(" шт."));
        Assert.Equal(new PxStringValue("2 шт."), joined);
    }
}
