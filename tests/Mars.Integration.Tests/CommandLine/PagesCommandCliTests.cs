using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Mars.Admin.Framework.Models;
using Mars.Admin.Host.CommandLine;
using Mars.CommandLine;

namespace Mars.Integration.Tests.CommandLine;

/// <summary>
/// pages CLI: фильтрация/резолв страниц и дерево команд System.CommandLine.
/// Инфраструктура не нужна — сервис страниц не вызывается.
/// </summary>
public class PagesCommandCliTests
{
    private static BlazorPageInfo Page(
        string name, string[] routes, EComponentType kind = EComponentType.Page,
        string? displayName = null, Assembly? assembly = null) => new()
        {
            Name = name,
            PageType = typeof(PagesCommandCliTests),
            Assembly = assembly ?? typeof(PagesCommandCliTests).Assembly,
            Kind = kind,
            Routes = routes,
            Roles = [],
            DisplayName = displayName ?? name,
        };

    private static readonly IReadOnlyList<BlazorPageInfo> SamplePages =
    [
        Page("DevTestPage", ["/tech/devtest"]),
        Page("UserEditPage", ["/user/{id:guid}", "/user/edit"]),
        Page("MainLayout", [], EComponentType.Layout),
        Page("CloudyTag", [], EComponentType.ComponentBase, displayName: "Cloudy Tag"),
    ];

    [Fact]
    public void FilterPages_DefaultKind_OnlyRoutedPages()
    {
        var result = PagesCommandCli.FilterPages(SamplePages, null, null, false, null);

        result.Select(p => p.Name).Should().BeEquivalentTo(["DevTestPage", "UserEditPage"]);
    }

    [Theory]
    [InlineData("layout", "MainLayout")]
    [InlineData("component", "CloudyTag")]
    public void FilterPages_Kind_SelectsMatching(string kind, string expected)
    {
        var result = PagesCommandCli.FilterPages(SamplePages, kind, null, false, null);

        result.Select(p => p.Name).Should().BeEquivalentTo([expected]);
    }

    [Fact]
    public void FilterPages_All_ReturnsEverything()
    {
        PagesCommandCli.FilterPages(SamplePages, "all", null, false, null)
            .Should().HaveCount(SamplePages.Count);
    }

    [Fact]
    public void FilterPages_UnknownKind_Throws()
    {
        var act = () => PagesCommandCli.FilterPages(SamplePages, "widget", null, false, null);

        act.Should().Throw<ArgumentException>().WithMessage("*widget*");
    }

    [Fact]
    public void FilterPages_AssemblyFilter_CaseInsensitiveSubstring()
    {
        var coreAssembly = typeof(object).Assembly;
        var pages = SamplePages.Concat([Page("ForeignPage", ["/foreign"], assembly: coreAssembly)]).ToList();

        var result = PagesCommandCli.FilterPages(pages, "page", "private", false, null);

        result.Select(p => p.Name).Should().BeEquivalentTo(["ForeignPage"]);
    }

    [Fact]
    public void FilterPages_StaticOnly_DropsParameterizedRoutes()
    {
        var result = PagesCommandCli.FilterPages(SamplePages, "page", null, staticOnly: true, null);

        result.Select(p => p.Name).Should().BeEquivalentTo(["DevTestPage", "UserEditPage"]);

        var onlyParam = new[] { Page("ParamPage", ["/x/{id}"]) };
        PagesCommandCli.FilterPages(onlyParam, "page", null, staticOnly: true, null).Should().BeEmpty();
    }

    [Fact]
    public void FilterPages_Regex_MatchesNameDisplayAndRoutes()
    {
        PagesCommandCli.FilterPages(SamplePages, "all", null, false, "devtest")
            .Select(p => p.Name).Should().BeEquivalentTo(["DevTestPage"]);

        PagesCommandCli.FilterPages(SamplePages, "all", null, false, "cloudy tag")
            .Select(p => p.Name).Should().BeEquivalentTo(["CloudyTag"]);

        PagesCommandCli.FilterPages(SamplePages, "page", null, false, "^/user/")
            .Select(p => p.Name).Should().BeEquivalentTo(["UserEditPage"]);
    }

