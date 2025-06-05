using System.Globalization;
using System.Reflection;
using FluentAssertions;
using HandlebarsDotNet;
using Mars.Host.Shared.Interfaces;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Templators.HandlebarsFunc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Localization;
using NSubstitute;
using static Mars.Host.Templators.HandlebarsFunc.MyHandlebarsContextFunctions;

namespace Test.Mars.WebSiteProcessor.Templators.HandlebarsEngine;

[Collection("Culture collection")]
public class MyHandlebarsContextFunctionsTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFeatureCollection _featureCollection;

    public MyHandlebarsContextFunctionsTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _featureCollection = Substitute.For<IFeatureCollection>();
    }

    string Render(string html, object? data = null, Action<MyHandlebars>? builder = null)
    {
        using var hbs = new MyHandlebars();
        hbs.RegisterContextFunctions();
        builder?.Invoke(hbs);
        var template = hbs.Compile(html);
        var pageRenderContext = new PageRenderContext()
        {
            IsDevelopment = true,
            RenderParam = new(),
            SysOptions = new(),
            Request = new(new Uri("http://localhost")),
            User = null,
            TemplateContextVaribles = ObjectToDictionary(data ?? new { })
        };
        var rctx = new HandlebarsHelperFunctionContext(pageRenderContext, _serviceProvider, CancellationToken.None);
        rctx.Features = _featureCollection;
        return template(data ?? new { }, new { rctx });
    }

    public static Dictionary<string, object?> ObjectToDictionary(object obj)
    {
        if (obj == null)
            return new Dictionary<string, object?>();

        return obj.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.CanRead)
                    .ToDictionary(
                        prop => prop.Name,
                        prop => prop.GetValue(obj)
                    );
    }

    [Fact]
    public void CultureCheck()
    {
        Assert.Equal("ru-RU", CultureInfo.CurrentCulture.Name);
    }

    [Fact]
    public void Localizer_Helper_Tests()
    {
        _ = nameof(Localizer_Helper);
        var stringLocalizer = Substitute.For<IStringLocalizer>();
        stringLocalizer["Title"].Returns(new LocalizedString("Title", "Заголовок"));
        var appFrontLocalizer = Substitute.For<IAppFrontLocalizer>();
        appFrontLocalizer.GetLocalizer(Arg.Any<string>()).Returns(stringLocalizer);
        _serviceProvider.GetService(typeof(IAppFrontLocalizer)).Returns(appFrontLocalizer);
        Render("{{#L Title}}").Should().Be("Заголовок");
    }

    [Fact]
    public void IffBlock_Tests()
    {
        _ = nameof(IffBlock);
        Render("{{#iff 2>1}}+{{else}}-{{/iff}}").Should().Be("+");
        Render("{{#iff '2 > 1'}}+{{else}}-{{/iff}}").Should().Be("+");
        Render("{{#iff 1>2}}+{{else}}-{{/iff}}").Should().Be("-");

        var ctx = new { x = 2, y = 1 };
        var d = ObjectToDictionary(ctx);
        d.Keys.Count.Should().Be(2);
        Render("{{#iff x>y}}+{{else}}-{{/iff}}", ctx).Should().Be("+");
        Render("{{#iff 'x==y'}}+{{else}}-{{/iff}}", ctx).Should().Be("-");
    }

    [Theory]
    [InlineData(["1 > 2"])]
    [InlineData(["1=2"])]
    [InlineData(["x==2"])]
    public void IffBlock_InvalidArguments_Tests(string exp)
    {
        _ = nameof(IffBlock);
        var action = () => Render($"{{{{#iff {exp}}}}}+{{{{else}}}}-{{{{/iff}}}}");
        action.Should().Throw<HandlebarsException>();
    }

    WebSiteTemplate EmptyWebSiteTemplate(WebPartSource[] parts) =>
        new WebSiteTemplate([
            new WebPartSource("<div>@Body</div>", "_root.hbs","","",""),
            new WebPartSource("@page /", "index.hbs","","",""),
            ..parts
            ]);

    [Fact]
    public void RawBlock_Tests()
    {
        _ = nameof(RawBlock);
        var webSiteTemplate = EmptyWebSiteTemplate([new("{{x}}", "block1", "", "", "")]);
        _featureCollection.Get<WebSiteTemplate>().Returns(webSiteTemplate);
        Render("{{#raw_block block1}}").Should().Be("{{x}}");
    }
}
