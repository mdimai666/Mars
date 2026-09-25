using FluentAssertions;
using Mars.Integration.Tests.Attributes;
using Mars.Options.Abstractions.Services;
using Mars.SiteEngine.Contracts.Options;
using Mars.SiteEngine.Integration.Tests.Common;
using Mars.SiteEngine.Integration.Tests.HandlebarsEngine;
using Mars.Test.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mars.SiteEngine.Integration.Tests.ScribanEngine;

/// <summary>
/// Рендер фронта на движке Scriban (тема sbnTheme, маунт /sbn) — тот же контракт,
/// что у дефолтного движка. Живёт в общей коллекции: один процесс = одно приложение.
/// </summary>
[Collection(HandlebarsAppFrontCollection.CollectionName)]
public class ScribanAppFrontTests : BaseAppFrontTests<HandlebarsAppFrontApplicationFixture>, IDefaultRenderEngineTests
{
    const string Slug = "sbn-theme";
    const string Mount = "/sbn";

    public ScribanAppFrontTests(HandlebarsAppFrontApplicationFixture appFixture) : base(appFixture)
    {
        EnsureScribanFront();
    }

    void EnsureScribanFront()
    {
        var optionService = AppFixture.ServiceProvider.GetRequiredService<IOptionService>();
        var option = optionService.GetOption<FrontsOption>();

        if (option.Fronts.Any(f => f.Slug == Slug)) return;

        var themeRoot = SolutionPathHelper.Resolve("tests", "Mars.SiteEngine.Integration.Tests", "ScribanEngine", "sbnTheme");

        option.Fronts.Add(new FrontItem
        {
            Slug = Slug,
            Title = "Scriban theme",
            Url = Mount,
            Path = themeRoot,
            EngineId = FrontItem.ScribanEngine,
            Enabled = true,
        });
        optionService.SaveOption(option);
    }

    [IntegrationFact]
    public async Task Basic_IndexPage_Succeeds()
    {
        //Act
        var render = await RenderRequestPage(Mount);

        //Assert
        render.Should().Contain("Hello, world! from sbnTheme!");
        render.Should().Contain("desktop", "переменная mobile доступна в Scriban-шаблоне");
    }

    [IntegrationFact]
    public async Task Basic_SecondPage_Succeeds()
    {
        //Act
        var render = await RenderRequestPage($"{Mount}/second");

        //Assert
        render.Should().Contain("SecondPageSbn");
        render.Should().Contain("[block1]", "include блока из parts работает");
    }

    [IntegrationFact]
    public async Task Basic_Page404_ReturnsStatusCode404()
    {
        //Act
        var (html, status) = await RenderRequestPageEx($"{Mount}/non_exist_pageUrl_for_404");

        //Assert — страница 404 у маунт-фронтов не детектится (WebSiteTemplate ищет Url == "/404"),
        //рендерится fallback на index со статусом 404
        status.Should().Be(StatusCodes.Status404NotFound);
        html.Should().Contain("Hello, world! from sbnTheme!");
    }
}
