using FluentAssertions;
using Mars.Host.Shared.WebSite.Models;

namespace Test.Mars.WebSiteProcessor.WebSiteParts;

public class WebPageTests
{
    private WebPage GetWebPageFromContent(string sourceContent, string? title = null)
    {
        var partSource = new WebPartSource(sourceContent, "index", title ?? "Page Title", "index.html", "index.html");
        return new WebPage(WebSitePart.FromHandlebarsSource(partSource));
    }
    private WebPage GetWebPageWithUrl(string url)
    {
        var partSource = new WebPartSource($"@page {url}\n content", "index", "Page Title", "index.html", "index.html");
        return new WebPage(WebSitePart.FromHandlebarsSource(partSource));
    }

    [Fact]
    public void WebPage_CorrectParse_ShuldSuccess()
    {
        // Arrange
        var content = """
            @page /

            content text
            """;
        // Act
        var page = GetWebPageFromContent(content, "title - 1");

        // Assert
        page.Title.Should().Be("title - 1");
        page.Url.ToString().Should().Be("/");
        page.Content.Trim().Should().Be("content text");
    }

    [Fact]
    public void WebPage_InvalidPareAttribute_ShouldException()
    {
        // Arrange
        var content = """
            @page url1

            content text
            """;
        // Act
        var action = () => GetWebPageFromContent(content);

        // Assert
        action.Should().Throw<ArgumentException>()
                        .WithMessage("The path in 'value' must start with '/'. (Parameter 'value')");
    }

    [Fact]
    public void Url_UrlisEqualSlash_ShouldTrue()
    {
        // Arrange
        var content = """
            @page /

            content text
            """;
        // Act
        var page = GetWebPageFromContent(content);

        // Assert
        (page.Url == "/").Should().BeTrue();
    }

    [Theory]
    //valid
    [InlineData("/page1", true)]
    [InlineData("/page1/", true)]
    [InlineData("/PAgE1", true)]
    //invalid
    [InlineData("/page-1", false)]
    [InlineData("/page1/exe", false)]
    [InlineData("/page1 space", false)]
    public void MatchUrl_MatchUrl_ShouldExpect(string url, bool result)
    {
        // Arrange
        var pageUrl = "/page1";

        // Act
        var page = GetWebPageWithUrl(pageUrl);

        // Assert
        page.MatchUrl(url).Should().Be(result);
    }

    [Theory]
    //valid
    [InlineData("/post/22", true)]
    [InlineData("/post/33/", true)]
    [InlineData("/POST/44/", true)]
    //invalid
    [InlineData("/post/22/123", false)]
    [InlineData("/post-88/33/", false)]
    [InlineData("/POST\\44/", false)]
    public void MatchUrl_MatchTemplateUrl_ShouldExpect(string url, bool result)
    {
        // Arrange
        var pageUrl = "/post/{id}";

        // Act
        var page = GetWebPageWithUrl(pageUrl);

        // Assert
        page.UrlIsContainCurlyBracket.Should().BeTrue();
        page.IsRoutePatternHasConstraints.Should().BeFalse();
        page.MatchUrl(url).Should().Be(result);
    }

    [Theory]
    //valid
    [InlineData("/post/{id:int}", "/post/22", true)]
    [InlineData("/post/{id:int}", "/post/33/", true)]
    [InlineData("/post/{id:int}", "/POST/44/", true)]
    //invalid
    [InlineData("/post/{id:int:max(10)}", "/post/strPar/", false)]
    //[InlineData("/post/{id:int:max(10)}", "/POST\\44/22xx", false)] // тут надо подумать
    [InlineData("/post/{id:int:max(10)}", "/post/99/", false)]
    public void MatchUrl_TypedMatchTemplateUrl_ShouldExpect(string pageUrl, string url, bool result)
    {
        // Arrange
        // Act
        var page = GetWebPageWithUrl(pageUrl);

        // Assert
        page.UrlIsContainCurlyBracket.Should().BeTrue();
        page.IsRoutePatternHasConstraints.Should().BeTrue();
        page.MatchUrl(url).Should().Be(result);
    }
}
