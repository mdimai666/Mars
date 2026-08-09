using FluentAssertions;
using Mars.Host.Shared.Managers;
using Mars.Host.Shared.Services;
using Mars.Services;
using Mars.Shared.Options;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;

namespace Mars.Integration.Tests.Services;

public class FrontManagerTests
{
    const string ContentRoot = @"C:\content";

    static FrontManager CreateManager(
        FrontsOption option,
        out IEventManager eventManager,
        out IOptionService optionService)
    {
        optionService = Substitute.For<IOptionService>();
        optionService.GetOption<FrontsOption>().Returns(option);

        eventManager = Substitute.For<IEventManager>();
        eventManager.Defaults.Returns(new EventManagerDefaults());

        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(ContentRoot);

        return new FrontManager(optionService, eventManager, env);
    }

    [Fact]
    public void FrontItem_Url_NormalizesToLowerAndTrimsSlash()
    {
        var item = new FrontItem { Url = "/App2/" };

        item.Url.Should().Be("/app2");

        item.Url = null!;
        item.Url.Should().BeEmpty();
    }

    [Fact]
    public void GetFrontForUrl_ReturnsMostSpecificEnabledFront()
    {
        var root = new FrontItem { Slug = "default", Url = "" };
        var app2 = new FrontItem { Slug = "app2", Url = "/app2" };
        var app2sub = new FrontItem { Slug = "app2sub", Url = "/app2/sub" };

        var manager = CreateManager(new FrontsOption { Fronts = [root, app2, app2sub] }, out _, out _);

        manager.GetFrontForUrl("/").Should().BeSameAs(root);
        manager.GetFrontForUrl("/any").Should().BeSameAs(root);
        manager.GetFrontForUrl("/app2").Should().BeSameAs(app2);
        manager.GetFrontForUrl("/app2/page").Should().BeSameAs(app2);
        manager.GetFrontForUrl("/app2/sub").Should().BeSameAs(app2sub);
        manager.GetFrontForUrl("/app2/sub/x").Should().BeSameAs(app2sub);
        manager.GetFrontForUrl("/app2x").Should().BeSameAs(root); // без совпадения по префиксу без границы сегмента
    }

    [Fact]
    public void GetFrontForUrl_SkipsDisabledAndReturnsNullWhenNothingMatches()
    {
        var disabled = new FrontItem { Slug = "off", Url = "", Enabled = false };
        var app2 = new FrontItem { Slug = "app2", Url = "/app2", Enabled = false };

        var manager = CreateManager(new FrontsOption { Fronts = [disabled, app2] }, out _, out _);

        manager.GetFrontForUrl("/").Should().BeNull();
        manager.GetFrontForUrl("/app2").Should().BeNull();
    }

    [Fact]
    public void ResolvePhysicalPath_Default_IsDataFrontsSlug()
    {
        var manager = CreateManager(new FrontsOption(), out _, out _);
        var front = new FrontItem { Slug = "my-site" };

        manager.ResolvePhysicalPath(front).Should().Be(Path.Combine(ContentRoot, "data", "fronts", "my-site"));
    }

    [Fact]
    public void ResolvePhysicalPath_ExternalFolder_AbsoluteAndRelative()
    {
        var manager = CreateManager(new FrontsOption(), out _, out _);

        var absolute = new FrontItem { Slug = "a", Path = @"C:\external\template" };
        manager.ResolvePhysicalPath(absolute).Should().Be(@"C:\external\template");

        var relative = new FrontItem { Slug = "r", Path = "ext-front" };
        manager.ResolvePhysicalPath(relative).Should().Be(Path.Combine(ContentRoot, "ext-front"));
    }

    [Fact]
    public void Changed_Raised_WhenFrontsOptionUpdated()
    {
        var initial = new FrontsOption { Fronts = [new FrontItem { Slug = "one", Url = "" }] };
        var updated = new FrontsOption { Fronts = [new FrontItem { Slug = "two", Url = "" }] };

        var optionService = Substitute.For<IOptionService>();
        optionService.GetOption<FrontsOption>().Returns(initial, updated);

        var eventManager = Substitute.For<IEventManager>();
        eventManager.Defaults.Returns(new EventManagerDefaults());

        string? listenedTopic = null;
        Action<ManagerEventPayload>? listener = null;
        eventManager.AddEventListener(
            Arg.Do<string>(t => listenedTopic = t),
            Arg.Do<Action<ManagerEventPayload>>(l => listener = l));

        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(ContentRoot);

        var manager = new FrontManager(optionService, eventManager, env);

        listenedTopic.Should().Be("Option.FrontsOption");

        var changedRaised = false;
        manager.Changed += () => changedRaised = true;

        listener!.Invoke(new ManagerEventPayload(listenedTopic!, updated));

        changedRaised.Should().BeTrue();
        manager.Fronts.Single().Slug.Should().Be("two");
    }

