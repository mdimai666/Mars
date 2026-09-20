using FluentAssertions;
using Mars.Datasource.Providers.File;

namespace Mars.Datasource.Tests.FileProviders;

public class ValFunctionTests
{
    [Theory]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("  12 ", 12L)]
    [InlineData("-7", -7L)]
    [InlineData("1.5", null)]
    [InlineData("abc", null)]
    public void Num_ParsesIntegersOnly(string? value, long? expected)
        => Val.Num(value).Should().Be(expected);

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("2", 2d)]
    [InlineData("", null)]
    [InlineData("1,5", null)]
    public void Dec_UsesInvariantCulture(string? value, double? expected)
        => Val.Dec(value).Should().Be(expected);

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ann ", " ann ")]
    public void Str_NeverReturnsNull(string? value, string expected)
        => Val.Str(value).Should().Be(expected);

    [Theory]
    [InlineData("2024-03-01", "2024-03-01")]
    [InlineData("01.03.2024", "2024-03-01")]
    [InlineData("2024-03-01 10:20:30", "2024-03-01T10:20:30")]
    [InlineData("", null)]
    [InlineData("not a date", null)]
    public void Date_AcceptsCommonFormats(string? value, string? expected)
    {
        var parsed = Val.Date(value);

        if (expected is null) parsed.Should().BeNull();
        else parsed.Should().Be(DateTime.Parse(expected));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("FALSE", false)]
    [InlineData("1", true)]
    [InlineData("no", false)]
    [InlineData("да", true)]
    [InlineData("", null)]
    [InlineData("maybe", null)]
    public void Flag_AcceptsYesNoForms(string? value, bool? expected)
        => Val.Flag(value).Should().Be(expected);

    [Fact]
    public void Id_ParsesGuid()
    {
        var id = Guid.NewGuid();

        Val.Id(id.ToString()).Should().Be(id);
        Val.Id("not-a-guid").Should().BeNull();
    }

    [Fact]
    public void Str_FormatsNumberInvariantly()
        => Val.Str(35.5d).Should().Be("35.5");
}

public class FileTypeInferenceTests
{
    [Theory]
    [InlineData(new[] { "1", "2", "" }, FileTypeInference.BigInt)]
    [InlineData(new[] { "1.5", "2" }, FileTypeInference.Double)]
    [InlineData(new[] { "true", "FALSE", "" }, FileTypeInference.Boolean)]
    [InlineData(new[] { "2024-03-01", "01.03.2024" }, FileTypeInference.Timestamp)]
    [InlineData(new[] { "ann", "1" }, FileTypeInference.Text)]
    [InlineData(new string[0], FileTypeInference.Text)]
    [InlineData(new[] { "", null }, FileTypeInference.Text)]
    public void Infer_PicksNarrowestType(string?[] values, string expected)
        => FileTypeInference.Infer(values).Should().Be(expected);

    [Fact]
    public void Infer_RecognizesGuidColumn()
    {
        var values = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

        FileTypeInference.Infer(values).Should().Be(FileTypeInference.Uuid);
    }

    [Fact]
    public void Infer_IgnoresEmptyCells()
    {
        // Пустая ячейка — не повод считать числовую колонку текстовой.
        FileTypeInference.Infer(new string?[] { "10", null, "", "20" }).Should().Be(FileTypeInference.BigInt);
    }
}
