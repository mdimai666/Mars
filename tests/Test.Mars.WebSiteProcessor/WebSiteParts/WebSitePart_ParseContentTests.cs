using Mars.Host.Shared.WebSite.Models;
using FluentAssertions;

namespace Test.Mars.WebSiteProcessor.WebSiteParts;

public class WebSitePart_ParseContentTests
{
    [Fact]
    public void ParseContent_ValidContent_ShouldSuccess()
    {
        // Arrange
        var fileContent = @"
@page ""/""
@title ""Title 1""
{{!-- commented; must ignored --}}
<h1>Title</h1>

<p>content</p>";

        // Act
        var (attrs, content) = WebSitePart.ParseContent(fileContent);

        // Assert
        attrs.Count.Should().Be(2);
        attrs["page"].Should().Be("/");
        attrs["title"].Should().Be("Title 1");
        content.TrimStart().StartsWith("<h1>Title</h1>");
    }

    [Fact]
    public void ParseContent_NonHeaderAttributeIgnore_MustIgnored()
    {
        // Arrange
        var fileContent = @"
@page ""/""
@title ""Title 1""
{{!-- commented; must ignored --}}
<h1>Title</h1>

@nonHeader to Be ignored 
<p>content</p>";

        // Act
        var (attrs, content) = WebSitePart.ParseContent(fileContent);

        // Assert
        attrs.Count.Should().Be(2);
        content.TrimStart().StartsWith("<h1>Title</h1>");
    }

    [Fact]
    public void ParseContent_ParseSpacedTextAttributeWithQuote_ShouldSuccess()
    {
        // Arrange
        var fileContent = @"
@title ""Title 1 text""
<p>content</p>";

        // Act
        var (attrs, content) = WebSitePart.ParseContent(fileContent);

        // Assert
        attrs["title"].Should().Be("Title 1 text");
    }

    [Fact]
    public void ParseContent_ParseSpacedTextAttributeWithoutQuote_ShouldSuccess()
    {
        // Arrange
        var fileContent = @"
@title Title 1 text
<p>content</p>";

        // Act
        var (attrs, content) = WebSitePart.ParseContent(fileContent);

        // Assert
        attrs["title"].Should().Be("Title 1 text");
    }
}