    [Theory]
    [InlineData("", "default")]
    [InlineData("/", "default")]
    [InlineData("/app2", "app2")]
    [InlineData("/App 2/x!", "app2-x")]
    public void MakeSlug_SanitizesUrl(string? url, string expected)
    {
        AppFrontMigration.MakeSlug(url, []).Should().Be(expected);
    }

    [Fact]
    public void MakeSlug_DuplicatesGetNumericSuffix()
    {
        AppFrontMigration.MakeSlug("/app2", ["app2"]).Should().Be("app21");
        AppFrontMigration.MakeSlug("/app2", ["app2", "app21"]).Should().Be("app22");
    }

    [Fact]
    public void MapToOption_MigratesOnlyHandlebarsModes()
    {
        var cfg = new AppFrontMigration.LegacyAppFrontCfg[]
        {
            new() { Mode = "HandlebarsTemplateStatic", Path = @"C:\tpl", Url = "" },
            new() { Mode = "HandlebarsTemplate", Path = "", Url = "/app2" },
            new() { Mode = "None", Path = "", Url = "/none" },
            new() { Mode = "ServeStaticBlazor", Path = "", Url = "/blazor" },
            new() { Mode = "BlazorPrerender", Path = "", Url = "/prerender" },
            new() { Mode = "HandlebarsTemplateStatic", Path = "", Url = "/app2" },
        };

        var option = AppFrontMigration.MapToOption(cfg);

        option.Fronts.Should().HaveCount(3);

        option.Fronts[0].Slug.Should().Be("default");
        option.Fronts[0].Url.Should().Be("");
        option.Fronts[0].Path.Should().Be(@"C:\tpl");
        option.Fronts[0].EngineId.Should().Be(FrontItem.HandlebarsEngine);
        option.Fronts[0].Enabled.Should().BeTrue();

        option.Fronts[1].Slug.Should().Be("app2");
        option.Fronts[1].Url.Should().Be("/app2");

        option.Fronts[2].Slug.Should().Be("app21");
        option.Fronts[2].Url.Should().Be("/app2");
    }

    [Fact]
    public void IsValidSlug_ChecksCharacters()
    {
        FrontManager.IsValidSlug("my-site_1").Should().BeTrue();
        FrontManager.IsValidSlug("").Should().BeFalse();
        FrontManager.IsValidSlug(null).Should().BeFalse();
        FrontManager.IsValidSlug("bad/slug").Should().BeFalse();
        FrontManager.IsValidSlug("bad slug").Should().BeFalse();
    }

    [Fact]
    public void AdminFront_IsSpecialFront_WithReservedSlugAndAdminPath()
    {
        var manager = CreateManager(new FrontsOption(), out _, out _);

        var admin = manager.AdminFront;
        admin.Slug.Should().Be(FrontManager.AdminFrontSlug);
        admin.EngineId.Should().Be(FrontItem.HandlebarsEngine);
        admin.Path.Should().Be(FrontManager.AdminFrontDirName);
        admin.Enabled.Should().BeTrue();

        manager.ResolvePhysicalPath(admin).Should().Be(Path.Combine(ContentRoot, "data", "admin", "front"));
    }

    [Fact]
    public void FindBySlug_ReturnsAdminFront_ForReservedSlug_CaseInsensitive()
    {
        var front = new FrontItem { Slug = "site", Url = "" };
        var manager = CreateManager(new FrontsOption { Fronts = [front] }, out _, out _);

        manager.FindBySlug("admin").Should().BeSameAs(manager.AdminFront);
        manager.FindBySlug("ADMIN").Should().BeSameAs(manager.AdminFront);
    }

    [Fact]
    public void FindBySlug_ReturnsConfiguredFront_OrNullOrAdmin()
    {
        var front = new FrontItem { Slug = "site", Url = "" };
        var manager = CreateManager(new FrontsOption { Fronts = [front] }, out _, out _);

        manager.FindBySlug("site").Should().BeSameAs(front);
        manager.FindBySlug("unknown").Should().BeNull();
    }

    [Fact]
    public void AdminFront_NotInFronts_And_NotResolvedByUrl()
    {
        var manager = CreateManager(new FrontsOption(), out _, out _);

        manager.Fronts.Should().BeEmpty();
        manager.GetFrontForUrl("/").Should().BeNull();
        manager.GetFrontForUrl("/admin").Should().BeNull();
    }
}
