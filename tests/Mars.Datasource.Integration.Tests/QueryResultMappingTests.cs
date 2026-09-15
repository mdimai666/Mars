using FluentAssertions;
using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Integration.Tests;

public class QueryResultMappingTests
{
    [Fact]
    public void Format_StringArray_ReturnsPostgresLiteral()
    {
        QueryResultMapping.Format(new[] { "a", "b" }).Should().Be("{a,b}");
    }

    [Fact]
    public void Format_ArrayItemWithComma_QuotesItem()
    {
        QueryResultMapping.Format(new[] { "a,b" }).Should().Be("{\"a,b\"}");
    }

    [Fact]
    public void Format_ArrayItemWithQuote_EscapesQuote()
    {
        QueryResultMapping.Format(new[] { "a\"b" }).Should().Be("{\"a\\\"b\"}");
    }

    [Fact]
    public void Format_ArrayWithNull_ReturnsNullKeyword()
    {
        QueryResultMapping.Format(new string?[] { "a", null }).Should().Be("{a,NULL}");
    }

    [Fact]
    public void Format_ArrayItemEmptyOrNullWord_QuotesItem()
    {
        QueryResultMapping.Format(new[] { "NULL", "" }).Should().Be("{\"NULL\",\"\"}");
    }

    [Fact]
    public void Format_NestedArray_ReturnsNestedLiteral()
    {
        QueryResultMapping.Format(new[] { new[] { 1, 2 }, new[] { 3 } }).Should().Be("{{1,2},{3}}");
    }

    [Fact]
    public void Format_ByteArray_ReturnsBase64()
    {
        QueryResultMapping.Format(new byte[] { 1, 2, 3 }).Should().Be("AQID");
    }

    [Fact]
    public void Format_DateTime_ReturnsInvariantTimestamp()
    {
        QueryResultMapping.Format(new DateTime(2026, 9, 15, 10, 30, 0, DateTimeKind.Utc))
            .Should().Be("2026-09-15T10:30:00.0000000Z");
    }

    [Theory]
    [InlineData("2026-09-15T10:30:00.0000000+03:00", "2026-09-15 10:30")]
    [InlineData("2026-09-15T10:30:00", "2026-09-15 10:30")]
    [InlineData("2026-09-15T10:30:59.1234567Z", "2026-09-15 10:30")]
    [InlineData("2026-09-15", "2026-09-15")]
    [InlineData("10:30:00.0000000", "10:30")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void DisplayDateTime_ValueInContractFormat_ReturnsValueWithoutSecondsAndZone(string? value, string? expected)
    {
        QueryResultMapping.DisplayDateTime(value).Should().Be(expected);
    }

    [Fact]
    public void Format_Null_ReturnsNull()
    {
        QueryResultMapping.Format(null).Should().BeNull();
        QueryResultMapping.Format(DBNull.Value).Should().BeNull();
    }
}
