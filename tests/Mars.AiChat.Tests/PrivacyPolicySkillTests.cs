using FluentAssertions;
using Mars.AiChat.Contracts.Options;
using Mars.AiChat.Host;
using Mars.AiChat.Host.Services;
using Mars.Contracts.Dto.Files;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mars.AiChat.Tests;

public class PrivacyPolicySkillTests : IDisposable
{
    readonly string _dataDir = Path.Combine(Path.GetTempPath(), "mars-aichat-tests-" + Guid.NewGuid().ToString("N"));
    readonly AiSkillCatalog _catalog;

    public PrivacyPolicySkillTests()
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
    public async Task Bundled_PrivacyPolicySkill_IsParsed()
    {
        var skills = await _catalog.GetSkillsAsync(CancellationToken.None);

        var skill = skills.FirstOrDefault(s => s.Name == "mars-privacy-policy");
        skill.Should().NotBeNull("bundled ai-skills/mars-privacy-policy/SKILL.md должен попадать в каталог");
        skill!.Description.Should().NotBeNullOrWhiteSpace();
        skill.Tags.Should().Contain("политика");
        skill.Body.Should().Contain("PrivacyPolicyOption");
        skill.Body.Should().Contain("CreatePost");
        skill.Body.Should().Contain("UpdateSiteOption");
    }

    [Fact]
    public async Task Search_ByTag_FindsPrivacyPolicySkill()
    {
        var found = await _catalog.SearchAsync("политика", CancellationToken.None);

        found.Should().Contain(s => s.Name == "mars-privacy-policy");
    }

    [Fact]
    public void Route_PrivacyPolicySettingsPage_PreloadsSkill()
    {
        var pageContext = "/Settings/Option/" + typeof(Mars.Cms.Contracts.Options.PrivacyPolicyOption).FullName!.Replace(".", "+");

        var skills = PageSkillRouter.Route(pageContext, frontEditorSlug: null, new AiChatOption());

        skills.Should().Contain("mars-privacy-policy");
    }

    [Fact]
    public void Route_PostEditPage_DoesNotPreloadPrivacySkill()
    {
        var skills = PageSkillRouter.Route("/EditPost/post/" + Guid.NewGuid(), frontEditorSlug: null, new AiChatOption());

        skills.Should().NotContain("mars-privacy-policy");
    }
}
