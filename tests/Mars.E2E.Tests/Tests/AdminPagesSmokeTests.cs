using AppFront.Shared.Interfaces;
using AppFront.Shared.Models;
using AppFront.Shared.Services;
using FluentAssertions;

namespace Mars.E2E.Tests.Tests;

/// <summary>
/// Смоук без поднятия сервера: IBlazorPagesService корректно читает страницы
/// из сборки админки (AppAdmin) — маршруты, роли, layout, путь к исходнику.
/// </summary>
public class AdminPagesSmokeTests
{
    private readonly IBlazorPagesService _service = new BlazorPagesService();
    private readonly System.Reflection.Assembly _adminAsm = typeof(AppAdmin.App).Assembly;

    [Fact]
    public void AppAdmin_ExposesKnownPages()
    {
        var pages = _service.GetRoutedPages([_adminAsm]);

        pages.Should().Contain(p => p.Name == "Index" && p.Routes.Contains("/"));
        pages.Should().Contain(p => p.Name == "LoginPage" && p.Routes.Contains("/Login"));
        pages.Should().Contain(p => p.Name == "UsersPage" && p.Routes.Contains("/Users"));
    }

    [Fact]
    public void AppAdmin_PagesHaveLayouts()
    {
        var pages = _service.GetRoutedPages([_adminAsm]);

        var login = pages.Single(p => p.Name == "LoginPage");
        login.LayoutType.Should().NotBeNull();
        login.LayoutType!.Name.Should().Be("BlankLayout");

        var users = pages.Single(p => p.Name == "UsersPage");
        users.LayoutType.Should().NotBeNull();
        users.LayoutType!.Name.Should().Be("AdminLayout");
    }

    [Fact]
    public void AppAdmin_StaticPages_ExcludesParameterizedOnly()
    {
        var staticPages = _service.GetStaticRoutedPages([_adminAsm]);

        // у LoginPage есть статический /Login — попадает в список
        staticPages.Should().Contain(p => p.Name == "LoginPage");
        // все маршруты с параметрами ({ID:guid} и т.п.) не должны давать «чисто параметризованные» страницы
        staticPages.Should().OnlyContain(p => p.Routes.Any(r => !r.Contains('{')));
    }

    [Fact]
    public void AppAdmin_FindPageByUrl_SettingsPage()
    {
        var page = _service.FindPageByUrl([_adminAsm], "/Settings/");

        page.Should().NotBeNull();
        page!.Name.Should().Be("SettingsPage");
    }

    [Fact]
    public void AppAdmin_SourcePath_ProducesRazorPath()
    {
        var pages = _service.GetRoutedPages([_adminAsm]);
        var index = pages.Single(p => p.Name == "Index");

        // относительный путь заполнен всегда и указывает на Pages/Index.razor
        index.SourceRelativePath.Should().Be("AppAdmin/Pages/Index.razor");

        // в Debug внутри репозитория может резолвиться и абсолютный путь
        var resolved = _service.ResolveSourceFilePath(index.PageType);
        resolved.Should().NotBeNullOrEmpty();
        resolved.Should().EndWith("Index.razor");
    }
}
