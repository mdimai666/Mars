using System.ComponentModel.DataAnnotations;
using AppFront.Shared.Interfaces;
using AppFront.Shared.Models;
using AppFront.Shared.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace AppFront.Tests.Services
{
    public class BlazorPagesServiceTests
    {
        private readonly IBlazorPagesService _service = new BlazorPagesService();
        private readonly System.Reflection.Assembly _asm = typeof(Fakes.Pages.DashboardPage).Assembly;

        [Fact]
        public void GetPages_ClassifiesKinds()
        {
            var pages = _service.GetPages(_asm);

            pages.Single(s => s.PageType == typeof(Fakes.Pages.DashboardPage)).Kind.Should().Be(EComponentType.Page);
            pages.Single(s => s.PageType == typeof(Fakes.MainLayout)).Kind.Should().Be(EComponentType.Layout);
            pages.Single(s => s.PageType == typeof(Fakes.PlainComponent)).Kind.Should().Be(EComponentType.ComponentBase);
        }

        [Fact]
        public void GetPages_ExtractsMultipleRoutes()
        {
            var page = _service.GetPages(_asm).Single(s => s.PageType == typeof(Fakes.Pages.DashboardPage));

            page.Routes.Should().BeEquivalentTo("/dashboard", "/dash/{ID:guid}");
        }

        [Fact]
        public void GetPages_ExtractsRolesAndAuthFlags()
        {
            var page = _service.GetPages(_asm).Single(s => s.PageType == typeof(Fakes.Pages.AdminPage));

            page.Roles.Should().BeEquivalentTo("Admin", "Editor");
            page.RequiresAuthorization.Should().BeTrue();
            page.AllowsAnonymous.Should().BeFalse();
        }

        [Fact]
        public void GetPages_AnonymousAttribute()
        {
            var page = _service.GetPages(_asm).Single(s => s.PageType == typeof(Fakes.Pages.PublicPage));

            page.AllowsAnonymous.Should().BeTrue();
        }

        [Fact]
        public void GetPages_AuthorizeWithoutRoles_SetsRequiresAuthorization()
        {
            var page = _service.GetPages(_asm).Single(s => s.PageType == typeof(Fakes.Pages.SecuredPage));

            page.RequiresAuthorization.Should().BeTrue();
            page.Roles.Should().BeEmpty();
        }

        [Fact]
        public void GetPages_ExtractsLayout()
        {
            var page = _service.GetPages(_asm).Single(s => s.PageType == typeof(Fakes.Pages.DashboardPage));

            page.LayoutType.Should().Be(typeof(Fakes.MainLayout));
        }

        [Fact]
        public void GetPages_DisplayName_FromAttribute()
        {
            var page = _service.GetPages(_asm).Single(s => s.PageType == typeof(Fakes.Pages.AdminPage));

            page.DisplayName.Should().Be("Панель администратора");
        }

        [Fact]
        public void GetPages_DisplayName_HumanizedFallback()
        {
            var page = _service.GetPages(_asm).Single(s => s.PageType == typeof(Fakes.Pages.DashboardPage));

            page.DisplayName.Should().Be("Dashboard");
        }

        [Fact]
        public void GetRoutedPages_OnlyPages()
        {
            var routed = _service.GetRoutedPages([_asm]);

            routed.Should().OnlyContain(s => s.Kind == EComponentType.Page);
            routed.Should().NotContain(s => s.PageType == typeof(Fakes.MainLayout));
            routed.Should().NotContain(s => s.PageType == typeof(Fakes.PlainComponent));
        }

        [Fact]
        public void GetStaticRoutedPages_RequiresParameterlessRoute()
        {
            var staticPages = _service.GetStaticRoutedPages([_asm]);

            // DashboardPage имеет и статический /dashboard, и параметризованный /dash/{ID:guid}
            staticPages.Should().Contain(s => s.PageType == typeof(Fakes.Pages.DashboardPage));
            // OnlyParamPage — все маршруты с параметрами
            staticPages.Should().NotContain(s => s.PageType == typeof(Fakes.Pages.OnlyParamPage));
        }

        [Theory]
        [InlineData("Dashboard")]        // имя класса
        [InlineData("dashboard")]        // маршрут
        [InlineData("администратора")]   // DisplayName
        public void Search_MatchesNameRouteOrDisplayName(string query)
        {
            _service.Search([_asm], query).Should().NotBeEmpty();
        }

        [Fact]
        public void FindPageByUrl_ExactMatch()
        {
            var page = _service.FindPageByUrl([_asm], "/dashboard");

            page.Should().NotBeNull();
            page!.PageType.Should().Be(typeof(Fakes.Pages.DashboardPage));
        }

        [Fact]
        public void FindPageByUrl_TrailingSlashAndCase()
        {
            var page = _service.FindPageByUrl([_asm], "/Dashboard/");

            page.Should().NotBeNull();
            page!.PageType.Should().Be(typeof(Fakes.Pages.DashboardPage));
        }

        [Fact]
        public void FindPageByUrl_GuidSubstitution()
        {
            var page = _service.FindPageByUrl([_asm], "/dash/3f2b1a9e-1c4d-4e5f-9a6b-7c8d9e0f1a2b");

            page.Should().NotBeNull();
            page!.PageType.Should().Be(typeof(Fakes.Pages.DashboardPage));
        }

        [Fact]
        public void FindPageByUrl_AbsoluteUrl_UsesPathOnly()
        {
            // /dev/dashboard не совпадает с /dashboard (префикс маунта сервис не срезает)
            _service.FindPageByUrl([_asm], "http://localhost/dev/dashboard?x=1").Should().BeNull();

            var root = _service.FindPageByUrl([_asm], "http://localhost/dashboard?x=1");
            root.Should().NotBeNull();
        }

        [Fact]
        public void BuildRelativeSourcePath_NamespaceToFolders()
        {
            var path = BlazorPagesService.BuildRelativeSourcePath(typeof(Fakes.Pages.DashboardPage));

            // имя тестовой сборки + хвост namespace
            path.Should().Be($"{_asm.GetName().Name}/Fakes/Pages/DashboardPage.razor");
        }

        [Fact]
        public void ResolveSourceFilePath_FallsBackToRelative()
        {
            // В тестовой сборке файла на диске нет — ожидаем относительный путь
            var path = _service.ResolveSourceFilePath(typeof(Fakes.Pages.DashboardPage));

            path.Should().NotBeNullOrEmpty();
            path.Should().EndWith("DashboardPage.razor");
        }
    }
}

// Фейковые компоненты для рефлексии — имитируют страницы/layout/компоненты Blazor
namespace AppFront.Tests.Fakes
{
    public class MainLayout : LayoutComponentBase
    {
    }

    public class PlainComponent : ComponentBase
    {
    }

    namespace Pages
    {
        [Route("/dashboard")]
        [Route("/dash/{ID:guid}")]
        [Layout(typeof(MainLayout))]
        public class DashboardPage : ComponentBase
        {
        }

        [Route("/admin")]
        [Authorize(Roles = "Admin, Editor")]
        [Display(Name = "Панель администратора")]
        public class AdminPage : ComponentBase
        {
        }

        [Route("/public")]
        [Authorize]
        [AllowAnonymous]
        public class PublicPage : ComponentBase
        {
        }

        [Route("/secured")]
        [Authorize]
        public class SecuredPage : ComponentBase
        {
        }

        [Route("/only/{ID:guid}")]
        public class OnlyParamPage : ComponentBase
        {
        }
    }
}
