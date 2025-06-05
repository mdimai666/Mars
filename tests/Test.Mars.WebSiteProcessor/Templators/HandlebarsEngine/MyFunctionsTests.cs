using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using Mars.Host.Templators.HandlebarsFunc;
using static Mars.Host.Templators.HandlebarsFunc.MyHandlebarsBasicFunctions;

namespace Test.Mars.WebSiteProcessor.Templators.HandlebarsEngine;

public class MyFunctionsTests
{
    string Render(string html, object? data = null)
    {
        using var hbs = new MyHandlebars();
        var template = hbs.Compile(html);
        return template(data ?? new { });
    }

    [Fact]
    public void EqualBlock_Tests()
    {
        _ = nameof(EqualBlock);
        Render("{{#eq 1 1}}+{{else}}-{{/eq}}").Should().Be("+");
        Render("{{#eq 1 2}}+{{else}}-{{/eq}}").Should().Be("-");
    }

    [Fact]
    public void NotEqualBlock_Tests()
    {
        _ = nameof(NotEqualBlock);
        Render("{{#neq 1 1}}+{{else}}-{{/neq}}").Should().Be("-");
        Render("{{#neq 1 2}}+{{else}}-{{/neq}}").Should().Be("+");
    }

    [Fact]
    public void GreaterThanBlock_Tests()
    {
        _ = nameof(GreaterThanBlock);
        Render("{{#gt 1 1}}+{{else}}-{{/gt}}").Should().Be("-");
        Render("{{#gt 1 2}}+{{else}}-{{/gt}}").Should().Be("-");
        Render("{{#gt 2 1}}+{{else}}-{{/gt}}").Should().Be("+");
    }

    [Fact]
    public void TextEllipsisHelper_Tests()
    {
        _ = nameof(TextEllipsisHelper);
        Render("{{#text_ellipsis 12345 10}}").Should().Be("12345");
        Render("{{#text_ellipsis 12345 2}}").Should().Be("12...");
    }

    [Fact]
    public void StripHtmlHelper_Tests()
    {
        _ = nameof(StripHtmlHelper);
        Render("{{#striphtml '<div>123</div>' }}").Should().Be("123");
    }

    [Fact]
    public void ToJsonHelper_Tests()
    {
        var opt = new JsonSerializerOptions()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        _ = nameof(ToJsonHelper);
        var obj = new { Name = "dima" };
        var objExpectJson = JsonSerializer.Serialize(obj, opt);
        var objCyrillic = new { Name = "дима" };
        var objCyrillicExpectJson = JsonSerializer.Serialize(objCyrillic, opt);
        Render("{{#tojson x}}", new { x = obj }).Should().Be(objExpectJson);
        Render("{{#tojson x}}", new { x = objCyrillic }).Should().Be(objCyrillicExpectJson);
    }
}
