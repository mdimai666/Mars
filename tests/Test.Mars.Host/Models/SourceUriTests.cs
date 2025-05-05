using System.Text.Json;
using Mars.Shared.Models;
using FluentAssertions;

namespace Test.Mars.Host.Models;

public class SourceUriTests
{
    [Theory]
    [InlineData([null])]
    [InlineData(["/template/1"])]
    [InlineData(["/template/a167c50a-5ec6-47e0-9d12-af2125d236b4"])]
    [InlineData(["/page/1/2/4"])]
    public void Validate_Success(string? value)
    {
        //Arrange
        SourceUri sourceUri = value;
        //Act
        //Assert
        sourceUri.Equals(value).Should().BeTrue();
    }

    [Theory]
    [InlineData(["template/1", SourceUri.PathMustStartWithSlashMessage])]
    [InlineData(["1", null])]
    [InlineData(["/a/dd%%", SourceUri.ValueContainNotAllowedCharsMessage])]
    public void Validate_Fails(string value, string? exceptionMessage)
    {
        Action act = () => new SourceUri(value);

        act.Should().Throw<ArgumentException>().WithMessage(exceptionMessage ?? "*");
    }

    class TestJson1
    {
        public SourceUri SourceUri { get; set; } = default!;
    }

    [Fact]
    public void AsJson()
    {
        //Arrange
        string value = "/template/myTemplate-1";
        var obj = new TestJson1
        {
            SourceUri = value,
        };
        string expectJson = @$"{{""sourceUri"":""{value}""}}";

        //Act
        var json = JsonSerializer.Serialize(obj);
        var objFromJson = JsonSerializer.Deserialize<TestJson1>(json);

        //Assert
        objFromJson.SourceUri.Equals(value).Should().BeTrue();
        json.ToLower().Should().Be(expectJson.ToLower());
        objFromJson.Should().BeEquivalentTo(obj);
    }

    [Fact]
    public void SegmentsTest()
    {
        //Arrange
        SourceUri sourceUri = "/template/top/myTemplate-1";

        //Act
        //Assert
        sourceUri.StartsWithSegments("/template").Should().BeTrue();
        sourceUri.StartsWithSegments("/template/top").Should().BeTrue();


        sourceUri.Root.Equals("template").Should().BeTrue();
        sourceUri[1].Equals("top").Should().BeTrue();
        sourceUri.SegmentsCount.Should().Be(3);
    }
}
