using System.Reflection;
using FluentAssertions;
using Mars.Host.Shared.Services;
using Mars.Host.Shared.WebSite.Models;
using Mars.Host.Shared.WebSite.SourceProviders;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// Стартовые шаблоны фронтов (Res/front_templates) должны быть валидными WebSiteTemplate:
/// их визард предлагает на последнем шаге, сломанный шаблон упадёт на первом запросе.
/// </summary>
public class StarterFrontTemplatesTests
{
    static string TemplatesRoot
    {
        get
        {
            var testDirPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..", ".."));
            return Path.GetFullPath(Path.Combine(testDirPath, "..", "..", "src", "Mars.WebApp", "Res", "front_templates"));
        }
    }

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
