using System.Text.Json;
using FluentAssertions;
using Mars.Datasource.Contracts.Ai;

namespace Mars.Datasource.Tests;

/// <summary>
/// Параметры операции из JSON-текста агента (parametersJson в run_query):
/// объект имя→значение, не-строки — их JSON-представлением, пустой текст — параметров нет.
/// </summary>
public class DatasourceParametersJsonTests
{
    [Fact]
    public void Parse_Object_MapsValues()
    {
        var parameters = DatasourceParametersJson.Parse("""{"page":"2","per_page":10,"search":"mars"}""");

        parameters.Should().NotBeNull();
        parameters.Should().HaveCount(3);
        parameters![0].Name.Should().Be("page");
        parameters[0].Value.Should().Be("2");
        parameters[1].Name.Should().Be("per_page");
        parameters[1].Value.Should().Be("10");
        parameters[2].Value.Should().Be("mars");
    }

    [Fact]
    public void Parse_NullAndBool_KeepJsonForm()
    {
        var parameters = DatasourceParametersJson.Parse("""{"a":null,"b":true,"c":{"x":1}}""");

        parameters.Should().NotBeNull();
        parameters![0].Value.Should().BeNull();
        parameters[1].Value.Should().Be("true");
        parameters[2].Value.Should().Be("{\"x\":1}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Empty_ReturnsNull(string? json)
    {
        DatasourceParametersJson.Parse(json).Should().BeNull();
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("\"строка\"")]
    [InlineData("42")]
    public void Parse_NotObject_Throws(string json)
    {
        var act = () => DatasourceParametersJson.Parse(json);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_BrokenJson_Throws()
    {
        var act = () => DatasourceParametersJson.Parse("{page:");

        act.Should().Throw<JsonException>();
    }
}