    [Fact]
    public void FilterPages_InvalidRegex_Throws()
    {
        var act = () => PagesCommandCli.FilterPages(SamplePages, "page", null, false, "[invalid");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResolveSingle_ExactName_Wins()
    {
        var page = PagesCommandCli.ResolveSingle(SamplePages, "devtestpage", out _);

        page!.Name.Should().Be("DevTestPage");
    }

    [Fact]
    public void ResolveSingle_ExactRoute_WithAndWithoutSlash()
    {
        PagesCommandCli.ResolveSingle(SamplePages, "/tech/devtest", out _)!.Name.Should().Be("DevTestPage");
        PagesCommandCli.ResolveSingle(SamplePages, "tech/devtest", out _)!.Name.Should().Be("DevTestPage");
    }

    [Fact]
    public void ResolveSingle_Substring_UniqueMatch()
    {
        PagesCommandCli.ResolveSingle(SamplePages, "DevTest", out _)!.Name.Should().Be("DevTestPage");
    }

    [Fact]
    public void ResolveSingle_Ambiguous_ReturnsNullWithCandidates()
    {
        var page = PagesCommandCli.ResolveSingle(SamplePages, "Page", out var candidates);

        page.Should().BeNull();
        candidates.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void ResolveSingle_NotFound_EmptyCandidates()
    {
        var page = PagesCommandCli.ResolveSingle(SamplePages, "NoSuchPage", out var candidates);

        page.Should().BeNull();
        candidates.Should().BeEmpty();
    }

    [Fact]
    public void GetSourceFilePaths_RazorWithCodeBehind_ReturnsBoth()
    {
        var dir = Directory.CreateTempSubdirectory("mars-pages-cli-");
        try
        {
            var razor = Path.Combine(dir.FullName, "SamplePage.razor");
            var codeBehind = razor + ".cs";
            File.WriteAllText(razor, "<h1>hi</h1>");
            File.WriteAllText(codeBehind, "public partial class SamplePage { }");

            var page = Page("SamplePage", ["/sample"]);
            page = new BlazorPageInfo
            {
                Name = page.Name,
                PageType = page.PageType,
                Assembly = page.Assembly,
                Kind = page.Kind,
                Routes = page.Routes,
                Roles = page.Roles,
                DisplayName = page.DisplayName,
                SourceFilePath = razor,
            };

            PagesCommandCli.GetSourceFilePaths(page).Should().BeEquivalentTo([razor, codeBehind]);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void GetSourceFilePaths_NoPath_ReturnsEmpty()
    {
        PagesCommandCli.GetSourceFilePaths(Page("GhostPage", ["/ghost"])).Should().BeEmpty();
    }

    [Fact]
    public void BuildDto_UrlsCarryMountPrefix()
    {
        var json = JsonSerializer.Serialize(PagesCommandCli.BuildDto(SamplePages[0], null));

        json.Should().Contain("/tech/devtest");
        json.Should().Contain(PagesCommandCli.MountPrefix + "/tech/devtest");
    }

    [Theory]
    [InlineData("pages", "list")]
    [InlineData("pages", "list", "--kind", "component", "--static", "--json")]
    [InlineData("pages", "list", "-f", "dev", "-a", "Mars.Admin")]
    [InlineData("pages", "show", "DevTestPage", "--code")]
    [InlineData("pages", "find", "--url", "/dev/tech/devtest", "--json")]
    [InlineData("pages", "layouts", "--json")]
    public void CommandTree_ParsesWithoutErrors(params string[] args)
    {
        var api = new CommandLineApi(typeof(Program).Assembly, []);
        _ = new PagesCommandCli(api);

        var parseResult = api.rootCommand.Parse(args);

        parseResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CommandTree_FindRequiresUrl()
    {
        var api = new CommandLineApi(typeof(Program).Assembly, []);
        _ = new PagesCommandCli(api);

        api.rootCommand.Parse(["pages", "find"]).Errors.Should().NotBeEmpty();
    }
}
