using Mars.Host.Shared.Models;
using FluentAssertions;

namespace Test.Mars.Host.Models;

public class VersionTokenHexTests
{
    [Fact]
    public void VersionToken()
    {
        // Arrange
        uint entityVersion = 255;
        var hex = new VersionTokenHex(entityVersion);
        var expect = "FF";

        // Act
        var result = hex.ToString();
        var compare = result == expect;

        // Assert
        expect.Should().Be(result);
        compare.Should().BeTrue();
    }

    [Fact]
    public void Convert()
    {
        // Arrange
        var request = "FF";

        // Act
        VersionTokenHex version = "FF";

        // Assert
        version.Should().Be(request);
        if (version != request)// != operator test
        {
            Assert.Fail();
        }
    }
}
