using FluentAssertions;
using Mars.Services;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;

namespace Mars.Integration.Tests.Services;

public class FrontTemplateServiceTests : IDisposable
{
    readonly string dir;

    public FrontTemplateServiceTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mars-front-template-tests", Guid.NewGuid().ToString("N"));

        // два стартовых шаблона с маркерными файлами
        WriteTemplateFile("default", "default-marker.txt");
        WriteTemplateFile("landing", "landing-marker.txt");
    }

    void WriteTemplateFile(string template, string file)
    {
        var templateDir = Path.Combine(dir, "Res", "front_templates", template);
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, file), template);
    }

    FrontTemplateService CreateService()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(dir);
        return new FrontTemplateService(env);
    }

    [Fact]
    public void CreateFrontFromTemplate_CopiesSelectedTemplate_NotDefault()
    {
        var service = CreateService();

        service.CreateFrontFromTemplate("landing", "landing");

        var dest = Path.Combine(dir, "data", "fronts", "landing");
        File.Exists(Path.Combine(dest, "landing-marker.txt")).Should().BeTrue("должен копироваться выбранный шаблон");
        File.Exists(Path.Combine(dest, "default-marker.txt")).Should().BeFalse("дефолтный шаблон подменять выбранный не должен");
    }

    public void Dispose()
    {
        try
        {
            var baseDir = Path.GetDirectoryName(dir)!;
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, true);
        }
        catch
        {
            // временная папка
        }
    }
}
