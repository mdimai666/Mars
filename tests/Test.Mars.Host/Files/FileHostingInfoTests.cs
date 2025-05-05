using Mars.Host.Shared.Dto.Files;
using FluentAssertions;

namespace Test.Mars.Host.Files;

public class FileHostingInfoTests
{
    private FileHostingInfo GetFileHostingInfo()
        => new()
        {
            Backend = new Uri("http://localhost"),
            wwwRoot = new Uri("C:\\www\\mars\\wwwRoot"),
            UploadSubPath = "upload"
        };

    [Theory]
    [InlineData("text.txt", "txt")]
    [InlineData("image.JPG", "jpg")]
    [InlineData("image.dot..png", "png")]
    [InlineData("/path/zuzu/image.dot.png", "png")]
    public void GetExtension_PassPath_ShouldExpectExt(string path, string expect)
    {
        // Arrange
        var hostingInfo = GetFileHostingInfo();

        // Act
        var ext = hostingInfo.GetExtension(path);

        // Assert
        ext.Should().Be(expect);
    }

    [Theory]
    [InlineData("text.txt", false)]
    [InlineData("image.JPG", true)]
    [InlineData("image.dot..png", true)]
    [InlineData("/path/zuzu/image.dot.png", true)]
    public void IsImage_PassPath_ShouldExpectResult(string path, bool expect)
    {
        // Arrange
        var hostingInfo = GetFileHostingInfo();
        var ext = hostingInfo.GetExtension(path);

        // Act
        var isImage = hostingInfo.ExtIsImage(ext);

        // Assert
        isImage.Should().Be(expect);
    }

    [Theory]
    [InlineData("text.txt", "text.txt")]
    [InlineData("/path/zuzu/image.dot.png", "path/zuzu/image.dot.png")]
    [InlineData("/path\\zuzu/image.dot.png", "path/zuzu/image.dot.png")]
    [InlineData("path\\zuzu/image.dot.png", "path/zuzu/image.dot.png")]
    [InlineData("\\path\\zuzu\\", "path/zuzu")]
    [InlineData("\\path\\zuzu", "path/zuzu")]
    [InlineData("path\\zuzu\\", "path/zuzu")]
    [InlineData("path\\zuzu/", "path/zuzu")]
    [InlineData("/path\\zuzu/", "path/zuzu")]
    public void NormalizePathSlash_PassPath_ShouldExpectResult(string path, string expect)
    {
        // Arrange
        // Act
        var newPath = FileHostingInfo.NormalizePathSlash(path);

        // Assert
        newPath.Should().Be(expect);
    }

    private const string localhostUpload = "http://localhost/upload/";

    [Theory]
    [InlineData("text.txt", localhostUpload + "text.txt")]
    [InlineData("/2024/document.docx", localhostUpload + "2024/document.docx")]
    [InlineData("\\2024\\file.xlsx", localhostUpload + "2024/file.xlsx")]
    public void FileAbsoluteUrlFromPath_PassPath_ShouldExpectResult(string filePath, string expectUrl)
    {
        // Arrange
        var hostingInfo = GetFileHostingInfo();

        // Act
        var absoluteUrl = hostingInfo.FileAbsoluteUrlFromPath(filePath);

        // Assert
        absoluteUrl.Should().Be(expectUrl);
    }

    private const string localhostRelativeUpload = "/sub/upload/";

    [Theory]
    [InlineData("text.txt", localhostRelativeUpload + "text.txt")]
    [InlineData("/2024/document.docx", localhostRelativeUpload + "2024/document.docx")]
    [InlineData("\\2024\\file.xlsx", localhostRelativeUpload + "2024/file.xlsx")]
    public void FileRelativeUrlFromPath_PassPath_ShouldExpectResult(string filePath, string expectUrl)
    {
        // Arrange
        var hostingInfo = new FileHostingInfo
        {
            Backend = new Uri("http://localhost/sub"),
            wwwRoot = new Uri("C:\\www\\mars\\wwwRoot"),
            UploadSubPath = "upload"
        };

        // Act
        var relativeUrl = hostingInfo.FileRelativeUrlFromPath(filePath);

        // Assert
        relativeUrl.Should().Be(expectUrl);
    }

    const string absoluteUploadPath = "C:/www/mars/wwwRoot/upload/";

    [Theory]
    [InlineData("text.txt", absoluteUploadPath + "text.txt")]
    [InlineData("/2024/document.docx", absoluteUploadPath + "2024/document.docx")]
    [InlineData("\\2024\\file.xlsx", absoluteUploadPath + "2024/file.xlsx")]
    public void FileAbsolutePath_PassPath_ShouldExpectResult(string filePath, string expectUrl)
    {
        // Arrange
        var hostingInfo = GetFileHostingInfo();

        // Act
        var absolutePath = hostingInfo.FileAbsolutePath(filePath);

        // Assert
        absolutePath.Should().Be(expectUrl);
    }

    [Fact]
    public void FileAbsolutePath_PassNullPath_FailException()
    {
        // Arrange
        var hostingInfo = GetFileHostingInfo();

        // Act
        var action = () => hostingInfo.FileAbsolutePath(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("../2024/file.xlsx")]
    [InlineData("..\\2024\\file.xlsx")]
    public void FileAbsolutePath_PassInvalidPath_FailException(string? filePath)
    {
        // Arrange
        var hostingInfo = GetFileHostingInfo();

        // Act
        var action = () => hostingInfo.FileAbsolutePath(filePath!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }
}
