using FluentAssertions;
using Mars.AiChat.Contracts.Options;
using Mars.AiChat.Host;
using Mars.AiChat.Host.Services;
using Mars.Contracts.Dto.Files;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.AiChat.Tests;

public class AdminPagesSkillTests : IDisposable
{
    readonly string _dataDir = Path.Combine(Path.GetTempPath(), "mars-aichat-tests-" + Guid.NewGuid().ToString("N"));
    readonly AiSkillCatalog _catalog;

    public AdminPagesSkillTests()
    {
        var hosting = new FileHostingInfo
        {
            Backend = null,
            PhysicalPath = new Uri(_dataDir),
            RequestPath = "/data",
        };
        _catalog = new AiSkillCatalog(Microsoft.Extensions.Options.Options.Create(hosting), NullLogger<AiSkillCatalog>.Instance);
    }

    public void Dispose()
    {
        _catalog.Dispose();
        if (Directory.Exists(_dataDir)) Directory.Delete(_dataDir, recursive: true);
    }

    [Fact]
    public async Task Bundled_AdminPagesSkill_IsParsed()
    {
        var skills = await _catalog.GetSkillsAsync(CancellationToken.None);

        var skill = skills.FirstOrDefault(s => s.Name == "mars-admin-pages");
        skill.Should().NotBeNull("bundled ai-skills/mars-admin-pages/SKILL.md должен попадать в каталог");
        skill!.Description.Should().NotBeNullOrWhiteSpace();
        skill.Body.Should().Contain("/dev/p/");
        skill.Body.Should().Contain("AddMenuItem");
        skill.Body.Should().Contain("RefreshInitialSiteData");
    }

    [Fact]
    public async Task FrontEditorSkill_MentionsAdminSidebar()
    {
        var skills = await _catalog.GetSkillsAsync(CancellationToken.None);

        var skill = skills.Single(s => s.Name == "mars-front-editor");
        skill.Body.Should().Contain("mars-admin-pages");
        skill.Body.Should().Contain("НЕ в blocks/");
    }

    [Fact]
    public void Route_AdminFrontPage_PreloadsAdminPagesSkill()
    {
        var skills = PageSkillRouter.Route("/p/contacts", frontEditorSlug: null, new AiChatOption());

        skills.Should().Contain("mars-admin-pages");
    }

    [Fact]
    public void Route_AdminFrontEditor_PreloadsBothSkills()
    {
        var skills = PageSkillRouter.Route("/tech/frontend/editor/admin", frontEditorSlug: "admin", new AiChatOption());

        skills.Should().Contain("mars-front-editor");
        skills.Should().Contain("mars-admin-pages");
    }

    [Fact]
    public void Route_OtherFrontEditor_DoesNotPreloadAdminPagesSkill()
    {
        var skills = PageSkillRouter.Route("/tech/frontend/editor/landing", frontEditorSlug: "landing", new AiChatOption());

        skills.Should().Contain("mars-front-editor");
        skills.Should().NotContain("mars-admin-pages");
    }

    [Fact]
    public void Route_EditNavMenuPage_PreloadsAdminPagesSkill()
    {
        var skills = PageSkillRouter.Route("/EditNavMenu/" + Guid.NewGuid(), frontEditorSlug: null, new AiChatOption());

        skills.Should().Contain("mars-admin-pages");
    }

    [Fact]
    public void Route_NavMenuListPage_PreloadsAdminPagesSkill()
    {
        var skills = PageSkillRouter.Route("/NavMenu", frontEditorSlug: null, new AiChatOption());

        skills.Should().Contain("mars-admin-pages");
    }

    [Fact]
    public void Route_PostEditPage_DoesNotPreloadAdminPagesSkill()
    {
        var skills = PageSkillRouter.Route("/EditPost/post/" + Guid.NewGuid(), frontEditorSlug: null, new AiChatOption());

        skills.Should().NotContain("mars-admin-pages");
    }
}