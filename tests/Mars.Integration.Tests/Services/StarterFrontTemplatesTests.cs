using FluentAssertions;
using Mars.SiteEngine.Abstractions.WebSite.Models;
using Mars.SiteEngine.Host.Services;
using Mars.SiteEngine.Host.WebSite.SourceProviders;
using Mars.Test.Common.Helpers;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// Стартовые шаблоны фронтов (Res/front_templates) должны быть валидными WebSiteTemplate:
/// их визард предлагает на последнем шаге, сломанный шаблон упадёт на первом запросе.
/// </summary>
public class StarterFrontTemplatesTests
{
    static string TemplatesRoot => SolutionPathHelper.Resolve("src", "Mars.WebApp", "Res", "front_templates");

    [Theory]
    [InlineData("default")]
    [InlineData("landing")]
    public void StarterTemplate_IsValidWebSiteTemplate(string templateName)
    {
        var path = Path.Combine(TemplatesRoot, templateName);
        Directory.Exists(path).Should().BeTrue($"шаблон '{templateName}' должен существовать в Res/front_templates");

        var source = new WebTemplateFilesystemSource(path, new WebFilesReadFilesystemService());
        var template = new WebSiteTemplate(source.ReadParts());

        template.RootPage.Should().NotBeNull();
        template.IndexPage.Should().NotBeNull();
        template.IndexPage.Url.Value.Should().Be("/");
    }
}
