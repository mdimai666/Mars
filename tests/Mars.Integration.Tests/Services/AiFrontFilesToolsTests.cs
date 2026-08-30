using FluentAssertions;
using Mars.AiChat.Host.Tools;
using Mars.Core.Models;
using Mars.SiteEngine.Abstractions.Models;
using Mars.SiteEngine.Abstractions.Services;
using Mars.SiteEngine.Abstractions.WebSite;
using Mars.SiteEngine.Abstractions.WebSite.Interfaces;
using Mars.SiteEngine.Contracts.Options;
using Mars.SiteEngine.Host.Services;
using NSubstitute;

namespace Mars.Integration.Tests.Services;

/// <summary>
/// ИИ-инструменты файлов фронта (Фаза 6): поверх реального FrontFilesService
/// с подменённым IFrontManager (временная папка вместо data/fronts/&lt;slug&gt;).
/// </summary>
public class AiFrontFilesToolsTests : IDisposable
{
    const string Slug = "site";

    readonly string dir;
    readonly FrontItem front;
    readonly IFrontManager frontManager;
    readonly MarsFrontFilesTools tools;

    public AiFrontFilesToolsTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mars-ai-front-tools-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);

        front = new FrontItem { Slug = Slug, Title = Slug, Url = "", Path = dir };
        frontManager = Substitute.For<IFrontManager>();
        frontManager.FindBySlug(Slug).Returns(front);
        frontManager.ResolvePhysicalPath(front).Returns(dir);

        tools = new MarsFrontFilesTools(new FrontFilesService(frontManager, Substitute.For<IWebRenderEngineLocator>()), Slug);
    }

    [Theory]
    [InlineData("/front/editor/default", "default")]
    [InlineData("/front/editor/my-front", "my-front")]
    [InlineData("/Front/Editor/Mixed-Case", "Mixed-Case")]
    [InlineData("/front/editor/", null)]
    [InlineData("/front/editor", null)]
    [InlineData("/EditPost/post/123", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void TryParseSlugFromPageContext_ParsesEditorUrl(string? pageContext, string? expected)
    {
        MarsFrontFilesTools.TryParseSlugFromPageContext(pageContext).Should().Be(expected);
    }

    [Fact]
    public void ListFrontFiles_ShowsTree_WithoutHiddenFolders()
    {
        File.WriteAllText(Path.Combine(dir, "_root.hbs"), "root");
        Directory.CreateDirectory(Path.Combine(dir, "pages"));
        File.WriteAllText(Path.Combine(dir, "pages", "index_page.hbs"), "index");
        Directory.CreateDirectory(Path.Combine(dir, "wwwroot", "css"));
        File.WriteAllText(Path.Combine(dir, "wwwroot", "css", "app.css"), "css");
        Directory.CreateDirectory(Path.Combine(dir, ".git"));
        File.WriteAllText(Path.Combine(dir, ".git", "config"), "");
        Directory.CreateDirectory(Path.Combine(dir, "node_modules"));
        File.WriteAllText(Path.Combine(dir, "node_modules", "y.js"), "");

        var tree = tools.ListFrontFiles();

        tree.Should().Contain("_root.hbs");
        tree.Should().Contain("pages/");
        tree.Should().Contain("index_page.hbs");
        tree.Should().Contain("app.css");
        tree.Should().NotContain(".git");
        tree.Should().NotContain("node_modules");
    }

    [Fact]
    public void WriteFrontFile_CreatesNestedFolders_AndReadReturnsContent()
    {
        var writeResult = tools.WriteFrontFile("pages/about.hbs", "<h1>about</h1>");

        writeResult.Should().Contain("сохранён");
        File.Exists(Path.Combine(dir, "pages", "about.hbs")).Should().BeTrue();

        tools.ReadFrontFile("pages/about.hbs").Should().Be("<h1>about</h1>");
    }

    [Fact]
    public void ReadFrontFile_MissingFile_ReturnsErrorString_NotThrow()
    {
        tools.ReadFrontFile("pages/nope.hbs").Should().StartWith("Ошибка");
    }

    [Fact]
    public void WriteFrontFile_PathOutsideFront_Rejected()
    {
        var result = tools.WriteFrontFile("../evil.hbs", "x");

        result.Should().StartWith("Ошибка");
        File.Exists(Path.Combine(dir, "..", "evil.hbs")).Should().BeFalse();
    }

    [Fact]
    public void CreateFrontFile_CreatesFileAndFolder_DuplicateRejected()
    {
        tools.CreateFrontFile("blocks/new_block.hbs").Should().Contain("Создано");
        tools.CreateFrontFile("new_folder", isFolder: true).Should().Contain("Создано");

        File.Exists(Path.Combine(dir, "blocks", "new_block.hbs")).Should().BeTrue();
        Directory.Exists(Path.Combine(dir, "new_folder")).Should().BeTrue();

        tools.CreateFrontFile("blocks/new_block.hbs").Should().StartWith("Ошибка");
    }

    [Fact]
    public void RenameFrontFile_MovesFile_OldPathGone()
    {
        tools.WriteFrontFile("pages/old.hbs", "content");

        tools.RenameFrontFile("pages/old.hbs", "pages/new.hbs").Should().Contain("Переименовано");

        File.Exists(Path.Combine(dir, "pages", "old.hbs")).Should().BeFalse("старый файл должен исчезать");
        tools.ReadFrontFile("pages/new.hbs").Should().Be("content");
    }

    [Fact]
    public void DeleteFrontFile_RemovesFile_MissingReturnsError()
    {
        tools.WriteFrontFile("temp.hbs", "x");

        tools.DeleteFrontFile("temp.hbs").Should().Contain("Удалено");
        File.Exists(Path.Combine(dir, "temp.hbs")).Should().BeFalse();

        tools.DeleteFrontFile("temp.hbs").Should().StartWith("Ошибка");
    }

    [Fact]
    public void WriteFrontFile_ImmediatelyNotifiesFrontRenderEngine()
    {
        // инвалидация кеша рендера не должна зависеть от FileSystemWatcher:
        // запись через FrontFilesService сразу уведомляет движок фронта
        var templateService = Substitute.For<IWebTemplateService>();
        var appFront = new MarsAppFront
        {
            Configuration = new AppFrontSettingsCfg { Path = dir, Url = "" },
            Front = front,
        };
        appFront.Features.Set<IWebTemplateService>(templateService);

        var locator = Substitute.For<IWebRenderEngineLocator>();
        locator.TryGetAppFrontBySlug(Slug).Returns(appFront);

        var localTools = new MarsFrontFilesTools(new FrontFilesService(frontManager, locator), Slug);
        localTools.WriteFrontFile("pages/x.hbs", "x");

        templateService.Received(1).NotifyFileChanged(Path.Combine(dir, "pages", "x.hbs"));
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
