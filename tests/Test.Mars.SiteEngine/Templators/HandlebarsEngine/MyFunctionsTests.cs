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
    public void GreaterThanOrEqualBlock_Tests()
    {
        _ = nameof(GreaterThanOrEqualBlock);
        Render("{{#gte 1 1}}+{{else}}-{{/gte}}").Should().Be("+");
        Render("{{#gte 1 2}}+{{else}}-{{/gte}}").Should().Be("-");
        Render("{{#gte 2 1}}+{{else}}-{{/gte}}").Should().Be("+");
    }

    [Fact]
    public void LessThanBlock_Tests()
    {
        _ = nameof(LessThanBlock);
        Render("{{#lt 1 1}}+{{else}}-{{/lt}}").Should().Be("-");
        Render("{{#lt 1 2}}+{{else}}-{{/lt}}").Should().Be("+");
        Render("{{#lt 2 1}}+{{else}}-{{/lt}}").Should().Be("-");
    }

    [Fact]
    public void LessThanOrEqualBlock_Tests()
    {
        _ = nameof(LessThanOrEqualBlock);
        Render("{{#lte 1 1}}+{{else}}-{{/lte}}").Should().Be("+");
        Render("{{#lte 1 2}}+{{else}}-{{/lte}}").Should().Be("+");
        Render("{{#lte 2 1}}+{{else}}-{{/lte}}").Should().Be("-");
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

    [Fact]
    public void AndBlock_Test()
    {
        _ = nameof(AndBlock);
        Render("{{#and true true}}yes{{else}}no{{/and}}").Should().Be("yes");
        Render("{{#and true false}}yes{{else}}no{{/and}}").Should().Be("no");
    }

    [Fact]
    public void OrBlock_Test()
    {
        _ = nameof(OrBlock);
        Render("{{#or false true}}yes{{else}}no{{/or}}").Should().Be("yes");
        Render("{{#or false false}}yes{{else}}no{{/or}}").Should().Be("no");
    }

    [Fact]
    public void IsEmptyBlock_Test()
    {
        _ = nameof(IsEmptyBlock);
        Render("{{#isEmpty ''}}empty{{else}}not empty{{/isEmpty}}").Should().Be("empty");
        Render("{{#isEmpty 'hello'}}empty{{else}}not empty{{/isEmpty}}").Should().Be("not empty");
    }

    [Fact]
    public void ContainsBlock_Test()
    {
        _ = nameof(ContainsBlock);
        Render("{{#contains 'hello world' 'world'}}yes{{else}}no{{/contains}}").Should().Be("yes");
        Render("{{#contains 'hello' 'xyz'}}yes{{else}}no{{/contains}}").Should().Be("no");

        var context = new { list = new[] { "a", "b", "c" } };
        Render("{{#contains list 'b'}}yes{{else}}no{{/contains}}", context).Should().Be("yes");
        Render("{{#contains list 'x'}}yes{{else}}no{{/contains}}", context).Should().Be("no");
    }
}
