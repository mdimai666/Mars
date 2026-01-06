using System.Text.Json;
using FluentAssertions;
using Mars.Shared.Models;

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
        SourceUri? sourceUri = value;
        //Act
        //Assert
        sourceUri?.Equals(value).Should().BeTrue();
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

    // ---------- Equals ----------

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        // Arrange
        SourceUri? left = null;
        SourceUri? right = null;

        // Act
        var result = left == right;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_OneNull_ReturnsFalse()
    {
        // Arrange
        SourceUri? left = null;
        SourceUri right = SourceUri.FromUriComponent("/test");

        // Act
        var result1 = left == right;
        var result2 = right == left;

        // Assert
        Assert.False(result1);
        Assert.False(result2);
    }

    [Fact]
    public void Equals_BothEmptyValue_ReturnsTrue()
    {
        // Arrange
        var left = SourceUri.FromUriComponent("/test");
        var right = SourceUri.FromUriComponent("/test");

        // Act
        var result = left.Equals(right);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_IgnoresCase_ReturnsTrue()
    {
        // Arrange
        var left = SourceUri.FromUriComponent("/Test/Path");
        var right = SourceUri.FromUriComponent("/test/path");

        // Act
        var result = left.Equals(right);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var left = SourceUri.FromUriComponent("/a");
        var right = SourceUri.FromUriComponent("/b");

        // Act
        var result = left.Equals(right);

        // Assert
        Assert.False(result);
    }

    // ---------- Operators ----------

    [Fact]
    public void OperatorEquals_BothSameInstance_ReturnsTrue()
    {
        // Arrange
        var uri = SourceUri.FromUriComponent("/test");

        // Act
        var result = uri == uri;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void OperatorNotEquals_DifferentValues_ReturnsTrue()
    {
        // Arrange
        var left = SourceUri.FromUriComponent("/a");
        var right = SourceUri.FromUriComponent("/b");

        // Act
        var result = left != right;

        // Assert
        Assert.True(result);
    }

    // ---------- GetHashCode ----------

    [Fact]
    public void GetHashCode_SameValue_IgnoresCase()
    {
        // Arrange
        var left = SourceUri.FromUriComponent("/Test");
        var right = SourceUri.FromUriComponent("/test");

        // Act
        var hash1 = left.GetHashCode();
        var hash2 = right.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_DifferentValues_NotEqual()
    {
        // Arrange
        var left = SourceUri.FromUriComponent("/a");
        var right = SourceUri.FromUriComponent("/b");

        // Act
        var hash1 = left.GetHashCode();
        var hash2 = right.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    // ---------- Null safety ----------

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        // Arrange
        var uri = SourceUri.FromUriComponent("/test");

        // Act
        var result = uri.Equals(null);

        // Assert
        Assert.False(result);
    }
}
