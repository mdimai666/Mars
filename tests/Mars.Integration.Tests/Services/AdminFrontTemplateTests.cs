using FluentAssertions;
using Mars.Host.Shared.WebSite.Models;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// Дефолтные файлы специального фронта админки обязаны собирать WebSiteTemplate:
/// конструктор требует и Root (StartPath "/"), и IndexPage (@page "/").
/// </summary>
public class AdminFrontTemplateTests
{
    const string RootContent = "@Body";

    const string IndexContent = """
@page "/"

<div>welcome</div>
""";

    [Fact]
    public void AdminFront_DefaultFiles_BuildTemplate()
    {
        var root = new WebPartSource(RootContent, "_root.hbs", "_root", "/front/_root.hbs", "_root.hbs");
        var index = new WebPartSource(IndexContent, "admin_index.hbs", "admin_index", "/front/admin_index.hbs", "admin_index.hbs");

        var template = new WebSiteTemplate(new[] { root, index });

        template.RootPage.Should().NotBeNull();
        template.RootPage.StartPath.Should().Be("/");
        template.RootPage.Content.Should().Be("@Body");

        template.IndexPage.Should().NotBeNull();
        template.IndexPage.Url.Value.Should().Be("/");
    }
}
