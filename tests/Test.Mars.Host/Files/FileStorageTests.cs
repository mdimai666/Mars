using Mars.Host.Shared.Dto.Files;
using Mars.Host.Shared.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Test.Mars.Host.Files;

public class FileStorageTests
{
    private readonly FileHostingInfo _fileHostingInfo;
    private readonly FileStorage _fileStorage;

    public FileStorageTests()
    {
        _fileHostingInfo = new FileHostingInfo
        {
            Backend = new Uri("http://localhost"),
            RequestPath = "upload",
            PhysicalPath = new Uri("C:\\www\\mars\\wwwRoot\\upload"),
        };

        _fileStorage = new FileStorage(Options.Create(_fileHostingInfo));
    }

    [Theory]
    [InlineData("file1.txt")]
    [InlineData("Media/file1.txt")]
    [InlineData("./Media/file1.txt")]
    public void AbsolutePath_ValidFilePath_ShouldReturnAbsolutePath(string filename)
    {
        // Arrange
        var expectPath = "C:/www/mars/wwwRoot/upload/" + filename;

        // Act
        var filepath = _fileStorage.AbsolutePath(filename);

        // Assert
        filepath.Should().Be(expectPath);
    }

    [Theory]
#if OS_WINDOWS
    [InlineData("C:\\file1.txt")]
    [InlineData("C:/file1.txt")]
#else
    [InlineData("/file1.txt")]
    [InlineData("/abs/file1.txt")]
#endif
    public void AbsolutePath_InvalidFilePath_ShouldException(string filename)
    {
        // Arrange
        var expectPath = "C:/www/mars/wwwRoot/upload/" + filename;

        // Act
        var action = () => _fileStorage.AbsolutePath(filename);

        // Assert
        action.Should().Throw<Exception>();
    }
}
