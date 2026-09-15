using FluentAssertions;
using Mars.Datasource.Abstractions.Models;

namespace Mars.Datasource.Integration.Tests;

public class QColumnMappingTests
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
        QColumnMapping.Kind(dataTypeName).Should().Be(QColumnKind.Number);
    }

    [Theory]
    [InlineData("boolean")]
    [InlineData("bool")]
    [InlineData("bit")]
    public void Kind_BooleanType_ReturnsBoolean(string dataTypeName)
    {
        QColumnMapping.Kind(dataTypeName).Should().Be(QColumnKind.Boolean);
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
        QColumnMapping.Kind(dataTypeName).Should().Be(QColumnKind.DateTime);
    }

    [Theory]
    [InlineData("uuid")]
    [InlineData("UNIQUEIDENTIFIER")]
    public void Kind_GuidType_ReturnsGuid(string dataTypeName)
    {
        QColumnMapping.Kind(dataTypeName).Should().Be(QColumnKind.Guid);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("jsonb")]
    [InlineData("JSON")]
    public void Kind_JsonType_ReturnsJson(string dataTypeName)
    {
        QColumnMapping.Kind(dataTypeName).Should().Be(QColumnKind.Json);
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
        QColumnMapping.Kind(dataTypeName).Should().Be(QColumnKind.Text);
    }
}
