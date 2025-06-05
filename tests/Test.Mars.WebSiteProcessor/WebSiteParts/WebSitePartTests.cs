using FluentAssertions;
using Mars.Host.Shared.WebSite.Models;
using Mars.Shared.Contracts.WebSite.Models;

namespace Test.Mars.WebSiteProcessor.WebSiteParts;

public class WebSitePartTests
{
    [Fact]
    public void Constructor_CanCreate_Success()
    {
        // Arrange
        var pageTitle = "Title 1";
        var pageContent = "<div>ok</div>";

        var attrs = new Dictionary<string, string>() { ["page"] = "/" };
        var part = new WebSitePart(
            type: WebSitePartType.Page,
            name: "index",
            fileRelPath: "index.html",
            fileFullPath: "C:\\www\\site1\\index.html",
            content: pageContent,
            attributes: attrs,
            title: pageTitle);

        var partSource = new WebPartSource(pageContent, "index", pageTitle, part.FileRelPath, part.FileFullPath);
        // Act
        var page = new WebPage(part);

        // Assert
        page.Should().BeEquivalentTo(part, options => options
            .ComparingRecordsByValue()
            .ComparingByMembers<WebSitePart>()
            .ExcludingMissingMembers());
        page.Title.Should().Be(pageTitle);
    }

}
