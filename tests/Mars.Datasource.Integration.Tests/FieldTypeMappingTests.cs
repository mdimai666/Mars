using FluentAssertions;
using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Integration.Tests;

public class FieldTypeMappingTests
{
    [Theory]
    [InlineData("smallint")]
    [InlineData("INTEGER")]
    [InlineData("bigint")]
    [InlineData(" numeric ")]
    [InlineData("money")]
    [InlineData("real")]
    [InlineData("double precision")]
    public void Kind_NumericType_ReturnsNumber(string dataTypeName)
    {
        FieldTypeMapping.Kind(dataTypeName).Should().Be(FieldKind.Number);
    }

    [Theory]
    [InlineData("boolean")]
    [InlineData("bool")]
    [InlineData("bit")]
    public void Kind_BooleanType_ReturnsBoolean(string dataTypeName)
    {
        FieldTypeMapping.Kind(dataTypeName).Should().Be(FieldKind.Boolean);
    }

    [Theory]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData("time without time zone")]
    [InlineData("timestamp")]
    [InlineData("timestamp with time zone")]
    [InlineData("datetime2")]
    [InlineData("datetimeoffset")]
    public void Kind_DateTimeType_ReturnsDateTime(string dataTypeName)
    {
        FieldTypeMapping.Kind(dataTypeName).Should().Be(FieldKind.DateTime);
    }

    [Theory]
    [InlineData("uuid")]
    [InlineData("UNIQUEIDENTIFIER")]
    public void Kind_GuidType_ReturnsGuid(string dataTypeName)
    {
        FieldTypeMapping.Kind(dataTypeName).Should().Be(FieldKind.Guid);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("jsonb")]
    [InlineData("JSON")]
    public void Kind_JsonType_ReturnsJson(string dataTypeName)
    {
        FieldTypeMapping.Kind(dataTypeName).Should().Be(FieldKind.Json);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("character varying")]
    [InlineData("nvarchar")]
    [InlineData("bytea")]
    [InlineData("interval")]
    [InlineData("inet")]
    [InlineData("")]
    [InlineData(null)]
    public void Kind_TextOrUnknownType_ReturnsText(string? dataTypeName)
    {
        FieldTypeMapping.Kind(dataTypeName).Should().Be(FieldKind.Text);
    }

    [Theory]
    [InlineData("timestamp with time zone", "timestamptz")]
    [InlineData("TIMESTAMP WITHOUT TIME ZONE", "timestamp")]
    [InlineData("time with time zone", "timetz")]
    [InlineData("time without time zone", "time")]
    [InlineData("character varying", "varchar")]
    [InlineData("character varying[]", "varchar[]")]
    [InlineData("double precision", "float8")]
    [InlineData("uuid", "uuid")]
    [InlineData("NVARCHAR", "NVARCHAR")]
    [InlineData(" int4 ", "int4")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ShortTypeName_ProviderTypeName_ReturnsShortName(string? dataTypeName, string expected)
    {
        FieldTypeMapping.ShortTypeName(dataTypeName).Should().Be(expected);
    }
}
